#!/usr/bin/env python3
"""
Train Quality Predictor using precomputed dataset.

Predicts mean quality score per sequence using global features.
Uses unified results system for reproducible experiments and paper-ready figures.

Usage:
    uv run python scripts/train_quality_precomputed.py
    uv run python scripts/train_quality_precomputed.py --epochs 200 --batch-size 64
"""

import argparse
import json
import sys
import time
from datetime import datetime
from pathlib import Path

import numpy as np
import torch
import torch.nn as nn
from torch.optim import AdamW
from torch.optim.lr_scheduler import CosineAnnealingLR
from torch.utils.data import DataLoader, Dataset

sys.path.insert(0, str(Path(__file__).parent.parent))

from src.ml.training import (
    DatasetInfo,
    EpochMetrics,
    HardwareInfo,
    PaperFigures,
    Predictions,
    ResultsWriter,
    TrainingResult,
)


class PrecomputedQualityDataset(Dataset):
    """Load precomputed quality dataset - predicts mean quality from features."""

    def __init__(self, data_dir: Path):
        self.data_dir = Path(data_dir)
        self.files = sorted(self.data_dir.glob("sample_*.npz"))

        if not self.files:
            raise ValueError(f"No sample files found in {data_dir}")

        # Detect feature dimension
        sample = np.load(self.files[0])
        self.num_features = sample["features"].shape[0]

    def __len__(self) -> int:
        return len(self.files)

    def __getitem__(self, idx: int) -> dict[str, torch.Tensor]:
        data = np.load(self.files[idx])

        features = data["features"].astype(np.float32)
        mean_quality = float(data["mean_quality"])

        return {
            "features": torch.tensor(features, dtype=torch.float32),
            "quality": torch.tensor(mean_quality, dtype=torch.float32),
        }


class QualityMLP(nn.Module):
    """Simple MLP for quality prediction from features."""

    def __init__(self, input_dim: int, hidden_dims: list[int], dropout: float = 0.2):
        super().__init__()

        layers = []
        prev_dim = input_dim

        for hidden_dim in hidden_dims:
            layers.extend([
                nn.Linear(prev_dim, hidden_dim),
                nn.BatchNorm1d(hidden_dim),
                nn.ReLU(),
                nn.Dropout(dropout),
            ])
            prev_dim = hidden_dim

        # Output layer (single value)
        layers.append(nn.Linear(prev_dim, 1))

        self.net = nn.Sequential(*layers)

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        return self.net(x).squeeze(-1)


def compute_metrics(outputs, targets, tolerance=1.0):
    """Compute MAE and accuracy (% predictions within tolerance)."""
    abs_errors = torch.abs(outputs - targets)
    mae = abs_errors.mean()
    accuracy = (abs_errors <= tolerance).float().mean()
    return mae, accuracy


def train_epoch(model, loader, optimizer, criterion, device, tolerance=1.0):
    model.train()
    total_loss = 0.0
    total_mae = 0.0
    total_correct = 0
    total_samples = 0

    for batch in loader:
        optimizer.zero_grad()

        features = batch["features"].to(device)
        quality = batch["quality"].to(device)

        outputs = model(features)
        loss = criterion(outputs, quality)

        loss.backward()
        optimizer.step()

        with torch.no_grad():
            mae, accuracy = compute_metrics(outputs, quality, tolerance)

        batch_size = features.size(0)
        total_loss += loss.item() * batch_size
        total_mae += mae.item() * batch_size
        total_correct += accuracy.item() * batch_size
        total_samples += batch_size

    return {
        "loss": total_loss / total_samples,
        "mae": total_mae / total_samples,
        "accuracy": total_correct / total_samples,
    }


def evaluate(model, loader, criterion, device, tolerance=1.0):
    model.eval()
    total_loss = 0.0
    total_mae = 0.0
    total_correct = 0
    total_samples = 0

    all_preds = []
    all_targets = []

    with torch.no_grad():
        for batch in loader:
            features = batch["features"].to(device)
            quality = batch["quality"].to(device)

            outputs = model(features)
            loss = criterion(outputs, quality)

            mae, accuracy = compute_metrics(outputs, quality, tolerance)

            batch_size = features.size(0)
            total_loss += loss.item() * batch_size
            total_mae += mae.item() * batch_size
            total_correct += accuracy.item() * batch_size
            total_samples += batch_size

            all_preds.extend(outputs.cpu().numpy())
            all_targets.extend(quality.cpu().numpy())

    return {
        "loss": total_loss / total_samples,
        "mae": total_mae / total_samples,
        "accuracy": total_correct / total_samples,
        "predictions": np.array(all_preds),
        "targets": np.array(all_targets),
    }


