#!/usr/bin/env python
"""Train the Hierarchical Taxonomy Classifier.

This script trains a multi-head neural network that can classify DNA sequences
at multiple taxonomic levels simultaneously (kingdom, phylum, class, etc.).

Usage:
    # Using precomputed dataset (recommended - faster):
    python scripts/train_taxonomy_hierarchical.py --dataset-dir datalake/datasets/taxonomy_hierarchical_kingdom_phylum_class --epochs 50

    # Using raw data (slower, extracts features on-the-fly):
    python scripts/train_taxonomy_hierarchical.py --data-dir datalake/raw --levels kingdom phylum class --epochs 50
"""

import argparse
import json
import matplotlib
matplotlib.use('Agg')  # Non-interactive backend for plots
from datetime import datetime
from pathlib import Path

import torch
from torch.utils.data import DataLoader, random_split, Subset

from src.ml.datasets import (
    PrecomputedHierarchicalTaxonomyDataset,
)
from src.ml.datasets.hierarchical_taxonomy_dataset import (
    HierarchicalTaxonomyDataset,
    TAXONOMY_LEVELS,
)
from src.ml.models.taxonomy import (
    TaxonomyClassifier,
    TaxonomyConfig,
)
from src.ml.training.losses import FocalLoss


def parse_args():
    parser = argparse.ArgumentParser(description="Train Hierarchical Taxonomy Classifier")

    # Data source (mutually exclusive)
    data_group = parser.add_mutually_exclusive_group(required=True)
    data_group.add_argument(
        "--dataset-dir",
        type=str,
        help="Path to precomputed dataset directory (recommended)",
    )
    data_group.add_argument(
        "--data-dir",
        type=str,
        help="Path to raw data directory (slower)",
    )

    parser.add_argument(
        "--levels",
        nargs="+",
        default=["kingdom", "phylum", "class"],
        choices=TAXONOMY_LEVELS,
        help="Taxonomic levels to train on (only for raw data)",
    )
    parser.add_argument("--epochs", type=int, default=50, help="Number of epochs")
    parser.add_argument("--batch-size", type=int, default=32, help="Batch size")
    parser.add_argument("--lr", type=float, default=1e-3, help="Learning rate")
    parser.add_argument("--max-seq-length", type=int, default=2000, help="Max sequence length")
    parser.add_argument("--min-samples", type=int, default=5, help="Min samples per class")
    parser.add_argument("--val-split", type=float, default=0.2, help="Validation split ratio (only for raw data)")
    parser.add_argument("--hierarchy-weight", type=float, default=0.1, help="Hierarchy loss weight")
    parser.add_argument("--checkpoint-dir", type=str, default="checkpoints/taxonomy", help="Checkpoint directory")
    parser.add_argument("--use-weights", action="stoREDACTED", help="Use class weights for imbalanced data")
    parser.add_argument("--num-workers", type=int, default=0, help="DataLoader workers")
    parser.add_argument("--sample-fraction", type=float, default=1, help="Fraction of dataset to use (0.15 = 15 percent)")
    parser.add_argument("--patience", type=int, default=50, help="Early stopping patience (0 to disable)")
    parser.add_argument("--focal-loss", action="stoREDACTED", help="Use focal loss for class imbalance")
    parser.add_argument("--focal-gamma", type=float, default=2.0, help="Focal loss gamma parameter")
    return parser.parse_args()


