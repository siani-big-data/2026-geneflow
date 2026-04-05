#!/usr/bin/env python3
"""
Create quality prediction dataset from real AB1 files.

Extracts chromatogram signals and quality scores from AB1 files,
applies augmentation to generate a larger training dataset.

The model learns to predict quality scores from chromatogram signals:
    Input:  signals (4, seq_len) - ACGT channel intensities
    Output: quality (seq_len,)   - Phred score per position

Usage:
    uv run python scripts/create_quality_dataset_ab1.py
    uv run python scripts/create_quality_dataset_ab1.py --target-samples 50000
"""

import argparse
import json
from pathlib import Path

import numpy as np
from Bio import SeqIO


def extract_ab1_data(ab1_path: Path) -> dict | None:
    """Extract signals and quality scores from AB1 file."""
    try:
        record = SeqIO.read(ab1_path, "abi")
        sequence = str(record.seq)
        quality = record.letter_annotations.get("phred_quality", [])

        if not quality:
            return None

        abif = record.annotations.get("abif_raw", {})

        # Get filter wheel order (channel assignment)
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

        # Normalize signals to [0, 1]
        signal_max = signals.max()
        if signal_max > 0:
            signals = signals / signal_max

        return {
            "signals": signals,
            "quality": np.array(quality, dtype=np.float32),
            "sequence": sequence,
        }

    except Exception as e:
        return None


class QualityAugmenter:
    """Augmentation strategies for quality prediction data."""

    def __init__(self, window_size: int = 500, max_quality: float = 60.0):
        self.window_size = window_size
        self.max_quality = max_quality

    def extract_windows(
        self, signals: np.ndarray, quality: np.ndarray, stride: int = 100
    ) -> list:
        """Extract sliding windows from a trace."""
        seq_len = signals.shape[1]
        windows = []

        if seq_len <= self.window_size:
            # Pad short sequences
            pad = self.window_size - seq_len
            sig_padded = np.pad(signals, ((0, 0), (0, pad)), constant_values=0)
            qual_padded = np.pad(quality, (0, pad), constant_values=0)
            mask_padded = np.zeros(self.window_size, dtype=np.float32)
            mask_padded[:seq_len] = 1.0
            windows.append((sig_padded, qual_padded, mask_padded))
        else:
            # Sliding windows
            for start in range(0, seq_len - self.window_size + 1, stride):
                end = start + self.window_size
                mask = np.ones(self.window_size, dtype=np.float32)
                windows.append((
                    signals[:, start:end].copy(),
                    quality[start:end].copy(),
                    mask,
                ))

        return windows

    def add_signal_noise(self, signals: np.ndarray, noise_level: float = 0.05) -> np.ndarray:
        """Add Gaussian noise to signals."""
        noise = np.random.normal(0, noise_level, signals.shape).astype(np.float32)
        noisy = signals + noise
        return np.clip(noisy, 0, 1)

    def scale_channels(self, signals: np.ndarray, scale_range: tuple = (0.8, 1.2)) -> np.ndarray:
        """Randomly scale individual channels."""
        scales = np.random.uniform(scale_range[0], scale_range[1], (4, 1)).astype(np.float32)
        scaled = signals * scales
        return np.clip(scaled, 0, 1)

    def add_baseline_drift(self, signals: np.ndarray, amplitude: float = 0.03) -> np.ndarray:
        """Add low-frequency baseline drift."""
        seq_len = signals.shape[1]
        freq = np.random.uniform(0.5, 2.0)
        phase = np.random.uniform(0, 2 * np.pi)
        x = np.linspace(0, freq * np.pi, seq_len)
        drift = amplitude * np.sin(x + phase)
        drifted = signals + drift.reshape(1, -1)
        return np.clip(drifted, 0, 1)

    def add_quality_noise(self, quality: np.ndarray, noise_level: float = 1.0) -> np.ndarray:
        """Add small noise to quality scores (simulates measurement variance)."""
        noise = np.random.normal(0, noise_level, quality.shape).astype(np.float32)
        noisy = quality + noise
        return np.clip(noisy, 0, self.max_quality)

    def simulate_quality_degradation(
        self, signals: np.ndarray, quality: np.ndarray, degradation: float = 0.1
    ) -> tuple:
        """Simulate quality degradation - noisier signals = lower quality."""
        # Add noise to signals
        noise = np.random.normal(0, degradation * 0.3, signals.shape).astype(np.float32)
        degraded_signals = np.clip(signals + noise, 0, 1)

        # Reduce quality scores proportionally
        quality_reduction = degradation * 10  # ~0-6 Phred points
        degraded_quality = np.clip(quality - quality_reduction, 0, self.max_quality)

        return degraded_signals, degraded_quality

    def augment(self, signals: np.ndarray, quality: np.ndarray, mask: np.ndarray) -> tuple:
        """Apply random augmentation."""
        aug_signals = signals.copy()
        aug_quality = quality.copy()

        # Random combination of augmentations
        if np.random.random() < 0.7:
            aug_signals = self.add_signal_noise(aug_signals, np.random.uniform(0.02, 0.08))

        if np.random.random() < 0.5:
            aug_signals = self.scale_channels(aug_signals)

        if np.random.random() < 0.4:
            aug_signals = self.add_baseline_drift(aug_signals, np.random.uniform(0.01, 0.04))

        if np.random.random() < 0.3:
            aug_signals, aug_quality = self.simulate_quality_degradation(
                aug_signals, aug_quality, np.random.uniform(0.05, 0.2)
            )

        if np.random.random() < 0.3:
            aug_quality = self.add_quality_noise(aug_quality, np.random.uniform(0.5, 1.5))

        return aug_signals, aug_quality, mask


