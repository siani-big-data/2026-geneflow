"""Training script for Quality Predictor."""

import argparse
from pathlib import Path

import torch
from torch.utils.data import DataLoader, random_split

from ...datasets.trace_dataset import TraceDataset
from ...training import (
    EarlyStopping,
    ModelCheckpoint,
    Trainer,
    TrainingConfig,
)
from ...training.callbacks import ProgressLogger
from .model import QualityLoss, QualityPredictor, QualityPredictorConfig


def train_quality_predictor(
    data_dir: str,
    output_dir: str = "checkpoints/quality_predictor",
    epochs: int = 100,
    batch_size: int = 32,
    learning_rate: float = 1e-4,
    hidden_channels: int = 64,
    num_layers: int = 6,
    val_split: float = 0.1,
    seed: int = 42,
):
    """Train the quality predictor model.

    Args:
        data_dir: Directory containing .ab1 trace files
        output_dir: Directory to save checkpoints
        epochs: Number of training epochs
        batch_size: Batch size
        learning_rate: Learning rate
        hidden_channels: Hidden channel size
        num_layers: Number of conv layers
        val_split: Validation split ratio
        seed: Random seed
    """
    # Set seed
    torch.manual_seed(seed)

    # Create output directory
    output_path = Path(output_dir)
    output_path.mkdir(parents=True, exist_ok=True)

    print(f"Loading data from {data_dir}...")

    # Create dataset
    dataset = TraceDataset(
        trace_dir=data_dir,
        max_length=1000,
        min_length=100,
        normalize=True,
    )

    print(f"Found {len(dataset)} trace files")

    if len(dataset) == 0:
        raise ValueError(f"No trace files found in {data_dir}")

    # Split dataset
    val_size = int(len(dataset) * val_split)
    train_size = len(dataset) - val_size

    train_dataset, val_dataset = random_split(
        dataset,
        [train_size, val_size],
        generator=torch.Generator().manual_seed(seed),
    )

    print(f"Train: {len(train_dataset)}, Val: {len(val_dataset)}")

    # Create data loaders
    train_loader = DataLoader(
        train_dataset,
        batch_size=batch_size,
        shuffle=True,
        num_workers=4,
        pin_memory=True,
    )

    val_loader = DataLoader(
        val_dataset,
        batch_size=batch_size,
        shuffle=False,
        num_workers=4,
        pin_memory=True,
    )

    # Create model
    model_config = QualityPredictorConfig(
        hidden_channels=hidden_channels,
        num_layers=num_layers,
        dropout=0.1,
        learning_rate=learning_rate,
        batch_size=batch_size,
        max_epochs=epochs,
    )

    model = QualityPredictor(model_config)
    print(f"Model parameters: {model.count_parameters():,}")

    # Create trainer
    training_config = TrainingConfig(
        epochs=epochs,
        batch_size=batch_size,
        learning_rate=learning_rate,
        weight_decay=0.01,
        scheduler="cosine",
        log_every=10,
        eval_every=1,
        save_dir=output_dir,
        mixed_precision=torch.cuda.is_available(),
    )

    loss_fn = QualityLoss(mse_weight=1.0, smooth_weight=0.1)

    callbacks = [
        ProgressLogger(),
        EarlyStopping(patience=15, metric="loss", mode="min"),
        ModelCheckpoint(
            save_dir=output_path,
            save_best=True,
            save_last=True,
            metric="loss",
            mode="min",
        ),
    ]

    trainer = Trainer(
        model=model,
        config=training_config,
        loss_fn=loss_fn,
        callbacks=callbacks,
    )

    # Train
    print("\nStarting training...")
    print(f"Device: {model.device}")

    summary = trainer.train(train_loader, val_loader)

    print("\nTraining completed!")
    print(f"Best validation loss: {summary.get('val/loss', {}).get('best', 'N/A'):.4f}")

    # Save final model
    model.save(output_path / "final_model.pt")
    print(f"Model saved to {output_path / 'final_model.pt'}")

    return summary


def main():
    parser = argparse.ArgumentParser(description="Train Quality Predictor")
    parser.add_argument("--data-dir", type=str, required=True, help="Directory with .ab1 files")
    parser.add_argument("--output-dir", type=str, default="checkpoints/quality_predictor")
    parser.add_argument("--epochs", type=int, default=100)
    parser.add_argument("--batch-size", type=int, default=32)
    parser.add_argument("--lr", type=float, default=1e-4)
    parser.add_argument("--hidden-channels", type=int, default=64)
    parser.add_argument("--num-layers", type=int, default=6)
    parser.add_argument("--val-split", type=float, default=0.1)
    parser.add_argument("--seed", type=int, default=42)

    args = parser.parse_args()

    train_quality_predictor(
        data_dir=args.data_dir,
        output_dir=args.output_dir,
        epochs=args.epochs,
        batch_size=args.batch_size,
        learning_rate=args.lr,
        hidden_channels=args.hidden_channels,
        num_layers=args.num_layers,
        val_split=args.val_split,
        seed=args.seed,
    )


if __name__ == "__main__":
    main()
