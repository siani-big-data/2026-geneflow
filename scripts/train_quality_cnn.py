#!/usr/bin/env python3
"""
Train Quality Predictor CNN model.

Predicts Phred quality scores per position from chromatogram signals (4 channels: ACGT).

Usage:
    uv run python scripts/train_quality_cnn.py
    uv run python scripts/train_quality_cnn.py --epochs 100 --batch-size 64
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

from src.ml.models.quality import QualityPredictor, QualityPredictorConfig

# =============================================================================
# Dataset
# =============================================================================

class QualityAB1Dataset(Dataset):
    """Dataset for per-position quality prediction from chromatogram signals."""

    def __init__(self, data_dir: Path):
        self.data_dir = Path(data_dir)
        self.samples = sorted(self.data_dir.glob("sample_*.npz"))

        if not self.samples:
            raise ValueError(f"No samples found in {data_dir}")

    def __len__(self):
        return len(self.samples)

    def __getitem__(self, idx):
        data = np.load(self.samples[idx])

        signals = data["signals"].astype(np.float32)  # (4, seq_len)
        quality = data["quality"].astype(np.float32)  # (seq_len,)
        mask = data["mask"].astype(np.float32)        # (seq_len,)

        return {
            "signals": torch.tensor(signals, dtype=torch.float32),
            "quality": torch.tensor(quality, dtype=torch.float32),
            "mask": torch.tensor(mask, dtype=torch.float32),
        }


# =============================================================================
# Training Functions
# =============================================================================

def train_epoch(model, dataloader, optimizer, criterion, device, tolerance=2.0):
    """Train for one epoch."""
    model.train()
    total_loss = 0
    total_mae = 0
    total_correct = 0
    total_valid = 0
    total_samples = 0

    for batch in dataloader:
        signals = batch["signals"].to(device)
        quality = batch["quality"].to(device)
        mask = batch["mask"].to(device)

        optimizer.zero_grad()
        output = model(signals)  # (batch, seq_len)

        # Masked loss - only compute on valid positions
        loss = criterion(output * mask, quality * mask)
        valid_positions = mask.sum()

        if valid_positions > 0:
            loss = loss * (mask.numel() / valid_positions)  # Normalize by valid positions

        loss.backward()
        torch.nn.utils.clip_grad_norm_(model.parameters(), 1.0)
        optimizer.step()

        with torch.no_grad():
            errors = (output - quality).abs() * mask
            mae = errors.sum() / mask.sum()
            correct = ((errors < tolerance) * mask).sum()

        total_loss += loss.item() * signals.size(0)
        total_mae += mae.item() * signals.size(0)
        total_correct += correct.item()
        total_valid += mask.sum().item()
        total_samples += signals.size(0)

    return {
        "loss": total_loss / total_samples,
        "mae": total_mae / total_samples,
        "accuracy": total_correct / total_valid if total_valid > 0 else 0,
    }


def evaluate(model, dataloader, criterion, device, tolerance=2.0):
    """Evaluate the model."""
    model.eval()
    total_loss = 0
    total_mae = 0
    total_correct = 0
    total_valid = 0
    total_samples = 0

    all_preds = []
    all_targets = []

    with torch.no_grad():
        for batch in dataloader:
            signals = batch["signals"].to(device)
            quality = batch["quality"].to(device)
            mask = batch["mask"].to(device)

            output = model(signals)

            # Masked loss
            loss = criterion(output * mask, quality * mask)
            valid_positions = mask.sum()

            if valid_positions > 0:
                loss = loss * (mask.numel() / valid_positions)

            # MAE on valid positions
            errors = (output - quality).abs() * mask
            mae = errors.sum() / mask.sum()

            # Accuracy: % of positions within tolerance
            correct = ((errors < tolerance) * mask).sum()

            total_loss += loss.item() * signals.size(0)
            total_mae += mae.item() * signals.size(0)
            total_correct += correct.item()
            total_valid += mask.sum().item()
            total_samples += signals.size(0)

            # Collect for analysis
            for i in range(signals.size(0)):
                m = mask[i].bool()
                all_preds.extend(output[i][m].cpu().numpy())
                all_targets.extend(quality[i][m].cpu().numpy())

    return {
        "loss": total_loss / total_samples,
        "mae": total_mae / total_samples,
        "accuracy": total_correct / total_valid if total_valid > 0 else 0,
        "predictions": np.array(all_preds),
        "targets": np.array(all_targets),
    }


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
    ax.set_ylabel("MAE (Phred points)")
    ax.set_title("Mean Absolute Error", fontweight="bold")
    ax.legend()
    ax.grid(True, alpha=0.3)

    # Accuracy plot
    ax = axes[2]
    train_acc_pct = [x * 100 for x in history["train_acc"]]
    val_acc_pct = [x * 100 for x in history["val_acc"]]
    ax.plot(epochs, train_acc_pct, label="Train", linewidth=2, color="#2196F3")
    ax.plot(epochs, val_acc_pct, label="Val", linewidth=2, color="#FF5722")
    ax.set_xlabel("Epoch")
    ax.set_ylabel("Accuracy (%)")
    ax.set_title("Accuracy (within ±2 Phred)", fontweight="bold")
    ax.set_ylim(0, 105)
    ax.legend()
    ax.grid(True, alpha=0.3)

    plt.tight_layout()
    fig.savefig(save_dir / "training_curves.png", dpi=150, bbox_inches="tight")
    plt.close(fig)

    return [save_dir / "training_curves.png"]


def generate_scatter_plot(predictions: np.ndarray, targets: np.ndarray, save_path: Path):
    """Generate predicted vs actual scatter plot."""
    fig, ax = plt.subplots(figsize=(8, 8))

    # Subsample if too many points
    n = len(predictions)
    if n > 10000:
        idx = np.random.choice(n, 10000, replace=False)
        predictions = predictions[idx]
        targets = targets[idx]

    ax.scatter(targets, predictions, alpha=0.3, s=5, c="#2196F3")

    # Perfect prediction line
    min_val = min(targets.min(), predictions.min())
    max_val = max(targets.max(), predictions.max())
    ax.plot([min_val, max_val], [min_val, max_val], 'k--', lw=1.5, label='Perfect')

    # Metrics
    mae = np.abs(targets - predictions).mean()
    rmse = np.sqrt(((targets - predictions) ** 2).mean())
    r2 = 1 - ((targets - predictions) ** 2).sum() / ((targets - targets.mean()) ** 2).sum()

    ax.text(0.05, 0.95, f'MAE: {mae:.2f}\nRMSE: {rmse:.2f}\nR²: {r2:.3f}',
            transform=ax.transAxes, fontsize=10, verticalalignment='top',
            bbox=dict(boxstyle='round', facecolor='white', alpha=0.8))

    ax.set_xlabel('Actual Quality (Phred)')
    ax.set_ylabel('Predicted Quality (Phred)')
    ax.set_title('Predicted vs Actual Quality Score', fontweight='bold')
    ax.legend(loc='lower right')

    fig.savefig(save_path, dpi=150, bbox_inches='tight')
    plt.close(fig)


# =============================================================================
# Main
# =============================================================================

def parse_args():
    parser = argparse.ArgumentParser(description="Train Quality Predictor CNN")

    parser.add_argument("--data-dir", type=str, default="datalake/datasets/quality",
                        help="Dataset directory")
    parser.add_argument("--checkpoint-dir", type=str, default="checkpoints/quality_cnn",
                        help="Checkpoint directory")
    parser.add_argument("--epochs", type=int, default=100, help="Number of epochs")
    parser.add_argument("--batch-size", type=int, default=32, help="Batch size")
    parser.add_argument("--lr", type=float, default=1e-3, help="Learning rate")
    parser.add_argument("--patience", type=int, default=15, help="Early stopping patience")
    parser.add_argument("--hidden-channels", type=int, default=64, help="Hidden channels")
    parser.add_argument("--num-layers", type=int, default=5, help="Number of conv layers")
    parser.add_argument("--dropout", type=float, default=0.3, help="Dropout rate")
    parser.add_argument("--tolerance", type=float, default=5.0,
                        help="Tolerance for accuracy (Phred points)")

    return parser.parse_args()


def main():
    args = parse_args()

    print("=" * 70)
    print("QUALITY PREDICTOR CNN TRAINING")
    print("=" * 70)
    print("Task: Predict Phred quality score per position from chromatogram signals")
    print()

    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    print(f"Device: {device}")

    # Load datasets
    data_dir = Path(args.data_dir)
    print(f"\nLoading data from: {data_dir}")

    train_dataset = QualityAB1Dataset(data_dir / "train")
    val_dataset = QualityAB1Dataset(data_dir / "val")

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
    config = QualityPredictorConfig(
        input_channels=4,
        hidden_channels=args.hidden_channels,
        num_layers=args.num_layers,
        dropout=args.dropout,
    )

    model = QualityPredictor(config).to(device)
    print("\nModel: QualityPredictor CNN")
    print(f"  Hidden channels: {args.hidden_channels}")
    print(f"  Num layers: {args.num_layers}")
    print(f"  Dropout: {args.dropout}")
    print(f"  Parameters: {sum(p.numel() for p in model.parameters()):,}")

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
    best_val_mae = float("inf")
    best_epoch = 0
    patience_counter = 0

    print("\n" + "=" * 70)
    print("STARTING TRAINING")
    print("=" * 70)
    print(f"Accuracy metric: %% positions within ±{args.tolerance} Phred points")

    for epoch in range(args.epochs):
        # Train
        train_metrics = train_epoch(
            model, train_loader, optimizer, criterion, device, args.tolerance
        )

        # Evaluate
        val_metrics = evaluate(model, val_loader, criterion, device, args.tolerance)

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
        print(
            f"Epoch {epoch+1:3d}/{args.epochs} | "
            f"Train: Loss={train_metrics['loss']:.4f} "
            f"MAE={train_metrics['mae']:.2f} Acc={train_metrics['accuracy']:.1%} | "
            f"Val: Loss={val_metrics['loss']:.4f} "
            f"MAE={val_metrics['mae']:.2f} Acc={val_metrics['accuracy']:.1%}"
        )

        # Save best model
        if val_metrics["loss"] < best_val_loss:
            best_val_loss = val_metrics["loss"]
            best_val_mae = val_metrics["mae"]
            best_epoch = epoch + 1
            model.save(checkpoint_dir / "best.pt")
            patience_counter = 0
            print(f"  -> New best model saved (acc: {val_metrics['accuracy']:.2%})")
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
    generate_plots(history, plots_dir)

    # Generate scatter plot
    val_metrics = evaluate(model, val_loader, criterion, device, args.tolerance)
    generate_scatter_plot(val_metrics["predictions"], val_metrics["targets"],
                          plots_dir / "scatter_val.png")

    print(f"Saved plots to {plots_dir}")

    # Test plots if exists
    test_dir = data_dir / "test"
    if test_dir.exists():
        print("\n" + "-" * 70)
        print("TEST SET EVALUATION")
        print("-" * 70)

        test_dataset = QualityAB1Dataset(test_dir)
        test_loader = DataLoader(test_dataset, batch_size=args.batch_size, shuffle=False)

        # Load best model
        model = QualityPredictor(config).to(device)
        model.load(checkpoint_dir / "best.pt")
        test_metrics = evaluate(model, test_loader, criterion, device, args.tolerance)

        print(
            f"Test | Loss: {test_metrics['loss']:.4f} | "
            f"MAE: {test_metrics['mae']:.2f} | Acc: {test_metrics['accuracy']:.1%}"
        )

        generate_scatter_plot(test_metrics["predictions"], test_metrics["targets"],
                              plots_dir / "scatter_test.png")

    # Save config
    run_config = {
        "timestamp": datetime.now().isoformat(),
        "epochs_trained": len(history["train_loss"]),
        "best_val_loss": best_val_loss,
        "best_val_mae": best_val_mae,
        "best_epoch": best_epoch,
        "args": vars(args),
    }
    with open(checkpoint_dir / "config.json", "w") as f:
        json.dump(run_config, f, indent=2)

    print("\n" + "=" * 70)
    print("TRAINING COMPLETE")
    print("=" * 70)
    print(f"Best @ epoch {best_epoch}: Loss={best_val_loss:.4f} | MAE={best_val_mae:.2f}")
    print(f"Checkpoints saved to: {checkpoint_dir}")


if __name__ == "__main__":
    main()
