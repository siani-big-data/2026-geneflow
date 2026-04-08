#!/usr/bin/env python3
"""
Train enhanced taxonomy classifier using precomputed features.

This script trains the EnhancedTaxonomyClassifier which combines:
- Sequence embeddings (CNN on nucleotide indices)
- Extracted features (GC content, k-mers, complexity metrics, etc.)

Usage:
    # First generate the dataset
    uv run python scripts/generate_training_datasets.py --taxonomy --level phylum

    # Then train
    uv run python scripts/train_taxonomy_enhanced.py \\
        --data datalake/datasets/taxonomy_phylum
    uv run python scripts/train_taxonomy_enhanced.py \\
        --data datalake/datasets/taxonomy_phylum --epochs 50 --batch-size 64
"""

import argparse
import json
import sys
from pathlib import Path

import torch
import torch.nn as nn
from torch.optim import AdamW
from torch.optim.lr_scheduler import CosineAnnealingLR
from torch.utils.data import DataLoader

# Add src to path
sys.path.insert(0, str(Path(__file__).parent.parent))

from src.ml.datasets import PrecomputedTaxonomyDataset
from src.ml.models.taxonomy import (
    EnhancedTaxonomyClassifier,
    EnhancedTaxonomyConfig,
    TaxonomyClassifier,
    TaxonomyClassifierConfig,
)


def train_epoch(model, loader, optimizer, criterion, device, use_features=True, scaler=None):
    """Train for one epoch."""
    model.train()
    total_loss = 0.0
    correct = 0
    total = 0

    for batch_idx, batch in enumerate(loader):
        optimizer.zero_grad()

        sequence = batch["sequence"].to(device)
        labels = batch["label"].to(device)

        if scaler:
            with torch.amp.autocast("cuda"):
                if use_features and "features" in batch:
                    features = batch["features"].to(device)
                    outputs = model(sequence, features)
                else:
                    outputs = model(sequence)
                loss = criterion(outputs, labels)

            scaler.scale(loss).backward()
            scaler.step(optimizer)
            scaler.update()
        else:
            if use_features and "features" in batch:
                features = batch["features"].to(device)
                outputs = model(sequence, features)
            else:
                outputs = model(sequence)

            loss = criterion(outputs, labels)
            loss.backward()
            optimizer.step()

        total_loss += loss.item()
        _, predicted = outputs.max(1)
        total += labels.size(0)
        correct += predicted.eq(labels).sum().item()

    return {
        "loss": total_loss / len(loader),
        "accuracy": correct / total,
    }


def evaluate(model, loader, criterion, device, use_features=True):
    """Evaluate model."""
    model.eval()
    total_loss = 0.0
    correct = 0
    total = 0

    with torch.no_grad():
        for batch in loader:
            sequence = batch["sequence"].to(device)
            labels = batch["label"].to(device)

            if use_features and "features" in batch:
                features = batch["features"].to(device)
                outputs = model(sequence, features)
            else:
                outputs = model(sequence)

            loss = criterion(outputs, labels)

            total_loss += loss.item()
            _, predicted = outputs.max(1)
            total += labels.size(0)
            correct += predicted.eq(labels).sum().item()

    return {
        "loss": total_loss / len(loader),
        "accuracy": correct / total,
    }


