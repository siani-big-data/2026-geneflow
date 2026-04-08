#!/usr/bin/env python3
"""
Generate plots for Quality Classifier results.

Usage:
    uv run python scripts/plot_quality_results.py
    uv run python scripts/plot_quality_results.py --checkpoint-dir checkpoints/quality_classifier
"""

import argparse
import json
from pathlib import Path

import matplotlib

matplotlib.use('Agg')
import matplotlib.pyplot as plt
import numpy as np
import torch
from torch.utils.data import DataLoader, Dataset

from src.ml.models.quality import QualityClassifierCNN, QualityClassifierCNNConfig

CLASS_NAMES = ["Q10", "Q20", "Q30", "Q40", "Q50+"]


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


def evaluate_model(model, loader, device, num_classes):
    """Evaluate model and collect all predictions."""
    model.eval()

    all_preds = []
    all_targets = []
    all_probs = []
    confusion = np.zeros((num_classes, num_classes), dtype=np.int64)

    with torch.no_grad():
        for batch in loader:
            features = batch["features"].to(device)
            classes = batch["classes"].to(device)
            mask = batch["mask"].to(device)

            logits = model(features)
            probs = torch.softmax(logits, dim=1)
            preds = logits.argmax(dim=1)

            for i in range(features.size(0)):
                m = mask[i] > 0
                all_preds.extend(preds[i][m].cpu().numpy())
                all_targets.extend(classes[i][m].cpu().numpy())
                all_probs.extend(probs[i, :, m].cpu().numpy().T)

                for true_c, pred_c in zip(classes[i][m].cpu().numpy(), preds[i][m].cpu().numpy()):
                    confusion[true_c, pred_c] += 1

    return {
        "predictions": np.array(all_preds),
        "targets": np.array(all_targets),
        "probabilities": np.array(all_probs),
        "confusion": confusion,
    }


def plot_confusion_matrix(confusion, class_names, save_path):
    """Plot confusion matrix."""
    fig, ax = plt.subplots(figsize=(10, 8))

    confusion_norm = confusion.astype(float) / (confusion.sum(axis=1, keepdims=True) + 1e-6)

    im = ax.imshow(confusion_norm, cmap="Blues", vmin=0, vmax=1)
    ax.set_xticks(range(len(class_names)))
    ax.set_yticks(range(len(class_names)))
    ax.set_xticklabels(class_names, fontsize=12)
    ax.set_yticklabels(class_names, fontsize=12)
    ax.set_xlabel("Predicted", fontsize=14)
    ax.set_ylabel("True", fontsize=14)
    ax.set_title("Confusion Matrix (Normalized)", fontsize=16)

    for i in range(len(class_names)):
        for j in range(len(class_names)):
            val = confusion_norm[i, j]
            color = "white" if val > 0.5 else "black"
            ax.text(j, i, f"{val:.2f}", ha="center", va="center", color=color, fontsize=14)

    plt.colorbar(im)
    plt.tight_layout()
    fig.savefig(save_path, dpi=150)
    plt.close()


def plot_per_class_metrics(confusion, class_names, save_path):
    """Plot precision, recall, F1 per class."""
    precision = np.diag(confusion) / (confusion.sum(axis=0) + 1e-6)
    recall = np.diag(confusion) / (confusion.sum(axis=1) + 1e-6)
    f1 = 2 * (precision * recall) / (precision + recall + 1e-6)

    fig, ax = plt.subplots(figsize=(12, 6))

    x = np.arange(len(class_names))
    width = 0.25

    bars1 = ax.bar(
        x - width, precision * 100, width, label='Precision', color='#3498db', alpha=0.8
    )
    bars2 = ax.bar(
        x, recall * 100, width, label='Recall', color='#2ecc71', alpha=0.8
    )
    bars3 = ax.bar(
        x + width, f1 * 100, width, label='F1', color='#9b59b6', alpha=0.8
    )

    ax.set_xlabel('Quality Class', fontsize=14)
    ax.set_ylabel('Score (%)', fontsize=14)
    ax.set_title('Per-Class Metrics', fontsize=16)
    ax.set_xticks(x)
    ax.set_xticklabels(class_names, fontsize=12)
    ax.legend(fontsize=12)
    ax.set_ylim(0, 100)
    ax.grid(True, alpha=0.3, axis='y')

    # Add value labels
    for bars in [bars1, bars2, bars3]:
        for bar in bars:
            height = bar.get_height()
            ax.annotate(f'{height:.1f}',
                       xy=(bar.get_x() + bar.get_width() / 2, height),
                       xytext=(0, 3), textcoords="offset points",
                       ha='center', va='bottom', fontsize=9)

    plt.tight_layout()
    fig.savefig(save_path, dpi=150)
    plt.close()

    return precision, recall, f1


