#!/usr/bin/env python3
"""
Train taxonomy classifier (CNN + Attention) on sequences.

Usage:
    uv run python scripts/train_taxonomy.py --data-dir datalake/curated --level kingdom
    uv run python scripts/train_taxonomy.py --data-dir datalake/curated --level kingdom --epochs 50
"""

import argparse
import json
import sys
import time
from collections import Counter
from datetime import datetime
from pathlib import Path

import torch
import torch.nn as nn
import torch.nn.functional as F
from torch.utils.data import DataLoader, Dataset

# Add src to path
sys.path.insert(0, str(Path(__file__).parent.parent))

from src.ml.models.taxonomy import TaxonomyClassifier, TaxonomyConfig, encode_sequence  # noqa: E402


# =============================================================================
# Kingdom normalization (5 classical kingdoms)
# =============================================================================

KINGDOM_NORMALIZE = {
    # Animalia
    "animalia": "animalia",
    "metazoa": "animalia",
    # Plantae
    "plantae": "plantae",
    "viridiplantae": "plantae",
    # Fungi
    "fungi": "fungi",
    # Protista (eukaryotes that aren't animals, plants, or fungi)
    "protista": "protista",
    "protozoa": "protista",
    "chromista": "protista",
    # Monera (prokaryotes: bacteria + archaea)
    "monera": "monera",
    "bacteria": "monera",
    "eubacteria": "monera",
    "pseudomonadati": "monera",
    "bacillati": "monera",
    "archaea": "monera",
    "methanobacteriati": "monera",
    "thermoproteati": "monera",
}


# =============================================================================
# Dataset
# =============================================================================

TAXONOMY_LEVELS = ["kingdom", "phylum", "class", "order", "family", "genus"]


class TaxonomySequenceDataset(Dataset):
    """Dataset for taxonomy classification from curated sequences."""

    def __init__(
        self,
        data_dir: str | Path,
        levels: list[str] | None = None,
        max_seq_length: int = 2000,
        max_samples: int = 0,
    ):
        self.data_dir = Path(data_dir)
        self.levels = levels or ["kingdom"]
        self.max_seq_length = max_seq_length

        self.samples = []
        # Per-level mappings
        self.class_to_idx = {level: {} for level in self.levels}
        self.idx_to_class = {level: {} for level in self.levels}

        self._load_samples(max_samples)

    def _load_samples(self, max_samples: int):
        """Load samples from JSON metadata files."""
        import gzip

        json_files = list(self.data_dir.rglob("*.json"))
        print(f"Found {len(json_files)} metadata files")

        label_counts = {level: Counter() for level in self.levels}

        for i, json_path in enumerate(json_files):
            if max_samples > 0 and len(self.samples) >= max_samples:
                break

            if (i + 1) % 10000 == 0:
                print(f"  Scanning {i + 1}/{len(json_files)}...")

            try:
                with open(json_path) as f:
                    metadata = json.load(f)
            except Exception:
                continue

            # Get taxonomy for all levels
            taxonomy = metadata.get("taxonomy", {})
            labels = {}
            skip = False

            for level in self.levels:
                label = taxonomy.get(level, "").lower()

                if not label:
                    skip = True
                    break

                # Normalize kingdom names
                if level == "kingdom":
                    label = KINGDOM_NORMALIZE.get(label)
                    if not label:
                        skip = True
                        break

                labels[level] = label

            if skip:
                continue

            # Find FASTA file
            fasta_path = json_path.with_suffix(".fasta.gz")
            if not fasta_path.exists():
                fasta_path = json_path.with_suffix(".fasta")
                if not fasta_path.exists():
                    continue

            # Load sequence
            try:
                if fasta_path.suffix == ".gz":
                    with gzip.open(fasta_path, "rt") as f:
                        lines = f.readlines()
                else:
                    with open(fasta_path) as f:
                        lines = f.readlines()

                sequence = "".join(
                    line.strip() for line in lines if not line.startswith(">")
                )

                if len(sequence) < 100:
                    continue

            except Exception:
                continue

            self.samples.append({
                "sequence": sequence,
                "labels": labels,
            })

            for level in self.levels:
                label_counts[level][labels[level]] += 1

        # Build class mappings per level
        for level in self.levels:
            unique_labels = sorted(label_counts[level].keys())
            self.class_to_idx[level] = {lab: idx for idx, lab in enumerate(unique_labels)}
            self.idx_to_class[level] = {idx: lab for lab, idx in self.class_to_idx[level].items()}

        print(f"Loaded {len(self.samples)} samples")
        for level in self.levels:
            n_classes = len(self.class_to_idx[level])
            print(f"  {level}: {n_classes} classes")

    def __len__(self):
        return len(self.samples)

    def __getitem__(self, idx):
        sample = self.samples[idx]
        sequence = encode_sequence(sample["sequence"], self.max_seq_length)

        # Labels for each level
        labels = {}
        for level in self.levels:
            labels[level] = torch.tensor(
                self.class_to_idx[level][sample["labels"][level]],
                dtype=torch.long,
            )

        return {"sequence": sequence, "labels": labels}

    def num_classes_per_level(self) -> dict[str, int]:
        return {level: len(self.class_to_idx[level]) for level in self.levels}

    def class_labels_per_level(self) -> dict[str, list[str]]:
        return {
            level: [self.idx_to_class[level][i] for i in range(len(self.class_to_idx[level]))]
            for level in self.levels
        }

    def get_class_weights(self, level: str) -> torch.Tensor:
        """Compute inverse frequency class weights for a level."""
        counts = Counter(s["labels"][level] for s in self.samples)
        total = sum(counts.values())
        n_classes = len(self.class_to_idx[level])
        weights = []
        for i in range(n_classes):
            label = self.idx_to_class[level][i]
            weight = total / (n_classes * counts[label])
            weights.append(weight)
        return torch.tensor(weights, dtype=torch.float32)


