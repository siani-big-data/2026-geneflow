#!/usr/bin/env python3
"""
Train QualityPredictor using FASTQ data.

FASTQ provides sequence + Phred quality_enhanced scores.
The model learns to predict quality_enhanced from sequence context.

Usage:
    # Download FASTQ first
    uv run python datalake/pipelines/download/sra_fastq.py

    # Then train
    uv run python scripts/train_quality_fastq.py --data .cache/datalake/raw/fastq --epochs 50
"""

import argparse
import json
import sys
from pathlib import Path

import torch
from torch.optim import AdamW
from torch.optim.lr_scheduler import CosineAnnealingLR
from torch.utils.data import DataLoader, random_split

sys.path.insert(0, str(Path(__file__).parent.parent))

from src.ml.datasets.fastq_dataset import FastqDataset
from src.ml.models.quality import (
    EnhancedQualityConfig,
    EnhancedQualityPredictor,
    QualityLoss,
    QualityPredictor,
    QualityPredictorConfig,
)


def train_epoch(model, loader, optimizer, criterion, device, use_aux=False, scaler=None):
    """Train for one epoch."""
    model.train()
    total_loss = 0.0
    total_mae = 0.0
    total_samples = 0

    for batch in loader:
        optimizer.zero_grad()

        signals = batch["signals"].to(device)
        quality = batch["quality_enhanced"].to(device)
        mask = batch["mask"].to(device)

        if scaler:
            with torch.amp.autocast("cuda"):
                if use_aux and "aux_features" in batch:
                    aux = batch["aux_features"].to(device)
                    outputs = model(signals, aux)
                else:
                    outputs = model(signals)
                loss = criterion(outputs, quality, mask)

            scaler.scale(loss).backward()
            scaler.step(optimizer)
            scaler.update()
        else:
            if use_aux and "aux_features" in batch:
                aux = batch["aux_features"].to(device)
                outputs = model(signals, aux)
            else:
                outputs = model(signals)
            loss = criterion(outputs, quality, mask)
            loss.backward()
            optimizer.step()

        # Compute MAE
        with torch.no_grad():
            pred_masked = outputs[mask]
            true_masked = quality[mask]
            mae = torch.abs(pred_masked - true_masked).mean()

        total_loss += loss.item() * signals.size(0)
        total_mae += mae.item() * mask.sum().item()
        total_samples += signals.size(0)

    return {
        "loss": total_loss / total_samples,
        "mae": total_mae / total_samples,
    }


def evaluate(model, loader, criterion, device, use_aux=False):
    """Evaluate model."""
    model.eval()
    total_loss = 0.0
    total_mae = 0.0
    total_samples = 0
    total_positions = 0

    with torch.no_grad():
        for batch in loader:
            signals = batch["signals"].to(device)
            quality = batch["quality_enhanced"].to(device)
            mask = batch["mask"].to(device)

            if use_aux and "aux_features" in batch:
                aux = batch["aux_features"].to(device)
                outputs = model(signals, aux)
            else:
                outputs = model(signals)

            loss = criterion(outputs, quality, mask)

            # MAE
            pred_masked = outputs[mask]
            true_masked = quality[mask]
            mae = torch.abs(pred_masked - true_masked).mean()

            total_loss += loss.item() * signals.size(0)
            total_mae += mae.item() * mask.sum().item()
            total_samples += signals.size(0)
            total_positions += mask.sum().item()

    return {
        "loss": total_loss / total_samples,
        "mae": total_mae / total_positions if total_positions > 0 else 0,
    }


