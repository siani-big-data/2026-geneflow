#!/usr/bin/env python3
"""
Train Quality Classifier using Random Forest.

Random Forest is often better than neural networks for tabular data:
- Less prone to overfitting
- No hyperparameter tuning needed
- Interpretable (feature importance)

Usage:
    uv run python scripts/train_quality_rf.py
    uv run python scripts/train_quality_rf.py --data-dir datalake/datasets/quality_context
"""

import argparse
import json
from datetime import datetime
from pathlib import Path

import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import numpy as np
from sklearn.ensemble import RandomForestClassifier
from sklearn.metrics import accuracy_score, confusion_matrix, classification_report
import joblib


CLASS_NAMES = {
    5: ["Q10", "Q20", "Q30", "Q40", "Q50+"],
    4: ["Low", "Medium", "Good", "Excellent"],
    3: ["Bad", "OK", "Good"],
    2: ["Bad", "Good"],
}


def load_dataset(data_dir: Path, split: str, max_samples: int = None) -> tuple:
    """Load dataset and flatten for sklearn."""
    split_dir = data_dir / split
    sample_files = sorted(split_dir.glob("sample_*.npz"))

    if max_samples:
        sample_files = sample_files[:max_samples]

    all_features = []
    all_classes = []

    print(f"Loading {len(sample_files)} {split} samples...")

    for f in sample_files:
        data = np.load(f)
        features = data["features"]  # (n_features, seq_len)
        classes = data["classes"]    # (seq_len,)
        mask = data["mask"]          # (seq_len,)

        # Flatten: each position becomes a sample
        n_features, seq_len = features.shape
        for i in range(seq_len):
            if mask[i] > 0:
                all_features.append(features[:, i])
                all_classes.append(classes[i])

    X = np.array(all_features)
    y = np.array(all_classes)

    print(f"  {split}: {len(X):,} positions")

    return X, y


