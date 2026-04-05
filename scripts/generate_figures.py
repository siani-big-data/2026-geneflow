#!/usr/bin/env python3
"""
Generate publication-ready figures from training results.

Usage:
    # From unified results directory
    uv run python scripts/generate_figures.py --run checkpoints/randomforest_kingdom_20260402

    # From legacy results (pkl + metadata)
    uv run python scripts/generate_figures.py --legacy checkpoints/taxonomy_rf/rf_kingdom_20260402_171647.pkl

    # List all available runs
    uv run python scripts/generate_figures.py --list
"""

import argparse
import json
import pickle
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).parent.parent))

from src.ml.training import (
    PaperFigures,
    ResultsReader,
    TrainingResult,
    Predictions,
)


def generate_from_unified(run_dir: Path, output_dir: Path = None):
    """Generate figures from unified results format."""
    reader = ResultsReader()
    result = reader.load(run_dir)

    output_dir = output_dir or run_dir / "figures"
    figures = PaperFigures(output_dir, format="png")

    # Load predictions if available
    predictions = None
    pred_path = run_dir / "predictions" / "val.npz"
    if pred_path.exists():
        predictions = reader.load_predictions(run_dir, "val")

    paths = figures.generate_all(result, predictions)

    print(f"Generated {len(paths)} figures in {output_dir}")
    for p in paths:
        print(f"  - {p.name}")

    return paths


def generate_from_legacy_rf(pkl_path: Path, output_dir: Path = None, data_dir: Path = None):
    """Generate figures from legacy RF pickle file.

    If data_dir is provided, re-evaluates the model to generate all figures.
    """
    print(f"Loading legacy model from: {pkl_path}")

    with open(pkl_path, "rb") as f:
        data = pickle.load(f)

    # Extract info
    class_names = data.get("class_names", [])
    featuREDACTED = data.get("featuREDACTED", [])
    clf = data.get("classifier")
    include_kmers = data.get("include_kmers", True)
    level = data.get("level", "kingdom")

    output_dir = output_dir or pkl_path.parent / "figures"
    output_dir.mkdir(parents=True, exist_ok=True)

    figures = PaperFigures(output_dir, format="png")
    generated = []

    # Feature importance
    if clf is not None and hasattr(clf, "featuREDACTED"):
        importances = clf.featuREDACTED
        if hasattr(importances, 'get'):
            importances = importances.get()

        importance_dict = dict(zip(featuREDACTED, importances.tolist()))
        path = figures.featuREDACTED(importance_dict, top_n=25)
        generated.append(path)
        print(f"  - {path.name}")

    # Load metadata
    metadata_path = pkl_path.with_name(pkl_path.stem + "_metadata.json")
    metadata = {}
    if metadata_path.exists():
        with open(metadata_path) as f:
            metadata = json.load(f)

    # Try to load cached features to re-evaluate
    cache_dir = Path("checkpoints/taxonomy_rf/cache")
    kmers_str = "kmers" if include_kmers else "nokmers"
    cache_path = cache_dir / f"features_{level}_{kmers_str}.npz"

    if cache_path.exists():
        print(f"\nRe-evaluating model with cached features...")
        cache_data = np.load(cache_path, allow_pickle=True)
        X = cache_data["X"]
        y = cache_data["y"]
        cached_class_names = cache_data["class_names"].tolist()

        # Split same as training (use same seed)
        from sklearn.model_selection import train_test_split
        seed = data.get("args", {}).get("seed", 42)
        val_ratio = data.get("args", {}).get("val_ratio", 0.15)

        X_train, X_val, y_train, y_val = train_test_split(
            X, y, test_size=val_ratio, random_state=seed, stratify=y
        )

        # Get predictions
        X_eval = X_val.astype(np.float32)
        y_pred = clf.predict(X_eval)
        y_proba = clf.predict_proba(X_eval)

        # Convert if needed (cupy/xgboost)
        if hasattr(y_pred, 'get'):
            y_pred = y_pred.get()
        if hasattr(y_proba, 'get'):
            y_proba = y_proba.get()

        # Use the class names from the model
        eval_class_names = class_names if class_names else cached_class_names

        # Confusion matrix
        from sklearn.metrics import confusion_matrix
        cm = confusion_matrix(y_val, y_pred)
        path = figures.confusion_matrix(cm, eval_class_names, normalize=True)
        generated.append(path)
        print(f"  - {path.name}")

        # ROC curves (if multiclass)
        if len(eval_class_names) > 1:
            try:
                path = figures.roc_curves(y_val, y_proba, eval_class_names)
                generated.append(path)
                print(f"  - {path.name}")
            except Exception as e:
                print(f"  (ROC curves skipped: {e})")

            try:
                path = figures.precision_recall_curves(y_val, y_proba, eval_class_names)
                generated.append(path)
                print(f"  - {path.name}")
            except Exception as e:
                print(f"  (PR curves skipped: {e})")

        # Class distribution
        from collections import Counter
        class_counts = Counter(y)
        class_dist = {eval_class_names[i]: int(c) for i, c in class_counts.items() if i < len(eval_class_names)}
        path = figures.class_distribution(class_dist)
        generated.append(path)
        print(f"  - {path.name}")

    # Print summary
    if metadata:
        print(f"\nModel Summary:")
        print(f"  Level: {metadata.get('level')}")
        print(f"  Classes: {metadata.get('n_classes')}")
        print(f"  Train Accuracy: {metadata.get('train_accuracy', 0)*100:.2f}%")
        print(f"  Val Accuracy: {metadata.get('val_accuracy', 0)*100:.2f}%")
        print(f"  Backend: {metadata.get('backend')}")

    print(f"\nGenerated {len(generated)} figures in {output_dir}")
    return generated


