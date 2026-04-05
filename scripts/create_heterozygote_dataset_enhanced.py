#!/usr/bin/env python3
"""
Create enhanced heterozygote_training dataset with explicit features.

The key insight: heterozygous positions have TWO high signal channels (ratio ~0.72),
while homozygous positions have ONE dominant channel (ratio ~0.50).

Features (8 channels):
- [0-3] ACGT normalized signals
- [4]   Max signal value
- [5]   Second max signal value
- [6]   Ratio of second/max (KEY FEATURE - r=0.7 correlation with het status)
- [7]   Difference between top 2

Usage:
    uv run python scripts/create_heterozygote_dataset_enhanced.py
    uv run python scripts/create_heterozygote_dataset_enhanced.py --target-samples 40000
"""

import argparse
import json
from pathlib import Path

import numpy as np
from Bio import SeqIO


# IUPAC ambiguity codes for heterozygotes
HET_CODES = set("MRWSYK")


def extract_ab1_signals(ab1_path: Path) -> dict | None:
    """Extract 4-channel chromatogram signals from AB1 file."""
    try:
        record = SeqIO.read(ab1_path, "abi")
        sequence = str(record.seq)
        quality = record.letter_annotations.get("phred_quality", [])

        abif = record.annotations.get("abif_raw", {})

        # Get filter wheel order
        fwo = "GATC"
        if "FWO_1" in abif:
            fwo_val = abif["FWO_1"]
            fwo = fwo_val.decode() if isinstance(fwo_val, bytes) else str(fwo_val)

        # Get processed trace data (DATA 9-12)
        trace_data = {}
        for i, base in enumerate(fwo):
            key = f"DATA{9 + i}"
            if key in abif:
                trace_data[base] = np.array(abif[key], dtype=np.float32)

        if len(trace_data) != 4:
            return None

        # Get peak locations
        ploc = None
        if "PLOC1" in abif:
            ploc = np.array(abif["PLOC1"], dtype=np.int32)

        if ploc is None or len(ploc) != len(sequence):
            return None

        # Sample trace data at peak locations
        seq_len = len(sequence)
        signals = np.zeros((4, seq_len), dtype=np.float32)

        for i, base in enumerate("ACGT"):
            if base in trace_data:
                trace = trace_data[base]
                for j, pos in enumerate(ploc):
                    if pos < len(trace):
                        signals[i, j] = trace[pos]

        # Normalize to sum to 1
        signal_sum = signals.sum(axis=0, keepdims=True)
        signal_sum = np.maximum(signal_sum, 1e-6)
        signals = signals / signal_sum

        # Create labels from IUPAC codes
        labels = np.zeros(seq_len, dtype=np.int64)
        for i, base in enumerate(sequence):
            if base.upper() in HET_CODES:
                labels[i] = 1
            elif base.upper() in "ACGT":
                labels[i] = 0
            else:
                labels[i] = -1  # Ignore N and other

        return {
            "signals": signals,
            "labels": labels,
            "sequence": sequence,
            "quality": np.array(quality, dtype=np.int32) if quality else np.zeros(seq_len, dtype=np.int32),
        }

    except Exception as e:
        return None


def compute_enhanced_features(signals: np.ndarray) -> np.ndarray:
    """
    Compute enhanced features from raw ACGT signals.

    Input: signals (4, seq_len) - normalized ACGT
    Output: features (8, seq_len) - ACGT + derived features
    """
    seq_len = signals.shape[1]
    features = np.zeros((8, seq_len), dtype=np.float32)

    # Copy original signals
    features[:4] = signals

    # Compute per-position features
    for i in range(seq_len):
        channel_vals = signals[:, i]
        sorted_vals = np.sort(channel_vals)[::-1]  # Descending

        max_val = sorted_vals[0]
        second_val = sorted_vals[1]

        features[4, i] = max_val
        features[5, i] = second_val
        features[6, i] = second_val / (max_val + 1e-6)  # KEY: ratio
        features[7, i] = max_val - second_val  # Difference

    return features


