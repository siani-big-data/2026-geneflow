#!/usr/bin/env python3
"""
Comprehensive plots of the Heterozygote Classifier.

Generates detailed plots and metrics for model analysis:
- Training curves (loss, F1, precision, recall)
- Precision-Recall trade-off
- Confusion matrix
- Per-position error analysis
- Feature importance visualization
- Threshold optimization

Usage:
    uv run python scripts/evaluate_heterozygote.py
    uv run python scripts/evaluate_heterozygote.py --checkpoint checkpoints/heterozygote
"""

import argparse
import json
from pathlib import Path

import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import numpy as np
import torch
from torch.utils.data import DataLoader

from src.ml.models.heterozygote import HeterozygoteClassifier


# =============================================================================
# Dataset (same as training)
# =============================================================================

class HeterozygoteEnhancedDataset:
    """Dataset for heterozygote_training classification with enhanced features."""

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

        features = data["features"].astype(np.float32)
        labels = data["labels"].astype(np.int64)

        seq_len = features.shape[1]

        if seq_len > self.max_length:
            start = (seq_len - self.max_length) // 2
            features = features[:, start:start + self.max_length]
            labels = labels[start:start + self.max_length]
        elif seq_len < self.max_length:
            pad = self.max_length - seq_len
            features = np.pad(features, ((0, 0), (0, pad)), constant_values=0)
            labels = np.pad(labels, (0, pad), constant_values=-1)

        return {
            "features": torch.tensor(features, dtype=torch.float32),
            "labels": torch.tensor(labels, dtype=torch.long),
        }


# =============================================================================
# Plot Functions
# =============================================================================

def plot_training_curves(history: dict, save_dir: Path):
    """Plot comprehensive training curves."""
    epochs = range(1, len(history["train_loss"]) + 1)

    fig, axes = plt.subplots(2, 2, figsize=(14, 10))

    # 1. Loss curves
    ax = axes[0, 0]
    ax.plot(epochs, history["train_loss"], label="Train", linewidth=2, color="#2196F3")
    ax.plot(epochs, history["val_loss"], label="Validation", linewidth=2, color="#FF5722")
    ax.set_xlabel("Epoch")
    ax.set_ylabel("Loss")
    ax.set_title("Loss Curves", fontweight="bold")
    ax.legend()
    ax.grid(True, alpha=0.3)
    ax.set_yscale("log")

    # 2. F1 Score
    ax = axes[0, 1]
    ax.plot(epochs, [x * 100 for x in history["train_f1"]], label="Train", linewidth=2, color="#2196F3")
    ax.plot(epochs, [x * 100 for x in history["val_f1"]], label="Validation", linewidth=2, color="#4CAF50")
    ax.set_xlabel("Epoch")
    ax.set_ylabel("F1 Score (%)")
    ax.set_title("F1 Score", fontweight="bold")
    ax.legend()
    ax.grid(True, alpha=0.3)
    ax.set_ylim(0, 100)

    # 3. Precision & Recall
    ax = axes[1, 0]
    ax.plot(epochs, [x * 100 for x in history["val_precision"]], label="Precision", linewidth=2, color="#9C27B0")
    ax.plot(epochs, [x * 100 for x in history["val_recall"]], label="Recall", linewidth=2, color="#FF9800")
    ax.set_xlabel("Epoch")
    ax.set_ylabel("Metric (%)")
    ax.set_title("Validation Precision & Recall", fontweight="bold")
    ax.legend()
    ax.grid(True, alpha=0.3)
    ax.set_ylim(0, 105)

    # 4. Accuracy
    ax = axes[1, 1]
    ax.plot(epochs, [x * 100 for x in history["train_acc"]], label="Train", linewidth=2, color="#2196F3")
    ax.plot(epochs, [x * 100 for x in history["val_acc"]], label="Validation", linewidth=2, color="#4CAF50")
    ax.set_xlabel("Epoch")
    ax.set_ylabel("Accuracy (%)")
    ax.set_title("Accuracy", fontweight="bold")
    ax.legend()
    ax.grid(True, alpha=0.3)
    ax.set_ylim(90, 100)

    plt.tight_layout()
    fig.savefig(save_dir / "training_curves.png", dpi=150, bbox_inches="tight")
    plt.close(fig)
    print(f"  Saved: training_curves.png")