def main():
    parser = argparse.ArgumentParser(description="Train Quality Predictor (mean quality)")
    parser.add_argument("--data-dir", type=str, default="datalake/datasets/quality",
                        help="Dataset directory")
    parser.add_argument("--checkpoint-dir", type=str, default="checkpoints/quality",
                        help="Checkpoint directory")
    parser.add_argument("--epochs", type=int, default=200)
    parser.add_argument("--batch-size", type=int, default=64)
    parser.add_argument("--lr", type=float, default=1e-4)
    parser.add_argument("--hidden-dims", type=int, nargs="+", default=[128, 64])
    parser.add_argument("--dropout", type=float, default=0.5)
    parser.add_argument(
        "--tolerance", type=float, default=1.0,
        help="Tolerance for accuracy (±X quality points)"
    )
    parser.add_argument("--patience", type=int, default=30)
    parser.add_argument("--seed", type=int, default=42)
    args = parser.parse_args()

    torch.manual_seed(args.seed)
    if torch.cuda.is_available():
        torch.cuda.manual_seed(args.seed)

    data_dir = Path(args.data_dir)
    checkpoint_dir = Path(args.checkpoint_dir)
    checkpoint_dir.mkdir(parents=True, exist_ok=True)

    print("=" * 70)
    print("QUALITY PREDICTOR TRAINING (Mean Quality per Sequence)")
    print("=" * 70)

    # Load metadata
    metadata_path = data_dir / "metadata.json"
    if not metadata_path.exists():
        print(f"ERROR: metadata.json not found in {data_dir}")
        return 1

    with open(metadata_path) as f:
        metadata = json.load(f)

    print(f"\nDataset: {data_dir}")
    print(f"Total samples: {metadata['total_samples']}")
    print(
        f"Train: {metadata['train_samples']} | Val: {metadata['val_samples']} | "
        f"Test: {metadata['test_samples']}"
    )
    print(f"Quality range: {metadata['quality_range']}")
    print(f"Mean quality: {metadata['mean_quality']:.2f}")

    # Load datasets
    print("\n" + "-" * 70)
    print("LOADING DATASETS")
    print("-" * 70)

    train_dataset = PrecomputedQualityDataset(data_dir / "train")
    val_dataset = PrecomputedQualityDataset(data_dir / "val")

    print(f"Train samples: {len(train_dataset)}")
    print(f"Val samples: {len(val_dataset)}")
    print(f"Feature dimension: {train_dataset.num_features}")

    train_loader = DataLoader(
        train_dataset,
        batch_size=args.batch_size,
        shuffle=True,
        num_workers=0,
        pin_memory=torch.cuda.is_available(),
    )
    val_loader = DataLoader(
        val_dataset,
        batch_size=args.batch_size,
        shuffle=False,
        num_workers=0,
        pin_memory=torch.cuda.is_available(),
    )

    # Model
    print("\n" + "-" * 70)
    print("MODEL CONFIGURATION")
    print("-" * 70)

    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

    model = QualityMLP(
        input_dim=train_dataset.num_features,
        hidden_dims=args.hidden_dims,
        dropout=args.dropout,
    ).to(device)

    print("Model: QualityMLP")
    print(f"Input: {train_dataset.num_features} features")
    print(f"Hidden layers: {args.hidden_dims}")
    print(f"Dropout: {args.dropout}")
    print(f"Parameters: {sum(p.numel() for p in model.parameters() if p.requires_grad):,}")
    print(f"Device: {device}")

    # Training setup
    criterion = nn.MSELoss()
    optimizer = AdamW(model.parameters(), lr=args.lr, weight_decay=0.01)
    scheduler = CosineAnnealingLR(optimizer, T_max=args.epochs, eta_min=args.lr * 0.01)

    # Training
    print("\n" + "=" * 70)
    print("STARTING TRAINING")
    print("=" * 70)
    print(f"Accuracy metric: % predictions within ±{args.tolerance} quality points")

    epoch_history = []
    best_val_loss = float('inf')
    best_val_mae = float('inf')
    best_val_acc = 0.0
    best_epoch = 0
    no_improve = 0
    best_model_state = None

    training_start = time.time()

    for epoch in range(args.epochs):
        train_metrics = train_epoch(
            model, train_loader, optimizer, criterion, device, args.tolerance
        )
        val_metrics = evaluate(model, val_loader, criterion, device, args.tolerance)
        lr = optimizer.param_groups[0]["lr"]
        scheduler.step()

        # Store epoch metrics
        epoch_history.append(EpochMetrics(
            epoch=epoch + 1,
            train_loss=train_metrics["loss"],
            val_loss=val_metrics["loss"],
            train_accuracy=train_metrics["accuracy"],
            val_accuracy=val_metrics["accuracy"],
            learning_rate=lr,
            extra={
                "train_mae": train_metrics["mae"],
                "val_mae": val_metrics["mae"],
            }
        ))

        print(
            f"Epoch {epoch + 1:3d}/{args.epochs} | "
            f"Train: Loss={train_metrics['loss']:.4f} "
            f"MAE={train_metrics['mae']:.2f} Acc={train_metrics['accuracy']:.1%} | "
            f"Val: Loss={val_metrics['loss']:.4f} "
            f"MAE={val_metrics['mae']:.2f} Acc={val_metrics['accuracy']:.1%}"
        )

        if val_metrics["loss"] < best_val_loss:
            best_val_loss = val_metrics["loss"]
            best_val_mae = val_metrics["mae"]
            best_val_acc = val_metrics["accuracy"]
            best_epoch = epoch + 1
            no_improve = 0
            best_model_state = model.state_dict().copy()
            print(
                f"         -> New best! Loss={best_val_loss:.4f} "
                f"MAE={best_val_mae:.2f} Acc={best_val_acc:.1%}"
            )
        else:
            no_improve += 1

        if no_improve >= args.patience:
            print(f"\nEarly stopping at epoch {epoch + 1}")
            break

    training_time = time.time() - training_start

    # Load best model for final plots
    if best_model_state:
        model.load_state_dict(best_model_state)

    # Final plots on validation
    final_val = evaluate(model, val_loader, criterion, device, args.tolerance)

    # Test plots
    test_metrics_dict = {}
    test_predictions = None
    test_dir = data_dir / "test"

    if test_dir.exists():
        print("\n" + "-" * 70)
        print("TEST SET EVALUATION")
        print("-" * 70)

        test_dataset = PrecomputedQualityDataset(test_dir)
        test_loader = DataLoader(test_dataset, batch_size=args.batch_size, shuffle=False)

        test_metrics = evaluate(model, test_loader, criterion, device, args.tolerance)
        test_metrics_dict = {
            "loss": test_metrics["loss"],
            "mae": test_metrics["mae"],
            "accuracy": test_metrics["accuracy"],
            "rmse": float(np.sqrt(test_metrics["loss"])),
        }
        test_predictions = Predictions(
            y_true=test_metrics["targets"],
            y_pred=test_metrics["predictions"],
        )

        print(
            f"Test | Loss: {test_metrics['loss']:.4f} | "
            f"MAE: {test_metrics['mae']:.2f} | Acc: {test_metrics['accuracy']:.1%}"
        )

        # Show some predictions
        print("\nSample predictions (first 10):")
        print(f"{'Predicted':>10} {'Actual':>10} {'Error':>10}")
        for pred, target in zip(test_metrics["predictions"][:10], test_metrics["targets"][:10]):
            error = pred - target
            print(f"{pred:>10.2f} {target:>10.2f} {error:>+10.2f}")

    # Build TrainingResult
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    run_id = f"quality_{timestamp}"

    result = TrainingResult(
        run_id=run_id,
        model_name="QualityMLP",
        model_type="MLP",
        dataset=DatasetInfo(
            name="quality_enhanced",
            task="regression",
            train_samples=len(train_dataset),
            val_samples=len(val_dataset),
            test_samples=len(test_dataset) if test_dir.exists() else 0,
            n_features=train_dataset.num_features,
        ),
        hardware=HardwareInfo(
            backend="pytorch_gpu" if torch.cuda.is_available() else "pytorch_cpu",
            device=str(device),
            gpu_name=torch.cuda.get_device_name(0) if torch.cuda.is_available() else None,
        ),
        hyperparameters={
            "hidden_dims": args.hidden_dims,
            "dropout": args.dropout,
            "learning_rate": args.lr,
            "batch_size": args.batch_size,
            "epochs": args.epochs,
            "patience": args.patience,
            "tolerance": args.tolerance,
            "seed": args.seed,
        },
        training_time_sec=training_time,
        train_metrics={
            "loss": epoch_history[-1].train_loss,
            "accuracy": epoch_history[-1].train_accuracy,
            "mae": epoch_history[-1].extra.get("train_mae", 0),
        },
        val_metrics={
            "loss": best_val_loss,
            "mae": best_val_mae,
            "accuracy": best_val_acc,
            "rmse": float(np.sqrt(best_val_loss)),
        },
        test_metrics=test_metrics_dict,
        history=epoch_history,
        metadata={
            "best_epoch": best_epoch,
            "total_epochs": len(epoch_history),
            "early_stopped": no_improve >= args.patience,
            "quality_range": metadata.get("quality_range"),
            "mean_quality": metadata.get("mean_quality"),
        },
    )

    # Save using unified system
    writer = ResultsWriter(base_dir=args.checkpoint_dir)

    # Prepare predictions
    train_eval = evaluate(model, train_loader, criterion, device, args.tolerance)
    train_preds = Predictions(y_true=train_eval["targets"], y_pred=train_eval["predictions"])
    val_preds = Predictions(y_true=final_val["targets"], y_pred=final_val["predictions"])

    # Save model as dict for portability
    model_data = {
        "model_state_dict": model.state_dict(),
        "input_dim": train_dataset.num_features,
        "hidden_dims": args.hidden_dims,
        "dropout": args.dropout,
    }

    checkpoint_dir = writer.save(
        result=result,
        model=model_data,
        train_predictions=train_preds,
        val_predictions=val_preds,
        test_predictions=test_predictions,
    )

    # Generate publication-ready figures
    print("\nGenerating figures...")
    figures = PaperFigures(checkpoint_dir / "figures", format="png")

    # Training curves
    history_dicts = [h.to_dict() for h in epoch_history]
    figures.training_curves(history_dicts, metrics=["loss", "accuracy"])

    # Regression-specific: scatter plot
    _generate_regression_figures(figures, val_preds, "Validation")
    if test_predictions:
        _generate_regression_figures(figures, test_predictions, "Test")

    print(f"  Generated figures in {checkpoint_dir / 'figures'}")

    # Print summary
    print("\n" + "=" * 70)
    print("TRAINING COMPLETE")
    print("=" * 70)
    print(f"  Run ID: {run_id}")
    print(f"  Best epoch: {best_epoch}/{len(epoch_history)}")
    print(f"  Training time: {training_time:.1f}s")
    print(f"  Val Loss: {best_val_loss:.4f}")
    print(f"  Val MAE: {best_val_mae:.2f}")
    print(f"  Val Accuracy (±{args.tolerance}): {best_val_acc:.1%}")
    if test_metrics_dict:
        print(f"  Test MAE: {test_metrics_dict['mae']:.2f}")
        print(f"  Test Accuracy: {test_metrics_dict['accuracy']:.1%}")
    print(f"\nResults saved to: {checkpoint_dir}")

    return 0