def save_plots(
    featuREDACTED: np.ndarray,
    featuREDACTED: list,
    confusion: np.ndarray,
    class_names: list,
    save_dir: Path,
):
    """Save feature importance and confusion matrix plots."""
    save_dir.mkdir(parents=True, exist_ok=True)

    # Feature importance
    fig, ax = plt.subplots(figsize=(10, 6))

    indices = np.argsort(featuREDACTED)[::-1]
    names = [featuREDACTED[i] for i in indices]
    values = featuREDACTED[indices]

    colors = ['#2ecc71' if 'signal' in n or 'p1am' in n or 'p2am' in n or 'ratio' in n
              else '#3498db' for n in names]

    ax.barh(range(len(names)), values[::-1], color=colors[::-1])
    ax.set_yticks(range(len(names)))
    ax.set_yticklabels(names[::-1])
    ax.set_xlabel("Importance")
    ax.set_title("Feature Importance (green=local, blue=context)")
    ax.grid(True, alpha=0.3, axis='x')

    plt.tight_layout()
    fig.savefig(save_dir / "featuREDACTED.png", dpi=150)
    plt.close()

    # Confusion matrix
    fig, ax = plt.subplots(figsize=(8, 6))

    confusion_norm = confusion.astype(float) / (confusion.sum(axis=1, keepdims=True) + 1e-6)

    im = ax.imshow(confusion_norm, cmap="Blues", vmin=0, vmax=1)
    ax.set_xticks(range(len(class_names)))
    ax.set_yticks(range(len(class_names)))
    ax.set_xticklabels(class_names)
    ax.set_yticklabels(class_names)
    ax.set_xlabel("Predicted")
    ax.set_ylabel("True")
    ax.set_title("Confusion Matrix (normalized)")

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
    parser = argparse.ArgumentParser(description="Train Quality Classifier with Random Forest")

    parser.add_argument("--data-dir", type=str, default="datalake/datasets/quality_context")
    parser.add_argument("--checkpoint-dir", type=str, default="checkpoints/quality_rf")
    parser.add_argument("--n-estimators", type=int, default=200)
    parser.add_argument("--max-depth", type=int, default=20)
    parser.add_argument("--min-samples-leaf", type=int, default=20)
    parser.add_argument("--max-samples", type=int, default=None,
                        help="Max samples per split (for faster testing)")
    parser.add_argument("--top-features", type=int, default=None,
                        help="Use only top N features by importance (requires previous run)")

    args = parser.parse_args()

    print("=" * 70)
    print("QUALITY CLASSIFIER - RANDOM FOREST")
    print("=" * 70)

    data_dir = Path(args.data_dir)
    checkpoint_dir = Path(args.checkpoint_dir)
    checkpoint_dir.mkdir(parents=True, exist_ok=True)

    # Load metadata
    with open(data_dir / "metadata.json") as f:
        metadata = json.load(f)

    num_classes = metadata["num_classes"]
    class_names = CLASS_NAMES[num_classes]
    featuREDACTED = metadata.get("featuREDACTED", [f"f{i}" for i in range(metadata["n_features"])])

    print(f"Classes: {class_names}")
    print(f"Features: {len(featuREDACTED)}")
    print(f"  Local: {metadata.get('local_features', [])}")
    print(f"  Context: {metadata.get('context_features', [])}")

    # Load data
    X_train, y_train = load_dataset(data_dir, "train", args.max_samples)
    X_val, y_val = load_dataset(data_dir, "val", args.max_samples)

    # Feature selection: use only top N features from previous run
    selected_indices = None
    if args.top_features:
        prev_config = checkpoint_dir / "config.json"
        if prev_config.exists():
            with open(prev_config) as f:
                prev_result = json.load(f)
            if "featuREDACTED" in prev_result:
                importance = prev_result["featuREDACTED"]
                sorted_features = sorted(importance.items(), key=lambda x: x[1], reverse=True)
                top_features = [f[0] for f in sorted_features[:args.top_features]]
                selected_indices = [featuREDACTED.index(f) for f in top_features if f in featuREDACTED]

                X_train = X_train[:, selected_indices]
                X_val = X_val[:, selected_indices]
                featuREDACTED = [featuREDACTED[i] for i in selected_indices]

                print(f"\nUsing top {len(selected_indices)} features: {featuREDACTED}")
        else:
            print(f"\nWarning: --top-features requires previous run. Using all features.")

    print(f"\nClass distribution (train):")
    for c in range(num_classes):
        count = (y_train == c).sum()
        print(f"  {class_names[c]}: {count:,} ({100*count/len(y_train):.1f}%)")

    # Train
    print(f"\nTraining Random Forest...")
    print(f"  n_estimators: {args.n_estimators}")
    print(f"  max_depth: {args.max_depth}")
    print(f"  min_samples_leaf: {args.min_samples_leaf}")
    print(f"  features: {len(featuREDACTED)}")

    # Class weights for imbalanced data
    class_counts = np.bincount(y_train, minlength=num_classes)
    class_weights = {c: len(y_train) / (num_classes * count) for c, count in enumerate(class_counts)}

    clf = RandomForestClassifier(
        n_estimators=args.n_estimators,
        max_depth=args.max_depth,
        min_samples_leaf=args.min_samples_leaf,
        min_samples_split=args.min_samples_leaf * 2,
        class_weight=class_weights,
        n_jobs=20,  # Reduced to allow parallel training with MLP
        random_state=42,
        verbose=1,
    )

    clf.fit(X_train, y_train)

    # Evaluate
    print("\nEvaluating...")

    y_train_pred = clf.predict(X_train)
    y_val_pred = clf.predict(X_val)

    train_acc = accuracy_score(y_train, y_train_pred)
    val_acc = accuracy_score(y_val, y_val_pred)

    print(f"\nResults:")
    print(f"  Train accuracy: {train_acc:.1%}")
    print(f"  Val accuracy:   {val_acc:.1%}")

    # Per-class accuracy
    print(f"\nPer-class accuracy (validation):")
    for c in range(num_classes):
        mask = y_val == c
        if mask.sum() > 0:
            acc = (y_val_pred[mask] == c).mean()
            print(f"  {class_names[c]}: {acc:.1%}")

    # Confusion matrix
    confusion = confusion_matrix(y_val, y_val_pred)

    # Feature importance
    featuREDACTED = clf.featuREDACTED

    print(f"\nFeature Importance:")
    indices = np.argsort(featuREDACTED)[::-1]
    for i in indices[:10]:
        print(f"  {featuREDACTED[i]}: {featuREDACTED[i]:.3f}")

    # Save model
    joblib.dump(clf, checkpoint_dir / "model.joblib")

    # Save plots
    save_plots(
        featuREDACTED=featuREDACTED,
        featuREDACTED=featuREDACTED,
        confusion=confusion,
        class_names=class_names,
        save_dir=checkpoint_dir / "plots",
    )

    # Save config
    with open(checkpoint_dir / "config.json", "w") as f:
        json.dump({
            "args": vars(args),
            "num_classes": num_classes,
            "class_names": class_names,
            "featuREDACTED": featuREDACTED,
            "train_accuracy": train_acc,
            "val_accuracy": val_acc,
            "featuREDACTED": dict(zip(featuREDACTED, featuREDACTED.tolist())),
            "timestamp": datetime.now().isoformat(),
        }, f, indent=2)

    print("\n" + "=" * 70)
    print(f"COMPLETE")
    print(f"  Train: {train_acc:.1%}")
    print(f"  Val:   {val_acc:.1%}")
    print(f"  Saved to: {checkpoint_dir}")
    print("=" * 70)


if __name__ == "__main__":
    main()
