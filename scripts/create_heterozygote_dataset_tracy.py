#!/usr/bin/env python3
"""
Create heterozygote_training dataset using Tracy for labeling.

Tracy (https://github.com/gear-genomics/tracy) performs Sanger trace deconvolution
and can identify heterozygous positions. We use Tracy's calls as ground truth labels.

Pipeline:
1. Download AB1 files from NCBI Trace Archive (or use local files)
2. Run Tracy decompose on each AB1 to get heterozygote_training calls
3. Extract 4-channel signals from AB1 files
4. Create training dataset with Tracy labels

Requirements:
- Tracy installed and in PATH (conda install -c bioconda tracy)
- Biopython for AB1 parsing

Usage:
    uv run python scripts/create_heterozygote_dataset_tracy.py
    uv run python scripts/create_heterozygote_dataset_tracy.py --ab1-dir datalake/ab1 --output-dir datalake/datasets/heterozygote_real
"""

import argparse
import asyncio
import json
import subprocess
import tempfile
from pathlib import Path

import numpy as np
from Bio import SeqIO

# Try to import our trace client
try:
    from src.external.ncbi_trace import NCBITraceClient
except ImportError:
    NCBITraceClient = None


def check_tracy_installed() -> bool:
    """Check if Tracy is installed and accessible."""
    try:
        result = subprocess.run(
            ["tracy", "--version"],
            captuREDACTED=True,
            text=True,
            timeout=10,
        )
        if result.returncode == 0:
            print(f"Tracy found: {result.stdout.strip()}")
            return True
    except (FileNotFoundError, subprocess.TimeoutExpired):
        pass
    return False


def extract_ab1_signals(ab1_path: Path) -> dict | None:
    """
    Extract 4-channel chromatogram signals from AB1 file.

    Returns:
        dict with keys: sequence, quality, signals (4, seq_len), channel_order
    """
    try:
        record = SeqIO.read(ab1_path, "abi")

        # Get the sequence and quality
        sequence = str(record.seq)
        quality = record.letter_annotations.get("phred_quality", [])

        # Get ABIF raw data
        abif = record.annotations.get("abif_raw", {})

        # Get filter wheel order (channel mapping)
        # FWO_1 contains the order like "GATC" meaning DATA9=G, DATA10=A, etc.
        fwo = "GATC"  # Default
        if "FWO_1" in abif:
            fwo_val = abif["FWO_1"]
            if isinstance(fwo_val, bytes):
                fwo = fwo_val.decode()
            else:
                fwo = str(fwo_val)

        # Get processed trace data (DATA 9-12)
        trace_data = {}
        for i, base in enumerate(fwo):
            key = f"DATA{9 + i}"
            if key in abif:
                trace_data[base] = np.array(abif[key], dtype=np.float32)

        if len(trace_data) != 4:
            # Fallback: try DATA 1-4 (raw data)
            trace_data = {}
            for i, base in enumerate(fwo):
                key = f"DATA{1 + i}"
                if key in abif:
                    trace_data[base] = np.array(abif[key], dtype=np.float32)

        if len(trace_data) != 4:
            print(f"  Warning: Could not extract all 4 channels from {ab1_path.name}")
            return None

        # Get peak locations (PLOC) to map trace positions to base calls
        ploc = None
        if "PLOC1" in abif:
            ploc = np.array(abif["PLOC1"], dtype=np.int32)

        if ploc is None or len(ploc) != len(sequence):
            print(f"  Warning: PLOC mismatch in {ab1_path.name}")
            return None

        # Sample trace data at peak locations to get per-base signals
        seq_len = len(sequence)
        signals = np.zeros((4, seq_len), dtype=np.float32)

        # Order: A, C, G, T (standard order for our model)
        base_order = "ACGT"
        for i, base in enumerate(base_order):
            if base in trace_data:
                trace = trace_data[base]
                for j, pos in enumerate(ploc):
                    if pos < len(trace):
                        signals[i, j] = trace[pos]

        # Normalize signals so they sum to 1 at each position
        signal_sum = signals.sum(axis=0, keepdims=True)
        signal_sum = np.maximum(signal_sum, 1e-6)  # Avoid division by zero
        signals = signals / signal_sum

        return {
            "sequence": sequence,
            "quality": np.array(quality, dtype=np.int32) if quality else np.zeros(seq_len, dtype=np.int32),
            "signals": signals,
            "ploc": ploc,
        }

    except Exception as e:
        print(f"  Error reading {ab1_path.name}: {e}")
        return None