def generate_plots(history: dict, levels: list, save_dir: Path) -> list:
    """Generate training plots for hierarchical model."""
    import matplotlib.pyplot as plt

    save_dir = Path(save_dir)
    save_dir.mkdir(parents=True, exist_ok=True)
    saved_plots = []

    train_metrics = history.get("train", [])
    val_metrics = history.get("val", [])

    if not train_metrics:
        return saved_plots

    epochs = list(range(1, len(train_metrics) + 1))

    # 1. Loss curve
    fig, ax = plt.subplots(figsize=(10, 6))
    train_loss = [m["loss"] for m in train_metrics]
    val_loss = [m["loss"] for m in val_metrics]
    ax.plot(epochs, train_loss, label="Train Loss", linewidth=2, color="#2196F3")
    ax.plot(epochs, val_loss, label="Val Loss", linewidth=2, color="#FF5722")
    ax.set_xlabel("Epoch", fontsize=12)
    ax.set_ylabel("Loss", fontsize=12)
    ax.set_title("Training & Validation Loss", fontsize=14, fontweight="bold")
    ax.legend(fontsize=11)
    ax.grid(True, alpha=0.3)
    loss_path = save_dir / "loss_curve.png"
    fig.savefig(loss_path, dpi=150, bbox_inches="tight")
    saved_plots.append(loss_path)
    plt.close(fig)

    # 2. Accuracy per level
    colors = ["#4CAF50", "#9C27B0", "#FF9800", "#00BCD4", "#E91E63", "#3F51B5"]
    fig, ax = plt.subplots(figsize=(12, 6))

    for i, level in enumerate(levels):
        color = colors[i % len(colors)]
        train_acc = [m.get(f"acc_{level}", 0) * 100 for m in train_metrics]
        val_acc = [m.get(f"acc_{level}", 0) * 100 for m in val_metrics]
        ax.plot(epochs, val_acc, label=f"{level.capitalize()} (val)", linewidth=2, color=color)
        ax.plot(epochs, train_acc, label=f"{level.capitalize()} (train)", linewidth=1, linestyle="--", color=color, alpha=0.5)

    ax.set_xlabel("Epoch", fontsize=12)
    ax.set_ylabel("Accuracy (%)", fontsize=12)
    ax.set_title("Accuracy per Taxonomic Level", fontsize=14, fontweight="bold")
    ax.legend(fontsize=10, loc="lower right")
    ax.set_ylim(0, 105)
    ax.grid(True, alpha=0.3)
    acc_path = save_dir / "accuracy_per_level.png"
    fig.savefig(acc_path, dpi=150, bbox_inches="tight")
    saved_plots.append(acc_path)
    plt.close(fig)

    # 3. Combined summary
    n_levels = len(levels)
    fig, axes = plt.subplots(1, n_levels + 1, figsize=(5 * (n_levels + 1), 5))

    # Loss subplot
    ax = axes[0]
    ax.plot(epochs, train_loss, label="Train", linewidth=2, color="#2196F3")
    ax.plot(epochs, val_loss, label="Val", linewidth=2, color="#FF5722")
    ax.set_title("Loss", fontweight="bold")
    ax.legend()
    ax.grid(True, alpha=0.3)

    # Accuracy subplots per level
    for i, level in enumerate(levels):
        ax = axes[i + 1]
        train_acc = [m.get(f"acc_{level}", 0) * 100 for m in train_metrics]
        val_acc = [m.get(f"acc_{level}", 0) * 100 for m in val_metrics]
        ax.plot(epochs, train_acc, label="Train", linewidth=2, color="#4CAF50")
        ax.plot(epochs, val_acc, label="Val", linewidth=2, color="#9C27B0")
        ax.set_title(f"{level.capitalize()} Accuracy", fontweight="bold")
        ax.set_ylim(0, 105)
        ax.legend()
        ax.grid(True, alpha=0.3)

    plt.tight_layout()
    summary_path = save_dir / "training_summary.png"
    fig.savefig(summary_path, dpi=150, bbox_inches="tight")
    saved_plots.append(summary_path)
    plt.close(fig)

    return saved_plots


def collate_fn(batch):
    """Custom collate function for hierarchical labels."""
    sequences = torch.stack([item["sequence"] for item in batch])

    # Get all level keys from first item
    levels = list(batch[0]["labels"].keys())

    labels = {}
    for level in levels:
        labels[level] = torch.stack([item["labels"][level] for item in batch])

    result = {"sequence": sequences, "labels": labels}

    # Include features if available
    if "features" in batch[0]:
        result["features"] = torch.stack([item["features"] for item in batch])

    return result