def plot_precision_recall_tradeoff(history: dict, save_dir: Path):
    """Plot precision vs recall over training."""
    fig, ax = plt.subplots(figsize=(10, 8))

    precision = [x * 100 for x in history["val_precision"]]
    recall = [x * 100 for x in history["val_recall"]]
    f1 = [x * 100 for x in history["val_f1"]]

    # Color by epoch
    colors = np.linspace(0, 1, len(precision))
    scatter = ax.scatter(recall, precision, c=colors, cmap="viridis", s=30, alpha=0.7)

    # Mark best F1 point
    best_idx = np.argmax(f1)
    ax.scatter([recall[best_idx]], [precision[best_idx]],
               color="red", s=200, marker="*", zorder=5, label=f"Best F1: {f1[best_idx]:.1f}%")

    # Add iso-F1 curves
    for f1_val in [0.6, 0.7, 0.8, 0.9]:
        r = np.linspace(0.01, 1, 100)
        p = f1_val * r / (2 * r - f1_val)
        valid = (p > 0) & (p <= 1)
        ax.plot(r[valid] * 100, p[valid] * 100, '--', color='gray', alpha=0.3)
        # Label
        idx = np.argmin(np.abs(r - 0.95))
        if valid[idx]:
            ax.text(r[idx] * 100, p[idx] * 100, f"F1={f1_val:.1f}", fontsize=8, color='gray')

    cbar = plt.colorbar(scatter, ax=ax)
    cbar.set_label("Epoch (normalized)")

    ax.set_xlabel("Recall (%)", fontsize=12)
    ax.set_ylabel("Precision (%)", fontsize=12)
    ax.set_title("Precision-Recall Trade-off During Training", fontsize=14, fontweight="bold")
    ax.set_xlim(0, 105)
    ax.set_ylim(0, 105)
    ax.legend(loc="lower left")
    ax.grid(True, alpha=0.3)

    fig.savefig(save_dir / "precision_recall_tradeoff.png", dpi=150, bbox_inches="tight")
    plt.close(fig)
    print(f"  Saved: precision_recall_tradeoff.png")


def plot_loss_vs_f1(history: dict, save_dir: Path):
    """Plot the relationship between loss and F1."""
    fig, axes = plt.subplots(1, 2, figsize=(14, 5))

    # Train
    ax = axes[0]
    ax.scatter(history["train_loss"], [x * 100 for x in history["train_f1"]],
               alpha=0.5, c=range(len(history["train_loss"])), cmap="viridis")
    ax.set_xlabel("Train Loss")
    ax.set_ylabel("Train F1 (%)")
    ax.set_title("Train: Loss vs F1", fontweight="bold")
    ax.grid(True, alpha=0.3)

    # Validation
    ax = axes[1]
    scatter = ax.scatter(history["val_loss"], [x * 100 for x in history["val_f1"]],
                         alpha=0.5, c=range(len(history["val_loss"])), cmap="viridis")
    ax.set_xlabel("Validation Loss")
    ax.set_ylabel("Validation F1 (%)")
    ax.set_title("Validation: Loss vs F1", fontweight="bold")
    ax.grid(True, alpha=0.3)

    cbar = plt.colorbar(scatter, ax=ax)
    cbar.set_label("Epoch")

    plt.tight_layout()
    fig.savefig(save_dir / "loss_vs_f1.png", dpi=150, bbox_inches="tight")
    plt.close(fig)
    print(f"  Saved: loss_vs_f1.png")


