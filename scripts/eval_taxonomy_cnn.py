#!/usr/bin/env python3
"""
Evaluate taxonomy CNN and generate figures.

Usage:
    uv run python scripts/eval_taxonomy_cnn.py --checkpoint checkpoints/taxonomy_cnn/kingdom_phylum_class_genus
"""

import argparse
import json
import sys
from collections import Counter
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np
import torch
from sklearn.metrics import classification_report, confusion_matrix
from torch.utils.data import DataLoader

sys.path.insert(0, str(Path(__file__).parent.parent))

from scripts.train_taxonomy import TaxonomySequenceDataset, KINGDOM_NORMALIZE  # noqa: E402
from src.ml.models.taxonomy import TaxonomyClassifier  # noqa: E402


def plot_confusion_matrix(y_true, y_pred, class_names, title, save_path):
    """Plot confusion matrix."""
    cm = confusion_matrix(y_true, y_pred)

    # Normalize
    cm_norm = cm.astype("float") / cm.sum(axis=1, keepdims=True)

    fig, ax = plt.subplots(figsize=(max(8, len(class_names) * 0.5), max(6, len(class_names) * 0.4)))

    im = ax.imshow(cm_norm, interpolation="nearest", cmap="Blues")
    ax.figure.colorbar(im, ax=ax)

    ax.set(
        xticks=np.arange(len(class_names)),
        yticks=np.arange(len(class_names)),
        xticklabels=class_names,
        yticklabels=class_names,
        ylabel="True",
        xlabel="Predicted",
        title=title,
    )

    plt.setp(ax.get_xticklabels(), rotation=45, ha="right", rotation_mode="anchor")

    # Add text annotations for small matrices
    if len(class_names) <= 20:
        thresh = cm_norm.max() / 2.0
        for i in range(len(class_names)):
            for j in range(len(class_names)):
                ax.text(
                    j, i, f"{cm_norm[i, j]:.2f}",
                    ha="center", va="center",
                    color="white" if cm_norm[i, j] > thresh else "black",
                    fontsize=8,
                )

    plt.tight_layout()
    fig.savefig(save_path, dpi=150, bbox_inches="tight")
    plt.close()
    print(f"  Saved: {save_path}")


def plot_class_distribution(class_counts, title, save_path):
    """Plot class distribution."""
    labels = list(class_counts.keys())
    counts = list(class_counts.values())

    # Sort by count
    sorted_pairs = sorted(zip(counts, labels), reverse=True)
    counts, labels = zip(*sorted_pairs)

    # Limit to top 30 for readability
    if len(labels) > 30:
        labels = labels[:30]
        counts = counts[:30]

    fig, ax = plt.subplots(figsize=(12, max(6, len(labels) * 0.25)))

    y_pos = np.arange(len(labels))
    ax.barh(y_pos, counts, color="steelblue")
    ax.set_yticks(y_pos)
    ax.set_yticklabels(labels)
    ax.invert_yaxis()
    ax.set_xlabel("Count")
    ax.set_title(title)

    plt.tight_layout()
    fig.savefig(save_path, dpi=150, bbox_inches="tight")
    plt.close()
    print(f"  Saved: {save_path}")


def plot_accuracy_by_level(accuracies, save_path):
    """Plot accuracy by taxonomic level."""
    levels = list(accuracies.keys())
    accs = [accuracies[l] * 100 for l in levels]

    fig, ax = plt.subplots(figsize=(10, 6))

    colors = ["#2ecc71" if a >= 90 else "#e74c3c" if a < 85 else "#f39c12" for a in accs]
    bars = ax.bar(levels, accs, color=colors)

    ax.axhline(y=90, color="green", linestyle="--", alpha=0.7, label="Target (90%)")
    ax.axhline(y=85, color="orange", linestyle="--", alpha=0.7, label="Min genus (85%)")

    ax.set_ylabel("Accuracy (%)")
    ax.set_xlabel("Taxonomic Level")
    ax.set_title("Validation Accuracy by Taxonomic Level")
    ax.set_ylim(0, 105)
    ax.legend()

    # Add value labels
    for bar, acc in zip(bars, accs):
        ax.text(
            bar.get_x() + bar.get_width() / 2, bar.get_height() + 1,
            f"{acc:.1f}%", ha="center", va="bottom", fontsize=11, fontweight="bold"
        )

    plt.tight_layout()
    fig.savefig(save_path, dpi=150)
    plt.close()
    print(f"  Saved: {save_path}")