def train_epoch(model, dataloader, optimizer, class_weights=None, focal_loss_fn=None):
    """Train for one epoch.

    Args:
        model: The model to train.
        dataloader: Training data loader.
        optimizer: Optimizer.
        class_weights: Optional class weights per level.
        focal_loss_fn: Optional dict of FocalLoss instances per level.
    """
    model.train()
    total_loss = 0
    level_losses = {level: 0 for level in model.active_levels}
    level_correct = {level: 0 for level in model.active_levels}
    level_total = {level: 0 for level in model.active_levels}

    for batch in dataloader:
        sequences = batch["sequence"].to(model.device)
        targets = {k: v.to(model.device) for k, v in batch["labels"].items()}

        optimizer.zero_grad()

        outputs = model(sequences)

        # Use focal loss if provided, otherwise use model's compute_loss
        if focal_loss_fn is not None:
            loss, losses = compute_focal_loss(
                model, outputs, targets, focal_loss_fn, class_weights
            )
        else:
            loss, losses = model.compute_loss(outputs, targets, class_weights)

        loss.backward()
        torch.nn.utils.clip_grad_norm_(model.parameters(), 1.0)
        optimizer.step()

        total_loss += loss.item()

        # Track per-level metrics
        for level in model.active_levels:
            if level in losses:
                level_losses[level] += losses[level]

            if level in outputs and level in targets:
                mask = targets[level] >= 0
                if mask.any():
                    preds = outputs[level][mask].argmax(dim=-1)
                    correct = (preds == targets[level][mask]).sum().item()
                    level_correct[level] += correct
                    level_total[level] += mask.sum().item()

    num_batches = len(dataloader)
    metrics = {
        "loss": total_loss / num_batches,
    }

    for level in model.active_levels:
        metrics[f"loss_{level}"] = level_losses[level] / num_batches
        if level_total[level] > 0:
            metrics[f"acc_{level}"] = level_correct[level] / level_total[level]
        else:
            metrics[f"acc_{level}"] = 0.0

    return metrics


def compute_focal_loss(model, outputs, targets, focal_loss_fn, class_weights=None):
    """Compute hierarchical loss using focal loss.

    Args:
        model: The model (for active_levels and config).
        outputs: Dictionary mapping level names to logit tensors.
        targets: Dictionary mapping level names to target tensors.
        focal_loss_fn: Dictionary of FocalLoss instances per level.
        class_weights: Optional class weights per level.

    Returns:
        Tuple of (total_loss, losses_dict).
    """
    import torch.nn.functional as F

    losses = {}
    total_loss = torch.tensor(0.0, device=model.device)

    # Level weights for hierarchical loss
    level_weights = {
        "kingdom": 0.3,
        "phylum": 0.5,
        "class": 0.6,
        "order": 0.7,
        "family": 0.8,
        "genus": 1.5,
    }

    for level in model.active_levels:
        if level not in targets:
            continue

        target = targets[level]
        mask = target >= 0

        if not mask.any():
            continue

        logits = outputs[level][mask]
        labels = target[mask]

        # Use focal loss for this level
        loss = focal_loss_fn[level](logits, labels)

        level_weight = level_weights.get(level, 1.0)
        weighted_loss = loss * level_weight

        losses[level] = loss.item()
        total_loss = total_loss + weighted_loss

    # Add hierarchy consistency loss
    if model.config.hierarchy_loss_weight > 0 and len(model.active_levels) >= 2:
        consistency_loss = torch.tensor(0.0, device=model.device)
        n_pairs = 0

        for i in range(len(model.active_levels) - 1):
            parent = model.active_levels[i]
            child = model.active_levels[i + 1]

            parent_probs = F.softmax(outputs[parent], dim=-1)
            child_probs = F.softmax(outputs[child], dim=-1)

            parent_entropy = -(parent_probs * (parent_probs + 1e-10).log()).sum(-1)
            child_entropy = -(child_probs * (child_probs + 1e-10).log()).sum(-1)

            diff = (parent_entropy - child_entropy).abs()
            consistency_loss = consistency_loss + diff.mean()
            n_pairs += 1

        if n_pairs > 0:
            consistency = consistency_loss / n_pairs
            losses["consistency"] = consistency.item()
            total_loss = total_loss + model.config.hierarchy_loss_weight * consistency

    return total_loss, losses