def plot_overfitting_analysis(history: dict, save_dir: Path):
    """Analyze overfitting by comparing train vs val metrics."""
    fig, axes = plt.subplots(1, 3, figsize=(15, 5))
    epochs = range(1, len(history["train_loss"]) + 1)

    # 1. Loss gap
    ax = axes[0]
    train_loss = np.array(history["train_loss"])
    val_loss = np.array(history["val_loss"])
    gap = val_loss - train_loss

    ax.fill_between(epochs, 0, gap, where=(gap > 0), alpha=0.3, color="red", label="Overfitting (val > train)")
    ax.fill_between(epochs, 0, gap, where=(gap <= 0), alpha=0.3, color="green", label="Underfitting (val <= train)")
    ax.plot(epochs, gap, color="black", linewidth=1)
    ax.axhline(y=0, color="black", linestyle="--", linewidth=0.5)
    ax.set_xlabel("Epoch")
    ax.set_ylabel("Loss Gap (Val - Train)")
    ax.set_title("Loss Gap Analysis", fontweight="bold")
    ax.legend(fontsize=8)
    ax.grid(True, alpha=0.3)

    # 2. F1 gap
    ax = axes[1]
    train_f1 = np.array(history["train_f1"]) * 100
    val_f1 = np.array(history["val_f1"]) * 100
    gap_f1 = train_f1 - val_f1

    ax.fill_between(epochs, 0, gap_f1, where=(gap_f1 > 0), alpha=0.3, color="red", label="Train > Val")
    ax.fill_between(epochs, 0, gap_f1, where=(gap_f1 <= 0), alpha=0.3, color="green", label="Val >= Train")
    ax.plot(epochs, gap_f1, color="black", linewidth=1)
    ax.axhline(y=0, color="black", linestyle="--", linewidth=0.5)
    ax.set_xlabel("Epoch")
    ax.set_ylabel("F1 Gap (Train - Val) %")
    ax.set_title("F1 Gap Analysis", fontweight="bold")
    ax.legend(fontsize=8)
    ax.grid(True, alpha=0.3)

    # 3. Generalization ratio
    ax = axes[2]
    # Avoid division by zero
    gen_ratio = np.where(train_f1 > 0, val_f1 / train_f1, 1.0)
    ax.plot(epochs, gen_ratio, color="#2196F3", linewidth=2)
    ax.axhline(y=1.0, color="green", linestyle="--", linewidth=1, label="Perfect generalization")
    ax.axhline(y=0.9, color="orange", linestyle="--", linewidth=1, label="10% gap")
    ax.set_xlabel("Epoch")
    ax.set_ylabel("Val F1 / Train F1")
    ax.set_title("Generalization Ratio", fontweight="bold")
    ax.legend(fontsize=8)
    ax.set_ylim(0.5, 1.2)
    ax.grid(True, alpha=0.3)

    plt.tight_layout()
    fig.savefig(save_dir / "overfitting_analysis.png", dpi=150, bbox_inches="tight")
    plt.close(fig)
    print(f"  Saved: overfitting_analysis.png")


def plot_learning_dynamics(history: dict, save_dir: Path):
    """Plot learning rate and gradient-related dynamics."""
    fig, axes = plt.subplots(2, 2, figsize=(14, 10))
    epochs = np.array(range(1, len(history["train_loss"]) + 1))

    # 1. Loss improvement rate
    ax = axes[0, 0]
    train_loss = np.array(history["train_loss"])
    loss_improvement = -np.diff(train_loss)  # Negative because loss should decrease
    ax.bar(epochs[1:], loss_improvement, color="#4CAF50", alpha=0.7, width=0.8)
    ax.axhline(y=0, color="black", linestyle="-", linewidth=0.5)
    ax.set_xlabel("Epoch")
    ax.set_ylabel("Loss Improvement")
    ax.set_title("Per-Epoch Loss Improvement", fontweight="bold")
    ax.grid(True, alpha=0.3)

    # 2. Cumulative improvement
    ax = axes[0, 1]
    cumulative_improvement = np.cumsum(loss_improvement)
    ax.plot(epochs[1:], cumulative_improvement, color="#2196F3", linewidth=2)
    ax.fill_between(epochs[1:], 0, cumulative_improvement, alpha=0.3)
    ax.set_xlabel("Epoch")
    ax.set_ylabel("Cumulative Loss Reduction")
    ax.set_title("Cumulative Learning Progress", fontweight="bold")
    ax.grid(True, alpha=0.3)

    # 3. F1 improvement rate
    ax = axes[1, 0]
    val_f1 = np.array(history["val_f1"]) * 100
    f1_improvement = np.diff(val_f1)
    colors = ["#4CAF50" if x > 0 else "#F44336" for x in f1_improvement]
    ax.bar(epochs[1:], f1_improvement, color=colors, alpha=0.7, width=0.8)
    ax.axhline(y=0, color="black", linestyle="-", linewidth=0.5)
    ax.set_xlabel("Epoch")
    ax.set_ylabel("F1 Change (%)")
    ax.set_title("Per-Epoch Val F1 Change", fontweight="bold")
    ax.grid(True, alpha=0.3)

    # 4. Rolling average F1
    ax = axes[1, 1]
    window = 10
    rolling_f1 = np.convolve(val_f1, np.ones(window)/window, mode='valid')
    rolling_epochs = epochs[window-1:]
    ax.plot(epochs, val_f1, alpha=0.3, color="#4CAF50", label="Raw")
    ax.plot(rolling_epochs, rolling_f1, color="#4CAF50", linewidth=2, label=f"Rolling avg (w={window})")
    ax.set_xlabel("Epoch")
    ax.set_ylabel("Val F1 (%)")
    ax.set_title("Validation F1 (Smoothed)", fontweight="bold")
    ax.legend()
    ax.grid(True, alpha=0.3)

    plt.tight_layout()
    fig.savefig(save_dir / "learning_dynamics.png", dpi=150, bbox_inches="tight")
    plt.close(fig)
    print(f"  Saved: learning_dynamics.png")


