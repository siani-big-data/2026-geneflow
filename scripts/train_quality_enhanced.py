#!/usr/bin/env python3
"""
Train Quality Predictor - Pointwise MLP.

Architecture: MLP without spatial context
- Each position predicted independently from its 7 features
- Generalizes well (avoids memorizing file-specific patterns)
- Val > Train indicates good generalization

Usage:
    uv run python scripts/train_quality_enhanced.py
    uv run python scripts/train_quality_enhanced.py --data-dir datalake/datasets/quality_enhanced_v2 --epochs 300
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

from src.ml.models.quality import QualityPredictorPointwise, QualityPredictorConfig


# =============================================================================
# Dataset
# =============================================================================

class QualityDataset(Dataset):
    def __init__(self, data_dir: Path, featuREDACTED: list[int] | None = None):
        self.samples = sorted(Path(data_dir).glob("sample_*.npz"))
        if not self.samples:
            raise ValueError(f"No samples found in {data_dir}")
        self.featuREDACTED = featuREDACTED

    def __len__(self):
        return len(self.samples)

    def __getitem__(self, idx):
        data = np.load(self.samples[idx])
        features = data["features"]
        if self.featuREDACTED is not None:
            features = features[self.featuREDACTED, :]
        return {
            "features": torch.tensor(features, dtype=torch.float32),
            "quality": torch.tensor(data["quality"], dtype=torch.float32),
            "mask": torch.tensor(data["mask"], dtype=torch.float32),
        }


# =============================================================================
# Training
# =============================================================================

def train_epoch(model, loader, optimizer, criterion, device, tolerance):
    model.train()
    total_loss, total_mae, total_correct, total_valid, n = 0, 0, 0, 0, 0

    for batch in loader:
        features = batch["features"].to(device)
        quality = batch["quality"].to(device)
        mask = batch["mask"].to(device)

        optimizer.zero_grad()
        output = model(features)

        loss = criterion(output * mask, quality * mask)
        if mask.sum() > 0:
            loss = loss * (mask.numel() / mask.sum())

        loss.backward()
        torch.nn.utils.clip_grad_norm_(model.parameters(), 1.0)
        optimizer.step()

        with torch.no_grad():
            errors = (output - quality).abs() * mask
            mae = errors.sum() / mask.sum()
            correct = ((errors < tolerance) * mask).sum()

        total_loss += loss.item() * features.size(0)
        total_mae += mae.item() * features.size(0)
        total_correct += correct.item()
        total_valid += mask.sum().item()
        n += features.size(0)

    return {"loss": total_loss / n, "mae": total_mae / n,
            "accuracy": total_correct / total_valid if total_valid > 0 else 0}


def evaluate(model, loader, criterion, device, tolerance):
    model.eval()
    total_loss, total_mae, total_correct, total_valid, n = 0, 0, 0, 0, 0
    all_preds, all_targets = [], []

    with torch.no_grad():
        for batch in loader:
            features = batch["features"].to(device)
            quality = batch["quality"].to(device)
            mask = batch["mask"].to(device)

            output = model(features)
            loss = criterion(output * mask, quality * mask)
            if mask.sum() > 0:
                loss = loss * (mask.numel() / mask.sum())

            errors = (output - quality).abs() * mask
            mae = errors.sum() / mask.sum()
            correct = ((errors < tolerance) * mask).sum()

            total_loss += loss.item() * features.size(0)
            total_mae += mae.item() * features.size(0)
            total_correct += correct.item()
            total_valid += mask.sum().item()
            n += features.size(0)

            for i in range(features.size(0)):
                m = mask[i].bool()
                all_preds.extend(output[i][m].cpu().numpy())
                all_targets.extend(quality[i][m].cpu().numpy())

    return {"loss": total_loss / n, "mae": total_mae / n,
            "accuracy": total_correct / total_valid if total_valid > 0 else 0,
            "predictions": np.array(all_preds), "targets": np.array(all_targets)}


# =============================================================================
# Visualization
# =============================================================================

def save_plots(history, val_preds, val_targets, save_dir):
    save_dir = Path(save_dir)
    save_dir.mkdir(parents=True, exist_ok=True)

    fig, axes = plt.subplots(1, 3, figsize=(15, 5))
    epochs = range(1, len(history["train_loss"]) + 1)

    for ax, (t, v), ylabel in zip(axes,
        [("train_loss", "val_loss"), ("train_mae", "val_mae"), ("train_acc", "val_acc")],
        ["Loss", "MAE", "Accuracy (%)"]):
        t_vals, v_vals = history[t], history[v]
        if "acc" in t:
            t_vals, v_vals = [x*100 for x in t_vals], [x*100 for x in v_vals]
        ax.plot(epochs, t_vals, label="Train")
        ax.plot(epochs, v_vals, label="Val")
        ax.set_xlabel("Epoch"); ax.set_ylabel(ylabel); ax.legend(); ax.grid(True, alpha=0.3)

    plt.tight_layout()
    fig.savefig(save_dir / "training_curves.png", dpi=150)
    plt.close()

    fig, ax = plt.subplots(figsize=(8, 8))
    idx = np.random.choice(len(val_preds), min(10000, len(val_preds)), replace=False)
    ax.scatter(val_targets[idx], val_preds[idx], alpha=0.3, s=5)
    lims = [min(val_targets.min(), val_preds.min()), max(val_targets.max(), val_preds.max())]
    ax.plot(lims, lims, 'k--')
    mae = np.abs(val_targets - val_preds).mean()
    r2 = 1 - ((val_targets - val_preds)**2).sum() / ((val_targets - val_targets.mean())**2).sum()
    ax.text(0.05, 0.95, f'MAE: {mae:.2f}\nR²: {r2:.3f}', transform=ax.transAxes, fontsize=12,
            verticalalignment='top', bbox=dict(facecolor='white'))
    ax.set_xlabel('Actual'); ax.set_ylabel('Predicted')
    fig.savefig(save_dir / "scatter.png", dpi=150)
    plt.close()


# =============================================================================
# Main
# =============================================================================

def main():
    parser = argparse.ArgumentParser(description="Train Quality Predictor - Local CNN")

    # Data
    parser.add_argument("--data-dir", type=str, default="datalake/datasets/quality_context")
    parser.add_argument("--checkpoint-dir", type=str, default="checkpoints/quality_mlp")
    parser.add_argument("--select-features", type=str, default=None,
                        help="Comma-separated list of features to use (e.g., 'ratio_norm,p2am_norm,p1am_norm')")

    # Architecture
    parser.add_argument("--hidden-channels", type=int, default=128)
    parser.add_argument("--num-layers", type=int, default=3)
    parser.add_argument("--dropout", type=float, default=0.3)

    # Training
    parser.add_argument("--epochs", type=int, default=300)
    parser.add_argument("--batch-size", type=int, default=128)
    parser.add_argument("--lr", type=float, default=1e-3)
    parser.add_argument("--patience", type=int, default=50)
    parser.add_argument("--tolerance", type=float, default=5.0)

    args = parser.parse_args()

    print("=" * 70)
    print("QUALITY PREDICTOR - POINTWISE MLP")
    print("=" * 70)
    print(f"Architecture: hidden={args.hidden_channels}, layers={args.num_layers}, dropout={args.dropout}")

    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    print(f"Device: {device}")

    # Data
    data_dir = Path(args.data_dir)

    # Load metadata to get feature names
    with open(data_dir / "metadata.json") as f:
        metadata = json.load(f)

    all_features = metadata.get("featuREDACTED", metadata.get("features", []))
    print(f"Available features: {all_features}")

    # Feature selection
    featuREDACTED = None
    selected_features = all_features
    if args.select_features:
        selected_features = [f.strip() for f in args.select_features.split(",")]
        featuREDACTED = [all_features.index(f) for f in selected_features if f in all_features]
        if len(featuREDACTED) != len(selected_features):
            missing = set(selected_features) - set(all_features)
            print(f"Warning: features not found: {missing}")
        selected_features = [all_features[i] for i in featuREDACTED]
        print(f"Selected features ({len(selected_features)}): {selected_features}")

    n_features = len(selected_features)

    train_dataset = QualityDataset(data_dir / "train", featuREDACTED)
    val_dataset = QualityDataset(data_dir / "val", featuREDACTED)
    print(f"Train: {len(train_dataset)} | Val: {len(val_dataset)}")

    train_loader = DataLoader(train_dataset, batch_size=args.batch_size, shuffle=True, num_workers=0)
    val_loader = DataLoader(val_dataset, batch_size=args.batch_size, shuffle=False, num_workers=0)

    # Model (Pointwise MLP - no spatial context, generalizes better)
    config = QualityPredictorConfig(
        input_channels=n_features,
        hidden_channels=args.hidden_channels,
        num_layers=args.num_layers,
        dropout=args.dropout,
    )
    model = QualityPredictorPointwise(config).to(device)
    print(f"Parameters: {sum(p.numel() for p in model.parameters()):,}")

    # Training
    criterion = nn.MSELoss()
    optimizer = torch.optim.AdamW(model.parameters(), lr=args.lr, weight_decay=1e-4)
    scheduler = torch.optim.lr_scheduler.CosineAnnealingLR(optimizer, T_max=args.epochs)

    checkpoint_dir = Path(args.checkpoint_dir)
    checkpoint_dir.mkdir(parents=True, exist_ok=True)

    history = {"train_loss": [], "train_mae": [], "train_acc": [],
               "val_loss": [], "val_mae": [], "val_acc": []}
    best_loss, best_epoch, patience_counter = float("inf"), 0, 0

    print(f"\nAccuracy = % within ±{args.tolerance} Phred")
    print("=" * 70)

    for epoch in range(args.epochs):
        train_m = train_epoch(model, train_loader, optimizer, criterion, device, args.tolerance)
        val_m = evaluate(model, val_loader, criterion, device, args.tolerance)
        scheduler.step()

        history["train_loss"].append(train_m["loss"])
        history["train_mae"].append(train_m["mae"])
        history["train_acc"].append(train_m["accuracy"])
        history["val_loss"].append(val_m["loss"])
        history["val_mae"].append(val_m["mae"])
        history["val_acc"].append(val_m["accuracy"])

        print(f"Epoch {epoch+1:3d}/{args.epochs} | "
              f"Train: Loss={train_m['loss']:.4f} MAE={train_m['mae']:.2f} Acc={train_m['accuracy']:.1%} | "
              f"Val: Loss={val_m['loss']:.4f} MAE={val_m['mae']:.2f} Acc={val_m['accuracy']:.1%}")

        if val_m["loss"] < best_loss:
            best_loss, best_epoch = val_m["loss"], epoch + 1
            model.save(checkpoint_dir / "best.pt")
            patience_counter = 0
            print(f"  -> New best (acc: {val_m['accuracy']:.2%})")
        else:
            patience_counter += 1

        if patience_counter >= args.patience:
            print(f"\nEarly stopping at epoch {epoch+1}")
            break

    # Save & plot
    model.load(checkpoint_dir / "best.pt")
    final = evaluate(model, val_loader, criterion, device, args.tolerance)
    save_plots(history, final["predictions"], final["targets"], checkpoint_dir / "plots")

    with open(checkpoint_dir / "config.json", "w") as f:
        json.dump({
            "args": vars(args),
            "best_epoch": best_epoch,
            "best_loss": best_loss,
            "accuracy": final["accuracy"],
            "n_features": n_features,
            "selected_features": selected_features,
            "timestamp": datetime.now().isoformat(),
        }, f, indent=2)

    print("\n" + "=" * 70)
    print(f"COMPLETE - Best: epoch {best_epoch}, Acc={final['accuracy']:.1%}")
    print(f"Saved to: {checkpoint_dir}")


if __name__ == "__main__":
    main()