def generate_from_legacy_nn(checkpoint_dir: Path, output_dir: Path = None):
    """Generate figures from legacy neural network training (history.json)."""
    import torch

    history_path = checkpoint_dir / "history.json"

    if not history_path.exists():
        print(f"ERROR: No history.json found in {checkpoint_dir}")
        return []

    with open(history_path) as f:
        history = json.load(f)

    output_dir = output_dir or checkpoint_dir / "figures"
    output_dir.mkdir(parents=True, exist_ok=True)

    figures = PaperFigures(output_dir, format="png")
    generated = []

    # Convert history format
    n_epochs = len(history.get("train_loss", history.get("val_loss", [])))
    history_list = []

    for i in range(n_epochs):
        epoch_data = {"epoch": i + 1}
        for key, values in history.items():
            if i < len(values):
                # Normalize key names
                if key == "train_loss":
                    epoch_data["train_loss"] = values[i]
                elif key == "val_loss":
                    epoch_data["val_loss"] = values[i]
                elif key in ("train_acc", "train_accuracy"):
                    epoch_data["train_accuracy"] = values[i]
                elif key in ("val_acc", "val_accuracy"):
                    epoch_data["val_accuracy"] = values[i]
                elif key == "train_mae":
                    epoch_data["train_mae"] = values[i]
                elif key == "val_mae":
                    epoch_data["val_mae"] = values[i]
        history_list.append(epoch_data)

    # Training curves
    if history_list:
        # Detect which metrics are available
        metrics = []
        if "train_loss" in history_list[0]:
            metrics.append("loss")
        if "train_accuracy" in history_list[0]:
            metrics.append("accuracy")
        if "train_mae" in history_list[0]:
            metrics.append("mae")

        if metrics:
            path = figures.training_curves(history_list, metrics=metrics[:3])
            generated.append(path)
            print(f"  - {path.name}")

    # Try to load model and re-evaluate for more figures
    best_model_path = checkpoint_dir / "best.pt"
    if best_model_path.exists():
        print(f"\nLoading model for re-plots...")

        # Detect model type from checkpoint
        checkpoint = torch.load(best_model_path, map_location="cpu", weights_only=False)

        # Check for quality model
        if "input_dim" in checkpoint and "hidden_dims" in checkpoint:
            generated.extend(_generate_quality_figures(checkpoint_dir, checkpoint, figures))

        # Check for trimming model
        if "config" in checkpoint and "TrimmingConfig" in str(checkpoint["config"]):
            generated.extend(_generate_trimming_figures(checkpoint_dir, checkpoint, figures))

    print(f"\nGenerated {len(generated)} figures in {output_dir}")
    return generated