def evaluate_model_on_dataset(model, dataloader, device):
    """Run model on dataset and collect predictions."""
    model.eval()
    all_preds = []
    all_labels = []
    all_probs = []

    with torch.no_grad():
        for batch in dataloader:
            features = batch["features"].to(device)
            labels = batch["labels"]

            logits = model(features)
            probs = torch.softmax(logits, dim=-1)
            preds = logits.argmax(dim=-1)

            # Only keep valid positions (not padded)
            for i in range(len(labels)):
                mask = labels[i] >= 0
                all_preds.extend(preds[i][mask].cpu().numpy())
                all_labels.extend(labels[i][mask].cpu().numpy())
                all_probs.extend(probs[i][mask, 1].cpu().numpy())

    return np.array(all_preds), np.array(all_labels), np.array(all_probs)


def plot_confusion_matrix(preds, labels, save_dir: Path):
    """Plot confusion matrix."""
    from sklearn.metrics import confusion_matrix

    cm = confusion_matrix(labels, preds)

    fig, ax = plt.subplots(figsize=(8, 7))

    # Normalize by row (true labels)
    cm_norm = cm.astype('float') / cm.sum(axis=1, keepdims=True)

    im = ax.imshow(cm_norm, cmap="Blues")

    # Add text annotations
    for i in range(2):
        for j in range(2):
            text = f"{cm[i, j]:,}\n({cm_norm[i, j]:.1%})"
            color = "white" if cm_norm[i, j] > 0.5 else "black"
            ax.text(j, i, text, ha="center", va="center", color=color, fontsize=14)

    ax.set_xticks([0, 1])
    ax.set_yticks([0, 1])
    ax.set_xticklabels(["Homozygous", "Heterozygous"])
    ax.set_yticklabels(["Homozygous", "Heterozygous"])
    ax.set_xlabel("Predicted", fontsize=12)
    ax.set_ylabel("True", fontsize=12)
    ax.set_title("Confusion Matrix", fontsize=14, fontweight="bold")

    plt.colorbar(im, ax=ax, label="Proportion")

    plt.tight_layout()
    fig.savefig(save_dir / "confusion_matrix.png", dpi=150, bbox_inches="tight")
    plt.close(fig)
    print(f"  Saved: confusion_matrix.png")


def plot_probability_distribution(probs, labels, save_dir: Path):
    """Plot probability distribution for each class."""
    fig, axes = plt.subplots(1, 2, figsize=(14, 5))

    # Separate by true class
    het_probs = probs[labels == 1]
    hom_probs = probs[labels == 0]

    # 1. Histogram
    ax = axes[0]
    ax.hist(hom_probs, bins=50, alpha=0.7, label=f"Homozygous (n={len(hom_probs):,})",
            color="#2196F3", density=True)
    ax.hist(het_probs, bins=50, alpha=0.7, label=f"Heterozygous (n={len(het_probs):,})",
            color="#FF5722", density=True)
    ax.axvline(x=0.5, color="black", linestyle="--", linewidth=1, label="Decision boundary")
    ax.set_xlabel("P(Heterozygous)")
    ax.set_ylabel("Density")
    ax.set_title("Prediction Probability Distribution", fontweight="bold")
    ax.legend()
    ax.grid(True, alpha=0.3)

    # 2. Box plot
    ax = axes[1]
    bp = ax.boxplot([hom_probs, het_probs], labels=["Homozygous", "Heterozygous"],
                     patch_artist=True)
    bp["boxes"][0].set_facecolor("#2196F3")
    bp["boxes"][1].set_facecolor("#FF5722")
    ax.axhline(y=0.5, color="black", linestyle="--", linewidth=1)
    ax.set_ylabel("P(Heterozygous)")
    ax.set_title("Probability by True Class", fontweight="bold")
    ax.grid(True, alpha=0.3)

    plt.tight_layout()
    fig.savefig(save_dir / "probability_distribution.png", dpi=150, bbox_inches="tight")
    plt.close(fig)
    print(f"  Saved: probability_distribution.png")


