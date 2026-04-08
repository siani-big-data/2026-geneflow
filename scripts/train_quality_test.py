#!/usr/bin/env python3
"""
Test training script with synthetic data.

This script tests the training pipeline using synthetic chromatogram-like data.
For production, use real .ab1 trace files.

Usage:
    uv run python scripts/train_quality_test.py
    uv run python scripts/train_quality_test.py --epochs 20 --batch-size 16
"""

import argparse
import sys
from pathlib import Path

import numpy as np
import torch
from torch.utils.data import DataLoader, Dataset, random_split

# Add src to path
sys.path.insert(0, str(Path(__file__).parent.parent))

from src.ml.models.quality import QualityLoss, QualityPredictor, QualityPredictorConfig
from src.ml.training import (
    DetailedProgressLogger,
    EarlyStopping,
    ModelCheckpoint,
    Trainer,
    TrainingConfig,
)


class SyntheticQualityDataset(Dataset):
    """Synthetic dataset that simulates chromatogram signals with quality_enhanced scores.

    Generates fake chromatogram signals where quality_enhanced correlates with signal clarity:
    - High quality_enhanced: one dominant channel, others low
    - Low quality_enhanced: noisy signals, no clear dominant channel
    """

    def __init__(self, num_samples: int = 1000, seq_length: int = 500, seed: int = 42):
        self.num_samples = num_samples
        self.seq_length = seq_length

        np.random.seed(seed)

        # Pre-generate all data
        self.signals = []
        self.qualities = []

        for _ in range(num_samples):
            signals, quality = self._generate_sample()
            self.signals.append(signals)
            self.qualities.append(quality)

    def _generate_sample(self):
        """Generate a single synthetic sample."""
        signals = np.zeros((4, self.seq_length), dtype=np.float32)
        quality = np.zeros(self.seq_length, dtype=np.float32)

        for pos in range(self.seq_length):
            # Random quality_enhanced level for this position (0-60 Phred scale)
            q = np.random.uniform(5, 55)
            quality[pos] = q

            # Convert quality_enhanced to signal clarity
            # Higher quality_enhanced = cleaner signal (one dominant channel)
            clarity = q / 60.0  # 0 to ~1

            # Choose dominant nucleotide
            dominant = np.random.randint(0, 4)

            # Generate signals based on clarity
            noise_level = (1 - clarity) * 0.3

            for channel in range(4):
                if channel == dominant:
                    # Dominant channel: high signal
                    signals[channel, pos] = clarity * 0.8 + np.random.normal(0, 0.1)
                else:
                    # Other channels: noise
                    signals[channel, pos] = np.random.normal(0, noise_level)

            # Normalize signals to sum to ~1
            signals[:, pos] = np.clip(signals[:, pos], 0, 1)
            total = signals[:, pos].sum()
            if total > 0:
                signals[:, pos] /= total

        return signals, quality

    def __len__(self):
        return self.num_samples

    def __getitem__(self, idx):
        return {
            "signals": torch.tensor(self.signals[idx], dtype=torch.float32),
            "quality_enhanced": torch.tensor(self.qualities[idx], dtype=torch.float32),
        }


def main():
    parser = argparse.ArgumentParser(description="Test Quality Predictor Training")
    parser.add_argument("--epochs", type=int, default=30, help="Number of epochs")
    parser.add_argument("--batch-size", type=int, default=32, help="Batch size")
    parser.add_argument("--lr", type=float, default=1e-3, help="Learning rate")
    parser.add_argument("--samples", type=int, default=2000, help="Number of synthetic samples")
    parser.add_argument("--seq-length", type=int, default=300, help="Sequence length")
    parser.add_argument("--output-dir", type=str, default="checkpoints/quality_test")
    parser.add_argument("--seed", type=int, default=42)

    args = parser.parse_args()

    # Set seed
    torch.manual_seed(args.seed)
    np.random.seed(args.seed)

    print("=" * 60)
    print("QUALITY PREDICTOR - TEST TRAINING")
    print("=" * 60)
    print("\nUsing synthetic data to test training pipeline")
    print(f"Samples: {args.samples}")
    print(f"Sequence length: {args.seq_length}")
    print(f"Epochs: {args.epochs}")
    print(f"Batch size: {args.batch_size}")
    print(f"Learning rate: {args.lr}")

    # Create dataset
    print("\nGenerating synthetic dataset...")
    dataset = SyntheticQualityDataset(
        num_samples=args.samples,
        seq_length=args.seq_length,
        seed=args.seed,
    )

    # Split
    val_size = int(len(dataset) * 0.15)
    train_size = len(dataset) - val_size
    train_dataset, val_dataset = random_split(
        dataset,
        [train_size, val_size],
        generator=torch.Generator().manual_seed(args.seed),
    )

    print(f"Train samples: {len(train_dataset)}")
    print(f"Val samples: {len(val_dataset)}")

    # Data loaders
    train_loader = DataLoader(
        train_dataset,
        batch_size=args.batch_size,
        shuffle=True,
        num_workers=0,  # Use 0 for Windows compatibility
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
    model_config = QualityPredictorConfig(
        hidden_channels=32,  # Smaller for test
        num_layers=4,
        dropout=0.1,
    )

    model = QualityPredictor(model_config)

    print(f"\nModel: {model.__class__.__name__}")
    print(f"Parameters: {model.count_parameters():,}")
    print(f"Device: {model.device}")

    # Training config
    output_path = Path(args.output_dir)
    output_path.mkdir(parents=True, exist_ok=True)

    training_config = TrainingConfig(
        epochs=args.epochs,
        batch_size=args.batch_size,
        learning_rate=args.lr,
        weight_decay=0.01,
        scheduler="cosine",
        log_every=max(1, len(train_loader) // 5),  # Log 5 times per epoch
        eval_every=1,
        save_dir=str(output_path),
        mixed_precision=torch.cuda.is_available(),
        accuracy_tolerance=5.0,  # Within 5 Phred units = correct
        generate_plots=True,
        generate_report=True,
    )

    # Loss and callbacks
    loss_fn = QualityLoss(mse_weight=1.0, smooth_weight=0.1)

    callbacks = [
        DetailedProgressLogger(),
        EarlyStopping(patience=10, metric="loss", mode="min"),
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
    print("\n" + "=" * 60)
    print("STARTING TRAINING")
    print("=" * 60)

    trainer.train(train_loader, val_loader)

    # Save final model
    model.save(output_path / "final_model.pt")

    print("\n" + "=" * 60)
    print("TRAINING COMPLETE")
    print("=" * 60)
    print(f"\nCheckpoints saved to: {output_path}")
    print("  - best.pt (best validation loss)")
    print("  - last.pt (last epoch)")
    print("  - final_model.pt (model weights only)")
    print("  - training_report.json")
    print("  - training_summary.png")

    return 0


if __name__ == "__main__":
    sys.exit(main())