def evaluate(model, dataloader, class_weights=None, focal_loss_fn=None):
    """Evaluate the model.

    Args:
        model: The model to evaluate.
        dataloader: Validation data loader.
        class_weights: Optional class weights per level.
        focal_loss_fn: Optional dict of FocalLoss instances per level.
    """
    model.eval()
    total_loss = 0
    level_correct = {level: 0 for level in model.active_levels}
    level_total = {level: 0 for level in model.active_levels}

    with torch.no_grad():
        for batch in dataloader:
            sequences = batch["sequence"].to(model.device)
            targets = {k: v.to(model.device) for k, v in batch["labels"].items()}

            outputs = model(sequences)

            # Use focal loss if provided, otherwise use model's compute_loss
            if focal_loss_fn is not None:
                loss, _ = compute_focal_loss(
                    model, outputs, targets, focal_loss_fn, class_weights
                )
            else:
                loss, _ = model.compute_loss(outputs, targets, class_weights)

            total_loss += loss.item()

            for level in model.active_levels:
                if level in outputs and level in targets:
                    mask = targets[level] >= 0
                    if mask.any():
                        preds = outputs[level][mask].argmax(dim=-1)
                        correct = (preds == targets[level][mask]).sum().item()
                        level_correct[level] += correct
                        level_total[level] += mask.sum().item()

    num_batches = len(dataloader)
    metrics = {"loss": total_loss / num_batches}

    for level in model.active_levels:
        if level_total[level] > 0:
            metrics[f"acc_{level}"] = level_correct[level] / level_total[level]
        else:
            metrics[f"acc_{level}"] = 0.0

    return metrics


