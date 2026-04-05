#!/usr/bin/env python3
"""
Create quality classification dataset from AB1 files.

Instead of predicting exact Phred scores (regression), classify into bins:
- Class 0 (Q10): Phred 0-9   - Very low quality (>10% error)
- Class 1 (Q20): Phred 10-19 - Low quality (1-10% error)
- Class 2 (Q30): Phred 20-29 - Acceptable (0.1-1% error)
- Class 3 (Q40): Phred 30-39 - Good (<0.1% error)
- Class 4 (Q50): Phred 40+   - Excellent (<0.01% error)

Usage:
    uv run python scripts/create_quality_class_dataset.py
    uv run python scripts/create_quality_class_dataset.py --num-classes 4
"""

import argparse
import json
from pathlib import Path

import numpy as np
from Bio import SeqIO


# Quality class definitions
CLASS_BINS = {
    5: [0, 10, 20, 30, 40, 100],    # Q10, Q20, Q30, Q40, Q50+
    4: [0, 15, 25, 35, 100],        # Low, Medium, Good, Excellent
    3: [0, 20, 35, 100],            # Bad, OK, Good
    2: [0, 20, 100],                # Bad, Good (binary)
}

CLASS_NAMES = {
    5: ["Q10 (0-9)", "Q20 (10-19)", "Q30 (20-29)", "Q40 (30-39)", "Q50+ (40+)"],
    4: ["Low (0-14)", "Medium (15-24)", "Good (25-34)", "Excellent (35+)"],
    3: ["Bad (0-19)", "OK (20-34)", "Good (35+)"],
    2: ["Bad (<20)", "Good (>=20)"],
}


def phred_to_class(phred: np.ndarray, num_classes: int) -> np.ndarray:
    """Convert Phred scores to class indices."""
    bins = CLASS_BINS[num_classes]
    # np.digitize returns 1-indexed, subtract 1 for 0-indexed classes
    classes = np.digitize(phred, bins[1:-1])
    return classes.astype(np.int64)


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

        # Get peak features
        p1am = abif.get("P1AM1")
        p2am = abif.get("P2AM1")

        if p1am is None or len(p1am) != len(sequence):
            return None

        p1am = np.array(p1am, dtype=np.float32)
        p2am = np.array(p2am, dtype=np.float32) if p2am and len(p2am) == len(sequence) else np.zeros_like(p1am)

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
        p2am_norm = p2am / (p1am.max() + 1e-6)

        # Compute primary/secondary ratio
        ratio = p1am / (p2am + 10.0)
        ratio_norm = np.clip(ratio / 20.0, 0, 1)

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


def extract_windows(
    features: np.ndarray,
    quality: np.ndarray,
    window_size: int,
    stride: int,
) -> list:
    """Extract sliding windows from a trace."""
    seq_len = features.shape[1]
    windows = []

    if seq_len <= window_size:
        pad = window_size - seq_len
        feat_padded = np.pad(features, ((0, 0), (0, pad)), constant_values=0)
        qual_padded = np.pad(quality, (0, pad), constant_values=0)
        mask_padded = np.zeros(window_size, dtype=np.float32)
        mask_padded[:seq_len] = 1.0
        windows.append((feat_padded, qual_padded, mask_padded))
    else:
        for start in range(0, seq_len - window_size + 1, stride):
            end = start + window_size
            mask = np.ones(window_size, dtype=np.float32)
            windows.append((
                features[:, start:end].copy(),
                quality[start:end].copy(),
                mask,
            ))

    return windows


