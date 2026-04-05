#!/usr/bin/env python3
"""
Create quality classification dataset with PRE-COMPUTED context features.

Instead of letting a CNN learn context (which overfits with limited data),
we pre-compute contextual statistics that cannot be memorized.

Features (13 total):
  Local (7):
    - signal_A, signal_C, signal_G, signal_T: Channel amplitudes
    - p1am_norm: Primary peak amplitude
    - p2am_norm: Secondary peak amplitude
    - ratio_norm: P1/P2 ratio

  Context (6):
    - position_relative: Position in trace (0=start, 1=end)
    - local_variance: Signal variance in 5-position window
    - local_snr: Local signal-to-noise ratio
    - peak_spacing_prev: Distance to previous peak
    - peak_spacing_next: Distance to next peak
    - signal_trend: Local signal trend (derivative)

Usage:
    uv run python scripts/create_quality_context_dataset.py
"""

import argparse
import json
from pathlib import Path

import numpy as np
from Bio import SeqIO


CLASS_BINS = {
    5: [0, 10, 20, 30, 40, 100],
    4: [0, 15, 25, 35, 100],
    3: [0, 20, 35, 100],
    2: [0, 20, 100],
}

CLASS_NAMES = {
    5: ["Q10 (0-9)", "Q20 (10-19)", "Q30 (20-29)", "Q40 (30-39)", "Q50+ (40+)"],
    4: ["Low (0-14)", "Medium (15-24)", "Good (25-34)", "Excellent (35+)"],
    3: ["Bad (0-19)", "OK (20-34)", "Good (35+)"],
    2: ["Bad (<20)", "Good (>=20)"],
}

FEATURE_NAMES = [
    # Local (7)
    "signal_A", "signal_C", "signal_G", "signal_T",
    "p1am_norm", "p2am_norm", "ratio_norm",
    # Context (6)
    "position_relative",
    "local_variance",
    "local_snr",
    "peak_spacing_prev",
    "peak_spacing_next",
    "signal_trend",
]


def phred_to_class(phred: np.ndarray, num_classes: int) -> np.ndarray:
    """Convert Phred scores to class indices."""
    bins = CLASS_BINS[num_classes]
    classes = np.digitize(phred, bins[1:-1])
    return classes.astype(np.int64)


def compute_context_features(
    signals: np.ndarray,
    ploc: np.ndarray,
    seq_len: int,
    trace_len: int,
    window: int = 5,
) -> np.ndarray:
    """Compute context features that cannot be memorized.

    Args:
        signals: (4, seq_len) normalized signal values at peak positions
        ploc: Peak locations in trace coordinates
        seq_len: Number of called bases
        trace_len: Total trace length
        window: Window size for local statistics

    Returns:
        context_features: (6, seq_len) context features
    """
    context = np.zeros((6, seq_len), dtype=np.float32)

    # 1. Position relative (0 = start, 1 = end)
    context[0] = np.linspace(0, 1, seq_len)

    # 2. Local variance (signal variance in window)
    signal_sum = signals.sum(axis=0)  # Combined signal
    half_w = window // 2
    for i in range(seq_len):
        start = max(0, i - half_w)
        end = min(seq_len, i + half_w + 1)
        context[1, i] = signal_sum[start:end].var()
    # Normalize variance
    if context[1].max() > 0:
        context[1] = context[1] / context[1].max()

    # 3. Local SNR (signal / noise estimate)
    # Noise = high frequency component (difference between adjacent)
    for i in range(seq_len):
        start = max(0, i - half_w)
        end = min(seq_len, i + half_w + 1)
        local_signal = signal_sum[start:end]
        signal_power = local_signal.mean()
        noise_power = np.abs(np.diff(local_signal)).mean() + 1e-6
        context[2, i] = signal_power / noise_power
    # Normalize SNR
    context[2] = np.clip(context[2] / 10.0, 0, 1)

    # 4 & 5. Peak spacing (distance to prev/next peak)
    if ploc is not None and len(ploc) == seq_len:
        # Previous spacing
        context[3, 0] = 0.5  # No previous for first
        context[3, 1:] = np.diff(ploc)

        # Next spacing
        context[4, :-1] = np.diff(ploc)
        context[4, -1] = 0.5  # No next for last

        # Normalize spacing (typical is ~10-15 trace points)
        context[3] = np.clip(context[3] / 20.0, 0, 1)
        context[4] = np.clip(context[4] / 20.0, 0, 1)
    else:
        # Default to middle value if PLOC not available
        context[3] = 0.5
        context[4] = 0.5

    # 6. Signal trend (local derivative)
    # Positive = increasing, negative = decreasing
    trend = np.zeros(seq_len, dtype=np.float32)
    for i in range(seq_len):
        start = max(0, i - half_w)
        end = min(seq_len, i + half_w + 1)
        if end - start > 1:
            x = np.arange(end - start)
            y = signal_sum[start:end]
            # Simple linear regression slope
            slope = np.polyfit(x, y, 1)[0]
            trend[i] = slope
    # Normalize trend to [-1, 1]
    max_trend = np.abs(trend).max() + 1e-6
    context[5] = np.clip(trend / max_trend, -1, 1)
    # Shift to [0, 1]
    context[5] = (context[5] + 1) / 2

    return context


