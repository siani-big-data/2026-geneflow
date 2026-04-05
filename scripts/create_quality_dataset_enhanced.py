#!/usr/bin/env python3
"""
Create enhanced quality prediction dataset from AB1 files.

Uses additional quality-predictive features beyond raw signals:
- P1AM1: Primary peak amplitude (r=0.41 with quality)
- P2AM1: Secondary peak amplitude (r=-0.50 with quality)
- P1WD1: Peak width
- Primary/Secondary ratio

Input:  features (7, seq_len) - 4 ACGT signals + 3 peak features
Output: quality (seq_len,) - Phred score per position

Usage:
    uv run python scripts/create_quality_dataset_enhanced.py
    uv run python scripts/create_quality_dataset_enhanced.py --target-samples 50000
"""

import argparse
import json
from pathlib import Path

import numpy as np
from Bio import SeqIO


def extract_ab1_data(ab1_path: Path) -> dict | None:
    """Extract signals, peak features, and quality scores from AB1 file."""
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

        # Get peak features (crucial for quality prediction)
        p1am = abif.get("P1AM1")  # Primary peak amplitude
        p2am = abif.get("P2AM1")  # Secondary peak amplitude
        p1wd = abif.get("P1WD1")  # Peak width

        if p1am is None or len(p1am) != len(sequence):
            return None

        p1am = np.array(p1am, dtype=np.float32)
        p2am = np.array(p2am, dtype=np.float32) if p2am and len(p2am) == len(sequence) else np.zeros_like(p1am)
        p1wd = np.array(p1wd, dtype=np.float32) if p1wd and len(p1wd) == len(sequence) else np.ones_like(p1am) * 300

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

        # Normalize peak features
        p1am_norm = p1am / (p1am.max() + 1e-6)
        p2am_norm = p2am / (p1am.max() + 1e-6)  # Use same scale as p1am
        p1wd_norm = p1wd / 1000.0  # Typical width is ~300-500

        # Compute primary/secondary ratio (key feature!)
        ratio = p1am / (p2am + 10.0)  # Add small value to avoid div by zero
        ratio_norm = np.clip(ratio / 20.0, 0, 1)  # Normalize to [0, 1]

        # Stack all features: 4 signals + p1am + p2am + ratio = 7 channels
        features = np.zeros((7, seq_len), dtype=np.float32)
        features[:4] = signals
        features[4] = p1am_norm
        features[5] = p2am_norm
        features[6] = ratio_norm

        return {
            "features": features,
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
        self, features: np.ndarray, quality: np.ndarray, stride: int = 100
    ) -> list:
        """Extract sliding windows from a trace."""
        seq_len = features.shape[1]
        windows = []

        if seq_len <= self.window_size:
            # Pad short sequences
            pad = self.window_size - seq_len
            feat_padded = np.pad(features, ((0, 0), (0, pad)), constant_values=0)
            qual_padded = np.pad(quality, (0, pad), constant_values=0)
            mask_padded = np.zeros(self.window_size, dtype=np.float32)
            mask_padded[:seq_len] = 1.0
            windows.append((feat_padded, qual_padded, mask_padded))
        else:
            # Sliding windows
            for start in range(0, seq_len - self.window_size + 1, stride):
                end = start + self.window_size
                mask = np.ones(self.window_size, dtype=np.float32)
                windows.append((
                    features[:, start:end].copy(),
                    quality[start:end].copy(),
                    mask,
                ))

        return windows

    def add_signal_noise(self, features: np.ndarray, noise_level: float = 0.05) -> np.ndarray:
        """Add Gaussian noise to features."""
        noise = np.random.normal(0, noise_level, features.shape).astype(np.float32)
        noisy = features + noise
        return np.clip(noisy, 0, 1)

    def scale_channels(self, features: np.ndarray, scale_range: tuple = (0.8, 1.2)) -> np.ndarray:
        """Randomly scale individual channels."""
        n_channels = features.shape[0]
        scales = np.random.uniform(scale_range[0], scale_range[1], (n_channels, 1)).astype(np.float32)
        scaled = features * scales
        return np.clip(scaled, 0, 1)

    def simulate_quality_degradation(
        self, features: np.ndarray, quality: np.ndarray, degradation: float = 0.1
    ) -> tuple:
        """Simulate quality degradation - noisier signals = lower quality."""
        # Add noise to features
        noise = np.random.normal(0, degradation * 0.3, features.shape).astype(np.float32)
        degraded_features = np.clip(features + noise, 0, 1)

        # Reduce quality scores proportionally
        quality_reduction = degradation * 10  # ~0-6 Phred points
        degraded_quality = np.clip(quality - quality_reduction, 0, self.max_quality)

        return degraded_features, degraded_quality

    def augment(self, features: np.ndarray, quality: np.ndarray, mask: np.ndarray) -> tuple:
        """Apply random augmentation."""
        aug_features = features.copy()
        aug_quality = quality.copy()

        # Random combination of augmentations
        if np.random.random() < 0.7:
            aug_features = self.add_signal_noise(aug_features, np.random.uniform(0.02, 0.08))

        if np.random.random() < 0.5:
            aug_features = self.scale_channels(aug_features)

        if np.random.random() < 0.3:
            aug_features, aug_quality = self.simulate_quality_degradation(
                aug_features, aug_quality, np.random.uniform(0.05, 0.2)
            )

        return aug_features, aug_quality, mask