def create_quality_class_dataset(
    ab1_dir: Path,
    output_dir: Path,
    num_classes: int = 5,
    target_samples: int = 50000,
    window_size: int = 500,
    train_ratio: float = 0.85,
) -> dict:
    """Create quality classification dataset."""

    # Get AB1 files
    ab1_files = list({str(f.resolve()).lower(): f for f in ab1_dir.glob("*.ab1")}.values())

    if not ab1_files:
        return {"error": f"No AB1 files found in {ab1_dir}"}

    print(f"Found {len(ab1_files)} AB1 files")

    # Load all AB1 data
    all_data = []
    for f in ab1_files:
        data = extract_ab1_data(f)
        if data is not None:
            all_data.append(data)

    if not all_data:
        return {"error": "No valid AB1 files could be loaded"}

    print(f"Loaded {len(all_data)} valid AB1 files")

    # Analyze class distribution in raw data
    all_quality = np.concatenate([d["quality"] for d in all_data])
    all_classes = phred_to_class(all_quality, num_classes)

    print(f"\nOriginal class distribution:")
    for c in range(num_classes):
        count = (all_classes == c).sum()
        pct = 100 * count / len(all_classes)
        print(f"  {CLASS_NAMES[num_classes][c]}: {count:,} ({pct:.1f}%)")

    # Split files into train/val
    np.random.shuffle(all_data)
    n_val = max(1, int(len(all_data) * (1 - train_ratio)))
    val_files = all_data[:n_val]
    train_files = all_data[n_val:]

    print(f"\nFile split: {len(train_files)} train, {len(val_files)} val")

    # Generate samples
    train_target = int(target_samples * train_ratio)
    val_target = target_samples - train_target

    def generate_samples(file_data_list, target_count) -> list:
        if not file_data_list:
            return []

        samples = []
        samples_per_file = target_count // len(file_data_list) + 1

        for data in file_data_list:
            features = data["features"]
            quality = data["quality"]

            windows = extract_windows(features, quality, window_size, stride=50)

            file_samples = 0
            while file_samples < samples_per_file and len(samples) < target_count:
                win_features, win_quality, win_mask = windows[np.random.randint(len(windows))]
                win_classes = phred_to_class(win_quality, num_classes)

                samples.append({
                    "features": win_features,
                    "classes": win_classes,
                    "quality": win_quality,  # Keep original for analysis
                    "mask": win_mask,
                })
                file_samples += 1

            if len(samples) >= target_count:
                break

        return samples

    print("\nGenerating train samples...")
    train_samples = generate_samples(train_files, train_target)

    print("Generating val samples...")
    val_samples = generate_samples(val_files, val_target)

    print(f"Generated {len(train_samples)} train, {len(val_samples)} val samples")

    # Shuffle
    np.random.shuffle(train_samples)
    np.random.shuffle(val_samples)

    # Compute class weights for imbalanced data
    all_train_classes = np.concatenate([s["classes"] * s["mask"] for s in train_samples])
    all_train_mask = np.concatenate([s["mask"] for s in train_samples])
    valid_classes = all_train_classes[all_train_mask > 0].astype(int)

    class_counts = np.bincount(valid_classes, minlength=num_classes)
    class_weights = len(valid_classes) / (num_classes * class_counts + 1)
    class_weights = class_weights / class_weights.sum() * num_classes  # Normalize

    print(f"\nClass weights for training:")
    for c in range(num_classes):
        print(f"  {CLASS_NAMES[num_classes][c]}: {class_weights[c]:.3f}")

    # Save samples
    for split_name, samples in [("train", train_samples), ("val", val_samples)]:
        split_dir = output_dir / split_name
        split_dir.mkdir(parents=True, exist_ok=True)

        print(f"Saving {len(samples)} {split_name} samples...")
        for i, sample in enumerate(samples):
            np.savez_compressed(
                split_dir / f"sample_{i:06d}.npz",
                features=sample["features"].astype(np.float32),
                classes=sample["classes"].astype(np.int64),
                mask=sample["mask"].astype(np.float32),
            )

    # Final class distribution
    train_classes = np.concatenate([s["classes"][s["mask"] > 0] for s in train_samples])
    val_classes = np.concatenate([s["classes"][s["mask"] > 0] for s in val_samples])

    train_dist = {CLASS_NAMES[num_classes][c]: int((train_classes == c).sum()) for c in range(num_classes)}
    val_dist = {CLASS_NAMES[num_classes][c]: int((val_classes == c).sum()) for c in range(num_classes)}

    # Metadata
    stats = {
        "num_classes": num_classes,
        "class_names": CLASS_NAMES[num_classes],
        "class_bins": CLASS_BINS[num_classes],
        "class_weights": class_weights.tolist(),
        "source_files": len(all_data),
        "train_files": len(train_files),
        "val_files": len(val_files),
        "total_samples": len(train_samples) + len(val_samples),
        "train_samples": len(train_samples),
        "val_samples": len(val_samples),
        "window_size": window_size,
        "n_features": 7,
        "features": ["signal_A", "signal_C", "signal_G", "signal_T", "p1am_norm", "p2am_norm", "ratio_norm"],
        "train_class_distribution": train_dist,
        "val_class_distribution": val_dist,
        "task": "quality_classification",
    }

    with open(output_dir / "metadata.json", "w") as f:
        json.dump(stats, f, indent=2)

    # Save class weights for training
    np.save(output_dir / "class_weights.npy", class_weights)

    return stats


def main():
    parser = argparse.ArgumentParser(description="Create quality classification dataset")

    parser.add_argument("--ab1-dir", type=str, default="datalake/ab1")
    parser.add_argument("--output-dir", type=str, default="datalake/datasets/quality_class")
    parser.add_argument("--num-classes", type=int, default=5, choices=[2, 3, 4, 5])
    parser.add_argument("--target-samples", type=int, default=50000)
    parser.add_argument("--window-size", type=int, default=500)

    args = parser.parse_args()

    print("=" * 70)
    print("QUALITY CLASSIFICATION DATASET")
    print("=" * 70)
    print(f"\nTask: Classify quality into {args.num_classes} bins")
    print(f"Classes:")
    for i, name in enumerate(CLASS_NAMES[args.num_classes]):
        print(f"  {i}: {name}")
    print()

    stats = create_quality_class_dataset(
        ab1_dir=Path(args.ab1_dir),
        output_dir=Path(args.output_dir),
        num_classes=args.num_classes,
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
        print(f"Classes: {stats['num_classes']}")
        print(f"Train: {stats['train_samples']} samples")
        print(f"Val: {stats['val_samples']} samples")


if __name__ == "__main__":
    main()