def _generate_quality_figures(checkpoint_dir: Path, checkpoint: dict, figures: PaperFigures) -> list:
    """Generate figures for quality prediction model."""
    import torch
    import torch.nn as nn
    from torch.utils.data import DataLoader

    generated = []

    # Find dataset
    data_dir = Path("datalake/datasets/quality")
    if not data_dir.exists():
        data_dir = Path("datalake/datasets/quality_enhanced")
    if not data_dir.exists():
        print("  Dataset not found, skipping re-plots")
        return generated

    # Build model
    class QualityMLP(nn.Module):
        def __init__(self, input_dim, hidden_dims, dropout=0.2):
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
            layers.append(nn.Linear(prev_dim, 1))
            self.net = nn.Sequential(*layers)

        def forward(self, x):
            return self.net(x).squeeze(-1)

    input_dim = checkpoint["input_dim"]
    hidden_dims = checkpoint["hidden_dims"]

    model = QualityMLP(input_dim, hidden_dims)
    model.load_state_dict(checkpoint["model_state_dict"])
    model.eval()

    # Load test data
    test_dir = data_dir / "test"
    if not test_dir.exists():
        test_dir = data_dir / "val"

    if not test_dir.exists():
        print("  Test/val data not found, skipping re-plots")
        return generated

    # Simple dataset loader
    files = sorted(test_dir.glob("sample_*.npz"))
    if not files:
        return generated

    all_preds = []
    all_targets = []

    with torch.no_grad():
        for f in files:
            data = np.load(f)
            features = torch.tensor(data["features"].astype(np.float32)).unsqueeze(0)
            target = float(data["mean_quality"])

            pred = model(features).item()
            all_preds.append(pred)
            all_targets.append(target)

    y_pred = np.array(all_preds)
    y_true = np.array(all_targets)

    # Scatter plot
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt

    fig, ax = plt.subplots(figsize=(8, 8))
    ax.scatter(y_true, y_pred, alpha=0.5, s=10, c=figures.COLORS[0])

    min_val = min(y_true.min(), y_pred.min())
    max_val = max(y_true.max(), y_pred.max())
    ax.plot([min_val, max_val], [min_val, max_val], 'k--', lw=1.5, label='Perfect')

    mae = np.abs(y_true - y_pred).mean()
    rmse = np.sqrt(((y_true - y_pred) ** 2).mean())
    r2 = 1 - ((y_true - y_pred) ** 2).sum() / ((y_true - y_true.mean()) ** 2).sum()

    ax.text(0.05, 0.95, f'MAE: {mae:.2f}\nRMSE: {rmse:.2f}\nR²: {r2:.3f}',
            transform=ax.transAxes, fontsize=10, verticalalignment='top',
            bbox=dict(boxstyle='round', facecolor='white', alpha=0.8))

    ax.set_xlabel('Actual Quality')
    ax.set_ylabel('Predicted Quality')
    ax.set_title('Predicted vs Actual Quality', fontweight='bold')
    ax.legend(loc='lower right')

    path = figures.output_dir / f"scatter_quality.{figures.format}"
    fig.savefig(path, dpi=figures.dpi, bbox_inches='tight', facecolor='white')
    plt.close(fig)
    generated.append(path)
    print(f"  - {path.name}")

    # Error distribution
    fig, ax = plt.subplots(figsize=(10, 6))
    errors = y_pred - y_true
    ax.hist(errors, bins=50, color=figures.COLORS[0], edgecolor='white', alpha=0.8)
    ax.axvline(0, color='red', linestyle='--', lw=1.5)
    ax.axvline(errors.mean(), color=figures.COLORS[1], linestyle='-', lw=1.5,
               label=f'Mean: {errors.mean():.2f}')
    ax.set_xlabel('Prediction Error')
    ax.set_ylabel('Count')
    ax.set_title('Error Distribution', fontweight='bold')
    ax.legend()

    path = figures.output_dir / f"error_distribution.{figures.format}"
    fig.savefig(path, dpi=figures.dpi, bbox_inches='tight', facecolor='white')
    plt.close(fig)
    generated.append(path)
    print(f"  - {path.name}")

    return generated