def extract_ab1_data(ab1_path: Path) -> dict | None:
    """Extract signals, context features, and quality from AB1 file."""
    try:
        record = SeqIO.read(ab1_path, "abi")
        sequence = str(record.seq)
        quality = record.letter_annotations.get("phred_quality", [])

        if not quality or len(quality) < 50:
            return None

        abif = record.annotations.get("abif_raw", {})

        # Get filter wheel order
        fwo = "GATC"
        if "FWO_1" in abif:
            fwo_val = abif["FWO_1"]
            fwo = fwo_val.decode() if isinstance(fwo_val, bytes) else str(fwo_val)

        # Get processed trace data
        trace_data = {}
        for i, base in enumerate(fwo):
            key = f"DATA{9 + i}"
            if key in abif:
                trace_data[base] = np.array(abif[key], dtype=np.float32)

        if len(trace_data) != 4:
            return None

        trace_len = min(len(trace_data[b]) for b in "ACGT")

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

        # Sample trace at peak locations
        seq_len = len(sequence)
        signals = np.zeros((4, seq_len), dtype=np.float32)

        for i, base in enumerate("ACGT"):
            if base in trace_data:
                trace = trace_data[base]
                for j, pos in enumerate(ploc):
                    if pos < len(trace):
                        signals[i, j] = trace[pos]

        # Normalize signals
        signal_max = signals.max()
        if signal_max > 0:
            signals = signals / signal_max

        # Normalize peak features
        p1am_norm = p1am / (p1am.max() + 1e-6)
        p2am_norm = p2am / (p1am.max() + 1e-6)
        ratio = p1am / (p2am + 10.0)
        ratio_norm = np.clip(ratio / 20.0, 0, 1)

        # Local features (7)
        local_features = np.zeros((7, seq_len), dtype=np.float32)
        local_features[:4] = signals
        local_features[4] = p1am_norm
        local_features[5] = p2am_norm
        local_features[6] = ratio_norm

        # Context features (6)
        context_features = compute_context_features(
            signals=signals,
            ploc=ploc,
            seq_len=seq_len,
            trace_len=trace_len,
        )

        # Combine all features (13)
        all_features = np.concatenate([local_features, context_features], axis=0)

        return {
            "features": all_features,
            "quality": np.array(quality, dtype=np.float32),
            "sequence": sequence,
        }

    except Exception as e:
        print(f"  Error: {e}")
        return None


def extract_windows(
    features: np.ndarray,
    quality: np.ndarray,
    window_size: int,
    stride: int,
) -> list:
    """Extract sliding windows."""
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