class ChromatogramAugmenter:
    """Augmentation strategies for chromatogram data."""

    def __init__(self, window_size: int = 500):
        self.window_size = window_size

    def extract_windows(self, signals: np.ndarray, labels: np.ndarray, stride: int = 100) -> list:
        """Extract sliding windows from a trace."""
        seq_len = signals.shape[1]
        windows = []

        if seq_len <= self.window_size:
            # Pad short sequences
            pad = self.window_size - seq_len
            sig_padded = np.pad(signals, ((0, 0), (0, pad)), constant_values=0.25)
            lab_padded = np.pad(labels, (0, pad), constant_values=-1)
            windows.append((sig_padded, lab_padded))
        else:
            # Sliding windows
            for start in range(0, seq_len - self.window_size + 1, stride):
                end = start + self.window_size
                windows.append((
                    signals[:, start:end].copy(),
                    labels[start:end].copy(),
                ))

        return windows

    def add_noise(self, signals: np.ndarray, noise_level: float = 0.05) -> np.ndarray:
        """Add Gaussian noise to signals."""
        noise = np.random.normal(0, noise_level, signals.shape).astype(np.float32)
        noisy = signals + noise
        # Re-normalize
        noisy = np.maximum(noisy, 0)
        noisy = noisy / np.maximum(noisy.sum(axis=0, keepdims=True), 1e-6)
        return noisy

    def scale_channels(self, signals: np.ndarray, scale_range: tuple = (0.8, 1.2)) -> np.ndarray:
        """Randomly scale individual channels."""
        scales = np.random.uniform(scale_range[0], scale_range[1], (4, 1)).astype(np.float32)
        scaled = signals * scales
        # Re-normalize
        scaled = scaled / np.maximum(scaled.sum(axis=0, keepdims=True), 1e-6)
        return scaled

    def simulate_quality_degradation(self, signals: np.ndarray, degradation: float = 0.1) -> np.ndarray:
        """Simulate quality degradation (more uniform signals)."""
        uniform = np.ones_like(signals) * 0.25
        degraded = (1 - degradation) * signals + degradation * uniform
        return degraded

    def augment(self, signals: np.ndarray, labels: np.ndarray) -> tuple:
        """Apply random augmentation to the 4 raw channels."""
        aug_signals = signals.copy()

        # Random combination of augmentations
        if np.random.random() < 0.7:
            aug_signals = self.add_noise(aug_signals, np.random.uniform(0.02, 0.08))

        if np.random.random() < 0.5:
            aug_signals = self.scale_channels(aug_signals)

        if np.random.random() < 0.3:
            aug_signals = self.simulate_quality_degradation(aug_signals, np.random.uniform(0.05, 0.15))

        return aug_signals, labels