def _generate_trimming_figures(checkpoint_dir: Path, checkpoint: dict, figures: PaperFigures) -> list:
    """Generate figures for trimming prediction model."""
    import torch
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt

    sys.path.insert(0, str(Path(__file__).parent.parent))
    from src.ml.models.trimming import TrimmingPredictor

    generated = []

    # Find dataset
    data_dir = Path("datalake/datasets/trimming")
    if not data_dir.exists():
        print("  Trimming dataset not found, skipping re-plots")
        return generated

    # Load model
    model = TrimmingPredictor(checkpoint["config"])
    model.load_state_dict(checkpoint["state_dict"])
    model.eval()

    # Load test data
    test_dir = data_dir / "test"
    if not test_dir.exists():
        test_dir = data_dir / "val"

    files = sorted(test_dir.glob("*.npz"))[:500]  # Limit for speed
    if not files:
        print("  No test files found")
        return generated

    print(f"  Evaluating on {len(files)} samples...")

    all_pred_start = []
    all_pred_end = []
    all_true_start = []
    all_true_end = []

    with torch.no_grad():
        for f in files:
            data = np.load(f)
            quality = data["quality"].astype(np.float32)
            seq_len = len(quality)

            # Get true ratios and convert to positions
            true_start_ratio = float(data["start_ratio"].item())
            true_end_ratio = float(data["end_ratio"].item())
            true_start = true_start_ratio * seq_len
            true_end = true_end_ratio * seq_len

            # Normalize and predict
            q_tensor = torch.tensor(quality / 60.0).unsqueeze(0)
            ratios = model(q_tensor)

            pred_start = ratios[0, 0].item() * seq_len
            pred_end = ratios[0, 1].item() * seq_len

            all_pred_start.append(pred_start)
            all_pred_end.append(pred_end)
            all_true_start.append(true_start)
            all_true_end.append(true_end)

    pred_start = np.array(all_pred_start)
    pred_end = np.array(all_pred_end)
    true_start = np.array(all_true_start)
    true_end = np.array(all_true_end)

    # Figure 1: Scatter plots for start and end positions
    fig, axes = plt.subplots(1, 2, figsize=(14, 6))

    for ax, pred, true, title in [
        (axes[0], pred_start, true_start, "Start Position"),
        (axes[1], pred_end, true_end, "End Position"),
    ]:
        ax.scatter(true, pred, alpha=0.5, s=10, c=figures.COLORS[0])

        min_val = min(true.min(), pred.min())
        max_val = max(true.max(), pred.max())
        ax.plot([min_val, max_val], [min_val, max_val], 'k--', lw=1.5)

        mae = np.abs(true - pred).mean()
        rmse = np.sqrt(((true - pred) ** 2).mean())

        ax.text(0.05, 0.95, f'MAE: {mae:.1f} bp\nRMSE: {rmse:.1f} bp',
                transform=ax.transAxes, fontsize=10, verticalalignment='top',
                bbox=dict(boxstyle='round', facecolor='white', alpha=0.8))

        ax.set_xlabel(f'Actual {title} (bp)')
        ax.set_ylabel(f'Predicted {title} (bp)')
        ax.set_title(title, fontweight='bold')

    fig.suptitle('Trimming Position Predictions', fontweight='bold', y=1.02)
    plt.tight_layout()

    path = figures.output_dir / f"scatter_trimming.{figures.format}"
    fig.savefig(path, dpi=figures.dpi, bbox_inches='tight', facecolor='white')
    plt.close(fig)
    generated.append(path)
    print(f"  - {path.name}")

    # Figure 2: Error distributions
    fig, axes = plt.subplots(1, 2, figsize=(14, 5))

    for ax, pred, true, title, color in [
        (axes[0], pred_start, true_start, "Start Position Error", figures.COLORS[0]),
        (axes[1], pred_end, true_end, "End Position Error", figures.COLORS[1]),
    ]:
        errors = pred - true
        ax.hist(errors, bins=50, color=color, edgecolor='white', alpha=0.8)
        ax.axvline(0, color='red', linestyle='--', lw=1.5)
        ax.axvline(errors.mean(), color='black', linestyle='-', lw=1.5,
                   label=f'Mean: {errors.mean():.1f} bp')

        ax.set_xlabel('Error (bp)')
        ax.set_ylabel('Count')
        ax.set_title(title, fontweight='bold')
        ax.legend()

    plt.tight_layout()

    path = figures.output_dir / f"error_distribution_trimming.{figures.format}"
    fig.savefig(path, dpi=figures.dpi, bbox_inches='tight', facecolor='white')
    plt.close(fig)
    generated.append(path)
    print(f"  - {path.name}")

    # Figure 3: Example predictions (show quality + trim positions)
    fig, axes = plt.subplots(2, 3, figsize=(15, 8))
    axes = axes.flatten()

    sample_indices = np.random.choice(len(files), min(6, len(files)), replace=False)

    for ax, idx in zip(axes, sample_indices):
        data = np.load(files[idx])
        quality = data["quality"]
        seq_len = len(quality)
        true_s = int(float(data["start_ratio"].item()) * seq_len)
        true_e = int(float(data["end_ratio"].item()) * seq_len)

        q_tensor = torch.tensor(quality.astype(np.float32) / 60.0).unsqueeze(0)
        with torch.no_grad():
            ratios = model(q_tensor)
        pred_s = int(ratios[0, 0].item() * len(quality))
        pred_e = int(ratios[0, 1].item() * len(quality))

        ax.plot(quality, color='gray', alpha=0.7, lw=0.8, label='Quality')
        ax.axvline(true_s, color=figures.COLORS[0], lw=2, linestyle='--', label=f'True start: {true_s}')
        ax.axvline(true_e, color=figures.COLORS[0], lw=2, linestyle='-', label=f'True end: {true_e}')
        ax.axvline(pred_s, color=figures.COLORS[1], lw=2, linestyle='--', label=f'Pred start: {pred_s}')
        ax.axvline(pred_e, color=figures.COLORS[1], lw=2, linestyle='-', label=f'Pred end: {pred_e}')

        ax.set_xlabel('Position')
        ax.set_ylabel('Quality')
        ax.set_ylim(0, 45)
        ax.legend(fontsize=7, loc='lower right')

    fig.suptitle('Example Predictions (blue=actual, orange=predicted)', fontweight='bold')
    plt.tight_layout()

    path = figures.output_dir / f"examples_trimming.{figures.format}"
    fig.savefig(path, dpi=figures.dpi, bbox_inches='tight', facecolor='white')
    plt.close(fig)
    generated.append(path)
    print(f"  - {path.name}")

    # Print summary metrics
    start_mae = np.abs(pred_start - true_start).mean()
    end_mae = np.abs(pred_end - true_end).mean()
    print(f"\n  Summary: Start MAE={start_mae:.1f}bp, End MAE={end_mae:.1f}bp")

    return generated