def create_quality_dataset(
    ab1_dir: Path,
    output_dir: Path,
    target_samples: int = 50000,
    window_size: int = 500,
    train_ratio: float = 0.8,
    val_ratio: float = 0.1,
) -> dict:
    """Create quality prediction dataset from AB1 files.

    Split is done at FILE level to prevent data leakage.
    """

    # Get AB1 files (deduplicated for case-insensitive filesystems like Windows)
    ab1_files = list({str(f.resolve()).lower(): f for f in ab1_dir.glob("*.ab1")}.values())

    if not ab1_files:
        return {"error": f"No AB1 files found in {ab1_dir}"}

    print(f"Found {len(ab1_files)} AB1 files")

    # Load all AB1 data
    all_data = []
    total_positions = 0
    quality_values = []

    for f in ab1_files:
        data = extract_ab1_data(f)
        if data is not None:
            all_data.append(data)
            total_positions += len(data["quality"])
            quality_values.extend(data["quality"].tolist())

    if not all_data:
        return {"error": "No valid AB1 files could be loaded"}

    print(f"Loaded {len(all_data)} valid AB1 files")
    print(f"Original data: {total_positions} positions")

    quality_values = np.array(quality_values)
    print(f"Quality stats: min={quality_values.min():.0f}, max={quality_values.max():.0f}, "
          f"mean={quality_values.mean():.1f}, std={quality_values.std():.1f}")

    # Split at FILE level to prevent data leakage
    np.random.shuffle(all_data)
    n_files = len(all_data)
    train_end = int(n_files * train_ratio)
    val_end = int(n_files * (train_ratio + val_ratio))

    train_files = all_data[:train_end]
    val_files = all_data[train_end:val_end]
    test_files = all_data[val_end:]

    print(f"\nFile-level split: {len(train_files)} train, {len(val_files)} val, {len(test_files)} test files")

    # Calculate target samples per split
    augmenter = QualityAugmenter(window_size=window_size)
    train_target = int(target_samples * train_ratio)
    val_target = int(target_samples * val_ratio)
    test_target = target_samples - train_target - val_target

    def generate_samples(file_data_list, target_count, augment: bool = True) -> list:
        """Generate samples from a list of file data."""
        if not file_data_list:
            return []

        samples = []
        samples_per_file = target_count // len(file_data_list) + 1

        for data in file_data_list:
            signals = data["signals"]
            quality = data["quality"]

            # Extract base windows
            windows = augmenter.extract_windows(signals, quality, stride=50)

            # Generate samples (with augmentation for train)
            file_samples = 0
            while file_samples < samples_per_file and len(samples) < target_count:
                win_signals, win_quality, win_mask = windows[np.random.randint(len(windows))]

                if augment:
                    aug_signals, aug_quality, aug_mask = augmenter.augment(
                        win_signals, win_quality, win_mask
                    )
                else:
                    aug_signals, aug_quality, aug_mask = win_signals, win_quality, win_mask

                samples.append({
                    "signals": aug_signals,
                    "quality": aug_quality,
                    "mask": aug_mask,
                })
                file_samples += 1

            if len(samples) >= target_count:
                break

        return samples

    print("\nGenerating train samples (with augmentation)...")
    train_samples = generate_samples(train_files, train_target, augment=True)

    print("Generating val samples (no augmentation)...")
    val_samples = generate_samples(val_files, val_target, augment=False)

    print("Generating test samples (no augmentation)...")
    test_samples = generate_samples(test_files, test_target, augment=False)

    print(f"\nGenerated {len(train_samples)} train, {len(val_samples)} val, {len(test_samples)} test samples")

    # Shuffle
    np.random.shuffle(train_samples)
    np.random.shuffle(val_samples)
    np.random.shuffle(test_samples)

    # Save
    for split_name, samples in [("train", train_samples), ("val", val_samples), ("test", test_samples)]:
        split_dir = output_dir / split_name
        split_dir.mkdir(parents=True, exist_ok=True)

        print(f"Saving {len(samples)} {split_name} samples...")
        for i, sample in enumerate(samples):
            np.savez_compressed(
                split_dir / f"sample_{i:06d}.npz",
                signals=sample["signals"].astype(np.float32),
                quality=sample["quality"].astype(np.float32),
                mask=sample["mask"].astype(np.float32),
            )

    # Compute stats
    all_samples = train_samples + val_samples + test_samples
    all_quality = np.concatenate([s["quality"] for s in all_samples])

    stats = {
        "source_files": len(all_data),
        "train_files": len(train_files),
        "val_files": len(val_files),
        "test_files": len(test_files),
        "total_samples": len(all_samples),
        "train_samples": len(train_samples),
        "val_samples": len(val_samples),
        "test_samples": len(test_samples),
        "window_size": window_size,
        "quality_min": float(all_quality.min()),
        "quality_max": float(all_quality.max()),
        "quality_mean": float(all_quality.mean()),
        "quality_std": float(all_quality.std()),
        "split_method": "file-level (no leakage)",
        "task": "per_position_quality_prediction",
    }

    with open(output_dir / "metadata.json", "w") as f:
        json.dump(stats, f, indent=2)

    return stats