def plot_threshold_analysis(probs, labels, save_dir: Path):
    """Analyze performance at different thresholds."""
    thresholds = np.linspace(0.01, 0.99, 99)
    precisions = []
    recalls = []
    f1s = []

    for thresh in thresholds:
        preds = (probs >= thresh).astype(int)
        tp = ((preds == 1) & (labels == 1)).sum()
        fp = ((preds == 1) & (labels == 0)).sum()
        fn = ((preds == 0) & (labels == 1)).sum()

        precision = tp / (tp + fp) if (tp + fp) > 0 else 0
        recall = tp / (tp + fn) if (tp + fn) > 0 else 0
        f1 = 2 * precision * recall / (precision + recall) if (precision + recall) > 0 else 0

        precisions.append(precision)
        recalls.append(recall)
        f1s.append(f1)

    fig, axes = plt.subplots(1, 2, figsize=(14, 5))

    # 1. Metrics vs threshold
    ax = axes[0]
    ax.plot(thresholds, [p * 100 for p in precisions], label="Precision", linewidth=2, color="#9C27B0")
    ax.plot(thresholds, [r * 100 for r in recalls], label="Recall", linewidth=2, color="#FF9800")
    ax.plot(thresholds, [f * 100 for f in f1s], label="F1", linewidth=2, color="#4CAF50")

    best_thresh = thresholds[np.argmax(f1s)]
    ax.axvline(x=best_thresh, color="red", linestyle="--", label=f"Best F1 @ {best_thresh:.2f}")
    ax.axvline(x=0.5, color="gray", linestyle=":", label="Default (0.5)")

    ax.set_xlabel("Threshold")
    ax.set_ylabel("Metric (%)")
    ax.set_title("Metrics vs Decision Threshold", fontweight="bold")
    ax.legend()
    ax.grid(True, alpha=0.3)
    ax.set_xlim(0, 1)
    ax.set_ylim(0, 105)

    # 2. Precision-Recall curve
    ax = axes[1]
    ax.plot([r * 100 for r in recalls], [p * 100 for p in precisions],
            linewidth=2, color="#2196F3")
    ax.scatter([recalls[np.argmax(f1s)] * 100], [precisions[np.argmax(f1s)] * 100],
               color="red", s=100, zorder=5, label=f"Best F1: {max(f1s)*100:.1f}%")
    ax.set_xlabel("Recall (%)")
    ax.set_ylabel("Precision (%)")
    ax.set_title("Precision-Recall Curve", fontweight="bold")
    ax.legend()
    ax.grid(True, alpha=0.3)
    ax.set_xlim(0, 105)
    ax.set_ylim(0, 105)

    plt.tight_layout()
    fig.savefig(save_dir / "threshold_analysis.png", dpi=150, bbox_inches="tight")
    plt.close(fig)
    print(f"  Saved: threshold_analysis.png")

    return best_thresh, max(f1s)