def run_tracy_decompose(ab1_path: Path, reference_path: Path | None = None) -> dict | None:
    """
    Run Tracy decompose to identify heterozygous positions.

    Tracy outputs a BCF/VCF file with variant calls.
    Without a reference, it does de novo decomposition.

    Returns:
        dict with heterozygote_training positions and alleles
    """
    try:
        with tempfile.TemporaryDirectory() as tmpdir:
            output_prefix = Path(tmpdir) / "tracy_out"

            cmd = ["tracy", "decompose"]

            if reference_path:
                cmd.extend(["-r", str(reference_path)])

            cmd.extend(["-o", str(output_prefix), str(ab1_path)])

            result = subprocess.run(
                cmd,
                captuREDACTED=True,
                text=True,
                timeout=60,
            )

            if result.returncode != 0:
                # Tracy may return non-zero for pure sequences (no heterozygotes)
                if "No heterozygous" in result.stderr or result.returncode == 1:
                    return {"positions": [], "alleles": []}
                print(f"  Tracy error: {result.stderr[:200]}")
                return None

            # Parse Tracy output
            # Tracy creates .tsv file with decomposition results
            tsv_path = Path(str(output_prefix) + ".tsv")

            if not tsv_path.exists():
                # No heterozygotes found
                return {"positions": [], "alleles": []}

            positions = []
            alleles = []

            with open(tsv_path) as f:
                header = f.readline()  # Skip header
                for line in f:
                    parts = line.strip().split("\t")
                    if len(parts) >= 4:
                        pos = int(parts[0])  # Position
                        ref = parts[1]  # Reference allele
                        alt = parts[2]  # Alternate allele

                        positions.append(pos)
                        alleles.append((ref, alt))

            return {"positions": positions, "alleles": alleles}

    except subprocess.TimeoutExpired:
        print(f"  Tracy timeout for {ab1_path.name}")
        return None
    except Exception as e:
        print(f"  Tracy error for {ab1_path.name}: {e}")
        return None


def create_sample(
    ab1_data: dict,
    sample_id: str,
) -> dict | None:
    """Create a training sample from AB1 data using IUPAC codes as labels.

    IUPAC ambiguity codes indicate heterozygotes:
    - M = A/C, R = A/G, W = A/T, S = C/G, Y = C/T, K = G/T
    - N = any (ambiguous, often low quality)
    """

    signals = ab1_data["signals"]  # (4, seq_len)
    sequence = ab1_data["sequence"]
    seq_len = signals.shape[1]

    # IUPAC codes for heterozygotes (two bases)
    het_codes = set("MRWSYK")  # Standard 2-base ambiguity codes

    # Create labels: 0 = homozygous, 1 = heterozygous, -1 = ignore (N or other)
    labels = np.zeros(seq_len, dtype=np.int64)
    het_count = 0

    for i, base in enumerate(sequence):
        base_upper = base.upper()
        if base_upper in het_codes:
            labels[i] = 1  # Heterozygous
            het_count += 1
        elif base_upper in "ACGT":
            labels[i] = 0  # Homozygous
        else:
            labels[i] = -1  # N or other ambiguous - ignore in training

    return {
        "sample_id": sample_id,
        "signals": signals,
        "labels": labels,
        "sequence": sequence,
        "quality": ab1_data["quality"],
        "het_count": het_count,
    }


async def download_ab1_files(output_dir: Path, count: int = 100) -> list[Path]:
    """Download AB1 files from NCBI Trace Archive."""

    if NCBITraceClient is None:
        print("NCBI Trace client not available")
        return []

    output_dir.mkdir(parents=True, exist_ok=True)

    client = NCBITraceClient()

    print(f"Downloading {count} AB1 files from NCBI Trace Archive...")

    # Download traces - these should be Homo sapiens Sanger traces
    # which are more likely to have heterozygotes
    downloaded = []

    # Use sample TIDs that are known to work
    sample_tids = [
        # Add more TIDs or use search
    ]

    # For now, use the sample download which has hardcoded TIDs
    stats = await client.download_sample_traces("Homo sapiens", min(count, 10))

    print(f"Downloaded {stats.successful}/{stats.total} traces")

    # The files are uploaded to MinIO, we need to download them locally
    # For now, return empty and expect user to provide local AB1 files

    await client.close()

    return downloaded