def list_runs(base_dir: Path = Path("checkpoints")):
    """List all available training runs."""
    print(f"\nAvailable runs in {base_dir}:\n")

    # Unified format runs
    unified = []
    for d in base_dir.iterdir():
        if d.is_dir() and (d / "result.json").exists():
            unified.append(d)

    if unified:
        print("Unified format (new):")
        for d in sorted(unified):
            print(f"  - {d.name}")

    # Legacy RF models
    legacy_rf = list(base_dir.rglob("*.pkl"))
    if legacy_rf:
        print("\nLegacy RF models:")
        for f in sorted(legacy_rf)[:10]:
            print(f"  - {f.relative_to(base_dir)}")
        if len(legacy_rf) > 10:
            print(f"  ... and {len(legacy_rf) - 10} more")

    # Legacy NN checkpoints
    legacy_nn = list(base_dir.rglob("history.json"))
    if legacy_nn:
        print("\nLegacy NN checkpoints:")
        for f in sorted(legacy_nn)[:10]:
            print(f"  - {f.parent.relative_to(base_dir)}")
        if len(legacy_nn) > 10:
            print(f"  ... and {len(legacy_nn) - 10} more")


def main():
    parser = argparse.ArgumentParser(description="Generate figures from training results")
    parser.add_argument("--run", type=str, help="Unified results directory")
    parser.add_argument("--legacy", type=str, help="Legacy pickle file or checkpoint directory")
    parser.add_argument("--output", type=str, help="Output directory for figures")
    parser.add_argument("--list", action="stoREDACTED", help="List available runs")
    parser.add_argument("--format", choices=["png", "pdf", "svg"], default="png")
    args = parser.parse_args()

    if args.list:
        list_runs()
        return 0

    if not args.run and not args.legacy:
        parser.print_help()
        return 1

    output_dir = Path(args.output) if args.output else None

    if args.run:
        run_dir = Path(args.run)
        generate_from_unified(run_dir, output_dir)

    elif args.legacy:
        legacy_path = Path(args.legacy)

        if legacy_path.suffix == ".pkl":
            generate_from_legacy_rf(legacy_path, output_dir)
        elif legacy_path.is_dir():
            if (legacy_path / "history.json").exists():
                generate_from_legacy_nn(legacy_path, output_dir)
            else:
                print(f"ERROR: No history.json found in {legacy_path}")
                return 1
        else:
            print(f"ERROR: Unknown format: {legacy_path}")
            return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