def main():
    args = parse_args()

    print("=" * 60)
    print("Hierarchical Taxonomy Classifier Training")
    print("=" * 60)

    # Load dataset based on source
    if args.dataset_dir:
        # Load precomputed dataset
        print(f"Loading precomputed dataset from: {args.dataset_dir}")
        dataset_dir = Path(args.dataset_dir)

        train_dataset = PrecomputedHierarchicalTaxonomyDataset(dataset_dir / "train")
        val_dataset = PrecomputedHierarchicalTaxonomyDataset(dataset_dir / "val")

        # Get metadata from train dataset
        num_classes_per_level = train_dataset.num_classes_per_level
        class_labels_per_level = train_dataset.class_labels_per_level
        levels = train_dataset.levels

        print(f"Levels: {levels}")

        # Subsample if requested
        if args.sample_fraction < 1.0:
            import random
            random.seed(42)

            train_size = int(len(train_dataset) * args.sample_fraction)
            val_size = int(len(val_dataset) * args.sample_fraction)

            train_indices = random.sample(range(len(train_dataset)), train_size)
            val_indices = random.sample(range(len(val_dataset)), val_size)

            train_dataset = Subset(train_dataset, train_indices)
            val_dataset = Subset(val_dataset, val_indices)

            print(f"Subsampled to {args.sample_fraction*100:.0f}% of data")

        print(f"Train samples: {len(train_dataset)}")
        print(f"Val samples: {len(val_dataset)}")

        # Get class weights from train dataset
        class_weights = None
        if args.use_weights:
            class_weights = train_dataset.get_all_class_weights()
            print("Using class weights for imbalanced data")
    else:
        # Load raw data
        print(f"Loading raw data from: {args.data_dir}")
        print(f"Levels: {args.levels}")

        dataset = HierarchicalTaxonomyDataset(
            data_dir=args.data_dir,
            levels=args.levels,
            max_seq_length=args.max_seq_length,
            min_samples_per_class=args.min_samples,
        )

        if len(dataset) == 0:
            print("ERROR: No samples found in dataset!")
            return

        # Split dataset
        val_size = int(len(dataset) * args.val_split)
        train_size = len(dataset) - val_size
        train_dataset, val_dataset = random_split(
            dataset,
            [train_size, val_size],
            generator=torch.Generator().manual_seed(42),
        )

        num_classes_per_level = dataset.num_classes_per_level
        class_labels_per_level = dataset.class_labels_per_level
        levels = args.levels

        # Subsample if requested
        if args.sample_fraction < 1.0:
            import random
            random.seed(42)

            new_train_size = int(len(train_dataset) * args.sample_fraction)
            new_val_size = int(len(val_dataset) * args.sample_fraction)

            train_indices = random.sample(range(len(train_dataset)), new_train_size)
            val_indices = random.sample(range(len(val_dataset)), new_val_size)

            train_dataset = Subset(train_dataset, train_indices)
            val_dataset = Subset(val_dataset, val_indices)

            train_size = new_train_size
            val_size = new_val_size

            print(f"Subsampled to {args.sample_fraction*100:.0f}% of data")

        print(f"Train samples: {train_size}")
        print(f"Val samples: {val_size}")

        # Get class weights
        class_weights = None
        if args.use_weights:
            class_weights = dataset.get_all_class_weights()
            print("Using class weights for imbalanced data")

    print(f"Epochs: {args.epochs}")
    print(f"Batch size: {args.batch_size}")
    print()

    # Create dataloaders
    train_loader = DataLoader(
        train_dataset,
        batch_size=args.batch_size,
        shuffle=True,
        collate_fn=collate_fn,
        num_workers=args.num_workers,
    )
    val_loader = DataLoader(
        val_dataset,
        batch_size=args.batch_size,
        shuffle=False,
        collate_fn=collate_fn,
        num_workers=args.num_workers,
    )

    # Create output directory
    checkpoint_dir = Path(args.checkpoint_dir)
    checkpoint_dir.mkdir(parents=True, exist_ok=True)

    print(f"\nOutput directory: {checkpoint_dir}")

    # Create model
    config = TaxonomyConfig(
        num_classes_per_level=num_classes_per_level,
        class_labels_per_level=class_labels_per_level,
        max_seq_length=args.max_seq_length,
        learning_rate=args.lr,
        batch_size=args.batch_size,
        max_epochs=args.epochs,
        hierarchy_loss_weight=args.hierarchy_weight,
    )

    print(f"\nModel configuration:")
    print(f"  Classes per level: {config.num_classes_per_level}")

    model = TaxonomyClassifier(config)
    print(f"  Active levels: {model.active_levels}")
    print(f"  Total parameters: {sum(p.numel() for p in model.parameters()):,}")

    # Create focal loss instances if enabled
    focal_loss_fn = None
    if args.focal_loss:
        print(f"Using Focal Loss (gamma={args.focal_gamma})")
        focal_loss_fn = {}
        for level in model.active_levels:
            weight = None
            if class_weights and level in class_weights:
                weight = class_weights[level].to(model.device)
            focal_loss_fn[level] = FocalLoss(
                gamma=args.focal_gamma,
                weight=weight,
            )

    # Save run configuration
    run_config = {
        "timestamp": datetime.now().isoformat(),
        "levels": levels,
        "num_classes_per_level": num_classes_per_level,
        "epochs": args.epochs,
        "batch_size": args.batch_size,
        "learning_rate": args.lr,
        "hierarchy_weight": args.hierarchy_weight,
        "use_weights": args.use_weights,
        "focal_loss": args.focal_loss,
        "focal_gamma": args.focal_gamma if args.focal_loss else None,
        "patience": args.patience,
        "train_samples": len(train_dataset),
        "val_samples": len(val_dataset),
    }
    with open(checkpoint_dir / "config.json", "w") as f:
        json.dump(run_config, f, indent=2)

    # Optimizer and scheduler
    optimizer = torch.optim.AdamW(model.parameters(), lr=args.lr, weight_decay=0.01)
    scheduler = torch.optim.lr_scheduler.CosineAnnealingLR(optimizer, T_max=args.epochs)

    # Training loop
    best_val_loss = float("inf")
    best_val_acc = 0
    best_epoch = 0
    patience_counter = 0

    print("\n" + "=" * 60)
    print("Training started")
    print("=" * 60)

    history = {"train": [], "val": []}

    for epoch in range(args.epochs):
        # Train
        train_metrics = train_epoch(model, train_loader, optimizer, class_weights, focal_loss_fn)
        history["train"].append(train_metrics)

        # Evaluate
        val_metrics = evaluate(model, val_loader, class_weights, focal_loss_fn)
        history["val"].append(val_metrics)

        # Update scheduler
        scheduler.step()

        # Print progress
        level_accs = " | ".join(
            f"{level}: {val_metrics.get(f'acc_{level}', 0)*100:.1f}%"
            for level in model.active_levels
        )
        print(
            f"Epoch {epoch+1:3d}/{args.epochs} | "
            f"Train Loss: {train_metrics['loss']:.4f} | "
            f"Val Loss: {val_metrics['loss']:.4f} | "
            f"{level_accs}"
        )

        # Save best model (based on val_loss to detect overfitting)
        avg_val_acc = sum(
            val_metrics.get(f"acc_{level}", 0) for level in model.active_levels
        ) / len(model.active_levels)

        if val_metrics["loss"] < best_val_loss:
            best_val_loss = val_metrics["loss"]
            best_val_acc = avg_val_acc
            best_epoch = epoch + 1
            patience_counter = 0
            model.save(checkpoint_dir / "best.pt")
            print(f"  -> New best model saved (loss: {best_val_loss:.4f}, avg acc: {avg_val_acc*100:.2f}%)")
        else:
            patience_counter += 1
            if args.patience > 0 and patience_counter >= args.patience:
                print(f"\nEarly stopping after {patience_counter} epochs without improvement")
                break

    # Save final model
    model.save(checkpoint_dir / "final.pt")

    # Save training history
    history_path = checkpoint_dir / "history.json"
    with open(history_path, "w") as f:
        json.dump(history, f, indent=2)

    # Update config with best results
    run_config["best_epoch"] = best_epoch
    run_config["best_loss"] = best_val_loss
    run_config["best_accuracy"] = best_val_acc
    with open(checkpoint_dir / "config.json", "w") as f:
        json.dump(run_config, f, indent=2)

    # Generate training plots
    print("\nGenerating training plots...")
    plots_dir = checkpoint_dir / "plots"
    plots = generate_plots(history, model.active_levels, plots_dir)
    print(f"Generated {len(plots)} plots in: {plots_dir}")

    print("\n" + "=" * 60)
    print("Training completed!")
    print("=" * 60)
    print(f"Best epoch: {best_epoch}")
    print(f"Best validation loss: {best_val_loss:.4f}")
    print(f"Best average validation accuracy: {best_val_acc*100:.2f}%")
    print(f"Final accuracies per level:")
    for level in model.active_levels:
        acc = history["val"][-1].get(f"acc_{level}", 0) * 100
        print(f"  {level}: {acc:.2f}%")
    print(f"\nResults saved to: {checkpoint_dir}/")
    print(f"  ├── config.json          # Run configuration")
    print(f"  ├── best.pt              # Best model checkpoint")
    print(f"  ├── final.pt             # Final model checkpoint")
    print(f"  ├── history.json         # Training history")
    print(f"  └── plots/               # Training plots")
    for p in plots:
        print(f"      └── {p.name}")


if __name__ == "__main__":
    main()
