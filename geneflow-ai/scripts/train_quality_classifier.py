#!/usr/bin/env python3
"""
Train Quality Classifier with CNN.

Uses dilated CNN with spatial context for better classification.

Usage:
    uv run python scripts/train_quality_classifier.py
    uv run python scripts/train_quality_classifier.py --epochs 200 --kernel-size 7
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
from torch.utils.data import DataLoader, Dataset

from src.ml.models.quality import FocalLoss, QualityClassifierCNN, QualityClassifierCNNConfig

CLASS_NAMES = {
    5: ["Q10", "Q20", "Q30", "Q40", "Q50+"],
    4: ["Low", "Medium", "Good", "Excellent"],
    3: ["Bad", "OK", "Good"],
    2: ["Bad", "Good"],
}


class QualityClassDataset(Dataset):
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
            "classes": torch.tensor(data["classes"], dtype=torch.long),
            "mask": torch.tensor(data["mask"], dtype=torch.float32),
        }


def train_epoch(model, loader, optimizer, criterion, device):
    model.train()
    total_loss, total_correct, total_valid = 0, 0, 0

    for batch in loader:
        features = batch["features"].to(device)
        classes = batch["classes"].to(device)
        mask = batch["mask"].to(device)

        optimizer.zero_grad()
        logits = model(features)  # (batch, num_classes, seq_len)

        loss = criterion(logits, classes, mask)
        loss.backward()

        torch.nn.utils.clip_grad_norm_(model.parameters(), 1.0)
        optimizer.step()

        with torch.no_grad():
            preds = logits.argmax(dim=1)  # (batch, seq_len)
            correct = ((preds == classes).float() * mask).sum()
            total_correct += correct.item()
            total_valid += mask.sum().item()

        total_loss += loss.item() * features.size(0)

    n = len(loader.dataset)
    return {
        "loss": total_loss / n,
        "accuracy": total_correct / total_valid if total_valid > 0 else 0,
    }


def evaluate(model, loader, criterion, device, num_classes):
    model.eval()
    total_loss, total_correct, total_valid = 0, 0, 0

    # Per-class metrics
    class_correct = np.zeros(num_classes)
    class_total = np.zeros(num_classes)

    # Confusion matrix
    confusion = np.zeros((num_classes, num_classes), dtype=np.int64)

    with torch.no_grad():
        for batch in loader:
            features = batch["features"].to(device)
            classes = batch["classes"].to(device)
            mask = batch["mask"].to(device)

            logits = model(features)
            loss = criterion(logits, classes, mask)

            preds = logits.argmax(dim=1)
            correct = ((preds == classes).float() * mask).sum()

            total_correct += correct.item()
            total_valid += mask.sum().item()
            total_loss += loss.item() * features.size(0)

            # Per-class accuracy
            for c in range(num_classes):
                class_mask = (classes == c) & (mask > 0)
                class_total[c] += class_mask.sum().item()
                class_correct[c] += ((preds == c) & class_mask).sum().item()

            # Confusion matrix
            for i in range(features.size(0)):
                m = mask[i] > 0
                for true_c, pred_c in zip(classes[i][m].cpu().numpy(), preds[i][m].cpu().numpy()):
                    confusion[true_c, pred_c] += 1

    n = len(loader.dataset)
    class_acc = class_correct / (class_total + 1e-6)

    diag = np.diag(confusion)
    precision_per_class = diag / (confusion.sum(axis=0) + 1e-6)
    recall_per_class = diag / (confusion.sum(axis=1) + 1e-6)
    pr_sum = precision_per_class + recall_per_class + 1e-6
    f1_per_class = 2 * (precision_per_class * recall_per_class) / pr_sum

    # Macro averages
    macro_precision = precision_per_class.mean()
    macro_recall = recall_per_class.mean()
    macro_f1 = f1_per_class.mean()

    return {
        "loss": total_loss / n,
        "accuracy": total_correct / total_valid if total_valid > 0 else 0,
        "class_accuracy": class_acc,
        "class_total": class_total,
        "confusion": confusion,
        "precision": macro_precision,
        "recall": macro_recall,
        "f1": macro_f1,
        "precision_per_class": precision_per_class,
        "recall_per_class": recall_per_class,
        "f1_per_class": f1_per_class,
    }


def save_plots(history, confusion, class_names, save_dir):
    save_dir = Path(save_dir)
    save_dir.mkdir(parents=True, exist_ok=True)

    epochs = range(1, len(history["train_loss"]) + 1)

    # Training curves (Loss + F1/Precision/Recall) - similar to heterozygote
    fig, axes = plt.subplots(1, 2, figsize=(15, 5))

    # Loss
    axes[0].plot(epochs, history["train_loss"], label="Train", linewidth=2)
    axes[0].plot(epochs, history["val_loss"], label="Val", linewidth=2)
    axes[0].set_xlabel("Epoch")
    axes[0].set_ylabel("Loss")
    axes[0].legend()
    axes[0].grid(True, alpha=0.3)
    axes[0].set_title("Loss")

    train_f1 = [x*100 for x in history["train_f1"]]
    val_f1 = [x*100 for x in history["val_f1"]]
    val_prec = [x*100 for x in history["val_precision"]]
    val_rec = [x*100 for x in history["val_recall"]]
    axes[1].plot(epochs, train_f1, label="Train F1", linewidth=2)
    axes[1].plot(epochs, val_f1, label="Val F1", linewidth=2)
    axes[1].plot(epochs, val_prec, '--', label="Val Precision", linewidth=1.5)
    axes[1].plot(epochs, val_rec, '--', label="Val Recall", linewidth=1.5)
    axes[1].set_xlabel("Epoch")
    axes[1].set_ylabel("%")
    axes[1].legend()
    axes[1].grid(True, alpha=0.3)
    axes[1].set_title("Quality Classification (F1)")

    plt.tight_layout()
    fig.savefig(save_dir / "training_curves.png", dpi=150)
    plt.close()

    # Per-class metrics
    fig, axes = plt.subplots(1, 3, figsize=(15, 5))

    diag = np.diag(confusion)
    precision_per_class = diag / (confusion.sum(axis=0) + 1e-6)
    recall_per_class = diag / (confusion.sum(axis=1) + 1e-6)
    pr_sum = precision_per_class + recall_per_class + 1e-6
    f1_per_class = 2 * (precision_per_class * recall_per_class) / pr_sum

    x = np.arange(len(class_names))
    width = 0.6

    for ax, values, title, color in zip(
        axes,
        [precision_per_class * 100, recall_per_class * 100, f1_per_class * 100],
        ["Precision per Class", "Recall per Class", "F1 per Class"],
        ["#3498db", "#2ecc71", "#9b59b6"]
    ):
        bars = ax.bar(x, values, width, color=color, alpha=0.8)
        ax.set_xticks(x)
        ax.set_xticklabels(class_names, rotation=45, ha="right")
        ax.set_ylabel("%")
        ax.set_ylim(0, 100)
        ax.set_title(title)
        ax.grid(True, alpha=0.3, axis='y')

        # Add value labels
        for bar, val in zip(bars, values):
            ax.text(bar.get_x() + bar.get_width()/2, bar.get_height() + 1,
                   f'{val:.1f}', ha='center', va='bottom', fontsize=10)

    plt.tight_layout()
    fig.savefig(save_dir / "per_class_metrics.png", dpi=150)
    plt.close()

    # Confusion matrix
    fig, ax = plt.subplots(figsize=(8, 6))

    # Normalize by row (true class)
    row_sums = confusion.sum(axis=1, keepdims=True) + 1e-6
    confusion_norm = confusion.astype(float) / row_sums

    im = ax.imshow(confusion_norm, cmap="Blues", vmin=0, vmax=1)
    ax.set_xticks(range(len(class_names)))
    ax.set_yticks(range(len(class_names)))
    ax.set_xticklabels(class_names)
    ax.set_yticklabels(class_names)
    ax.set_xlabel("Predicted")
    ax.set_ylabel("True")
    ax.set_title("Confusion Matrix (normalized)")

    # Add text annotations
    for i in range(len(class_names)):
        for j in range(len(class_names)):
            val = confusion_norm[i, j]
            color = "white" if val > 0.5 else "black"
            ax.text(j, i, f"{val:.2f}", ha="center", va="center", color=color)

    plt.colorbar(im)
    plt.tight_layout()
    fig.savefig(save_dir / "confusion_matrix.png", dpi=150)
    plt.close()


def main():
    parser = argparse.ArgumentParser(description="Train Quality Classifier CNN")

    # Data
    parser.add_argument("--data-dir", type=str, default="datalake/datasets/quality_context")
    parser.add_argument("--checkpoint-dir", type=str, default="checkpoints/quality_classifier")
    parser.add_argument(
        "--select-features", type=str, default=None,
        help="Comma-separated list of features (e.g., 'ratio_norm,p2am_norm')"
    )

    # Architecture
    parser.add_argument("--num-classes", type=int, default=5)
    parser.add_argument("--hidden-channels", type=int, default=16)
    parser.add_argument("--num-layers", type=int, default=2)
    parser.add_argument("--kernel-size", type=int, default=3)
    parser.add_argument("--dropout", type=float, default=0.5)

    # Training
    parser.add_argument("--epochs", type=int, default=200)
    parser.add_argument("--batch-size", type=int, default=128)
    parser.add_argument("--lr", type=float, default=1e-4)
    parser.add_argument("--patience", type=int, default=30)
    parser.add_argument("--focal-gamma", type=float, default=2.0)

    args = parser.parse_args()

    print("=" * 70)
    print("QUALITY CLASSIFIER CNN TRAINING")
    print("=" * 70)
    print(f"Architecture: hidden={args.hidden_channels}, layers={args.num_layers}, "
          f"kernel={args.kernel_size}, dropout={args.dropout}")

    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    print(f"Device: {device}")

    # Data
    data_dir = Path(args.data_dir)

    # Load metadata
    with open(data_dir / "metadata.json") as f:
        metadata = json.load(f)

    num_classes = metadata["num_classes"]
    class_names = metadata["class_names"]
    all_features = metadata.get("featuREDACTED", metadata.get("features", []))

    print(f"Classes: {class_names}")
    print(f"Available features: {all_features}")

    # Feature selection
    featuREDACTED = None
    selected_features = all_features
    if args.select_features:
        selected_features = [f.strip() for f in args.select_features.split(",")]
        featuREDACTED = [
        all_features.index(f) for f in selected_features if f in all_features
    ]
        if len(featuREDACTED) != len(selected_features):
            missing = set(selected_features) - set(all_features)
            print(f"Warning: features not found: {missing}")
        selected_features = [all_features[i] for i in featuREDACTED]
        print(f"Selected features ({len(selected_features)}): {selected_features}")

    n_features = len(selected_features)

    train_dataset = QualityClassDataset(data_dir / "train", featuREDACTED)
    val_dataset = QualityClassDataset(data_dir / "val", featuREDACTED)
    print(f"Train: {len(train_dataset)} | Val: {len(val_dataset)}")

    # Load class weights
    class_weights = None
    weights_path = data_dir / "class_weights.npy"
    if weights_path.exists():
        class_weights = torch.tensor(np.load(weights_path), dtype=torch.float32)
        print(f"Class weights: {class_weights.numpy()}")

    train_loader = DataLoader(
        train_dataset, batch_size=args.batch_size, shuffle=True, num_workers=0
    )
    val_loader = DataLoader(
        val_dataset, batch_size=args.batch_size, shuffle=False, num_workers=0
    )

    # Model
    config = QualityClassifierCNNConfig(
        input_channels=n_features,
        hidden_channels=args.hidden_channels,
        num_layers=args.num_layers,
        num_classes=num_classes,
        kernel_size=args.kernel_size,
        dropout=args.dropout,
        use_dilations=True,
    )
    model = QualityClassifierCNN(config).to(device)
    print(f"Parameters: {sum(p.numel() for p in model.parameters()):,}")

    # Loss with class weights
    criterion = FocalLoss(alpha=class_weights, gamma=args.focal_gamma)

    # Optimizer
    optimizer = torch.optim.AdamW(model.parameters(), lr=args.lr, weight_decay=1e-4)
    scheduler = torch.optim.lr_scheduler.CosineAnnealingLR(optimizer, T_max=args.epochs)

    # Training
    checkpoint_dir = Path(args.checkpoint_dir)
    checkpoint_dir.mkdir(parents=True, exist_ok=True)

    history = {
        "train_loss": [], "train_acc": [], "train_f1": [],
        "val_loss": [], "val_acc": [], "val_f1": [],
        "val_precision": [], "val_recall": [],
    }
    best_loss, best_acc, best_epoch, patience_counter = float("inf"), 0, 0, 0

    print("\n" + "=" * 70)

    for epoch in range(args.epochs):
        train_m = train_epoch(model, train_loader, optimizer, criterion, device)
        val_m = evaluate(model, val_loader, criterion, device, num_classes)
        scheduler.step()

        history["train_loss"].append(train_m["loss"])
        history["train_acc"].append(train_m["accuracy"])
        history["train_f1"].append(train_m["accuracy"])  # Approximate with accuracy for train
        history["val_loss"].append(val_m["loss"])
        history["val_acc"].append(val_m["accuracy"])
        history["val_f1"].append(val_m["f1"])
        history["val_precision"].append(val_m["precision"])
        history["val_recall"].append(val_m["recall"])

        # Per-class accuracy string
        class_acc_str = " | ".join([
            f"{name}: {acc:.0%}"
            for name, acc in zip(CLASS_NAMES[num_classes], val_m["class_accuracy"])
        ])

        t_loss, t_acc = train_m['loss'], train_m['accuracy']
        v_loss, v_acc, v_f1 = val_m['loss'], val_m['accuracy'], val_m['f1']
        print(
            f"Epoch {epoch+1:3d}/{args.epochs} | "
            f"Train: Loss={t_loss:.4f} Acc={t_acc:.1%} | "
            f"Val: Loss={v_loss:.4f} Acc={v_acc:.1%} F1={v_f1:.1%}"
        )
        print(f"         Per-class: {class_acc_str}")

        # Use val_loss as criterion (lower is better)
        if val_m["loss"] < best_loss:
            best_loss = val_m["loss"]
            best_acc, best_epoch = val_m["accuracy"], epoch + 1
            val_m["confusion"]
            model.save(checkpoint_dir / "best.pt")
            patience_counter = 0
            print(f"  -> New best (loss={best_loss:.4f}, acc={best_acc:.1%})")
        else:
            patience_counter += 1

        if patience_counter >= args.patience:
            print(f"\nEarly stopping at epoch {epoch+1}")
            break

    # Final plots
    model.load(checkpoint_dir / "best.pt")
    final = evaluate(model, val_loader, criterion, device, num_classes)

    # Save plots
    plots_dir = checkpoint_dir / "plots"
    save_plots(history, final["confusion"], CLASS_NAMES[num_classes], plots_dir)

    # Save config
    with open(checkpoint_dir / "config.json", "w") as f:
        json.dump({
            "args": vars(args),
            "num_classes": num_classes,
            "class_names": class_names,
            "n_features": n_features,
            "selected_features": selected_features,
            "best_epoch": best_epoch,
            "best_loss": best_loss,
            "best_accuracy": best_acc,
            "best_f1": final["f1"],
            "best_precision": final["precision"],
            "best_recall": final["recall"],
            "class_accuracy": final["class_accuracy"].tolist(),
            "class_f1": final["f1_per_class"].tolist(),
            "class_precision": final["precision_per_class"].tolist(),
            "class_recall": final["recall_per_class"].tolist(),
            "timestamp": datetime.now().isoformat(),
        }, f, indent=2)

    print("\n" + "=" * 70)
    final_f1 = final['f1']
    print(
        f"COMPLETE - Best: epoch {best_epoch}, "
        f"Loss={best_loss:.4f}, Acc={best_acc:.1%}, F1={final_f1:.1%}"
    )
    print("\nPer-class metrics:")
    print(f"  {'Class':<12} {'Acc':>8} {'Prec':>8} {'Recall':>8} {'F1':>8} {'Samples':>12}")
    print(f"  {'-'*60}")
    for name, acc, prec, rec, f1, total in zip(
        CLASS_NAMES[num_classes],
        final["class_accuracy"],
        final["precision_per_class"],
        final["recall_per_class"],
        final["f1_per_class"],
        final["class_total"]
    ):
        print(f"  {name:<12} {acc:>7.1%} {prec:>7.1%} {rec:>7.1%} {f1:>7.1%} {int(total):>12,}")
    print(f"\nSaved to: {checkpoint_dir}")


if __name__ == "__main__":
    main()
