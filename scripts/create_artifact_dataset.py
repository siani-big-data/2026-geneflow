#!/usr/bin/env python3
"""
Create artifact detection dataset from AB1 files.

Extracts windows from chromatograms with features relevant for artifact detection:
- Dye blobs: Large fluorescent artifacts early in trace
- Pull-up: Crosstalk between channels (false peaks)
- Spikes: Sharp aberrant peaks (electrical noise)
- Baseline drift: Gradual shift in signal baseline
- Mixed sequence: Multiple templates causing double peaks

Output structure:
    output_dir/
        samples/
            sample_000000.npz  # Raw data + features
            sample_000001.npz
            ...
        visualizations/
            sample_000000.png  # For manual labeling
            ...
        labels.json  # Label file (to be filled manually)
        metadata.json

Usage:
    uv run python scripts/create_artifact_dataset.py
    uv run python scripts/create_artifact_dataset.py --ab1-dir datalake/ab1 \\
        --output-dir datalake/datasets/artifacts
"""

import argparse
import json
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Literal

import numpy as np
from Bio import SeqIO

# Optional: matplotlib for visualizations
try:
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    HAS_MATPLOTLIB = True
except ImportError:
    HAS_MATPLOTLIB = False


# =============================================================================
# Artifact Types
# =============================================================================

ArtifactType = Literal[
    "clean",           # No artifacts
    "dye_blob",        # Dye blob artifact
    "pull_up",         # Pull-up/crosstalk
    "spike",           # Electrical spike
    "baseline_drift",  # Baseline drift
    "mixed_sequence",  # Mixed/contaminated sequence
    "low_signal",      # Very low signal (failed)
    "other",           # Other artifact
]

ARTIFACT_TYPES = [
    "clean",
    "dye_blob",
    "pull_up",
    "spike",
    "baseline_drift",
    "mixed_sequence",
    "low_signal",
    "other",
]


# =============================================================================
# Feature Extraction
# =============================================================================

@dataclass
class ArtifactFeatures:
    """Features indicative of artifacts."""

    # Position info
    position_start: int
    position_end: int
    position_relative: float  # 0=start, 1=end of trace

    # Signal statistics
    signal_mean: float
    signal_max: float
    signal_std: float
    signal_snr: float  # Signal-to-noise ratio

    # Cross-channel correlation (pull-up indicator)
    channel_correlation_mean: float
    channel_correlation_max: float

    # Spike detection
    spike_count: int
    spike_max_amplitude: float

    # Baseline
    baseline_mean: float
    baseline_std: float
    baseline_slope: float  # Drift indicator

    # Peak features
    peak_count: int
    peak_spacing_std: float  # Irregular spacing = problem
    secondary_peak_ratio: float  # High = mixed sequence

    # Quality (if available)
    quality_mean: float
    quality_min: float


def extract_raw_traces(ab1_path: Path) -> dict | None:
    """Extract raw trace data from AB1 file."""
    try:
        record = SeqIO.read(ab1_path, "abi")
        abif = record.annotations.get("abif_raw", {})

        # Get filter wheel order
        fwo = "GATC"
        if "FWO_1" in abif:
            fwo_val = abif["FWO_1"]
            fwo = fwo_val.decode() if isinstance(fwo_val, bytes) else str(fwo_val)

        # Get RAW trace data (DATA 1-4) or processed (DATA 9-12)
        # Try processed first, fall back to raw
        traces = {}
        for i, base in enumerate(fwo):
            # Try processed data first (better quality)
            key_processed = f"DATA{9 + i}"
            key_raw = f"DATA{1 + i}"

            if key_processed in abif:
                traces[base] = np.array(abif[key_processed], dtype=np.float32)
            elif key_raw in abif:
                traces[base] = np.array(abif[key_raw], dtype=np.float32)

        if len(traces) != 4:
            return None

        # Stack into array (4, trace_len)
        trace_len = min(len(traces[b]) for b in "ACGT")
        trace_array = np.zeros((4, trace_len), dtype=np.float32)
        for i, base in enumerate("ACGT"):
            trace_array[i] = traces[base][:trace_len]

        # Get peak locations
        ploc = None
        if "PLOC1" in abif:
            ploc = np.array(abif["PLOC1"], dtype=np.int32)

        # Get quality scores
        quality = record.letter_annotations.get("phred_quality", [])
        quality = np.array(quality, dtype=np.float32) if quality else None

        # Get sequence
        sequence = str(record.seq)

        # Get peak amplitudes
        p1am = abif.get("P1AM1")
        p2am = abif.get("P2AM1")

        p1am = np.array(p1am, dtype=np.float32) if p1am else None
        p2am = np.array(p2am, dtype=np.float32) if p2am else None

        return {
            "traces": trace_array,
            "ploc": ploc,
            "quality": quality,
            "sequence": sequence,
            "p1am": p1am,
            "p2am": p2am,
            "filename": ab1_path.name,
        }

    except Exception as e:
        print(f"  Error reading {ab1_path.name}: {e}")
        return None