def create_enhanced_dataset(
    ab1_dir: Path,
    output_dir: Path,
    target_samples: int = 40000,
    window_size: int = 500,
    train_ratio: float = 0.8,
) -> dict:
    """Create enhanced dataset with explicit features."""

    # Get AB1 files (deduplicated for case-insensitive filesystems like Windows)
    ab1_files = list({str(f.resolve()).lower(): f for f in ab1_dir.glob("*.ab1")}.values())

    if not ab1_files:
        return {"error": "No AB1 files found"}

    print(f"Found {len(ab1_files)} AB1 files")

    # Load all AB1 data
    all_data = []
    total_het = 0
    total_pos = 0

    for f in ab1_files:
        data = extract_ab1_signals(f)
        if data is not None:
            all_data.append(data)
            total_het += (data["labels"] == 1).sum()
            total_pos += len(data["labels"])

    print(f"Loaded {len(all_data)} valid AB1 files")
    print(f"Original data: {total_pos} positions, {total_het} heterozygotes ({100*total_het/total_pos:.1f}%)")

    # Verify feature discrimination
    print("\nVerifying feature discrimination...")
    het_ratios = []
    hom_ratios = []

    for data in all_data[:20]:  # Sample first 20 files
        signals = data["signals"]
        labels = data["labels"]

        for i in range(len(labels)):
            if labels[i] < 0:
                continue
            vals = np.sort(signals[:, i])[::-1]
            ratio = vals[1] / (vals[0] + 1e-6)

            if labels[i] == 1:
                het_ratios.append(ratio)
            else:
                hom_ratios.append(ratio)

    print(f"  Het positions: second/max ratio = {np.mean(het_ratios):.3f} +/- {np.std(het_ratios):.3f}")
    print(f"  Hom positions: second/max ratio = {np.mean(hom_ratios):.3f} +/- {np.std(hom_ratios):.3f}")
    print(f"  Ratio difference: {np.mean(het_ratios) - np.mean(hom_ratios):.3f} (should be positive)")

    # Split at FILE level
    np.random.shuffle(all_data)
    split_idx = int(len(all_data) * train_ratio)
    train_files = all_data[:split_idx]
    val_files = all_data[split_idx:]

    print(f"\nFile-level split: {len(train_files)} train files, {len(val_files)} val files")

    augmenter = ChromatogramAugmenter(window_size=window_size)
    train_target = int(target_samples * train_ratio)
    val_target = target_samples - train_target

    train_samples_per_file = train_target // len(train_files) + 1
    val_samples_per_file = val_target // len(val_files) + 1

    print(f"Target: {train_target} train, {val_target} val samples")

    def generate_samples(file_data_list, samples_per_file, max_samples, augment: bool = True):
        """Generate samples with enhanced features."""
        samples = []
        for data in file_data_list:
            signals = data["signals"]  # (4, seq_len) - raw ACGT
            labels = data["labels"]

            # Extract windows from raw signals
            windows = augmenter.extract_windows(signals, labels, stride=50)

            file_samples = 0
            while file_samples < samples_per_file and len(samples) < max_samples:
                win_signals, win_labels = windows[np.random.randint(len(windows))]

                if augment:
                    aug_signals, aug_labels = augmenter.augment(win_signals, win_labels)
                else:
                    aug_signals, aug_labels = win_signals.copy(), win_labels.copy()

                # Compute enhanced features AFTER augmentation
                features = compute_enhanced_features(aug_signals)

                samples.append({"features": features, "labels": aug_labels})
                file_samples += 1

            if len(samples) >= max_samples:
                break

        return samples

    print("\nGenerating train samples (with augmentation)...")
    train_samples = generate_samples(train_files, train_samples_per_file, train_target, augment=True)

    print("Generating val samples (no augmentation)...")
    val_samples = generate_samples(val_files, val_samples_per_file, val_target, augment=False)

    print(f"\nGenerated {len(train_samples)} train, {len(val_samples)} val samples")

    # Compute statistics
    all_samples = train_samples + val_samples
    total_positions = sum(s["labels"].shape[0] for s in all_samples)
    het_positions = sum((s["labels"] == 1).sum() for s in all_samples)
    het_ratio = 100 * het_positions / total_positions

    print(f"Total positions: {total_positions}")
    print(f"Heterozygote positions: {het_positions} ({het_ratio:.1f}%)")

    # Shuffle
    np.random.shuffle(train_samples)
    np.random.shuffle(val_samples)

    # Save
    train_dir = output_dir / "train"
    val_dir = output_dir / "val"
    train_dir.mkdir(parents=True, exist_ok=True)
    val_dir.mkdir(parents=True, exist_ok=True)

    print(f"\nSaving {len(train_samples)} train, {len(val_samples)} val samples...")

    for i, sample in enumerate(train_samples):
        np.savez_compressed(
            train_dir / f"sample_{i:06d}.npz",
            features=sample["features"].astype(np.float32),
            labels=sample["labels"].astype(np.int64),
        )

    for i, sample in enumerate(val_samples):
        np.savez_compressed(
            val_dir / f"sample_{i:06d}.npz",
            features=sample["features"].astype(np.float32),
            labels=sample["labels"].astype(np.int64),
        )

    # Save stats
    stats = {
        "source_files": len(all_data),
        "train_files": len(train_files),
        "val_files": len(val_files),
        "total_samples": len(train_samples) + len(val_samples),
        "train_samples": len(train_samples),
        "val_samples": len(val_samples),
        "total_positions": int(total_positions),
        "het_positions": int(het_positions),
        "het_ratio": float(het_ratio),
        "window_size": window_size,
        "n_features": 8,
        "features": ["A", "C", "G", "T", "max_val", "second_val", "ratio", "diff"],
        "split_method": "file-level (no leakage)",
    }

    with open(output_dir / "stats.json", "w") as f:
        json.dump(stats, f, indent=2)

    return stats


def main():
    parser = argparse.ArgumentParser(description="Create enhanced heterozygote_training dataset")

    parser.add_argument("--ab1-dir", type=str, default="datalake/ab1",
                        help="Directory with AB1 files")
    parser.add_argument("--output-dir", type=str, default="datalake/datasets/heterozygote_enhanced",
                        help="Output directory")
    parser.add_argument("--target-samples", type=int, default=40000,
                        help="Target number of samples")
    parser.add_argument("--window-size", type=int, default=500,
                        help="Window size for samples")

    args = parser.parse_args()

    print("=" * 70)
    print("ENHANCED HETEROZYGOTE DATASET")
    print("=" * 70)
    print("Features (8 channels):")
    print("  [0-3] ACGT normalized signals")
    print("  [4]   Max signal value")
    print("  [5]   Second max signal value")
    print("  [6]   Ratio second/max (KEY FEATURE)")
    print("  [7]   Difference top 2")
    print()

    stats = create_enhanced_dataset(
        ab1_dir=Path(args.ab1_dir),
        output_dir=Path(args.output_dir),
        target_samples=args.target_samples,
        window_size=args.window_size,
    )

    print("\n" + "=" * 70)
    print("COMPLETE")
    print("=" * 70)

    if "error" not in stats:
        print(f"Dataset: {args.output_dir}")
        print(f"Features: {stats['n_features']} channels")
        print(f"Train: {stats['train_samples']} samples")
        print(f"Val: {stats['val_samples']} samples")
        print(f"Het ratio: {stats['het_ratio']:.1f}%")


if __name__ == "__main__":
    main()
