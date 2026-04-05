#!/usr/bin/env python3
"""
Train taxonomy classifier on downloaded sequences.

Usage:
    uv run python scripts/train_taxonomy.py --data-dir datalake/raw
    uv run python scripts/train_taxonomy.py --data-dir datalake/raw --level phylum --epochs 50
"""

import argparse
import sys
from pathlib import Path

import torch
import torch.nn as nn
from torch.utils.data import DataLoader, random_split

# Add src to path
sys.path.insert(0, str(Path(__file__).parent.parent))

from src.ml.datasets.taxonomy_dataset import TaxonomyDataset
from src.ml.models.taxonomy import TaxonomyClassifier, TaxonomyClassifierConfig
from src.ml.training import (
    DetailedProgressLogger,
    EarlyStopping,
    ModelCheckpoint,
    Trainer,
    TrainingConfig,
)


def main():
    parser = argparse.ArgumentParser(description="Train Taxonomy Classifier")
    parser.add_argument(
        "--data-dir",
        type=str,
        default="datalake/raw",
        help="Directory with downloaded sequences",
    )
    parser.add_argument(
        "--level",
        type=str,
        default="kingdom",
        choices=["kingdom", "phylum", "class", "order", "family", "genus", "species"],
        help="Taxonomy level to classify",
    )
    parser.add_argument("--output-dir", type=str, default="checkpoints/taxonomy")
    parser.add_argument("--epochs", type=int, default=50)
    parser.add_argument("--batch-size", type=int, default=32)
    parser.add_argument("--lr", type=float, default=1e-3)
    parser.add_argument("--max-seq-length", type=int, default=2000)
    parser.add_argument("--hidden-channels", type=int, default=128)
    parser.add_argument("--num-layers", type=int, default=4)
    parser.add_argument("--val-split", type=float, default=0.15)
    parser.add_argument("--max-samples-per-class", type=int, default=0)
    parser.add_argument("--seed", type=int, default=42)

    args = parser.parse_args()

    # Set seed
    torch.manual_seed(args.seed)

    print("=" * 70)
    print("TAXONOMY CLASSIFIER TRAINING")
    print("=" * 70)
    print(f"\nData directory: {args.data_dir}")
    print(f"Classification level: {args.level}")
    print(f"Max sequence length: {args.max_seq_length}")
    print(f"Epochs: {args.epochs}")
    print(f"Batch size: {args.batch_size}")
    print(f"Learning rate: {args.lr}")

    # Create dataset
    print("\n" + "-" * 70)
    print("LOADING DATASET")
    print("-" * 70)

    dataset = TaxonomyDataset(
        data_dir=args.data_dir,
        classification_level=args.level,
        max_seq_length=args.max_seq_length,
        max_samples_per_class=args.max_samples_per_class,
        min_seq_length=100,
    )

    if len(dataset) == 0:
        print("ERROR: No samples found!")
        return 1

    print(f"\nTotal samples: {len(dataset)}")
    print(f"Number of classes: {dataset.num_classes}")
    print(f"Class labels: {dataset.class_labels}")

    # Split dataset
    val_size = int(len(dataset) * args.val_split)
    train_size = len(dataset) - val_size

    train_dataset, val_dataset = random_split(
        dataset,
        [train_size, val_size],
        generator=torch.Generator().manual_seed(args.seed),
    )

    print(f"\nTrain samples: {len(train_dataset)}")
    print(f"Val samples: {len(val_dataset)}")

    # Data loaders
    train_loader = DataLoader(
        train_dataset,
        batch_size=args.batch_size,
        shuffle=True,
        num_workers=0,  # Windows compatibility
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

    model_config = TaxonomyClassifierConfig(
        num_classes=dataset.num_classes,
        max_seq_length=args.max_seq_length,
        hidden_channels=args.hidden_channels,
        num_layers=args.num_layers,
        dropout=0.2,
        learning_rate=args.lr,
        batch_size=args.batch_size,
        max_epochs=args.epochs,
        class_labels=dataset.class_labels,
    )

    model = TaxonomyClassifier(model_config)

    print(f"Model: {model.name}")
    print(f"Parameters: {model.count_parameters():,}")
    print(f"Device: {model.device}")
    print(f"Classes: {model_config.class_labels}")

    # Training config
    output_path = Path(args.output_dir) / args.level
    output_path.mkdir(parents=True, exist_ok=True)

    training_config = TrainingConfig(
        epochs=args.epochs,
        batch_size=args.batch_size,
        learning_rate=args.lr,
        weight_decay=0.01,
        scheduler="cosine",
        log_every=max(1, len(train_loader) // 5),
        eval_every=1,
        save_dir=str(output_path),
        mixed_precision=torch.cuda.is_available(),
        task_type="classification",
        generate_plots=True,
        generate_report=True,
        input_key="sequence",  # TaxonomyDataset uses 'sequence'
        target_key="label",    # TaxonomyDataset uses 'label'
    )

    # Loss with class weights for imbalanced data
    class_weights = dataset.get_class_weights().to(model.device)
    print(f"\nClass weights: {class_weights.tolist()}")
    loss_fn = nn.CrossEntropyLoss(weight=class_weights)

    callbacks = [
        DetailedProgressLogger(),
        EarlyStopping(patience=15, metric="loss", mode="min"),
        ModelCheckpoint(
            save_dir=output_path,
            save_best=True,
            save_last=True,
            metric="loss",
            mode="min",
        ),
    ]

    # Create trainer
    trainer = Trainer(
        model=model,
        config=training_config,
        loss_fn=loss_fn,
        callbacks=callbacks,
    )

    # Train
    print("\n" + "=" * 70)
    print("STARTING TRAINING")
    print("=" * 70)

    summary = trainer.train(train_loader, val_loader)

    # Save final model
    model.save(output_path / "final_model.pt")

    print("\n" + "=" * 70)
    print("TRAINING COMPLETE")
    print("=" * 70)
    print(f"\nCheckpoints saved to: {output_path}")
    print("  - best.pt (best validation loss)")
    print("  - last.pt (last epoch)")
    print("  - final_model.pt (model with config)")
    print("  - training_report.json")

    # Test inference
    print("\n" + "-" * 70)
    print("INFERENCE TEST")
    print("-" * 70)

    model.eval()

    # Get a sample from validation set
    sample_batch = next(iter(val_loader))
    sample_seq = sample_batch["sequence"][:3].to(model.device)
    sample_labels = sample_batch["label"][:3]

    with torch.no_grad():
        logits = model(sample_seq)
        probs = torch.softmax(logits, dim=-1)
        preds = probs.argmax(dim=-1)

    print("\nSample predictions:")
    for i in range(min(3, len(sample_labels))):
        true_label = dataset.class_labels[sample_labels[i].item()]
        pred_label = dataset.class_labels[preds[i].item()]
        conf = probs[i, preds[i]].item()
        correct = "✓" if true_label == pred_label else "✗"
        print(f"  {correct} True: {true_label:15} | Pred: {pred_label:15} | Conf: {conf:.2%}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