def main():
    parser = argparse.ArgumentParser(description="Train QualityPredictor on FASTQ")
    parser.add_argument(
        "--data",
        type=str,
        default=".cache/datalake/raw/fastq",
        help="Directory with FASTQ files",
    )
    parser.add_argument("--output-dir", type=str, default="checkpoints/quality_fastq")
    parser.add_argument("--model", type=str, default="base", choices=["base", "enhanced"])
    parser.add_argument("--epochs", type=int, default=50)
    parser.add_argument("--batch-size", type=int, default=32)
    parser.add_argument("--lr", type=float, default=1e-3)
    parser.add_argument("--max-length", type=int, default=300)
    parser.add_argument("--val-split", type=float, default=0.15)
    parser.add_argument("--patience", type=int, default=10)
    parser.add_argument("--seed", type=int, default=42)

    args = parser.parse_args()

    torch.manual_seed(args.seed)
    if torch.cuda.is_available():
        torch.cuda.manual_seed(args.seed)

    data_dir = Path(args.data)
    output_path = Path(args.output_dir)
    output_path.mkdir(parents=True, exist_ok=True)

    print("=" * 70)
    print("QUALITY PREDICTOR TRAINING (FASTQ)")
    print("=" * 70)

    # Check for data
    if not data_dir.exists():
        print(f"\nERROR: Data directory not found: {data_dir}")
        print("Run first: uv run python datalake/pipelines/download/sra_fastq.py")
        return 1

    # Create dataset
    print("\n" + "-" * 70)
    print("LOADING DATASET")
    print("-" * 70)

    use_aux = args.model == "enhanced"

    dataset = FastqDataset(
        fastq_dir=data_dir,
        max_length=args.max_length,
        min_length=50,
        include_aux_features=use_aux,
    )

    if len(dataset) == 0:
        print("ERROR: No FASTQ records found!")
        return 1

    stats = dataset.compute_statistics()
    print("\nDataset statistics:")
    print(f"  Records: {stats['num_records']}")
    print(f"  Total bases: {stats['total_bases']:,}")
    print(f"  Mean length: {stats['mean_length']:.1f}")
    print(f"  Mean quality_enhanced: {stats['mean_quality']:.1f}")
    print(f"  Std quality_enhanced: {stats['std_quality']:.1f}")
    print(f"  Quality range: [{stats['min_quality']:.0f}, {stats['max_quality']:.0f}]")

    # Split
    val_size = int(len(dataset) * args.val_split)
    train_size = len(dataset) - val_size

    train_dataset, val_dataset = random_split(
        dataset,
        [train_size, val_size],
        generator=torch.Generator().manual_seed(args.seed),
    )

    print(f"\nTrain: {len(train_dataset)} | Val: {len(val_dataset)}")

    # Loaders
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

    # Model
    print("\n" + "-" * 70)
    print("MODEL CONFIGURATION")
    print("-" * 70)

    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

    if use_aux:
        config = EnhancedQualityConfig(
            input_channels=4,
            aux_channels=8,
            hidden_channels=64,
            num_layers=6,
            max_quality=float(stats['max_quality']),
        )
        model = EnhancedQualityPredictor(config)
        print("Model: EnhancedQualityPredictor")
        print("Input: 4 sequence channels + 8 auxiliary")
    else:
        config = QualityPredictorConfig(
            input_channels=4,
            hidden_channels=64,
            num_layers=6,
            max_quality=float(stats['max_quality']),
        )
        model = QualityPredictor(config)
        print("Model: QualityPredictor")
        print("Input: 4 sequence channels (one-hot)")

    model = model.to(device)
    print(f"Parameters: {sum(p.numel() for p in model.parameters() if p.requires_grad):,}")
    print(f"Device: {device}")

    # Loss, optimizer, scheduler
    criterion = QualityLoss(mse_weight=1.0, smooth_weight=0.1)
    optimizer = AdamW(model.parameters(), lr=args.lr, weight_decay=0.01)
    scheduler = CosineAnnealingLR(optimizer, T_max=args.epochs, eta_min=args.lr * 0.01)

    scaler = torch.amp.GradScaler("cuda") if torch.cuda.is_available() else None

    # Training
    print("\n" + "=" * 70)
    print("STARTING TRAINING")
    print("=" * 70)

    history = {"train_loss": [], "train_mae": [], "val_loss": [], "val_mae": []}
    best_val_mae = float('inf')
    best_epoch = 0
    no_improve = 0

    for epoch in range(args.epochs):
        print(f"\nEpoch {epoch + 1}/{args.epochs}")
        print("-" * 40)

        train_metrics = train_epoch(
            model, train_loader, optimizer, criterion, device,
            use_aux=use_aux, scaler=scaler
        )
        val_metrics = evaluate(model, val_loader, criterion, device, use_aux=use_aux)

        scheduler.step()

        history["train_loss"].append(train_metrics["loss"])
        history["train_mae"].append(train_metrics["mae"])
        history["val_loss"].append(val_metrics["loss"])
        history["val_mae"].append(val_metrics["mae"])

        print(f"Train Loss: {train_metrics['loss']:.4f} | Train MAE: {train_metrics['mae']:.2f}")
        print(f"Val Loss: {val_metrics['loss']:.4f} | Val MAE: {val_metrics['mae']:.2f}")

        if val_metrics["mae"] < best_val_mae:
            best_val_mae = val_metrics["mae"]
            best_epoch = epoch + 1
            no_improve = 0

            torch.save({
                "epoch": epoch,
                "model_state_dict": model.state_dict(),
                "optimizer_state_dict": optimizer.state_dict(),
                "val_mae": best_val_mae,
            }, output_path / "best.pt")
            print(f"  -> New best! MAE: {best_val_mae:.2f}")
        else:
            no_improve += 1

        if no_improve >= args.patience:
            print(f"\nEarly stopping at epoch {epoch + 1}")
            break

    # Save final
    torch.save(model.state_dict(), output_path / "final.pt")

    with open(output_path / "history.json", "w") as f:
        json.dump(history, f, indent=2)

    print("\n" + "=" * 70)
    print("TRAINING COMPLETE")
    print("=" * 70)
    print(f"\nBest validation MAE: {best_val_mae:.2f} at epoch {best_epoch}")
    print(f"Checkpoints saved to: {output_path}")

    # Test predictions
    print("\n" + "-" * 70)
    print("SAMPLE PREDICTIONS")
    print("-" * 70)

    model.eval()
    sample_batch = next(iter(val_loader))
    signals = sample_batch["signals"][:3].to(device)
    quality = sample_batch["quality_enhanced"][:3]
    mask = sample_batch["mask"][:3]

    with torch.no_grad():
        if use_aux:
            aux = sample_batch["aux_features"][:3].to(device)
            preds = model(signals, aux)
        else:
            preds = model(signals)

    for i in range(3):
        m = mask[i]
        true_q = quality[i][m].numpy()
        pred_q = preds[i][m].cpu().numpy()

        mae = abs(true_q - pred_q).mean()
        print(f"\nSample {i + 1}:")
        print(f"  Length: {m.sum().item()}")
        print(f"  True quality_enhanced (first 10): {true_q[:10].round(1)}")
        print(f"  Pred quality_enhanced (first 10): {pred_q[:10].round(1)}")
        print(f"  MAE: {mae:.2f}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
