#!/usr/bin/env python3
"""
Train the Heterozygote Classifier model.

Classifies each position as homozygous or heterozygous.

Usage:
    uv run python scripts/train_heterozygote.py
    uv run python scripts/train_heterozygote.py --epochs 100 --batch-size 64
"""

import argparse
import json
from datetime import datetime
from pathlib import Path

import matplotlib

matplotlib.use('Agg')
import matplotlib.pyplot as plt
import numpy as np
import torch
import torch.nn as nn
from torch.utils.data import DataLoader, Dataset

from src.ml.models.heterozygote import HeterozygoteClassifier, HeterozygoteConfig

# =============================================================================
# Dataset
# =============================================================================

class HeterozygoteDataset(Dataset):
    """Dataset for heterozygote_training classification."""

    def __init__(self, data_dir: Path, max_length: int = 500):
        self.data_dir = Path(data_dir)
        self.max_length = max_length
        self.samples = sorted(self.data_dir.glob("sample_*.npz"))

        if not self.samples:
            raise ValueError(f"No samples found in {data_dir}")

    def __len__(self):
        return len(self.samples)

    def __getitem__(self, idx):
        data = np.load(self.samples[idx])

        signals = data["signals"].astype(np.float32)  # (4, seq_len) - A, C, G, T
        labels = data["labels"].astype(np.int64)  # (seq_len,)

        seq_len = signals.shape[1]

        # Pad or truncate to max_length
        if seq_len > self.max_length:
            start = (seq_len - self.max_length) // 2
            signals = signals[:, start:start + self.max_length]
            labels = labels[start:start + self.max_length]
        elif seq_len < self.max_length:
            pad = self.max_length - seq_len
            signals = np.pad(signals, ((0, 0), (0, pad)), constant_values=0)
            labels = np.pad(labels, (0, pad), constant_values=-1)  # -1 = ignore

        return {
            "signals": torch.tensor(signals, dtype=torch.float32),
            "labels": torch.tensor(labels, dtype=torch.long),
        }


# =============================================================================
# Training Functions
# =============================================================================

def train_epoch(model, dataloader, optimizer, criterion, device):
    """Train for one epoch."""
    model.train()
    total_loss = 0
    total_positions = 0

    # For F1 calculation
    tp, fp, fn = 0, 0, 0

    for batch in dataloader:
        signals = batch["signals"].to(device)  # (batch, 4, seq_len)
        labels = batch["labels"].to(device)

        optimizer.zero_grad()
        logits = model(signals)  # (batch, seq_len, 2)

        # Reshape for loss
        logits_flat = logits.reshape(-1, 2)
        labels_flat = labels.view(-1)

        # Ignore padded positions (label = -1)
        mask = labels_flat >= 0
        loss = criterion(logits_flat[mask], labels_flat[mask])

        loss.backward()
        torch.nn.utils.clip_grad_norm_(model.parameters(), 1.0)
        optimizer.step()

        with torch.no_grad():
            preds = logits_flat[mask].argmax(dim=-1)
            labels_masked = labels_flat[mask]

            tp += ((preds == 1) & (labels_masked == 1)).sum().item()
            fp += ((preds == 1) & (labels_masked == 0)).sum().item()
            fn += ((preds == 0) & (labels_masked == 1)).sum().item()

        total_loss += loss.item() * mask.sum().item()
        total_positions += mask.sum().item()

    precision = tp / (tp + fp) if (tp + fp) > 0 else 0
    recall = tp / (tp + fn) if (tp + fn) > 0 else 0
    f1 = 2 * precision * recall / (precision + recall) if (precision + recall) > 0 else 0

    return {
        "loss": total_loss / total_positions,
        "f1": f1,
        "precision": precision,
        "recall": recall,
    }


def evaluate(model, dataloader, criterion, device):
    """Evaluate the model."""
    model.eval()
    total_loss = 0
    total_positions = 0

    # Per-class metrics
    tp = 0  # True positives (heterozygous correctly predicted)
    fp = 0  # False positives
    fn = 0  # False negatives

    with torch.no_grad():
        for batch in dataloader:
            signals = batch["signals"].to(device)  # (batch, 4, seq_len)
            labels = batch["labels"].to(device)

            logits = model(signals)

            logits_flat = logits.reshape(-1, 2)
            labels_flat = labels.view(-1)

            mask = labels_flat >= 0
            loss = criterion(logits_flat[mask], labels_flat[mask])

            preds = logits_flat[mask].argmax(dim=-1)

            # Compute TP, FP, FN for heterozygous class (1)
            tp += ((preds == 1) & (labels_flat[mask] == 1)).sum().item()
            fp += ((preds == 1) & (labels_flat[mask] == 0)).sum().item()
            fn += ((preds == 0) & (labels_flat[mask] == 1)).sum().item()

            total_loss += loss.item() * mask.sum().item()
            total_positions += mask.sum().item()

    precision = tp / (tp + fp) if (tp + fp) > 0 else 0
    recall = tp / (tp + fn) if (tp + fn) > 0 else 0
    f1 = 2 * precision * recall / (precision + recall) if (precision + recall) > 0 else 0

    return {
        "loss": total_loss / total_positions,
        "precision": precision,
        "recall": recall,
        "f1": f1,
    }