def _generate_regression_figures(figures: PaperFigures, predictions: Predictions, split_name: str):
    """Generate regression-specific figures."""
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt

    y_true = predictions.y_true
    y_pred = predictions.y_pred

    # Scatter plot: predicted vs actual
    fig, ax = plt.subplots(figsize=(8, 8))

    ax.scatter(y_true, y_pred, alpha=0.5, s=10, c=figures.COLORS[0])

    # Perfect prediction line
    min_val = min(y_true.min(), y_pred.min())
    max_val = max(y_true.max(), y_pred.max())
    ax.plot([min_val, max_val], [min_val, max_val], 'k--', lw=1.5, label='Perfect prediction')

    # Metrics
    mae = np.abs(y_true - y_pred).mean()
    rmse = np.sqrt(((y_true - y_pred) ** 2).mean())
    r2 = 1 - ((y_true - y_pred) ** 2).sum() / ((y_true - y_true.mean()) ** 2).sum()

    ax.text(0.05, 0.95, f'MAE: {mae:.2f}\nRMSE: {rmse:.2f}\nR²: {r2:.3f}',
            transform=ax.transAxes, fontsize=10, verticalalignment='top',
            bbox=dict(boxstyle='round', facecolor='white', alpha=0.8))

    ax.set_xlabel('Actual Quality')
    ax.set_ylabel('Predicted Quality')
    ax.set_title(f'{split_name}: Predicted vs Actual Quality', fontweight='bold')
    ax.legend(loc='lower right')

    path = figures.output_dir / f"scatter_{split_name.lower()}.{figures.format}"
    fig.savefig(path, dpi=figures.dpi, bbox_inches='tight', facecolor='white')
    plt.close(fig)

    # Error distribution
    fig, ax = plt.subplots(figsize=(10, 6))

    errors = y_pred - y_true
    ax.hist(errors, bins=50, color=figures.COLORS[0], edgecolor='white', alpha=0.8)
    ax.axvline(0, color='red', linestyle='--', lw=1.5, label='Zero error')
    ax.axvline(errors.mean(), color=figures.COLORS[1], linestyle='-', lw=1.5,
               label=f'Mean error: {errors.mean():.2f}')

    ax.set_xlabel('Prediction Error (Predicted - Actual)')
    ax.set_ylabel('Count')
    ax.set_title(f'{split_name}: Error Distribution', fontweight='bold')
    ax.legend()

    path = figures.output_dir / f"error_dist_{split_name.lower()}.{figures.format}"
    fig.savefig(path, dpi=figures.dpi, bbox_inches='tight', facecolor='white')
    plt.close(fig)


if __name__ == "__main__":
    sys.exit(main())