def create_dataset(
    ab1_dir: Path,
    output_dir: Path,
    num_classes: int = 5,
    target_samples: int = 50000,
    window_size: int = 500,
    train_ratio: float = 0.85,
) -> dict:
    """Create quality classification dataset with context features."""

    ab1_files = list({str(f.resolve()).lower(): f for f in ab1_dir.glob("*.ab1")}.values())

    if not ab1_files:
        return {"error": f"No AB1 files found in {ab1_dir}"}

    print(f"Found {len(ab1_files)} AB1 files")

    # Load data
    all_data = []
    for f in ab1_files:
        data = extract_ab1_data(f)
        if data is not None:
            all_data.append(data)
            print(f"  Loaded {f.name}: {data['features'].shape[1]} positions")

    if not all_data:
        return {"error": "No valid AB1 files"}

    print(f"\nLoaded {len(all_data)} valid files")
    print(f"Features per position: {all_data[0]['features'].shape[0]} ({len(FEATURE_NAMES)})")

    # Class distribution
    all_quality = np.concatenate([d["quality"] for d in all_data])
    all_classes = phred_to_class(all_quality, num_classes)

    print(f"\nClass distribution:")
    for c in range(num_classes):
        count = (all_classes == c).sum()
        pct = 100 * count / len(all_classes)
        print(f"  {CLASS_NAMES[num_classes][c]}: {count:,} ({pct:.1f}%)")

    # Split
    np.random.shuffle(all_data)
    n_val = max(1, int(len(all_data) * (1 - train_ratio)))
    val_files = all_data[:n_val]
    train_files = all_data[n_val:]

    print(f"\nSplit: {len(train_files)} train, {len(val_files)} val files")

    # Generate samples
    train_target = int(target_samples * train_ratio)
    val_target = target_samples - train_target

    def generate_samples(file_data_list, target_count) -> list:
        if not file_data_list:
            return []

        samples = []
        samples_per_file = target_count // len(file_data_list) + 1

        for data in file_data_list:
            windows = extract_windows(data["features"], data["quality"], window_size, stride=50)

            file_samples = 0
            while file_samples < samples_per_file and len(samples) < target_count:
                win_features, win_quality, win_mask = windows[np.random.randint(len(windows))]
                win_classes = phred_to_class(win_quality, num_classes)

                samples.append({
                    "features": win_features,
                    "classes": win_classes,
                    "mask": win_mask,
                })
                file_samples += 1

            if len(samples) >= target_count:
                break

        return samples

    print("\nGenerating samples...")
    train_samples = generate_samples(train_files, train_target)
    val_samples = generate_samples(val_files, val_target)

    print(f"Generated: {len(train_samples)} train, {len(val_samples)} val")

    np.random.shuffle(train_samples)
    np.random.shuffle(val_samples)

    # Class weights
    all_train_classes = np.concatenate([s["classes"] * s["mask"] for s in train_samples])
    all_train_mask = np.concatenate([s["mask"] for s in train_samples])
    valid_classes = all_train_classes[all_train_mask > 0].astype(int)

    class_counts = np.bincount(valid_classes, minlength=num_classes)
    class_weights = len(valid_classes) / (num_classes * class_counts + 1)
    class_weights = class_weights / class_weights.sum() * num_classes

    print(f"\nClass weights:")
    for c in range(num_classes):
        print(f"  {CLASS_NAMES[num_classes][c]}: {class_weights[c]:.3f}")

    # Save
    for split_name, samples in [("train", train_samples), ("val", val_samples)]:
        split_dir = output_dir / split_name
        split_dir.mkdir(parents=True, exist_ok=True)

        for i, sample in enumerate(samples):
            np.savez_compressed(
                split_dir / f"sample_{i:06d}.npz",
                features=sample["features"].astype(np.float32),
                classes=sample["classes"].astype(np.int64),
                mask=sample["mask"].astype(np.float32),
            )

    # Metadata
    train_classes = np.concatenate([s["classes"][s["mask"] > 0] for s in train_samples])
    val_classes = np.concatenate([s["classes"][s["mask"] > 0] for s in val_samples])

    stats = {
        "num_classes": num_classes,
        "class_names": CLASS_NAMES[num_classes],
        "class_bins": CLASS_BINS[num_classes],
        "class_weights": class_weights.tolist(),
        "n_features": len(FEATURE_NAMES),
        "featuREDACTED": FEATURE_NAMES,
        "local_features": FEATURE_NAMES[:7],
        "context_features": FEATURE_NAMES[7:],
        "source_files": len(all_data),
        "train_files": len(train_files),
        "val_files": len(val_files),
        "train_samples": len(train_samples),
        "val_samples": len(val_samples),
        "window_size": window_size,
        "train_class_dist": {CLASS_NAMES[num_classes][c]: int((train_classes == c).sum()) for c in range(num_classes)},
        "val_class_dist": {CLASS_NAMES[num_classes][c]: int((val_classes == c).sum()) for c in range(num_classes)},
    }

    with open(output_dir / "metadata.json", "w") as f:
        json.dump(stats, f, indent=2)

    np.save(output_dir / "class_weights.npy", class_weights)

    return stats


def main():
    parser = argparse.ArgumentParser(description="Create quality dataset with context features")

    parser.add_argument("--ab1-dir", type=str, default="datalake/ab1")
    parser.add_argument("--output-dir", type=str, default="datalake/datasets/quality_context")
    parser.add_argument("--num-classes", type=int, default=5, choices=[2, 3, 4, 5])
    parser.add_argument("--target-samples", type=int, default=50000)
    parser.add_argument("--window-size", type=int, default=500)

    args = parser.parse_args()

    print("=" * 70)
    print("QUALITY DATASET WITH CONTEXT FEATURES")
    print("=" * 70)
    print(f"\nFeatures ({len(FEATURE_NAMES)} total):")
    print(f"  Local (7):   {', '.join(FEATURE_NAMES[:7])}")
    print(f"  Context (6): {', '.join(FEATURE_NAMES[7:])}")
    print(f"\nClasses ({args.num_classes}):")
    for i, name in enumerate(CLASS_NAMES[args.num_classes]):
        print(f"  {i}: {name}")
    print()

    stats = create_dataset(
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
        print(f"Output: {args.output_dir}")
        print(f"Features: {stats['n_features']} ({len(stats['local_features'])} local + {len(stats['context_features'])} context)")
        print(f"Samples: {stats['train_samples']} train, {stats['val_samples']} val")


if __name__ == "__main__":
    main()