def main():
    parser = argparse.ArgumentParser(description="Train Enhanced Taxonomy Classifier")
    parser.add_argument(
        "--data",
        type=str,
        required=True,
        help="Directory with precomputed train/val/test splits",
    )
    parser.add_argument(
        "--model",
        type=str,
        default="enhanced",
        choices=["enhanced", "base"],
        help="Model type: enhanced (with features) or base (sequence only)",
    )
    parser.add_argument("--output-dir", type=str, default="checkpoints/taxonomy")
    parser.add_argument("--epochs", type=int, default=100)
    parser.add_argument("--batch-size", type=int, default=32)
    parser.add_argument("--lr", type=float, default=1e-3)
    parser.add_argument("--hidden-channels", type=int, default=128)
    parser.add_argument("--num-layers", type=int, default=4)
    parser.add_argument("--feature-hidden", type=int, default=128)
    parser.add_argument("--fusion-dim", type=int, default=256)
    parser.add_argument("--dropout", type=float, default=0.2)
    parser.add_argument("--patience", type=int, default=15)
    parser.add_argument("--seed", type=int, default=42)

    args = parser.parse_args()

    # Set seed
    torch.manual_seed(args.seed)
    if torch.cuda.is_available():
        torch.cuda.manual_seed(args.seed)

    data_dir = Path(args.data)
    output_path = Path(args.output_dir)
    output_path.mkdir(parents=True, exist_ok=True)

    print("=" * 70)
    print("ENHANCED TAXONOMY CLASSIFIER TRAINING")
    print("=" * 70)

    # Load metadata
    metadata_path = data_dir / "metadata.json"
    if not metadata_path.exists():
        print(f"ERROR: metadata.json not found in {data_dir}")
        print("Run scripts/generate_training_datasets.py first")
        return 1

    with open(metadata_path) as f:
        metadata = json.load(f)

    num_classes = metadata["num_classes"]
    class_labels = metadata["class_labels"]
    config_info = metadata.get("config", {})

    print(f"\nDataset: {data_dir}")
    print(f"Classification level: {config_info.get('classification_level', 'unknown')}")
    print(f"Total samples: {metadata['total_samples']}")
    train_n = metadata['train_samples']
    val_n = metadata['val_samples']
    test_n = metadata['test_samples']
    print(f"Train: {train_n} | Val: {val_n} | Test: {test_n}")
    print(f"Number of classes: {num_classes}")
    if len(class_labels) > 10:
        print(f"Classes: {class_labels[:10]}...")
    else:
        print(f"Classes: {class_labels}")

    # Create datasets
    print("\n" + "-" * 70)
    print("LOADING DATASETS")
    print("-" * 70)

    use_features = args.model == "enhanced"

    train_dataset = PrecomputedTaxonomyDataset(
        data_dir / "train",
        include_features=use_features,
    )
    val_dataset = PrecomputedTaxonomyDataset(
        data_dir / "val",
        include_features=use_features,
    )

    # Optional test dataset
    test_dir = data_dir / "test"
    test_dataset = None
    if test_dir.exists():
        test_dataset = PrecomputedTaxonomyDataset(
            test_dir,
            include_features=use_features,
        )

    print(f"Train samples loaded: {len(train_dataset)}")
    print(f"Val samples loaded: {len(val_dataset)}")
    if test_dataset:
        print(f"Test samples loaded: {len(test_dataset)}")

    # Data loaders
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

    # Create model
    print("\n" + "-" * 70)
    print("MODEL CONFIGURATION")
    print("-" * 70)

    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

    if use_features:
        # Get number of features from a sample
        sample = train_dataset[0]
        num_features = sample["features"].shape[0] if "features" in sample else 101

        model_config = EnhancedTaxonomyConfig(
            num_classes=num_classes,
            num_features=num_features,
            hidden_channels=args.hidden_channels,
            num_conv_layers=args.num_layers,
            featuREDACTED=args.featuREDACTED,
            fusion_dim=args.fusion_dim,
            dropout=args.dropout,
            class_labels=class_labels,
        )
        model = EnhancedTaxonomyClassifier(model_config)
        print("Model: EnhancedTaxonomyClassifier")
        print(f"Sequence encoder: CNN with {args.num_layers} layers")
        print(f"Feature encoder: {num_features} dims -> MLP")
        print(f"Fusion dimension: {args.fusion_dim}")
    else:
        model_config = TaxonomyClassifierConfig(
            num_classes=num_classes,
            hidden_channels=args.hidden_channels,
            num_layers=args.num_layers,
            dropout=args.dropout,
            class_labels=class_labels,
        )
        model = TaxonomyClassifier(model_config)
        print("Model: TaxonomyClassifier (base)")

    model = model.to(device)
    n_params = sum(p.numel() for p in model.parameters() if p.requires_grad)
    print(f"Parameters: {n_params:,}")
    print(f"Device: {device}")

    # Loss with class weights
    class_weights = train_dataset.get_class_weights().to(device)
    criterion = nn.CrossEntropyLoss(weight=class_weights)

    # Optimizer and scheduler
    optimizer = AdamW(model.parameters(), lr=args.lr, weight_decay=0.01)
    scheduler = CosineAnnealingLR(optimizer, T_max=args.epochs, eta_min=args.lr * 0.01)

    # Mixed precision scaler
    scaler = torch.amp.GradScaler("cuda") if torch.cuda.is_available() else None

    # Training history
    history = {
        "train_loss": [],
        "train_acc": [],
        "val_loss": [],
        "val_acc": [],
    }

    # Early stopping
    best_val_acc = 0.0
    best_epoch = 0
    no_improve = 0

    # Train
    print("\n" + "=" * 70)
    print("STARTING TRAINING")
    print("=" * 70)

    for epoch in range(args.epochs):
        print(f"\nEpoch {epoch + 1}/{args.epochs}")
        print("-" * 40)

        # Train
        train_metrics = train_epoch(
            model, train_loader, optimizer, criterion, device,
            use_features=use_features, scaler=scaler
        )

        # Validate
        val_metrics = evaluate(model, val_loader, criterion, device, use_features=use_features)

        # Update scheduler
        scheduler.step()

        # Save history
        history["train_loss"].append(train_metrics["loss"])
        history["train_acc"].append(train_metrics["accuracy"])
        history["val_loss"].append(val_metrics["loss"])
        history["val_acc"].append(val_metrics["accuracy"])

        t_loss, t_acc = train_metrics['loss'], train_metrics['accuracy']
        print(f"Train Loss: {t_loss:.4f} | Train Acc: {t_acc:.2%}")
        v_loss, v_acc = val_metrics['loss'], val_metrics['accuracy']
        print(f"Val Loss: {v_loss:.4f} | Val Acc: {v_acc:.2%}")
        print(f"LR: {scheduler.get_last_lr()[0]:.6f}")

        # Save best model
        if val_metrics["accuracy"] > best_val_acc:
            best_val_acc = val_metrics["accuracy"]
            best_epoch = epoch + 1
            no_improve = 0

            torch.save({
                "epoch": epoch,
                "model_state_dict": model.state_dict(),
                "optimizer_state_dict": optimizer.state_dict(),
                "val_accuracy": best_val_acc,
            }, output_path / "best.pt")
            print(f"  -> New best model saved! Accuracy: {best_val_acc:.2%}")
        else:
            no_improve += 1

        # Early stopping
        if no_improve >= args.patience:
            print(
                f"\nEarly stopping at epoch {epoch + 1} "
                f"(no improvement for {args.patience} epochs)"
            )
            break

    # Save final model
    model.save(output_path / "final_model.pt")

    # Save history
    with open(output_path / "history.json", "w") as f:
        json.dump(history, f, indent=2)

    print("\n" + "=" * 70)
    print("TRAINING COMPLETE")
    print("=" * 70)
    print(f"\nBest validation accuracy: {best_val_acc:.2%} at epoch {best_epoch}")
    print(f"Checkpoints saved to: {output_path}")

    # Test plots
    if test_dataset:
        print("\n" + "-" * 70)
        print("TEST SET EVALUATION")
        print("-" * 70)

        test_loader = DataLoader(
            test_dataset,
            batch_size=args.batch_size,
            shuffle=False,
            num_workers=0,
        )

        # Load best model
        checkpoint = torch.load(output_path / "best.pt", map_location=device, weights_only=True)
        model.load_state_dict(checkpoint["model_state_dict"])

        test_metrics = evaluate(model, test_loader, criterion, device, use_features=use_features)

        print(f"\nTest Loss: {test_metrics['loss']:.4f}")
        print(f"Test Accuracy: {test_metrics['accuracy']:.2%}")

        # Per-class accuracy
        model.eval()
        class_correct = {label: 0 for label in class_labels}
        class_total = {label: 0 for label in class_labels}

        with torch.no_grad():
            for batch in test_loader:
                sequence = batch["sequence"].to(device)
                labels = batch["label"].to(device)

                if use_features and "features" in batch:
                    features = batch["features"].to(device)
                    outputs = model(sequence, features)
                else:
                    outputs = model(sequence)

                _, predicted = outputs.max(1)

                for i in range(labels.size(0)):
                    label = class_labels[labels[i].item()]
                    class_total[label] += 1
                    if predicted[i].eq(labels[i]):
                        class_correct[label] += 1

        print("\nPer-class accuracy (top 10):")
        class_stats = [
            (label, class_correct[label] / max(class_total[label], 1), class_total[label])
            for label in class_labels if class_total[label] > 0
        ]
        sorted_classes = sorted(class_stats, key=lambda x: -x[2])[:10]

        for label, acc, count in sorted_classes:
            print(f"  {label}: {acc:.2%} ({class_correct[label]}/{count})")

    return 0


if __name__ == "__main__":
    sys.exit(main())