def plot_prediction_distribution(targets, predictions, class_names, save_path):
    """Plot distribution of predictions vs ground truth."""
    fig, axes = plt.subplots(1, 2, figsize=(14, 5))

    # Ground truth distribution
    target_counts = np.bincount(targets, minlength=len(class_names))
    pred_counts = np.bincount(predictions, minlength=len(class_names))

    x = np.arange(len(class_names))
    width = 0.35

    axes[0].bar(
        x - width/2, target_counts, width, label='Ground Truth',
        color='#3498db', alpha=0.8
    )
    axes[0].bar(
        x + width/2, pred_counts, width, label='Predictions',
        color='#e74c3c', alpha=0.8
    )
    axes[0].set_xlabel('Quality Class', fontsize=12)
    axes[0].set_ylabel('Count', fontsize=12)
    axes[0].set_title('Class Distribution: Ground Truth vs Predictions', fontsize=14)
    axes[0].set_xticks(x)
    axes[0].set_xticklabels(class_names)
    axes[0].legend()
    axes[0].grid(True, alpha=0.3, axis='y')

    # Error distribution (where predictions differ from truth)
    errors = predictions != targets
    error_by_class = np.zeros(len(class_names))
    total_by_class = np.zeros(len(class_names))

    for true_c in range(len(class_names)):
        mask = targets == true_c
        total_by_class[true_c] = mask.sum()
        error_by_class[true_c] = errors[mask].sum()

    error_rate = error_by_class / (total_by_class + 1e-6) * 100

    colors = [
        '#2ecc71' if e < 30 else '#f39c12' if e < 50 else '#e74c3c'
        for e in error_rate
    ]
    axes[1].bar(x, error_rate, color=colors, alpha=0.8)
    axes[1].set_xlabel('True Quality Class', fontsize=12)
    axes[1].set_ylabel('Error Rate (%)', fontsize=12)
    axes[1].set_title('Error Rate by Class', fontsize=14)
    axes[1].set_xticks(x)
    axes[1].set_xticklabels(class_names)
    axes[1].set_ylim(0, 100)
    axes[1].grid(True, alpha=0.3, axis='y')

    for i, (rate, total) in enumerate(zip(error_rate, total_by_class)):
        axes[1].annotate(f'{rate:.1f}%\n({int(total_by_class[i]):,})',
                        xy=(i, rate), xytext=(0, 5),
                        textcoords="offset points", ha='center', va='bottom', fontsize=10)

    plt.tight_layout()
    fig.savefig(save_path, dpi=150)
    plt.close()


def plot_confidence_distribution(probabilities, targets, predictions, class_names, save_path):
    """Plot confidence distribution for correct vs incorrect predictions."""
    fig, axes = plt.subplots(1, 2, figsize=(14, 5))

    # Get max probability (confidence) for each prediction
    confidences = probabilities.max(axis=1)
    correct = predictions == targets

    # Histogram of confidence for correct vs incorrect
    bins = np.linspace(0, 1, 21)

    axes[0].hist(
        confidences[correct], bins=bins, alpha=0.7, label='Correct',
        color='#2ecc71', density=True
    )
    axes[0].hist(
        confidences[~correct], bins=bins, alpha=0.7, label='Incorrect',
        color='#e74c3c', density=True
    )
    axes[0].set_xlabel('Confidence (max probability)', fontsize=12)
    axes[0].set_ylabel('Density', fontsize=12)
    axes[0].set_title('Confidence Distribution', fontsize=14)
    axes[0].legend()
    axes[0].grid(True, alpha=0.3)

    # Accuracy vs confidence (calibration-like plot)
    confidence_bins = np.linspace(0, 1, 11)
    bin_accs = []
    bin_counts = []
    bin_centers = []

    for i in range(len(confidence_bins) - 1):
        mask = (confidences >= confidence_bins[i]) & (confidences < confidence_bins[i+1])
        if mask.sum() > 0:
            bin_accs.append(correct[mask].mean() * 100)
            bin_counts.append(mask.sum())
            bin_centers.append((confidence_bins[i] + confidence_bins[i+1]) / 2)

    axes[1].bar(bin_centers, bin_accs, width=0.08, alpha=0.8, color='#3498db')
    axes[1].plot([0, 1], [0, 100], 'k--', alpha=0.5, label='Perfect calibration')
    axes[1].set_xlabel('Confidence', fontsize=12)
    axes[1].set_ylabel('Accuracy (%)', fontsize=12)
    axes[1].set_title('Accuracy vs Confidence', fontsize=14)
    axes[1].set_xlim(0, 1)
    axes[1].set_ylim(0, 100)
    axes[1].legend()
    axes[1].grid(True, alpha=0.3)

    plt.tight_layout()
    fig.savefig(save_path, dpi=150)
    plt.close()