def evaluate_model(model, loader, device, levels):
    """Evaluate model and collect predictions."""
    model.eval()

    all_preds = {level: [] for level in levels}
    all_labels = {level: [] for level in levels}

    with torch.no_grad():
        for batch in loader:
            sequence = batch["sequence"].to(device)
            labels = {level: batch["labels"][level] for level in levels}

            outputs = model(sequence, features=None)

            for level in levels:
                preds = outputs[level].argmax(dim=-1).cpu().numpy()
                all_preds[level].extend(preds)
                all_labels[level].extend(labels[level].numpy())

    return all_preds, all_labels


def main():
    parser = argparse.ArgumentParser(description="Evaluate Taxonomy CNN and generate figures")
    parser.add_argument(
        "--checkpoint", type=str, required=True,
        help="Checkpoint directory",
    )
    parser.add_argument(
        "--data-dir", type=str, default="datalake/curated",
        help="Data directory",
    )
    parser.add_argument("--batch-size", type=int, default=128)
    parser.add_argument("--max-samples", type=int, default=50000)

    args = parser.parse_args()

    checkpoint_dir = Path(args.checkpoint)
    figures_dir = checkpoint_dir / "figures"
    figures_dir.mkdir(exist_ok=True)

    print("=" * 60)
    print("TAXONOMY CNN EVALUATION")
    print("=" * 60)

    # Load config
    with open(checkpoint_dir / "config.json") as f:
        config = json.load(f)

    levels = config["levels"]
    best_acc = config["best_val_accuracy"]

    print(f"Levels: {levels}")
    print(f"Best accuracies: {best_acc}")

    # Plot accuracy by level
    print("\nGenerating figures...")
    plot_accuracy_by_level(best_acc, figures_dir / "accuracy_by_level.png")

    # Load model
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    print(f"\nDevice: {device}")

    model = TaxonomyClassifier.load_from_checkpoint(checkpoint_dir / "best.pt")
    model = model.to(device)
    model.eval()

    print(f"Model loaded: {sum(p.numel() for p in model.parameters()):,} parameters")

    # Load dataset
    print("\nLoading evaluation data...")
    dataset = TaxonomySequenceDataset(
        data_dir=args.data_dir,
        levels=levels,
        max_samples=args.max_samples,
    )

    loader = DataLoader(dataset, batch_size=args.batch_size, shuffle=False, num_workers=0)

    # Evaluate
    print("\nRunning evaluation...")
    all_preds, all_labels = evaluate_model(model, loader, device, levels)

    # Generate figures per level
    for level in levels:
        print(f"\n--- {level.upper()} ---")

        y_true = np.array(all_labels[level])
        y_pred = np.array(all_preds[level])

        # Accuracy
        acc = (y_true == y_pred).mean()
        print(f"Accuracy: {acc:.2%}")

        # Class names
        class_labels = config["class_labels_per_level"][level]

        # Confusion matrix (only for levels with <= 50 classes)
        if len(class_labels) <= 50:
            plot_confusion_matrix(
                y_true, y_pred, class_labels,
                f"Confusion Matrix - {level.capitalize()}",
                figures_dir / f"confusion_{level}.png",
            )

        # Class distribution
        class_counts = Counter(dataset.samples[i]["labels"][level] for i in range(len(dataset.samples)))
        plot_class_distribution(
            class_counts,
            f"Class Distribution - {level.capitalize()}",
            figures_dir / f"distribution_{level}.png",
        )

        # Classification report (top classes)
        if len(class_labels) <= 50:
            report = classification_report(
                y_true, y_pred, target_names=class_labels, output_dict=True
            )

            # Save report
            with open(figures_dir / f"report_{level}.json", "w") as f:
                json.dump(report, f, indent=2)

    print("\n" + "=" * 60)
    print("COMPLETE")
    print("=" * 60)
    print(f"Figures saved to: {figures_dir}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