def compute_artifact_features(
    traces: np.ndarray,
    start: int,
    end: int,
    total_len: int,
    ploc: np.ndarray | None = None,
    quality: np.ndarray | None = None,
    p1am: np.ndarray | None = None,
    p2am: np.ndarray | None = None,
) -> ArtifactFeatures:
    """Compute artifact-indicative features for a window."""

    window = traces[:, start:end]  # (4, window_len)

    # Basic signal stats
    signal_mean = float(window.mean())
    signal_max = float(window.max())
    signal_std = float(window.std())

    # SNR: signal / noise (using high frequencies as noise estimate)
    noise_estimate = np.abs(np.diff(window, axis=1)).mean()
    signal_snr = signal_mean / (noise_estimate + 1e-6)

    # Cross-channel correlation (pull-up indicator)
    correlations = []
    for i in range(4):
        for j in range(i + 1, 4):
            if window[i].std() > 0 and window[j].std() > 0:
                corr = np.corrcoef(window[i], window[j])[0, 1]
                if not np.isnan(corr):
                    correlations.append(abs(corr))

    channel_corr_mean = float(np.mean(correlations)) if correlations else 0.0
    channel_corr_max = float(np.max(correlations)) if correlations else 0.0

    # Spike detection (sharp peaks in derivative)
    derivatives = np.abs(np.diff(window, axis=1))
    threshold = derivatives.mean() + 3 * derivatives.std()
    spikes = derivatives > threshold
    spike_count = int(spikes.sum())
    spike_max = float(derivatives.max())

    # Baseline estimation (10th percentile)
    baseline = np.percentile(window, 10, axis=1)
    baseline_mean = float(baseline.mean())
    baseline_std = float(baseline.std())

    # Baseline slope (drift)
    x = np.arange(window.shape[1])
    slopes = []
    for ch in range(4):
        if len(x) > 1:
            slope = np.polyfit(x, window[ch], 1)[0]
            slopes.append(abs(slope))
    baseline_slope = float(np.mean(slopes)) if slopes else 0.0

    # Peak features (from called positions in window)
    peak_count = 0
    peak_spacing_std = 0.0
    secondary_ratio = 0.0

    if ploc is not None:
        peaks_in_window = ploc[(ploc >= start) & (ploc < end)]
        peak_count = len(peaks_in_window)

        if len(peaks_in_window) > 1:
            spacings = np.diff(peaks_in_window)
            peak_spacing_std = float(spacings.std())

        # Secondary peak ratio (from p1am/p2am)
        if p1am is not None and p2am is not None:
            # Find which bases are in this window
            base_indices = np.where((ploc >= start) & (ploc < end))[0]
            if len(base_indices) > 0:
                p1_window = p1am[base_indices]
                p2_window = p2am[base_indices]
                # Ratio of secondary to primary (high = mixed)
                ratios = p2_window / (p1_window + 1.0)
                secondary_ratio = float(ratios.mean())

    # Quality stats
    quality_mean = 0.0
    quality_min = 0.0

    if quality is not None and ploc is not None:
        base_indices = np.where((ploc >= start) & (ploc < end))[0]
        if len(base_indices) > 0 and base_indices.max() < len(quality):
            valid_indices = base_indices[base_indices < len(quality)]
            if len(valid_indices) > 0:
                q_window = quality[valid_indices]
                quality_mean = float(q_window.mean())
                quality_min = float(q_window.min())

    return ArtifactFeatures(
        position_start=start,
        position_end=end,
        position_relative=start / total_len,
        signal_mean=signal_mean,
        signal_max=signal_max,
        signal_std=signal_std,
        signal_snr=signal_snr,
        channel_correlation_mean=channel_corr_mean,
        channel_correlation_max=channel_corr_max,
        spike_count=spike_count,
        spike_max_amplitude=spike_max,
        baseline_mean=baseline_mean,
        baseline_std=baseline_std,
        baseline_slope=baseline_slope,
        peak_count=peak_count,
        peak_spacing_std=peak_spacing_std,
        secondary_peak_ratio=secondary_ratio,
        quality_mean=quality_mean,
        quality_min=quality_min,
    )