def plot_summary(config, precision, recall, f1, save_path):
    """Plot summary of model performance."""
    fig, ax = plt.subplots(figsize=(10, 6))

    # Create summary text
    summary_text = f"""
Quality Classifier CNN - Summary

Model Configuration:
  Hidden Channels: {config['args']['hidden_channels']}
  Num Layers: {config['args']['num_layers']}
  Kernel Size: {config['args']['kernel_size']}
  Dropout: {config['args']['dropout']}

Best Results (Epoch {config['best_epoch']}):
  Loss: {config['best_loss']:.4f}
  Accuracy: {config['best_accuracy']*100:.1f}%

Per-Class Performance:
"""

    for i, name in enumerate(CLASS_NAMES):
        prec = precision[i] * 100
        rec = recall[i] * 100
        f1_val = f1[i] * 100
        summary_text += f"  {name}: Prec={prec:.1f}% Rec={rec:.1f}% F1={f1_val:.1f}%\n"

    summary_text += "\nMacro Averages:\n"
    summary_text += f"  Precision: {precision.mean()*100:.1f}%\n"
    summary_text += f"  Recall: {recall.mean()*100:.1f}%\n"
    summary_text += f"  F1: {f1.mean()*100:.1f}%"

    ax.text(0.05, 0.95, summary_text, transform=ax.transAxes, fontsize=12,
            verticalalignment='top', fontfamily='monospace',
            bbox=dict(boxstyle='round', facecolor='white', alpha=0.8))
    ax.axis('off')

    plt.tight_layout()
    fig.savefig(save_path, dpi=150)
    plt.close()


def main():
    parser = argparse.ArgumentParser(description="Generate Quality Classifier plots")
    parser.add_argument("--checkpoint-dir", type=str, default="checkpoints/quality_classifier")
    parser.add_argument("--data-dir", type=str, default="datalake/datasets/quality_context")
    parser.add_argument("--batch-size", type=int, default=64)
    args = parser.parse_args()

    checkpoint_dir = Path(args.checkpoint_dir)
    data_dir = Path(args.data_dir)
    plots_dir = checkpoint_dir / "plots"
    plots_dir.mkdir(parents=True, exist_ok=True)

    print("=" * 60)
    print("QUALITY CLASSIFIER - GENERATING PLOTS")
    print("=" * 60)

    # Load config
    with open(checkpoint_dir / "config.json") as f:
        config = json.load(f)

    print(f"Checkpoint: {checkpoint_dir}")
    print(f"Best epoch: {config['best_epoch']}, Acc: {config['best_accuracy']*100:.1f}%")

    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    print(f"Device: {device}")

    # Load model
    model_config = QualityClassifierCNNConfig(
        input_channels=config["n_features"],
        hidden_channels=config["args"]["hidden_channels"],
        num_layers=config["args"]["num_layers"],
        num_classes=config["num_classes"],
        kernel_size=config["args"]["kernel_size"],
        dropout=config["args"]["dropout"],
    )
    model = QualityClassifierCNN(model_config).to(device)
    model.load(checkpoint_dir / "best.pt")
    print(f"Model loaded: {sum(p.numel() for p in model.parameters()):,} parameters")

    # Load validation data
    val_dataset = QualityClassDataset(data_dir / "val")
    val_loader = DataLoader(val_dataset, batch_size=args.batch_size, shuffle=False, num_workers=0)
    print(f"Validation samples: {len(val_dataset)}")

    # Evaluate
    print("\nEvaluating model...")
    results = evaluate_model(model, val_loader, device, config["num_classes"])

    # Generate plots
    print("\nGenerating plots...")

    print("  - Confusion matrix")
    plot_confusion_matrix(results["confusion"], CLASS_NAMES, plots_dir / "confusion_matrix.png")

    print("  - Per-class metrics")
    precision, recall, f1 = plot_per_class_metrics(
        results["confusion"], CLASS_NAMES, plots_dir / "per_class_metrics.png"
    )

    print("  - Prediction distribution")
    plot_prediction_distribution(
        results["targets"], results["predictions"], CLASS_NAMES,
        plots_dir / "prediction_distribution.png"
    )

    print("  - Confidence distribution")
    plot_confidence_distribution(
        results["probabilities"], results["targets"], results["predictions"],
        CLASS_NAMES, plots_dir / "confidence_distribution.png"
    )

    print("  - Summary")
    plot_summary(config, precision, recall, f1, plots_dir / "summary.png")

    print(f"\nPlots saved to: {plots_dir}")
    print("=" * 60)


if __name__ == "__main__":
    main()