def process_ab1_directory(
    ab1_dir: Path,
    output_dir: Path,
    max_samples: int = 50000,
    train_ratio: float = 0.8,
) -> dict:
    """Process all AB1 files in a directory.

    Uses IUPAC ambiguity codes (M, R, W, S, Y, K) as heterozygote_training labels.
    No external tools required.
    """

    # Get AB1 files (deduplicated for case-insensitive filesystems like Windows)
    ab1_files = list({str(f.resolve()).lower(): f for f in ab1_dir.glob("*.ab1")}.values())

    if not ab1_files:
        print(f"No AB1 files found in {ab1_dir}")
        return {"error": "No AB1 files found"}

    print(f"Found {len(ab1_files)} AB1 files")
    print("Using IUPAC ambiguity codes for heterozygote_training labels")
    print("  M=A/C, R=A/G, W=A/T, S=C/G, Y=C/T, K=G/T")

    # Process files
    samples = []
    stats = {
        "total_files": len(ab1_files),
        "processed": 0,
        "failed": 0,
        "total_het_positions": 0,
        "total_positions": 0,
        "files_with_het": 0,
    }

    for i, ab1_path in enumerate(ab1_files[:max_samples]):
        if (i + 1) % 10 == 0:
            print(f"Processing {i + 1}/{len(ab1_files)}...")

        # Extract signals from AB1
        ab1_data = extract_ab1_signals(ab1_path)
        if ab1_data is None:
            stats["failed"] += 1
            continue

        # Create sample using IUPAC codes as labels
        sample = create_sample(ab1_data, sample_id=ab1_path.stem)

        if sample:
            samples.append(sample)
            stats["processed"] += 1
            stats["total_het_positions"] += sample["het_count"]
            stats["total_positions"] += len(sample["sequence"])
            if sample["het_count"] > 0:
                stats["files_with_het"] += 1

    het_ratio = stats["total_het_positions"] / max(stats["total_positions"], 1) * 100
    print(f"\nProcessed {stats['processed']} files, {stats['failed']} failed")
    print(f"Total positions: {stats['total_positions']}")
    print(f"Heterozygote positions: {stats['total_het_positions']} ({het_ratio:.1f}%)")
    print(f"Files with heterozygotes: {stats['files_with_het']}")

    if not samples:
        return {"error": "No samples created"}

    # Split into train/val
    np.random.shuffle(samples)
    split_idx = int(len(samples) * train_ratio)
    train_samples = samples[:split_idx]
    val_samples = samples[split_idx:]

    # Save datasets
    train_dir = output_dir / "train"
    val_dir = output_dir / "val"
    train_dir.mkdir(parents=True, exist_ok=True)
    val_dir.mkdir(parents=True, exist_ok=True)

    for i, sample in enumerate(train_samples):
        np.savez_compressed(
            train_dir / f"sample_{i:06d}.npz",
            signals=sample["signals"],
            labels=sample["labels"],
        )

    for i, sample in enumerate(val_samples):
        np.savez_compressed(
            val_dir / f"sample_{i:06d}.npz",
            signals=sample["signals"],
            labels=sample["labels"],
        )

    print(f"\nSaved {len(train_samples)} train, {len(val_samples)} val samples")

    # Save stats
    stats["train_samples"] = len(train_samples)
    stats["val_samples"] = len(val_samples)

    with open(output_dir / "stats.json", "w") as f:
        json.dump(stats, f, indent=2)

    return stats


def main():
    parser = argparse.ArgumentParser(description="Create heterozygote_training dataset using Tracy")

    parser.add_argument(
        "--ab1-dir",
        type=str,
        default="datalake/ab1",
        help="Directory containing AB1 files",
    )
    parser.add_argument(
        "--output-dir",
        type=str,
        default="datalake/datasets/heterozygote_real",
        help="Output directory for dataset",
    )
    parser.add_argument(
        "--download",
        action="stoREDACTED",
        help="Download AB1 files from NCBI Trace Archive",
    )
    parser.add_argument(
        "--download-count",
        type=int,
        default=100,
        help="Number of AB1 files to download",
    )
    parser.add_argument(
        "--max-samples",
        type=int,
        default=50000,
        help="Maximum number of samples to create",
    )

    args = parser.parse_args()

    ab1_dir = Path(args.ab1_dir)
    output_dir = Path(args.output_dir)

    print("=" * 70)
    print("HETEROZYGOTE DATASET CREATION (Tracy)")
    print("=" * 70)

    # Download if requested
    if args.download:
        asyncio.run(download_ab1_files(ab1_dir, args.download_count))

    # Check for AB1 files
    if not ab1_dir.exists():
        print(f"\nAB1 directory not found: {ab1_dir}")
        print("\nTo get AB1 files:")
        print("1. Download from NCBI Trace Archive manually")
        print("2. Or use --download flag (requires MinIO)")
        print("3. Or copy your own AB1 files to the directory")
        return

    # Process
    stats = process_ab1_directory(
        ab1_dir,
        output_dir,
        max_samples=args.max_samples,
    )

    print("\n" + "=" * 70)
    print("COMPLETE")
    print("=" * 70)

    if "error" not in stats:
        print(f"Dataset saved to: {output_dir}")
        print(f"Train: {stats.get('train_samples', 0)} samples")
        print(f"Val: {stats.get('val_samples', 0)} samples")


if __name__ == "__main__":
    main()