# =============================================================================
# Training
# =============================================================================

def train_epoch(model, loader, optimizer, criterions, device, levels):
    """Train one epoch, returns metrics per level."""
    model.train()
    total_loss = 0
    correct = {level: 0 for level in levels}
    total = 0

    for batch in loader:
        sequence = batch["sequence"].to(device)
        labels = {level: batch["labels"][level].to(device) for level in levels}

        optimizer.zero_grad()

        # Forward - model returns dict of logits per level
        outputs = model(sequence, features=None)

        # Compute loss for each level
        loss = 0
        for level in levels:
            level_loss = criterions[level](outputs[level], labels[level])
            loss = loss + level_loss

        loss.backward()
        torch.nn.utils.clip_grad_norm_(model.parameters(), 1.0)
        optimizer.step()

        total_loss += loss.item() * sequence.size(0)

        # Accuracy per level
        for level in levels:
            preds = outputs[level].argmax(dim=-1)
            correct[level] += (preds == labels[level]).sum().item()

        total += sequence.size(0)

    metrics = {"loss": total_loss / total}
    for level in levels:
        metrics[f"{level}_acc"] = correct[level] / total

    return metrics


def evaluate(model, loader, criterions, device, levels):
    """Evaluate, returns metrics per level."""
    model.eval()
    total_loss = 0
    correct = {level: 0 for level in levels}
    total = 0

    with torch.no_grad():
        for batch in loader:
            sequence = batch["sequence"].to(device)
            labels = {level: batch["labels"][level].to(device) for level in levels}

            outputs = model(sequence, features=None)

            loss = 0
            for level in levels:
                level_loss = criterions[level](outputs[level], labels[level])
                loss = loss + level_loss

            total_loss += loss.item() * sequence.size(0)

            for level in levels:
                preds = outputs[level].argmax(dim=-1)
                correct[level] += (preds == labels[level]).sum().item()

            total += sequence.size(0)

    metrics = {"loss": total_loss / total}
    for level in levels:
        metrics[f"{level}_acc"] = correct[level] / total

    return metrics


# =============================================================================
# Main
# =============================================================================

