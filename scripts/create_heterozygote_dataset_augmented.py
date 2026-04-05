#!/usr/bin/env python3
"""
Create augmented heterozygote_training dataset from real AB1 files.

Takes real AB1 chromatogram data and applies augmentation to generate
a larger training dataset (40k samples).

Augmentation strategies:
1. Sliding windows - extract multiple windows from each trace
2. Signal noise - add realistic noise to chromatogram signals
3. Signal scaling - vary channel intensities
4. Quality variation - simulate quality degradation
5. Base perturbation - slightly modify signal ratios

Usage:
    uv run python scripts/create_heterozygote_dataset_augmented.py
    uv run python scripts/create_heterozygote_dataset_augmented.py --target-samples 40000
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

        # Normalize
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

    def add_baseline_wobble(self, signals: np.ndarray, amplitude: float = 0.02) -> np.ndarray:
        """Add low-frequency baseline variation."""
        seq_len = signals.shape[1]
        freq = np.random.uniform(0.5, 2.0)
        phase = np.random.uniform(0, 2 * np.pi)
        x = np.linspace(0, freq * np.pi, seq_len)
        wobble = amplitude * np.sin(x + phase)

        wobbled = signals + wobble.reshape(1, -1)
        wobbled = np.maximum(wobbled, 0)
        wobbled = wobbled / np.maximum(wobbled.sum(axis=0, keepdims=True), 1e-6)
        return wobbled

    def simulate_quality_degradation(self, signals: np.ndarray, degradation: float = 0.1) -> np.ndarray:
        """Simulate quality degradation (more uniform signals)."""
        uniform = np.ones_like(signals) * 0.25
        degraded = (1 - degradation) * signals + degradation * uniform
        return degraded

    def perturb_heterozygote_ratio(self, signals: np.ndarray, labels: np.ndarray, perturbation: float = 0.1) -> np.ndarray:
        """Slightly vary the ratio at heterozygote_training positions."""
        perturbed = signals.copy()
        het_positions = np.where(labels == 1)[0]

        for pos in het_positions:
            # Find the two highest channels
            channel_vals = perturbed[:, pos]
            sorted_idx = np.argsort(channel_vals)[::-1]
            top2 = sorted_idx[:2]

            # Perturb the ratio between them
            delta = np.random.uniform(-perturbation, perturbation)
            perturbed[top2[0], pos] += delta
            perturbed[top2[1], pos] -= delta

        # Re-normalize
        perturbed = np.maximum(perturbed, 0)
        perturbed = perturbed / np.maximum(perturbed.sum(axis=0, keepdims=True), 1e-6)
        return perturbed

    def augment(self, signals: np.ndarray, labels: np.ndarray) -> tuple:
        """Apply random augmentation."""
        aug_signals = signals.copy()

        # Random combination of augmentations
        if np.random.random() < 0.7:
            aug_signals = self.add_noise(aug_signals, np.random.uniform(0.02, 0.08))

        if np.random.random() < 0.5:
            aug_signals = self.scale_channels(aug_signals)

        if np.random.random() < 0.4:
            aug_signals = self.add_baseline_wobble(aug_signals, np.random.uniform(0.01, 0.03))

        if np.random.random() < 0.3:
            aug_signals = self.simulate_quality_degradation(aug_signals, np.random.uniform(0.05, 0.15))

        if np.random.random() < 0.4:
            aug_signals = self.perturb_heterozygote_ratio(aug_signals, labels, np.random.uniform(0.05, 0.15))

        return aug_signals, labels


def create_augmented_dataset(
    ab1_dir: Path,
    output_dir: Path,
    target_samples: int = 40000,
    window_size: int = 500,
    train_ratio: float = 0.8,
) -> dict:
    """Create augmented dataset from AB1 files.

    IMPORTANT: Split is done at FILE level to prevent data leakage.
    Train files generate only train samples, val files generate only val samples.
    """

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

    # IMPORTANT: Split at FILE level to prevent data leakage
    np.random.shuffle(all_data)
    split_idx = int(len(all_data) * train_ratio)
    train_files = all_data[:split_idx]
    val_files = all_data[split_idx:]

    print(f"\nFile-level split: {len(train_files)} train files, {len(val_files)} val files")

    # Calculate samples per file
    augmenter = ChromatogramAugmenter(window_size=window_size)
    train_target = int(target_samples * train_ratio)
    val_target = target_samples - train_target

    train_samples_per_file = train_target // len(train_files) + 1
    val_samples_per_file = val_target // len(val_files) + 1

    print(f"Target: {train_target} train, {val_target} val samples")

    def generate_samples(file_data_list, samples_per_file, max_samples):
        """Generate augmented samples from a list of file data."""
        samples = []
        for file_idx, data in enumerate(file_data_list):
            signals = data["signals"]
            labels = data["labels"]

            # Extract base windows
            windows = augmenter.extract_windows(signals, labels, stride=50)

            # Generate augmented samples
            file_samples = 0
            while file_samples < samples_per_file and len(samples) < max_samples:
                win_signals, win_labels = windows[np.random.randint(len(windows))]
                aug_signals, aug_labels = augmenter.augment(win_signals, win_labels)
                samples.append({"signals": aug_signals, "labels": aug_labels})
                file_samples += 1

            if len(samples) >= max_samples:
                break

        return samples

    print("\nGenerating train samples...")
    train_samples = generate_samples(train_files, train_samples_per_file, train_target)

    print("Generating val samples...")
    val_samples = generate_samples(val_files, val_samples_per_file, val_target)

    print(f"\nGenerated {len(train_samples)} train, {len(val_samples)} val samples")

    # Compute statistics
    all_samples = train_samples + val_samples
    total_positions = sum(s["labels"].shape[0] for s in all_samples)
    het_positions = sum((s["labels"] == 1).sum() for s in all_samples)
    het_ratio = 100 * het_positions / total_positions

    print(f"Total positions: {total_positions}")
    print(f"Heterozygote positions: {het_positions} ({het_ratio:.1f}%)")

    # Shuffle within each set
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
            signals=sample["signals"].astype(np.float32),
            labels=sample["labels"].astype(np.int64),
        )

    for i, sample in enumerate(val_samples):
        np.savez_compressed(
            val_dir / f"sample_{i:06d}.npz",
            signals=sample["signals"].astype(np.float32),
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
        "split_method": "file-level (no leakage)",
    }

    with open(output_dir / "stats.json", "w") as f:
        json.dump(stats, f, indent=2)

    return stats


def main():
    parser = argparse.ArgumentParser(description="Create augmented heterozygote_training dataset")

    parser.add_argument("--ab1-dir", type=str, default="datalake/ab1",
                        help="Directory with AB1 files")
    parser.add_argument("--output-dir", type=str, default="datalake/datasets/heterozygote_augmented",
                        help="Output directory")
    parser.add_argument("--target-samples", type=int, default=40000,
                        help="Target number of samples")
    parser.add_argument("--window-size", type=int, default=500,
                        help="Window size for samples")

    args = parser.parse_args()

    print("=" * 70)
    print("HETEROZYGOTE DATASET - AUGMENTED FROM REAL AB1")
    print("=" * 70)

    stats = create_augmented_dataset(
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
        print(f"Train: {stats['train_samples']} samples")
        print(f"Val: {stats['val_samples']} samples")
        print(f"Het ratio: {stats['het_ratio']:.1f}%")


if __name__ == "__main__":
    main()