def generate_plots(history: dict, save_dir: Path):
    """Generate training plots."""
    save_dir.mkdir(parents=True, exist_ok=True)

    epochs = range(1, len(history["train_loss"]) + 1)

    fig, axes = plt.subplots(1, 2, figsize=(12, 5))

    # Loss plot
    ax = axes[0]
    ax.plot(
        epochs, history["train_loss"], label="Train", linewidth=2, color="#2196F3"
    )
    ax.plot(
        epochs, history["val_loss"], label="Val", linewidth=2, color="#FF5722"
    )
    ax.set_xlabel("Epoch")
    ax.set_ylabel("Loss")
    ax.set_title("Loss", fontweight="bold")
    ax.legend()
    ax.grid(True, alpha=0.3)

    # F1 / Precision / Recall plot
    ax = axes[1]
    train_f1 = [x * 100 for x in history["train_f1"]]
    ax.plot(epochs, train_f1, label="Train F1", linewidth=2, color="#2196F3")
    val_f1 = [x * 100 for x in history["val_f1"]]
    ax.plot(epochs, val_f1, label="Val F1", linewidth=2, color="#4CAF50")
    val_prec = [x * 100 for x in history["val_precision"]]
    ax.plot(
        epochs, val_prec, label="Val Precision",
        linewidth=2, color="#9C27B0", linestyle="--"
    )
    val_rec = [x * 100 for x in history["val_recall"]]
    ax.plot(
        epochs, val_rec, label="Val Recall",
        linewidth=2, color="#FF9800", linestyle="--"
    )
    ax.set_xlabel("Epoch")
    ax.set_ylabel("%")
    ax.set_title("Heterozygote Detection (F1)", fontweight="bold")
    ax.set_ylim(0, 105)
    ax.legend()
    ax.grid(True, alpha=0.3)

    plt.tight_layout()
    fig.savefig(save_dir / "training_curves.png", dpi=150, bbox_inches="tight")
    plt.close(fig)

    return [save_dir / "training_curves.png"]


# =============================================================================
# Main
# =============================================================================

def parse_args():
    parser = argparse.ArgumentParser(description="Train Heterozygote Classifier")

    parser.add_argument("--data-dir", type=str, default="datalake/datasets/heterozygote_training",
                        help="Dataset directory")
    parser.add_argument("--checkpoint-dir", type=str, default="checkpoints/heterozygote_training",
                        help="Checkpoint directory")
    parser.add_argument("--epochs", type=int, default=100, help="Number of epochs")
    parser.add_argument("--batch-size", type=int, default=64, help="Batch size")
    parser.add_argument("--lr", type=float, default=1e-3, help="Learning rate")
    parser.add_argument("--patience", type=int, default=20, help="Early stopping patience")
    parser.add_argument("--max-length", type=int, default=500, help="Max sequence length")
    parser.add_argument("--hidden-dim", type=int, default=128, help="Hidden dimension")
    parser.add_argument("--num-layers", type=int, default=5, help="Number of residual blocks")
    parser.add_argument("--dropout", type=float, default=0.3, help="Dropout rate")

    return parser.parse_args()