def main():
    parser = argparse.ArgumentParser(description="Train Taxonomy CNN Classifier")
    parser.add_argument(
        "--data-dir", type=str, default="datalake/curated",
        help="Directory with curated sequences",
    )
    parser.add_argument(
        "--levels", type=str, default="kingdom",
        help="Taxonomy levels (comma-separated, e.g., 'kingdom,phylum,class')",
    )
    parser.add_argument("--output-dir", type=str, default="checkpoints/taxonomy_cnn")
    parser.add_argument("--epochs", type=int, default=50)
    parser.add_argument("--batch-size", type=int, default=64)
    parser.add_argument("--lr", type=float, default=1e-3)
    parser.add_argument("--max-seq-length", type=int, default=2000)
    parser.add_argument("--max-samples", type=int, default=0)
    parser.add_argument("--val-split", type=float, default=0.15)
    parser.add_argument("--patience", type=int, default=15)
    parser.add_argument("--seed", type=int, default=42)

    args = parser.parse_args()

    # Parse levels
    levels = [l.strip() for l in args.levels.split(",")]
    for level in levels:
        if level not in TAXONOMY_LEVELS:
            print(f"ERROR: Invalid level '{level}'")
            return 1

    torch.manual_seed(args.seed)

    print("=" * 70)
    print("TAXONOMY CNN CLASSIFIER (Hierarchical)")
    print("=" * 70)
    print(f"Data directory: {args.data_dir}")
    print(f"Levels: {levels}")
    print(f"Epochs: {args.epochs}")
    print(f"Batch size: {args.batch_size}")

    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    print(f"Device: {device}")

    # Load dataset
    print("\n" + "-" * 70)
    print("LOADING DATA")
    print("-" * 70)

    dataset = TaxonomySequenceDataset(
        data_dir=args.data_dir,
        levels=levels,
        max_seq_length=args.max_seq_length,
        max_samples=args.max_samples,
    )

    if len(dataset) < 10:
        print("ERROR: Not enough samples")
        return 1

    # Split
    val_size = int(len(dataset) * args.val_split)
    train_size = len(dataset) - val_size

    train_dataset, val_dataset = torch.utils.data.random_split(
        dataset,
        [train_size, val_size],
        generator=torch.Generator().manual_seed(args.seed),
    )

    print(f"Train: {len(train_dataset)} | Val: {len(val_dataset)}")

    train_loader = DataLoader(
        train_dataset, batch_size=args.batch_size, shuffle=True, num_workers=0
    )
    val_loader = DataLoader(
        val_dataset, batch_size=args.batch_size, shuffle=False, num_workers=0
    )

    # Create model
    print("\n" + "-" * 70)
    print("MODEL")
    print("-" * 70)

    config = TaxonomyConfig(
        num_classes_per_level=dataset.num_classes_per_level(),
        class_labels_per_level=dataset.class_labels_per_level(),
        max_seq_length=args.max_seq_length,
        learning_rate=args.lr,
        batch_size=args.batch_size,
        max_epochs=args.epochs,
    )

    model = TaxonomyClassifier(config).to(device)

    n_params = sum(p.numel() for p in model.parameters())
    print(f"Parameters: {n_params:,}")
    for level in levels:
        n_classes = dataset.num_classes_per_level()[level]
        print(f"  {level}: {n_classes} classes")

    # Training setup - criterion per level
    criterions = {}
    for level in levels:
        weights = dataset.get_class_weights(level).to(device)
        criterions[level] = nn.CrossEntropyLoss(weight=weights)

    optimizer = torch.optim.AdamW(model.parameters(), lr=args.lr, weight_decay=1e-4)
    scheduler = torch.optim.lr_scheduler.CosineAnnealingLR(optimizer, T_max=args.epochs)

    output_dir = Path(args.output_dir) / "_".join(levels)
    output_dir.mkdir(parents=True, exist_ok=True)

    # Training loop
    print("\n" + "=" * 70)
    print("TRAINING")
    print("=" * 70)

    # Header
    header = "Epoch  | Loss   |"
    for level in levels:
        header += f" {level[:6]:>6} |"
    print(header)
    print("-" * len(header))

    best_val_acc = {level: 0 for level in levels}
    best_avg_acc = 0
    patience_counter = 0

    for epoch in range(args.epochs):
        start = time.time()

        train_metrics = train_epoch(
            model, train_loader, optimizer, criterions, device, levels
        )
        val_metrics = evaluate(model, val_loader, criterions, device, levels)

        scheduler.step()
        elapsed = time.time() - start

        # Print metrics
        line = f"{epoch+1:3d}/{args.epochs} | {val_metrics['loss']:.4f} |"
        for level in levels:
            acc = val_metrics[f"{level}_acc"]
            line += f" {acc:6.1%} |"
        line += f" {elapsed:.0f}s"
        print(line)

        # Track best (average across levels)
        avg_acc = sum(val_metrics[f"{level}_acc"] for level in levels) / len(levels)
        if avg_acc > best_avg_acc:
            best_avg_acc = avg_acc
            for level in levels:
                best_val_acc[level] = val_metrics[f"{level}_acc"]
            model.save(output_dir / "best.pt")
            patience_counter = 0
            print(f"  -> New best avg: {best_avg_acc:.2%}")
        else:
            patience_counter += 1

        if patience_counter >= args.patience:
            print(f"\nEarly stopping at epoch {epoch + 1}")
            break

    # Save final
    model.save(output_dir / "final.pt")

    # Save config
    with open(output_dir / "config.json", "w") as f:
        json.dump({
            "levels": levels,
            "num_classes_per_level": dataset.num_classes_per_level(),
            "class_labels_per_level": dataset.class_labels_per_level(),
            "best_val_accuracy": best_val_acc,
            "best_avg_accuracy": best_avg_acc,
            "epochs_trained": epoch + 1,
            "timestamp": datetime.now().isoformat(),
        }, f, indent=2)

    print("\n" + "=" * 70)
    print("RESULTS")
    print("=" * 70)
    for level in levels:
        print(f"  {level}: {best_val_acc[level]:.2%}")
    print(f"  Average: {best_avg_acc:.2%}")
    print(f"\nSaved to: {output_dir}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