def create_quality_dataset(
    ab1_dir: Path,
    output_dir: Path,
    target_samples: int = 40000,
    window_size: int = 500,
    train_ratio: float = 0.85,
) -> dict:
    """Create enhanced quality prediction dataset from AB1 files."""

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

    # Stratified split at FILE level to ensure similar quality distribution
    # Sort files by average quality, then distribute alternately
    for data in all_data:
        data["_avg_quality"] = data["quality"].mean()

    all_data.sort(key=lambda x: x["_avg_quality"])

    # Distribute in round-robin: every Nth file goes to val
    n_val = max(1, int(len(all_data) * (1 - train_ratio)))
    step = max(1, len(all_data) // n_val)
    val_indices = set(list(range(0, len(all_data), step))[:n_val])

    train_files = []
    val_files = []
    for i, data in enumerate(all_data):
        del data["_avg_quality"]  # Clean up temp field
        if i in val_indices:
            val_files.append(data)
        else:
            train_files.append(data)

    # Shuffle within splits
    np.random.shuffle(train_files)
    np.random.shuffle(val_files)

    # Report quality distribution per split
    train_q = np.concatenate([d["quality"] for d in train_files])
    val_q = np.concatenate([d["quality"] for d in val_files])
    print(f"\nStratified file-level split:")
    print(f"  Train: {len(train_files)} files, quality mean={train_q.mean():.1f}, median={np.median(train_q):.0f}")
    print(f"  Val:   {len(val_files)} files, quality mean={val_q.mean():.1f}, median={np.median(val_q):.0f}")

    # Calculate target samples per split
    augmenter = QualityAugmenter(window_size=window_size)
    train_target = int(target_samples * train_ratio)
    val_target = target_samples - train_target

    def generate_samples(file_data_list, target_count, augment: bool = True) -> list:
        """Generate samples from a list of file data."""
        if not file_data_list:
            return []

        samples = []
        samples_per_file = target_count // len(file_data_list) + 1

        for data in file_data_list:
            features = data["features"]
            quality = data["quality"]

            # Extract base windows
            windows = augmenter.extract_windows(features, quality, stride=50)

            # Generate samples (with augmentation for train)
            file_samples = 0
            while file_samples < samples_per_file and len(samples) < target_count:
                win_features, win_quality, win_mask = windows[np.random.randint(len(windows))]

                if augment:
                    aug_features, aug_quality, aug_mask = augmenter.augment(
                        win_features, win_quality, win_mask
                    )
                else:
                    aug_features, aug_quality, aug_mask = win_features, win_quality, win_mask

                samples.append({
                    "features": aug_features,
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

    print(f"\nGenerated {len(train_samples)} train, {len(val_samples)} val samples")

    # Shuffle
    np.random.shuffle(train_samples)
    np.random.shuffle(val_samples)

    # Save
    for split_name, samples in [("train", train_samples), ("val", val_samples)]:
        split_dir = output_dir / split_name
        split_dir.mkdir(parents=True, exist_ok=True)

        print(f"Saving {len(samples)} {split_name} samples...")
        for i, sample in enumerate(samples):
            np.savez_compressed(
                split_dir / f"sample_{i:06d}.npz",
                features=sample["features"].astype(np.float32),
                quality=sample["quality"].astype(np.float32),
                mask=sample["mask"].astype(np.float32),
            )

    # Compute stats
    all_samples = train_samples + val_samples
    all_quality = np.concatenate([s["quality"] for s in all_samples])

    stats = {
        "source_files": len(all_data),
        "train_files": len(train_files),
        "val_files": len(val_files),
        "total_samples": len(all_samples),
        "train_samples": len(train_samples),
        "val_samples": len(val_samples),
        "window_size": window_size,
        "n_features": 7,
        "features": ["signal_A", "signal_C", "signal_G", "signal_T", "p1am_norm", "p2am_norm", "ratio_norm"],
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
    parser = argparse.ArgumentParser(description="Create enhanced quality dataset from AB1")

    parser.add_argument("--ab1-dir", type=str, default="datalake/ab1",
                        help="Directory with AB1 files")
    parser.add_argument("--output-dir", type=str, default="datalake/datasets/quality_enhanced",
                        help="Output directory")
    parser.add_argument("--target-samples", type=int, default=40000,
                        help="Target number of samples")
    parser.add_argument("--window-size", type=int, default=500,
                        help="Window size for samples")

    args = parser.parse_args()

    print("=" * 70)
    print("ENHANCED QUALITY PREDICTION DATASET - FROM AB1 FILES")
    print("=" * 70)
    print(f"Task: Predict Phred quality score per position")
    print(f"Input: features (7, {args.window_size}) - 4 ACGT signals + peak features")
    print(f"Output: quality ({args.window_size},) - Phred score per position")
    print()
    print("Features:")
    print("  [0-3] ACGT channel intensities")
    print("  [4]   P1AM1 (primary peak amplitude) - r=0.41 with quality")
    print("  [5]   P2AM1 (secondary peak amplitude) - r=-0.50 with quality")
    print("  [6]   Primary/Secondary ratio - key quality indicator")
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
        print(f"Features: {stats['n_features']} channels")
        print(f"Train: {stats['train_samples']} samples ({stats['train_files']} files)")
        print(f"Val: {stats['val_samples']} samples ({stats['val_files']} files)")
        print(f"Quality range: [{stats['quality_min']:.0f}, {stats['quality_max']:.0f}]")
        print(f"Quality mean: {stats['quality_mean']:.1f} +/- {stats['quality_std']:.1f}")


if __name__ == "__main__":
    main()