def main():
    parser = argparse.ArgumentParser(description="Create quality prediction dataset from AB1")

    parser.add_argument("--ab1-dir", type=str, default="datalake/ab1",
                        help="Directory with AB1 files")
    parser.add_argument("--output-dir", type=str, default="datalake/datasets/quality",
                        help="Output directory")
    parser.add_argument("--target-samples", type=int, default=50000,
                        help="Target number of samples")
    parser.add_argument("--window-size", type=int, default=500,
                        help="Window size for samples")

    args = parser.parse_args()

    print("=" * 70)
    print("QUALITY PREDICTION DATASET - FROM AB1 FILES")
    print("=" * 70)
    print(f"Task: Predict Phred quality score per position from chromatogram signals")
    print(f"Input: signals (4, {args.window_size}) - ACGT channel intensities")
    print(f"Output: quality ({args.window_size},) - Phred score per position")
    print()

    stats = create_quality_dataset(
        ab1_dir=Path(args.ab1_dir),
        output_dir=Path(args.output_dir),
        target_samples=args.target_samples,
        window_size=args.window_size,
    )

    print("\n" + "=" * 70)
    if "error" in stats:
        print(f"ERROR: {stats['error']}")
    else:
        print("COMPLETE")
        print("=" * 70)
        print(f"Dataset: {args.output_dir}")
        print(f"Train: {stats['train_samples']} samples")
        print(f"Val: {stats['val_samples']} samples")
        print(f"Test: {stats['test_samples']} samples")
        print(f"Quality range: [{stats['quality_min']:.0f}, {stats['quality_max']:.0f}]")
        print(f"Quality mean: {stats['quality_mean']:.1f} +/- {stats['quality_std']:.1f}")


if __name__ == "__main__":
    main()