def heuristic_artifact_label(features: ArtifactFeatures) -> tuple[str, float]:
    """
    Heuristic pre-labeling based on features.

    Returns (label, confidence) where confidence is 0-1.
    These are SUGGESTIONS - manual review is needed!
    """

    # Dye blob: early position + high signal + high correlation
    if features.position_relative < 0.15:
        if features.signal_max > 5000 and features.channel_correlation_max > 0.7:
            return "dye_blob", 0.7

    # Low signal: very low amplitude
    if features.signal_max < 100:
        return "low_signal", 0.8

    # Spikes: many spike detections
    if features.spike_count > 50:
        return "spike", 0.6

    # Pull-up: high cross-channel correlation throughout
    if features.channel_correlation_mean > 0.6:
        return "pull_up", 0.5

    # Mixed sequence: high secondary peak ratio
    if features.secondary_peak_ratio > 0.4:
        return "mixed_sequence", 0.5

    # Baseline drift: high slope
    if features.baseline_slope > 1.0:
        return "baseline_drift", 0.5

    # Low quality region (potential artifact)
    if features.quality_mean < 15 and features.quality_mean > 0:
        return "other", 0.3

    # Probably clean
    return "clean", 0.4


# =============================================================================
# Visualization
# =============================================================================

def create_visualization(
    traces: np.ndarray,
    start: int,
    end: int,
    features: ArtifactFeatures,
    suggested_label: str,
    output_path: Path,
    ploc: np.ndarray | None = None,
    quality: np.ndarray | None = None,
):
    """Create visualization for manual labeling."""

    if not HAS_MATPLOTLIB:
        return

    window = traces[:, start:end]
    x = np.arange(start, end)

    fig, axes = plt.subplots(2, 1, figsize=(14, 8), height_ratios=[3, 1])

    # Main trace plot
    ax1 = axes[0]
    colors = ['green', 'blue', 'black', 'red']  # A, C, G, T
    labels = ['A', 'C', 'G', 'T']

    for i, (color, label) in enumerate(zip(colors, labels)):
        ax1.plot(x, window[i], color=color, label=label, alpha=0.7, linewidth=0.8)

    ax1.set_xlim(start, end)
    ax1.set_ylabel("Signal Intensity")
    ax1.legend(loc='upper right')
    ax1.set_title(f"Window [{start}:{end}] - Suggested: {suggested_label.upper()}")
    ax1.grid(True, alpha=0.3)

    # Mark peak locations if available
    if ploc is not None:
        peaks_in_window = ploc[(ploc >= start) & (ploc < end)]
        for p in peaks_in_window:
            ax1.axvline(p, color='gray', alpha=0.2, linewidth=0.5)

    # Feature summary plot
    ax2 = axes[1]

    corr_mean = features.channel_correlation_mean
    corr_max = features.channel_correlation_max
    featuREDACTED = (
        f"Position: {features.position_relative:.1%} | "
        f"SNR: {features.signal_snr:.1f} | "
        f"Ch.Corr: {corr_mean:.2f} (max: {corr_max:.2f}) | "
        f"Spikes: {features.spike_count} | "
        f"2nd Peak Ratio: {features.secondary_peak_ratio:.2f} | "
        f"Quality: {features.quality_mean:.0f}"
    )

    ax2.text(0.5, 0.5, featuREDACTED, transform=ax2.transAxes,
             fontsize=10, ha='center', va='center',
             bbox=dict(boxstyle='round', facecolor='wheat', alpha=0.5))
    ax2.axis('off')

    plt.tight_layout()
    fig.savefig(output_path, dpi=100, bbox_inches='tight')
    plt.close()