def plot_summary_card(history: dict, config: dict, save_dir: Path):
    """Create a summary card with key metrics."""
    fig, ax = plt.subplots(figsize=(10, 8))
    ax.axis("off")

    # Get best metrics
    best_f1_idx = np.argmax(history["val_f1"])
    best_f1 = history["val_f1"][best_f1_idx] * 100
    best_precision = history["val_precision"][best_f1_idx] * 100
    best_recall = history["val_recall"][best_f1_idx] * 100
    best_acc = history["val_acc"][best_f1_idx] * 100

    # Final metrics
    final_f1 = history["val_f1"][-1] * 100
    final_train_f1 = history["train_f1"][-1] * 100

    text = f"""
    ╔══════════════════════════════════════════════════════════════╗
    ║           HETEROZYGOTE CLASSIFIER - EVALUATION REPORT         ║
    ╠══════════════════════════════════════════════════════════════╣
    ║                                                               ║
    ║  MODEL CONFIGURATION                                          ║
    ║  ─────────────────                                            ║
    ║  Hidden dim:     {config['args']['hidden_dim']:>6}                                       ║
    ║  Num layers:     {config['args']['num_layers']:>6}                                       ║
    ║  Dropout:        {config['args']['dropout']:>6.2f}                                       ║
    ║  Class weight:   {config['args']['class_weight']:>6.1f}                                       ║
    ║  Batch size:     {config['args']['batch_size']:>6}                                       ║
    ║  Learning rate:  {config['args']['lr']:>6.0e}                                       ║
    ║                                                               ║
    ║  TRAINING SUMMARY                                             ║
    ║  ─────────────────                                            ║
    ║  Epochs trained: {config['epochs_trained']:>6}                                       ║
    ║  Best epoch:     {best_f1_idx + 1:>6}                                       ║
    ║                                                               ║
    ║  BEST VALIDATION METRICS (Epoch {best_f1_idx + 1})                           ║
    ║  ─────────────────────────────────                            ║
    ║  F1 Score:       {best_f1:>6.1f}%                                      ║
    ║  Precision:      {best_precision:>6.1f}%                                      ║
    ║  Recall:         {best_recall:>6.1f}%                                      ║
    ║  Accuracy:       {best_acc:>6.2f}%                                      ║
    ║                                                               ║
    ║  GENERALIZATION                                               ║
    ║  ─────────────────                                            ║
    ║  Final Train F1: {final_train_f1:>6.1f}%                                      ║
    ║  Final Val F1:   {final_f1:>6.1f}%                                      ║
    ║  Gap:            {final_train_f1 - final_f1:>6.1f}%                                      ║
    ║                                                               ║
    ╚══════════════════════════════════════════════════════════════╝
    """

    ax.text(0.5, 0.5, text, transform=ax.transAxes, fontsize=11,
            verticalalignment='center', horizontalalignment='center',
            fontfamily='monospace', bbox=dict(boxstyle='round', facecolor='white', alpha=0.8))

    print(f"  Saved: summary_card.png")


# =============================================================================
# Main
# =============================================================================

def main():
    parser = argparse.ArgumentParser(description="Evaluate Heterozygote Classifier")
    parser.add_argument("--checkpoint", type=str, default="checkpoints/heterozygote",
                        help="Checkpoint directory")
    parser.add_argument("--data-dir", type=str, default="datalake/datasets/heterozygote_enhanced",
                        help="Dataset directory")
    parser.add_argument("--output-dir", type=str, default=None,
                        help="Output directory for plots (default: checkpoint/plots)")
    args = parser.parse_args()

    checkpoint_dir = Path(args.checkpoint)
    data_dir = Path(args.data_dir)
    output_dir = Path(args.output_dir) if args.output_dir else checkpoint_dir / "plots"
    output_dir.mkdir(parents=True, exist_ok=True)

    print("=" * 70)
    print("HETEROZYGOTE CLASSIFIER EVALUATION")
    print("=" * 70)
    print(f"Checkpoint: {checkpoint_dir}")
    print(f"Output: {output_dir}")

    # Load history
    print("\n[1/4] Loading training history...")
    with open(checkpoint_dir / "history.json") as f:
        history = json.load(f)
    with open(checkpoint_dir / "config.json") as f:
        config = json.load(f)
    print(f"  Epochs: {len(history['train_loss'])}")

    # Generate history-based plots
    print("\n[2/4] Generating training analysis plots...")
    plot_training_curves(history, output_dir)
    plot_precision_recall_tradeoff(history, output_dir)
    plot_loss_vs_f1(history, output_dir)
    plot_overfitting_analysis(history, output_dir)
    plot_learning_dynamics(history, output_dir)
    plot_summary_card(history, config, output_dir)

    # Load model and evaluate on validation set
    print("\n[3/4] Loading model and evaluating on validation set...")
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    model = HeterozygoteClassifier.load(checkpoint_dir / "best.pt").to(device)

    val_dataset = HeterozygoteEnhancedDataset(data_dir / "val")
    val_loader = DataLoader(val_dataset, batch_size=64, shuffle=False)

    preds, labels, probs = evaluate_model_on_dataset(model, val_loader, device)
    print(f"  Evaluated {len(labels):,} positions")

    # Generate model-based plots
    print("\n[4/4] Generating model plots plots...")
    plot_confusion_matrix(preds, labels, output_dir)
    plot_probability_distribution(probs, labels, output_dir)
    best_thresh, best_f1 = plot_threshold_analysis(probs, labels, output_dir)

    print("\n" + "=" * 70)
    print("EVALUATION COMPLETE")
    print("=" * 70)
    print(f"Best F1: {config['best_val_f1']*100:.1f}%")
    print(f"Optimal threshold: {best_thresh:.2f} (F1: {best_f1*100:.1f}%)")
    print(f"Plots saved to: {output_dir}")


if __name__ == "__main__":
    main()