def main():
    args = parse_args()

    print("=" * 70)
    print("HETEROZYGOTE CLASSIFIER TRAINING")
    print("=" * 70)

    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    print(f"Device: {device}")

    # Load datasets
    data_dir = Path(args.data_dir)
    print(f"\nLoading data from: {data_dir}")

    train_dataset = HeterozygoteDataset(data_dir / "train", max_length=args.max_length)
    val_dataset = HeterozygoteDataset(data_dir / "val", max_length=args.max_length)

    print(f"Train samples: {len(train_dataset)}")
    print(f"Val samples: {len(val_dataset)}")

    train_loader = DataLoader(
        train_dataset,
        batch_size=args.batch_size,
        shuffle=True,
        num_workers=0,
    )
    val_loader = DataLoader(
        val_dataset,
        batch_size=args.batch_size,
        shuffle=False,
        num_workers=0,
    )

    # Create model
    config = HeterozygoteConfig(
        max_length=args.max_length,
        hidden_dim=args.hidden_dim,
        num_layers=args.num_layers,
        dropout=args.dropout,
    )

    model = HeterozygoteClassifier(config).to(device)
    print(f"Model parameters: {sum(p.numel() for p in model.parameters()):,}")

    # Setup training with class weights (heterozygotes are ~1.3% of positions)
    # Weight the minority class (heterozygous=1) higher to improve recall
    class_weights = torch.tensor([1.0, 50.0], device=device)
    criterion = nn.CrossEntropyLoss(weight=class_weights)
    print(f"Class weights: homo={class_weights[0]:.1f}, het={class_weights[1]:.1f}")

    optimizer = torch.optim.AdamW(model.parameters(), lr=args.lr, weight_decay=1e-4)
    scheduler = torch.optim.lr_scheduler.CosineAnnealingLR(optimizer, T_max=args.epochs)

    # Checkpoint directory
    checkpoint_dir = Path(args.checkpoint_dir)
    checkpoint_dir.mkdir(parents=True, exist_ok=True)
    print(f"Checkpoints: {checkpoint_dir}")

    # Training history
    history = {
        "train_loss": [], "train_f1": [],
        "val_loss": [], "val_f1": [],
        "val_precision": [], "val_recall": [],
    }

    best_val_loss = float("inf")
    best_epoch = 0
    patience_counter = 0

    print("\n" + "=" * 70)
    print("STARTING TRAINING")
    print("=" * 70)

    for epoch in range(args.epochs):
        train_metrics = train_epoch(model, train_loader, optimizer, criterion, device)
        val_metrics = evaluate(model, val_loader, criterion, device)
        scheduler.step()

        # Save history
        history["train_loss"].append(train_metrics["loss"])
        history["train_f1"].append(train_metrics["f1"])
        history["val_loss"].append(val_metrics["loss"])
        history["val_f1"].append(val_metrics["f1"])
        history["val_precision"].append(val_metrics["precision"])
        history["val_recall"].append(val_metrics["recall"])

        # Print progress
        print(
            f"Epoch {epoch+1:3d}/{args.epochs} | "
            f"Train: Loss={train_metrics['loss']:.4f} F1={train_metrics['f1']:.1%} | "
            f"Val: Loss={val_metrics['loss']:.4f} F1={val_metrics['f1']:.1%} "
            f"(P={val_metrics['precision']:.1%} R={val_metrics['recall']:.1%})"
        )

        # Save best model based on validation loss
        if val_metrics["loss"] < best_val_loss:
            best_val_loss = val_metrics["loss"]
            best_epoch = epoch + 1
            model.save(checkpoint_dir / "best.pt")
            patience_counter = 0
            print(f"  -> New best model saved (F1: {val_metrics['f1']:.1%})")
        else:
            patience_counter += 1

        if patience_counter >= args.patience:
            print(f"\nEarly stopping at epoch {epoch+1}")
            break

    # Save final model
    model.save(checkpoint_dir / "final.pt")

    # Save history
    with open(checkpoint_dir / "history.json", "w") as f:
        json.dump(history, f, indent=2)

    # Generate plots
    print("\nGenerating plots...")
    plots_dir = checkpoint_dir / "plots"
    plots = generate_plots(history, plots_dir)
    print(f"Saved {len(plots)} plots to {plots_dir}")

    # Save config
    run_config = {
        "timestamp": datetime.now().isoformat(),
        "epochs_trained": len(history["train_loss"]),
        "best_val_loss": best_val_loss,
        "best_val_f1": history["val_f1"][best_epoch - 1],
        "best_val_precision": history["val_precision"][best_epoch - 1],
        "best_val_recall": history["val_recall"][best_epoch - 1],
        "args": vars(args),
    }
    with open(checkpoint_dir / "config.json", "w") as f:
        json.dump(run_config, f, indent=2)

    print("\n" + "=" * 70)
    print("TRAINING COMPLETE")
    print("=" * 70)
    best_f1 = history['val_f1'][best_epoch-1]
    best_p = history['val_precision'][best_epoch-1]
    best_r = history['val_recall'][best_epoch-1]
    print(
        f"Best @ epoch {best_epoch}: Loss={best_val_loss:.4f} F1={best_f1:.1%} "
        f"(P={best_p:.1%} R={best_r:.1%})"
    )
    print(f"Checkpoints saved to: {checkpoint_dir}")


if __name__ == "__main__":
    main()
