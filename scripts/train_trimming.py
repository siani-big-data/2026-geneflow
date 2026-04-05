#!/usr/bin/env python3
"""
Train the Trimming Predictor model.

Predicts optimal trim points (start, end) for DNA sequences based on quality scores.

Usage:
    uv run python scripts/train_trimming.py
    uv run python scripts/train_trimming.py --epochs 100 --batch-size 64
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
from torch.utils.data import Dataset, DataLoader

from src.ml.models.trimming import TrimmingPredictor, TrimmingConfig


# =============================================================================
# Dataset
# =============================================================================

class TrimmingDataset(Dataset):
    """Dataset for trimming prediction."""

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

        quality = data["quality"].astype(np.float32)
        start_ratio = data["start_ratio"][0]
        end_ratio = data["end_ratio"][0]

        # Pad or truncate to max_length
        if len(quality) > self.max_length:
            # Take center portion
            start = (len(quality) - self.max_length) // 2
            quality = quality[start:start + self.max_length]
        elif len(quality) < self.max_length:
            # Pad with zeros
            pad = self.max_length - len(quality)
            quality = np.pad(quality, (0, pad), constant_values=0)

        # Normalize quality to [0, 1]
        quality = quality / 60.0
        quality = np.clip(quality, 0, 1)

        return {
            "quality": torch.tensor(quality, dtype=torch.float32),
            "target": torch.tensor([start_ratio, end_ratio], dtype=torch.float32),
        }


# =============================================================================
# Training Functions
# =============================================================================

def train_epoch(model, dataloader, optimizer, criterion, device, tolerance=0.05):
    """Train for one epoch."""
    model.train()
    total_loss = 0
    total_mae = 0
    total_correct = 0
    n_samples = 0

    for batch in dataloader:
        quality = batch["quality"].to(device)
        target = batch["target"].to(device)

        optimizer.zero_grad()
        output = model(quality)
        loss = criterion(output, target)

        loss.backward()
        torch.nn.utils.clip_grad_norm_(model.parameters(), 1.0)
        optimizer.step()

        with torch.no_grad():
            errors = (output - target).abs()
            max_error = errors.max(dim=1)[0]
            correct = (max_error < tolerance).sum().item()

        total_loss += loss.item() * len(quality)
        total_mae += errors.mean().item() * len(quality)
        total_correct += correct
        n_samples += len(quality)

    return {
        "loss": total_loss / n_samples,
        "mae": total_mae / n_samples,
        "accuracy": total_correct / n_samples,
    }


def evaluate(model, dataloader, criterion, device, tolerance=0.05, verbose=False):
    """Evaluate the model."""
    model.eval()
    total_loss = 0
    total_mae = 0
    total_correct = 0
    n_samples = 0

    # Collect predictions for diagnostics
    all_preds_start = []
    all_preds_end = []
    all_targets_start = []
    all_targets_end = []

    with torch.no_grad():
        for batch in dataloader:
            quality = batch["quality"].to(device)
            target = batch["target"].to(device)

            output = model(quality)
            loss = criterion(output, target)

            errors = (output - target).abs()
            max_error = errors.max(dim=1)[0]
            correct = (max_error < tolerance).sum().item()

            total_loss += loss.item() * len(quality)
            total_mae += errors.mean().item() * len(quality)
            total_correct += correct
            n_samples += len(quality)

            # Collect for diagnostics
            all_preds_start.extend(output[:, 0].cpu().tolist())
            all_preds_end.extend(output[:, 1].cpu().tolist())
            all_targets_start.extend(target[:, 0].cpu().tolist())
            all_targets_end.extend(target[:, 1].cpu().tolist())

    result = {
        "loss": total_loss / n_samples,
        "mae": total_mae / n_samples,
        "accuracy": total_correct / n_samples,
    }

    # Add diagnostics
    if verbose:
        preds_start = np.array(all_preds_start)
        preds_end = np.array(all_preds_end)
        result["pred_start_mean"] = preds_start.mean()
        result["pred_start_std"] = preds_start.std()
        result["pred_end_mean"] = preds_end.mean()
        result["pred_end_std"] = preds_end.std()

    return result


def generate_plots(history: dict, save_dir: Path):
    """Generate training plots."""
    save_dir.mkdir(parents=True, exist_ok=True)

    epochs = range(1, len(history["train_loss"]) + 1)

    fig, axes = plt.subplots(1, 3, figsize=(15, 5))

    # Loss plot
    ax = axes[0]
    ax.plot(epochs, history["train_loss"], label="Train", linewidth=2, color="#2196F3")
    ax.plot(epochs, history["val_loss"], label="Val", linewidth=2, color="#FF5722")
    ax.set_xlabel("Epoch")
    ax.set_ylabel("Loss (MSE)")
    ax.set_title("Loss", fontweight="bold")
    ax.legend()
    ax.grid(True, alpha=0.3)

    # MAE plot
    ax = axes[1]
    ax.plot(epochs, history["train_mae"], label="Train", linewidth=2, color="#2196F3")
    ax.plot(epochs, history["val_mae"], label="Val", linewidth=2, color="#FF5722")
    ax.set_xlabel("Epoch")
    ax.set_ylabel("MAE")
    ax.set_title("Mean Absolute Error", fontweight="bold")
    ax.legend()
    ax.grid(True, alpha=0.3)

    # Accuracy plot
    ax = axes[2]
    ax.plot(epochs, [x * 100 for x in history["train_acc"]], label="Train", linewidth=2, color="#2196F3")
    ax.plot(epochs, [x * 100 for x in history["val_acc"]], label="Val", linewidth=2, color="#FF5722")
    ax.set_xlabel("Epoch")
    ax.set_ylabel("Accuracy (%)")
    ax.set_title("Accuracy", fontweight="bold")
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
    parser = argparse.ArgumentParser(description="Train Trimming Predictor")

    parser.add_argument("--data-dir", type=str, default="datalake/datasets/trimming",
                        help="Dataset directory")
    parser.add_argument("--checkpoint-dir", type=str, default="checkpoints/trimming",
                        help="Checkpoint directory")
    parser.add_argument("--epochs", type=int, default=100, help="Number of epochs")
    parser.add_argument("--batch-size", type=int, default=64, help="Batch size")
    parser.add_argument("--lr", type=float, default=1e-3, help="Learning rate")
    parser.add_argument("--patience", type=int, default=15, help="Early stopping patience")
    parser.add_argument("--max-length", type=int, default=500, help="Max sequence length")
    parser.add_argument("--hidden-dim", type=int, default=64, help="Hidden dimension")
    parser.add_argument("--num-layers", type=int, default=4, help="Number of conv layers")
    parser.add_argument("--dropout", type=float, default=0.5, help="Dropout rate")
    parser.add_argument("--tolerance", type=float, default=0.1, help="Tolerance for accuracy (ratio, default 0.1 = 10%%)")

    return parser.parse_args()


def main():
    args = parse_args()

    print("=" * 60)
    print("Trimming Predictor Training")
    print("=" * 60)

    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    print(f"Device: {device}")

    # Load datasets
    data_dir = Path(args.data_dir)
    print(f"\nLoading data from: {data_dir}")

    train_dataset = TrimmingDataset(data_dir / "train", max_length=args.max_length)
    val_dataset = TrimmingDataset(data_dir / "val", max_length=args.max_length)

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
    config = TrimmingConfig(
        max_length=args.max_length,
        hidden_dim=args.hidden_dim,
        num_layers=args.num_layers,
        dropout=args.dropout,
    )

    model = TrimmingPredictor(config).to(device)
    print(f"\nModel: 1D CNN with {args.num_layers} layers, hidden_dim={args.hidden_dim}, dropout={args.dropout}")
    print(f"Model parameters: {sum(p.numel() for p in model.parameters()):,}")

    # Setup training
    criterion = nn.MSELoss()
    optimizer = torch.optim.AdamW(model.parameters(), lr=args.lr, weight_decay=1e-4)
    scheduler = torch.optim.lr_scheduler.CosineAnnealingLR(optimizer, T_max=args.epochs)

    # Checkpoint directory
    checkpoint_dir = Path(args.checkpoint_dir)
    checkpoint_dir.mkdir(parents=True, exist_ok=True)
    print(f"Checkpoints: {checkpoint_dir}")

    # Training history
    history = {
        "train_loss": [], "train_mae": [], "train_acc": [],
        "val_loss": [], "val_mae": [], "val_acc": [],
    }

    best_val_loss = float("inf")
    best_val_acc = 0.0
    best_epoch = 0
    patience_counter = 0

    print("\n" + "=" * 70)
    print("STARTING TRAINING")
    print("=" * 70)
    print(f"Accuracy metric: % predictions within ±{args.tolerance*100:.0f}% of correct trim points")

    for epoch in range(args.epochs):
        # Train
        train_metrics = train_epoch(model, train_loader, optimizer, criterion, device, args.tolerance)

        # Evaluate with diagnostics every 10 epochs
        verbose = (epoch + 1) % 10 == 0 or epoch == 0
        val_metrics = evaluate(model, val_loader, criterion, device, args.tolerance, verbose=verbose)

        # Update scheduler
        scheduler.step()

        # Save history
        history["train_loss"].append(train_metrics["loss"])
        history["train_mae"].append(train_metrics["mae"])
        history["train_acc"].append(train_metrics["accuracy"])
        history["val_loss"].append(val_metrics["loss"])
        history["val_mae"].append(val_metrics["mae"])
        history["val_acc"].append(val_metrics["accuracy"])

        # Print progress
        print(f"Epoch {epoch+1:3d}/{args.epochs} | "
              f"Train: Loss={train_metrics['loss']:.4f} MAE={train_metrics['mae']:.4f} Acc={train_metrics['accuracy']:.1%} | "
              f"Val: Loss={val_metrics['loss']:.4f} MAE={val_metrics['mae']:.4f} Acc={val_metrics['accuracy']:.1%}")

        # Save best model
        if val_metrics["loss"] < best_val_loss:
            best_val_loss = val_metrics["loss"]
            best_val_acc = val_metrics["accuracy"]
            best_epoch = epoch + 1
            model.save(checkpoint_dir / "best.pt")
            patience_counter = 0
            print(f"  -> New best model saved (acc: {best_val_acc:.2%})")
        else:
            patience_counter += 1

        # Early stopping
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
        "best_val_acc": best_val_acc,
        "final_val_mae": history["val_mae"][-1],
        "final_val_acc": history["val_acc"][-1],
        "args": vars(args),
    }
    with open(checkpoint_dir / "config.json", "w") as f:
        json.dump(run_config, f, indent=2)

    print("\n" + "=" * 70)
    print("TRAINING COMPLETE")
    print("=" * 70)
    print(f"Best @ epoch {best_epoch}: Loss={best_val_loss:.4f} | MAE={history['val_mae'][best_epoch-1]:.4f} | Acc={best_val_acc:.1%}")
    print(f"Checkpoints saved to: {checkpoint_dir}")


if __name__ == "__main__":
    main()