# =============================================================================
# Dataset Creation
# =============================================================================

def create_artifact_dataset(
    ab1_dir: Path,
    output_dir: Path,
    window_size: int = 1000,
    stride: int = 500,
    max_samples_per_file: int = 10,
    visualize: bool = True,
) -> dict:
    """Create artifact detection dataset."""

    # Setup directories
    samples_dir = output_dir / "samples"
    viz_dir = output_dir / "visualizations"
    samples_dir.mkdir(parents=True, exist_ok=True)
    if visualize and HAS_MATPLOTLIB:
        viz_dir.mkdir(parents=True, exist_ok=True)

    # Get AB1 files
    ab1_files = list(ab1_dir.glob("*.ab1"))
    if not ab1_files:
        return {"error": f"No AB1 files found in {ab1_dir}"}

    print(f"Found {len(ab1_files)} AB1 files")

    samples = []
    labels_data = {}
    sample_idx = 0

    for ab1_path in ab1_files:
        print(f"  Processing {ab1_path.name}...")

        data = extract_raw_traces(ab1_path)
        if data is None:
            continue

        traces = data["traces"]
        trace_len = traces.shape[1]

        if trace_len < window_size:
            continue

        # Extract windows
        file_samples = 0
        for start in range(0, trace_len - window_size + 1, stride):
            if file_samples >= max_samples_per_file:
                break

            end = start + window_size

            # Compute features
            features = compute_artifact_features(
                traces=traces,
                start=start,
                end=end,
                total_len=trace_len,
                ploc=data["ploc"],
                quality=data["quality"],
                p1am=data["p1am"],
                p2am=data["p2am"],
            )

            # Heuristic label
            suggested_label, confidence = heuristic_artifact_label(features)

            # Sample ID
            sample_id = f"sample_{sample_idx:06d}"

            # Save raw data
            np.savez_compressed(
                samples_dir / f"{sample_id}.npz",
                traces=traces[:, start:end].astype(np.float32),
                features=np.array([
                    features.position_relative,
                    features.signal_mean,
                    features.signal_max,
                    features.signal_std,
                    features.signal_snr,
                    features.channel_correlation_mean,
                    features.channel_correlation_max,
                    features.spike_count,
                    features.spike_max_amplitude,
                    features.baseline_mean,
                    features.baseline_std,
                    features.baseline_slope,
                    features.peak_count,
                    features.peak_spacing_std,
                    features.secondary_peak_ratio,
                    features.quality_mean,
                    features.quality_min,
                ], dtype=np.float32),
                source_file=data["filename"],
                position_start=start,
                position_end=end,
            )

            # Create visualization
            if visualize and HAS_MATPLOTLIB:
                create_visualization(
                    traces=traces,
                    start=start,
                    end=end,
                    features=features,
                    suggested_label=suggested_label,
                    output_path=viz_dir / f"{sample_id}.png",
                    ploc=data["ploc"],
                    quality=data["quality"],
                )

            # Add to labels (to be verified manually)
            labels_data[sample_id] = {
                "source_file": data["filename"],
                "position": [start, end],
                "suggested_label": suggested_label,
                "confidence": confidence,
                "verified": False,
                "label": None,  # To be filled manually
                "notes": "",
            }

            samples.append({
                "id": sample_id,
                "features": asdict(features),
                "suggested_label": suggested_label,
            })

            sample_idx += 1
            file_samples += 1

    # Save labels file
    with open(output_dir / "labels.json", "w") as f:
        json.dump(labels_data, f, indent=2)

    # Summary by suggested label
    label_counts = {}
    for s in samples:
        lbl = s["suggested_label"]
        label_counts[lbl] = label_counts.get(lbl, 0) + 1

    # Metadata
    metadata = {
        "total_samples": len(samples),
        "ab1_files_processed": len(ab1_files),
        "window_size": window_size,
        "stride": stride,
        "artifact_types": ARTIFACT_TYPES,
        "featuREDACTED": [
            "position_relative",
            "signal_mean",
            "signal_max",
            "signal_std",
            "signal_snr",
            "channel_correlation_mean",
            "channel_correlation_max",
            "spike_count",
            "spike_max_amplitude",
            "baseline_mean",
            "baseline_std",
            "baseline_slope",
            "peak_count",
            "peak_spacing_std",
            "secondary_peak_ratio",
            "quality_mean",
            "quality_min",
        ],
        "suggested_label_distribution": label_counts,
        "labeling_instructions": (
            "1. Open visualizations/ folder\n"
            "2. Review each sample image\n"
            "3. Edit labels.json: set 'label' field and 'verified': true\n"
            "4. Artifact types: " + ", ".join(ARTIFACT_TYPES)
        ),
    }

    with open(output_dir / "metadata.json", "w") as f:
        json.dump(metadata, f, indent=2)

    return metadata


# =============================================================================
# Main
# =============================================================================

def main():
    parser = argparse.ArgumentParser(
        description="Create artifact detection dataset from AB1 files"
    )

    parser.add_argument(
        "--ab1-dir", type=str, default="datalake/ab1",
        help="Directory containing AB1 files"
    )
    parser.add_argument(
        "--output-dir", type=str, default="datalake/datasets/artifacts",
        help="Output directory for dataset"
    )
    parser.add_argument(
        "--window-size", type=int, default=1000,
        help="Window size in trace points"
    )
    parser.add_argument(
        "--stride", type=int, default=500,
        help="Stride between windows"
    )
    parser.add_argument(
        "--max-per-file", type=int, default=10,
        help="Maximum samples per AB1 file"
    )
    parser.add_argument(
        "--no-viz", action="stoREDACTED",
        help="Skip visualization generation"
    )

    args = parser.parse_args()

    print("=" * 70)
    print("ARTIFACT DETECTION DATASET GENERATOR")
    print("=" * 70)
    print()
    print("Artifact types to detect:")
    for i, atype in enumerate(ARTIFACT_TYPES):
        print(f"  {i}. {atype}")
    print()
    print(f"Input: {args.ab1_dir}")
    print(f"Output: {args.output_dir}")
    print(f"Window: {args.window_size} points, stride {args.stride}")
    print()

    if not HAS_MATPLOTLIB:
        print("WARNING: matplotlib not available, skipping visualizations")

    stats = create_artifact_dataset(
        ab1_dir=Path(args.ab1_dir),
        output_dir=Path(args.output_dir),
        window_size=args.window_size,
        stride=args.stride,
        max_samples_per_file=args.max_per_file,
        visualize=not args.no_viz and HAS_MATPLOTLIB,
    )

    print()
    print("=" * 70)

    if "error" in stats:
        print(f"ERROR: {stats['error']}")
        return

    print("DATASET CREATED")
    print("=" * 70)
    print(f"Total samples: {stats['total_samples']}")
    print(f"AB1 files processed: {stats['ab1_files_processed']}")
    print()
    print("Suggested label distribution (HEURISTIC - needs manual verification):")
    for label, count in stats["suggested_label_distribution"].items():
        pct = 100 * count / stats["total_samples"]
        print(f"  {label:20s}: {count:4d} ({pct:5.1f}%)")
    print()
    print("NEXT STEPS:")
    print("  1. Review visualizations in: " + str(Path(args.output_dir) / "visualizations"))
    print("  2. Edit labels in: " + str(Path(args.output_dir) / "labels.json"))
    print("  3. Set 'label' field for each sample and 'verified': true")
    print("  4. Run training script (to be created)")


if __name__ == "__main__":
    main()
