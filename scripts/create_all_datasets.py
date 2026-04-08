#!/usr/bin/env python3
"""
Create training datasets for all ML models from FASTA/FASTQ data.

Uses data augmentation to generate training data for:
- Trimming: Quality-based trim point prediction
- Heterozygote: Per-position heterozygosity detection
- Variants: Variant type classification (SNP/Ins/Del/Complex)
- ORF: Open reading frame detection
- Consensus: Consensus sequence prediction from multiple reads
- Alignment: Sequence similarity scoring

Usage:
    uv run python scripts/create_all_datasets.py
    uv run python scripts/create_all_datasets.py --models trimming orf
    uv run python scripts/create_all_datasets.py --samples-per-model 10000
"""

import argparse
import gzip
import json
import random
import sys
from pathlib import Path
from typing import Iterator

import numpy as np

# Add project root to path
sys.path.insert(0, str(Path(__file__).parent.parent))


# =============================================================================
# Constants
# =============================================================================

NUCLEOTIDES = ["A", "C", "G", "T"]
NUCLEOTIDE_MAP = {"A": 0, "C": 1, "G": 2, "T": 3, "N": 4}
START_CODONS = {"ATG"}
STOP_CODONS = {"TAA", "TAG", "TGA"}

DEFAULT_FASTQ_DIR = Path("datalake/fastq_real")
DEFAULT_FASTA_DIR = Path("datalake/curated")
DEFAULT_OUTPUT_DIR = Path("datalake/datasets")


# =============================================================================
# Data Loading
# =============================================================================

def read_fastq(path: Path) -> Iterator[tuple[str, str, np.ndarray]]:
    """Read FASTQ file, yield (name, sequence, quality_scores)."""
    opener = gzip.open if str(path).endswith('.gz') else open

    with opener(path, 'rt') as f:
        while True:
            header = f.readline().strip()
            if not header:
                break
            seq = f.readline().strip()
            f.readline()  # + line
            qual_str = f.readline().strip()

            # Convert quality string to Phred scores
            quality = np.array([ord(c) - 33 for c in qual_str], dtype=np.float32)
            name = header[1:].split()[0]

            yield name, seq.upper(), quality


def read_fasta(path: Path) -> Iterator[tuple[str, str]]:
    """Read FASTA file, yield (name, sequence)."""
    opener = gzip.open if str(path).endswith('.gz') else open

    with opener(path, 'rt') as f:
        name = None
        seq_parts = []

        for line in f:
            line = line.strip()
            if line.startswith('>'):
                if name is not None:
                    yield name, ''.join(seq_parts).upper()
                name = line[1:].split()[0]
                seq_parts = []
            else:
                seq_parts.append(line)

        if name is not None:
            yield name, ''.join(seq_parts).upper()


def load_all_fastq(
    fastq_dir: Path, max_samples: int = None
) -> list[tuple[str, str, np.ndarray]]:
    """Load all FASTQ files from directory."""
    samples = []

    for fq_file in fastq_dir.glob("*.fastq*"):
        for name, seq, qual in read_fastq(fq_file):
            if len(seq) >= 100:  # Minimum length
                samples.append((name, seq, qual))
                if max_samples and len(samples) >= max_samples:
                    return samples

    return samples


def load_all_fasta(fasta_dir: Path, max_samples: int = None) -> list[tuple[str, str]]:
    """Load all FASTA files from directory (recursively)."""
    samples = []

    for fa_file in fasta_dir.rglob("*.fasta*"):
        for name, seq in read_fasta(fa_file):
            if len(seq) >= 100:
                samples.append((name, seq))
                if max_samples and len(samples) >= max_samples:
                    return samples

    # Also check .fa files
    for fa_file in fasta_dir.rglob("*.fa*"):
        for name, seq in read_fasta(fa_file):
            if len(seq) >= 100:
                samples.append((name, seq))
                if max_samples and len(samples) >= max_samples:
                    return samples

    return samples


# =============================================================================
# Data Augmentation Utilities
# =============================================================================

def mutate_sequence(seq: str, mutation_rate: float = 0.01) -> str:
    """Introduce random mutations into a sequence."""
    seq_list = list(seq)
    for i in range(len(seq_list)):
        if random.random() < mutation_rate:
            seq_list[i] = random.choice(NUCLEOTIDES)
    return ''.join(seq_list)


def add_quality_noise(quality: np.ndarray, noise_std: float = 3.0) -> np.ndarray:
    """Add noise to quality scores."""
    noisy = quality + np.random.normal(0, noise_std, len(quality))
    return np.clip(noisy, 0, 60).astype(np.float32)


def simulate_quality_profile(length: int, pattern: str = "random") -> np.ndarray:
    """Generate synthetic quality profile."""
    if pattern == "good":
        # High quality throughout
        base = np.random.uniform(30, 40, length)
    elif pattern == "degrading":
        # Quality degrades towards end
        base = np.linspace(35, 10, length) + np.random.normal(0, 3, length)
    elif pattern == "bad_start":
        # Low quality at start
        base = np.linspace(10, 35, length) + np.random.normal(0, 3, length)
    elif pattern == "bad_ends":
        # Low quality at both ends
        x = np.linspace(0, 1, length)
        base = 35 - 25 * (4 * (x - 0.5) ** 2) + np.random.normal(0, 3, length)
    else:  # random
        base = np.random.uniform(15, 40, length)

    return np.clip(base, 0, 60).astype(np.float32)


def encode_sequence_onehot(seq: str, max_length: int = None) -> np.ndarray:
    """Encode sequence as one-hot (4, seq_len)."""
    if max_length:
        seq = seq[:max_length]

    onehot = np.zeros((4, len(seq)), dtype=np.float32)
    for i, base in enumerate(seq):
        if base in NUCLEOTIDE_MAP and NUCLEOTIDE_MAP[base] < 4:
            onehot[NUCLEOTIDE_MAP[base], i] = 1.0

    return onehot


# =============================================================================
# Dataset Generators
# =============================================================================

def generate_trimming_dataset(
    fastq_samples: list,
    num_samples: int,
    output_dir: Path,
    quality_threshold: float = 20.0,
    window_size: int = 10,
    train_ratio: float = 0.85,
    val_ratio: float = 0.15,
    label_noise_std: float = 0.03,
):
    """
    Generate trimming dataset with FILE-LEVEL split to prevent data leakage.

    Labels are (start_ratio, end_ratio) indicating optimal trim points.
    Noise is added to labels to simulate human annotation variability.
    """
    print(f"\n{'='*60}")
    print("Generating TRIMMING dataset (FILE-LEVEL SPLIT)")
    print(f"{'='*60}")

    output_dir.mkdir(parents=True, exist_ok=True)

    def find_trim_points(quality: np.ndarray, threshold: float, window: int) -> tuple[float, float]:
        """Find optimal trim points based on quality threshold."""
        n = len(quality)

        # Find start: first position where rolling mean >= threshold
        start = 0
        for i in range(n - window):
            if np.mean(quality[i:i+window]) >= threshold:
                start = i
                break

        # Find end: last position where rolling mean >= threshold
        end = n
        for i in range(n - 1, window - 1, -1):
            if np.mean(quality[i-window:i]) >= threshold:
                end = i
                break

        # Convert to ratios
        start_ratio = start / n
        end_ratio = end / n

        return start_ratio, end_ratio

    def add_label_noise(
        start_ratio: float, end_ratio: float, noise_std: float
    ) -> tuple[float, float]:
        """Add realistic noise to trim point labels.

        Simulates variability in human annotation:
        - Gaussian noise with variable std
        - Occasional larger deviations (outliers)
        - Asymmetric noise (start and end independent)
        """
        # Base noise for each endpoint (independent)
        start_noise = random.gauss(0, noise_std)
        end_noise = random.gauss(0, noise_std)

        # Occasionally add larger noise (simulates ambiguous cases)
        if random.random() < 0.15:
            start_noise += random.gauss(0, noise_std * 2)
        if random.random() < 0.15:
            end_noise += random.gauss(0, noise_std * 2)

        # Apply noise
        noisy_start = start_ratio + start_noise
        noisy_end = end_ratio + end_noise

        # Ensure valid range [0, 1]
        noisy_start = max(0.0, min(0.5, noisy_start))
        noisy_end = max(0.5, min(1.0, noisy_end))

        # Ensure minimum trim region (at least 30% of sequence)
        if noisy_end - noisy_start < 0.3:
            # Expand symmetrically
            center = (noisy_start + noisy_end) / 2
            noisy_start = max(0.0, center - 0.15)
            noisy_end = min(1.0, center + 0.15)

        return noisy_start, noisy_end

    # Quality patterns for augmentation
    patterns = ["good", "degrading", "bad_start", "bad_ends", "random"]

    # STEP 1: Split samples at SOURCE level to prevent data leakage
    # Shuffle and split the source samples FIRST
    random.shuffle(fastq_samples)
    n_train = int(len(fastq_samples) * train_ratio)

    train_sources = fastq_samples[:n_train]
    val_sources = fastq_samples[n_train:]

    print(f"  Source split: train={len(train_sources)}, val={len(val_sources)}")
    print(f"  Label noise std: {label_noise_std} ({label_noise_std*100:.1f}% of sequence)")

    # STEP 2: Generate samples from each split independently
    def generate_samples_from_sources(sources, target_count, split_name):
        """Generate augmented samples only from the given source samples."""
        samples = []
        source_idx = 0
        attempts = 0
        max_attempts = target_count * 3

        while len(samples) < target_count and attempts < max_attempts:
            _, seq, quality = sources[source_idx % len(sources)]

            # Vary threshold and window for each sample (adds variability)
            sample_threshold = quality_threshold + random.uniform(-3, 3)
            sample_window = window_size + random.randint(-3, 3)
            sample_window = max(5, sample_window)

            # Augmentation options
            if random.random() < 0.5:
                aug_quality = add_quality_noise(quality, noise_std=random.uniform(1, 5))
            else:
                pattern = random.choice(patterns)
                aug_quality = simulate_quality_profile(len(seq), pattern)

            # Compute base trim points
            start_ratio, end_ratio = find_trim_points(aug_quality, sample_threshold, sample_window)

            # Skip if base trim points are invalid
            if end_ratio <= start_ratio or end_ratio - start_ratio < 0.3:
                source_idx += 1
                attempts += 1
                continue

            # Add noise to labels (simulates human annotation variability)
            noisy_start, noisy_end = add_label_noise(start_ratio, end_ratio, label_noise_std)

            samples.append({
                'quality': aug_quality,
                'start_ratio': noisy_start,
                'end_ratio': noisy_end,
                'length': len(seq),
            })

            source_idx += 1
            attempts += 1

        return samples

    # Calculate target counts per split
    n_train_samples = int(num_samples * train_ratio)
    n_val_samples = num_samples - n_train_samples

    print(f"  Generating train samples ({n_train_samples})...")
    train_samples = generate_samples_from_sources(train_sources, n_train_samples, "train")

    print(f"  Generating val samples ({n_val_samples})...")
    val_samples = generate_samples_from_sources(val_sources, n_val_samples, "val")

    # STEP 3: Save samples
    for split in ["train", "val"]:
        split_dir = output_dir / split
        split_dir.mkdir(exist_ok=True)

    split_data = {"train": train_samples, "val": val_samples}
    split_counts = {}

    sample_idx = 0
    for split_name, samples in split_data.items():
        split_counts[split_name] = len(samples)
        for sample in samples:
            sample_path = output_dir / split_name / f"sample_{sample_idx:06d}.npz"
            np.savez_compressed(
                sample_path,
                quality=sample['quality'],
                start_ratio=np.array([sample['start_ratio']], dtype=np.float32),
                end_ratio=np.array([sample['end_ratio']], dtype=np.float32),
                length=np.array([sample['length']], dtype=np.int32),
            )
            sample_idx += 1

    # Save metadata
    metadata = {
        "model": "trimming",
        "num_samples": sum(split_counts.values()),
        "splits": split_counts,
        "source_splits": {
            "train": len(train_sources),
            "val": len(val_sources),
        },
        "quality_threshold": quality_threshold,
        "window_size": window_size,
        "split_method": "source-level (no data leakage)",
    }
    with open(output_dir / "metadata.json", "w") as f:
        json.dump(metadata, f, indent=2)

    print(f"  Saved {sum(split_counts.values())} samples to {output_dir}")
    print(f"  Splits: {split_counts}")


def generate_heterozygote_dataset(
    fastq_samples: list,
    num_samples: int,
    output_dir: Path,
    het_rate: float = 0.05,
    max_length: int = 500,
    train_ratio: float = 0.85,
    val_ratio: float = 0.15,
):
    """
    Generate heterozygote_training detection dataset with FILE-LEVEL split to prevent data leakage.

    Simulates 4-channel chromatogram data (A, T, C, G intensities).
    Heterozygous positions have two high signals instead of one.
    Labels are per-position: 0=homozygous, 1=heterozygous.
    """
    print(f"\n{'='*60}")
    print("Generating HETEROZYGOTE dataset (FILE-LEVEL SPLIT)")
    print(f"{'='*60}")

    output_dir.mkdir(parents=True, exist_ok=True)

    def simulate_chromatogram(seq: str, quality: np.ndarray, het_positions: list) -> np.ndarray:
        """Simulate 4-channel normalized chromatogram signals with realistic noise.

        Returns (4, seq_len): Normalized signals (A, C, G, T) - sum to 1 per position.
        Includes overlapping distributions to make classification non-trivial.
        """
        seq_len = len(seq)
        raw_signals = np.zeros((4, seq_len), dtype=np.float32)

        base_to_idx = {'A': 0, 'C': 1, 'G': 2, 'T': 3}

        for i, base in enumerate(seq):
            if base not in base_to_idx:
                raw_signals[:, i] = np.random.uniform(20, 50, 4)
                continue

            base_idx = base_to_idx[base]
            base_intensity = random.uniform(100, 1000)

            if i in het_positions:
                # Heterozygous: two signals, but with MORE VARIATION
                other_bases = [b for b in range(4) if b != base_idx]
                other_base = random.choice(other_bases)

                # Ratio varies MORE (0.5-1.0) - some hets look almost homo
                ratio = random.uniform(0.5, 0.98)

                # More noise in signal levels
                raw_signals[base_idx, i] = base_intensity + random.gauss(0, base_intensity * 0.15)
                noise = random.gauss(0, base_intensity * 0.15)
                raw_signals[other_base, i] = base_intensity * ratio + noise

                # Higher noise floor for other channels
                for b in other_bases:
                    if b != other_base:
                        noise_level = base_intensity * random.uniform(0.1, 0.35)
                        raw_signals[b, i] = noise_level + random.gauss(0, noise_level * 0.4)
            else:
                # Homozygous: but sometimes with HIGH secondary signal (looks like het)
                raw_signals[base_idx, i] = base_intensity + random.gauss(0, base_intensity * 0.15)

                # Secondary signal can be quite high sometimes (overlap with het)
                secondary_ratios = [random.uniform(0.1, 0.55) for _ in range(3)]
                for j, b in enumerate([x for x in range(4) if x != base_idx]):
                    noise_level = base_intensity * secondary_ratios[j]
                    raw_signals[b, i] = noise_level + random.gauss(0, noise_level * 0.4)

        # Clip negative values
        raw_signals = np.clip(raw_signals, 0, None)

        # Add global noise (baseline wobble)
        global_noise = np.random.normal(0, 0.02, raw_signals.shape)
        raw_signals = raw_signals + np.abs(global_noise) * raw_signals.max()

        # Normalize per position (sum to 1)
        for i in range(seq_len):
            total = raw_signals[:, i].sum() + 1e-8
            raw_signals[:, i] = raw_signals[:, i] / total

        return raw_signals

    # STEP 1: Split samples at SOURCE level to prevent data leakage
    random.shuffle(fastq_samples)
    n_train = int(len(fastq_samples) * train_ratio)

    train_sources = fastq_samples[:n_train]
    val_sources = fastq_samples[n_train:]

    print(f"  Source split: train={len(train_sources)}, val={len(val_sources)}")

    # STEP 2: Generate samples from each split independently
    def generate_samples_from_sources(sources, target_count, split_name):
        """Generate samples only from the given source samples."""
        samples = []
        source_idx = 0
        attempts = 0
        max_attempts = target_count * 3

        while len(samples) < target_count and attempts < max_attempts:
            _, seq, quality = sources[source_idx % len(sources)]

            # Truncate/pad to max_length
            if len(seq) > max_length:
                start = random.randint(0, len(seq) - max_length)
                seq = seq[start:start + max_length]
                quality = quality[start:start + max_length]

            seq_len = len(seq)

            # Generate heterozygote_training labels
            labels = np.zeros(seq_len, dtype=np.int64)
            het_positions = set()

            for i in range(seq_len):
                if random.random() < het_rate:
                    labels[i] = 1
                    het_positions.add(i)

            # Simulate chromatogram signals
            signals = simulate_chromatogram(seq, quality, het_positions)

            samples.append({
                'signals': signals,
                'labels': labels,
                'length': seq_len,
            })

            source_idx += 1
            attempts += 1

        return samples

    # Calculate target counts per split
    n_train_samples = int(num_samples * train_ratio)
    n_val_samples = num_samples - n_train_samples

    for split in ["train", "val"]:
        split_dir = output_dir / split
        split_dir.mkdir(exist_ok=True)

    print(f"  Generating train samples ({n_train_samples})...")
    train_samples = generate_samples_from_sources(train_sources, n_train_samples, "train")

    print(f"  Generating val samples ({n_val_samples})...")
    val_samples = generate_samples_from_sources(val_sources, n_val_samples, "val")

    # STEP 3: Save samples
    split_data = {"train": train_samples, "val": val_samples}
    split_counts = {}

    sample_idx = 0
    for split_name, samples in split_data.items():
        split_counts[split_name] = len(samples)
        for sample in samples:
            sample_path = output_dir / split_name / f"sample_{sample_idx:06d}.npz"
            np.savez_compressed(
                sample_path,
                signals=sample['signals'],
                labels=sample['labels'],
                length=np.array([sample['length']], dtype=np.int32),
            )
            sample_idx += 1

    # Save metadata
    metadata = {
        "model": "heterozygote_training",
        "num_samples": sum(split_counts.values()),
        "splits": split_counts,
        "source_splits": {
            "train": len(train_sources),
            "val": len(val_sources),
        },
        "het_rate": het_rate,
        "max_length": max_length,
        "num_channels": 4,
        "channels": ["A_norm", "C_norm", "G_norm", "T_norm"],
        "num_classes": 2,
        "class_labels": ["homozygous", "heterozygous"],
        "split_method": "source-level (no data leakage)",
    }
    with open(output_dir / "metadata.json", "w") as f:
        json.dump(metadata, f, indent=2)

    print(f"  Saved {sum(split_counts.values())} samples to {output_dir}")
    print(f"  Splits: {split_counts}")


def generate_variants_dataset(
    fasta_samples: list,
    num_samples: int,
    output_dir: Path,
    context_size: int = 20,
    train_ratio: float = 0.85,
    val_ratio: float = 0.15,
):
    """
    Generate variant classification dataset with FILE-LEVEL split to prevent data leakage.

    Creates variants by modifying reference sequences.
    Labels: 0=SNP, 1=Insertion, 2=Deletion, 3=Complex
    """
    print(f"\n{'='*60}")
    print("Generating VARIANTS dataset (FILE-LEVEL SPLIT)")
    print(f"{'='*60}")

    output_dir.mkdir(parents=True, exist_ok=True)

    def create_variant(seq: str, pos: int, var_type: int) -> tuple[str, str, str]:
        """Create a variant at position. Returns (ref_context, alt_context)."""
        context_start = max(0, pos - context_size)
        context_end = min(len(seq), pos + context_size + 1)

        ref_context = seq[context_start:context_end]

        if var_type == 0:  # SNP
            ref_allele = seq[pos]
            alt_allele = random.choice([b for b in NUCLEOTIDES if b != ref_allele])
            alt_seq = seq[:pos] + alt_allele + seq[pos+1:]

        elif var_type == 1:  # Insertion
            ref_allele = seq[pos]
            insert_len = random.randint(1, 5)
            insertion = ''.join(random.choices(NUCLEOTIDES, k=insert_len))
            alt_allele = ref_allele + insertion
            alt_seq = seq[:pos+1] + insertion + seq[pos+1:]

        elif var_type == 2:  # Deletion
            del_len = random.randint(1, 5)
            del_len = min(del_len, len(seq) - pos - 1)
            ref_allele = seq[pos:pos+del_len+1]
            alt_allele = seq[pos]
            alt_seq = seq[:pos+1] + seq[pos+del_len+1:]

        else:  # Complex (SNP + small indel)
            ref_allele = seq[pos:pos+2]
            rand_len = random.randint(1, 3)
            suffix = ''.join(random.choices(NUCLEOTIDES, k=rand_len))
            alt_allele = random.choice(NUCLEOTIDES) + suffix
            alt_seq = seq[:pos] + alt_allele + seq[pos+2:]

        alt_context = alt_seq[context_start:context_start + len(ref_context)]

        return ref_context, alt_context

    # STEP 1: Split samples at SOURCE level to prevent data leakage
    random.shuffle(fasta_samples)
    n_train = int(len(fasta_samples) * train_ratio)

    train_sources = fasta_samples[:n_train]
    val_sources = fasta_samples[n_train:]

    print(f"  Source split: train={len(train_sources)}, val={len(val_sources)}")

    # STEP 2: Generate samples from each split independently
    def generate_samples_from_sources(sources, target_count, split_name):
        """Generate samples only from the given source samples."""
        samples = []
        class_counts = {0: 0, 1: 0, 2: 0, 3: 0}
        source_idx = 0
        attempts = 0
        max_attempts = target_count * 5

        while len(samples) < target_count and attempts < max_attempts:
            _, seq = sources[source_idx % len(sources)]

            # Skip if too short
            if len(seq) < context_size * 3:
                source_idx += 1
                attempts += 1
                continue

            # Pick random position (avoiding edges)
            pos = random.randint(context_size, len(seq) - context_size - 10)

            # Pick variant type (balanced)
            var_type = len(samples) % 4

            try:
                ref_context, alt_context = create_variant(seq, pos, var_type)
            except Exception:
                source_idx += 1
                attempts += 1
                continue

            # Encode contexts
            ref_onehot = encode_sequence_onehot(ref_context, context_size * 2 + 1)
            alt_onehot = encode_sequence_onehot(alt_context, context_size * 2 + 1)

            # Pad if needed
            target_len = context_size * 2 + 1
            if ref_onehot.shape[1] < target_len:
                pad = target_len - ref_onehot.shape[1]
                ref_onehot = np.pad(ref_onehot, ((0, 0), (0, pad)))
            if alt_onehot.shape[1] < target_len:
                pad = target_len - alt_onehot.shape[1]
                alt_onehot = np.pad(alt_onehot, ((0, 0), (0, pad)))

            samples.append({
                'ref_context': ref_onehot[:, :target_len],
                'alt_context': alt_onehot[:, :target_len],
                'var_type': var_type,
            })
            class_counts[var_type] += 1

            source_idx += 1
            attempts += 1

        return samples, class_counts

    # Calculate target counts per split
    n_train_samples = int(num_samples * train_ratio)
    n_val_samples = num_samples - n_train_samples

    for split in ["train", "val"]:
        split_dir = output_dir / split
        split_dir.mkdir(exist_ok=True)

    print(f"  Generating train samples ({n_train_samples})...")
    train_samples, train_class_counts = generate_samples_from_sources(
        train_sources, n_train_samples, "train"
    )

    print(f"  Generating val samples ({n_val_samples})...")
    val_samples, val_class_counts = generate_samples_from_sources(val_sources, n_val_samples, "val")

    # STEP 3: Save samples
    split_data = {"train": train_samples, "val": val_samples}
    split_counts = {}
    total_class_counts = {0: 0, 1: 0, 2: 0, 3: 0}

    sample_idx = 0
    for split_name, samples in split_data.items():
        split_counts[split_name] = len(samples)
        for sample in samples:
            sample_path = output_dir / split_name / f"sample_{sample_idx:06d}.npz"
            np.savez_compressed(
                sample_path,
                ref_context=sample['ref_context'],
                alt_context=sample['alt_context'],
                label=np.array([sample['var_type']], dtype=np.int64),
            )
            total_class_counts[sample['var_type']] += 1
            sample_idx += 1

    # Save metadata
    metadata = {
        "model": "variants",
        "num_samples": sum(split_counts.values()),
        "splits": split_counts,
        "source_splits": {
            "train": len(train_sources),
            "val": len(val_sources),
        },
        "class_counts": total_class_counts,
        "context_size": context_size,
        "num_classes": 4,
        "class_labels": ["SNP", "Insertion", "Deletion", "Complex"],
        "split_method": "source-level (no data leakage)",
    }
    with open(output_dir / "metadata.json", "w") as f:
        json.dump(metadata, f, indent=2)

    print(f"  Saved {sum(split_counts.values())} samples to {output_dir}")
    print(f"  Splits: {split_counts}")
    print(f"  Classes: {total_class_counts}")


def generate_orf_dataset(
    fasta_samples: list,
    num_samples: int,
    output_dir: Path,
    max_length: int = 500,
    train_ratio: float = 0.85,
    val_ratio: float = 0.15,
):
    """
    Generate ORF detection dataset with FILE-LEVEL split to prevent data leakage.

    Labels per position: 0=Non-coding, 1=Start codon, 2=Coding, 3=Stop codon
    """
    print(f"\n{'='*60}")
    print("Generating ORF dataset (FILE-LEVEL SPLIT)")
    print(f"{'='*60}")

    output_dir.mkdir(parents=True, exist_ok=True)

    def find_orfs(seq: str) -> np.ndarray:
        """Find ORFs and return per-position labels."""
        labels = np.zeros(len(seq), dtype=np.int64)

        # Search in all 3 reading frames
        for frame in range(3):
            i = frame
            in_orf = False

            while i < len(seq) - 2:
                codon = seq[i:i+3]

                if not in_orf:
                    if codon in START_CODONS:
                        in_orf = True
                        labels[i:i+3] = 1  # Start codon
                else:
                    if codon in STOP_CODONS:
                        # Mark stop codon
                        labels[i:i+3] = 3
                        in_orf = False
                    else:
                        # Mark as coding
                        labels[i:i+3] = np.maximum(labels[i:i+3], 2)

                i += 3

        return labels

    # STEP 1: Split samples at SOURCE level to prevent data leakage
    random.shuffle(fasta_samples)
    n_train = int(len(fasta_samples) * train_ratio)

    train_sources = fasta_samples[:n_train]
    val_sources = fasta_samples[n_train:]

    print(f"  Source split: train={len(train_sources)}, val={len(val_sources)}")

    # STEP 2: Generate samples from each split independently
    def generate_samples_from_sources(sources, target_count, split_name):
        """Generate samples only from the given source samples."""
        samples = []
        source_idx = 0
        attempts = 0
        max_attempts = target_count * 3

        while len(samples) < target_count and attempts < max_attempts:
            _, seq = sources[source_idx % len(sources)]

            # Skip if too short
            if len(seq) < 100:
                source_idx += 1
                attempts += 1
                continue

            # Take a random subsequence
            if len(seq) > max_length:
                start = random.randint(0, len(seq) - max_length)
                seq = seq[start:start + max_length]

            # Clean sequence (only ACGT)
            seq = ''.join(c if c in 'ACGT' else 'N' for c in seq)

            # Augment with mutations
            if random.random() < 0.5:
                seq = mutate_sequence(seq, mutation_rate=random.uniform(0.001, 0.02))

            # Find ORFs
            labels = find_orfs(seq)

            # Encode sequence
            onehot = encode_sequence_onehot(seq)

            samples.append({
                'sequence': onehot,
                'labels': labels,
                'length': len(seq),
            })

            source_idx += 1
            attempts += 1

        return samples

    # Calculate target counts per split
    n_train_samples = int(num_samples * train_ratio)
    n_val_samples = num_samples - n_train_samples

    for split in ["train", "val"]:
        split_dir = output_dir / split
        split_dir.mkdir(exist_ok=True)

    print(f"  Generating train samples ({n_train_samples})...")
    train_samples = generate_samples_from_sources(train_sources, n_train_samples, "train")

    print(f"  Generating val samples ({n_val_samples})...")
    val_samples = generate_samples_from_sources(val_sources, n_val_samples, "val")

    # STEP 3: Save samples
    split_data = {"train": train_samples, "val": val_samples}
    split_counts = {}

    sample_idx = 0
    for split_name, samples in split_data.items():
        split_counts[split_name] = len(samples)
        for sample in samples:
            sample_path = output_dir / split_name / f"sample_{sample_idx:06d}.npz"
            np.savez_compressed(
                sample_path,
                sequence=sample['sequence'],
                labels=sample['labels'],
                length=np.array([sample['length']], dtype=np.int32),
            )
            sample_idx += 1

    # Save metadata
    metadata = {
        "model": "orf",
        "num_samples": sum(split_counts.values()),
        "splits": split_counts,
        "source_splits": {
            "train": len(train_sources),
            "val": len(val_sources),
        },
        "max_length": max_length,
        "num_classes": 4,
        "class_labels": ["Non-coding", "Start", "Coding", "Stop"],
        "split_method": "source-level (no data leakage)",
    }
    with open(output_dir / "metadata.json", "w") as f:
        json.dump(metadata, f, indent=2)

    print(f"  Saved {sum(split_counts.values())} samples to {output_dir}")
    print(f"  Splits: {split_counts}")


def generate_consensus_dataset(
    fastq_samples: list,
    num_samples: int,
    output_dir: Path,
    num_reads: int = 10,
    max_length: int = 200,
    error_rate: float = 0.05,
    train_ratio: float = 0.85,
    val_ratio: float = 0.15,
):
    """
    Generate consensus prediction dataset with FILE-LEVEL split to prevent data leakage.

    Simulates multiple reads from a reference sequence with errors.
    The original sequence is the consensus ground truth.
    """
    print(f"\n{'='*60}")
    print("Generating CONSENSUS dataset (FILE-LEVEL SPLIT)")
    print(f"{'='*60}")

    output_dir.mkdir(parents=True, exist_ok=True)

    def simulate_reads(
    seq: str, quality: np.ndarray, n_reads: int, error_rate: float
) -> tuple[np.ndarray, np.ndarray]:
        """Simulate multiple reads with errors."""
        seq_len = len(seq)

        # Output: (n_reads, 5, seq_len) - 4 channels for bases + 1 for quality
        reads = np.zeros((n_reads, 5, seq_len), dtype=np.float32)
        coverage = np.zeros(seq_len, dtype=np.float32)

        for r in range(n_reads):
            # Simulate read with errors
            read_seq = list(seq)
            read_qual = add_quality_noise(quality, noise_std=5)

            for i in range(seq_len):
                # Error rate modulated by quality
                q = read_qual[i]
                p_error = error_rate * (1 + (40 - q) / 40)  # Higher error at lower quality

                if random.random() < p_error:
                    # Introduce error
                    read_seq[i] = random.choice(NUCLEOTIDES)

            # Encode read
            for i, base in enumerate(read_seq):
                if base in NUCLEOTIDE_MAP and NUCLEOTIDE_MAP[base] < 4:
                    reads[r, NUCLEOTIDE_MAP[base], i] = 1.0
                reads[r, 4, i] = read_qual[i] / 60.0  # Normalized quality

            coverage += 1

        return reads, coverage

    # STEP 1: Split samples at SOURCE level to prevent data leakage
    random.shuffle(fastq_samples)
    n_train = int(len(fastq_samples) * train_ratio)

    train_sources = fastq_samples[:n_train]
    val_sources = fastq_samples[n_train:]

    print(f"  Source split: train={len(train_sources)}, val={len(val_sources)}")

    # STEP 2: Generate samples from each split independently
    def generate_samples_from_sources(sources, target_count, split_name):
        """Generate samples only from the given source samples."""
        samples = []
        source_idx = 0
        attempts = 0
        max_attempts = target_count * 3

        while len(samples) < target_count and attempts < max_attempts:
            _, seq, quality = sources[source_idx % len(sources)]

            # Take a random subsequence
            if len(seq) > max_length:
                start = random.randint(0, len(seq) - max_length)
                seq = seq[start:start + max_length]
                quality = quality[start:start + max_length]

            seq_len = len(seq)

            # Simulate reads
            actual_n_reads = random.randint(3, num_reads)
            actual_error_rate = random.uniform(0.02, 0.1)

            reads, coverage = simulate_reads(seq, quality, actual_n_reads, actual_error_rate)

            # Encode consensus (ground truth)
            consensus = np.zeros(seq_len, dtype=np.int64)
            for i, base in enumerate(seq):
                if base in NUCLEOTIDE_MAP and NUCLEOTIDE_MAP[base] < 4:
                    consensus[i] = NUCLEOTIDE_MAP[base]

            samples.append({
                'reads': reads,
                'coverage': coverage,
                'consensus': consensus,
                'length': seq_len,
                'n_reads': actual_n_reads,
            })

            source_idx += 1
            attempts += 1

        return samples

    # Calculate target counts per split
    n_train_samples = int(num_samples * train_ratio)
    n_val_samples = num_samples - n_train_samples

    for split in ["train", "val"]:
        split_dir = output_dir / split
        split_dir.mkdir(exist_ok=True)

    print(f"  Generating train samples ({n_train_samples})...")
    train_samples = generate_samples_from_sources(train_sources, n_train_samples, "train")

    print(f"  Generating val samples ({n_val_samples})...")
    val_samples = generate_samples_from_sources(val_sources, n_val_samples, "val")

    # STEP 3: Save samples
    split_data = {"train": train_samples, "val": val_samples}
    split_counts = {}

    sample_idx = 0
    for split_name, samples in split_data.items():
        split_counts[split_name] = len(samples)
        for sample in samples:
            sample_path = output_dir / split_name / f"sample_{sample_idx:06d}.npz"
            np.savez_compressed(
                sample_path,
                reads=sample['reads'],
                coverage=sample['coverage'],
                consensus=sample['consensus'],
                length=np.array([sample['length']], dtype=np.int32),
                n_reads=np.array([sample['n_reads']], dtype=np.int32),
            )
            sample_idx += 1

    # Save metadata
    metadata = {
        "model": "consensus",
        "num_samples": sum(split_counts.values()),
        "splits": split_counts,
        "source_splits": {
            "train": len(train_sources),
            "val": len(val_sources),
        },
        "max_reads": num_reads,
        "max_length": max_length,
        "error_rate": error_rate,
        "num_classes": 4,
        "class_labels": ["A", "C", "G", "T"],
        "split_method": "source-level (no data leakage)",
    }
    with open(output_dir / "metadata.json", "w") as f:
        json.dump(metadata, f, indent=2)

    print(f"  Saved {sum(split_counts.values())} samples to {output_dir}")
    print(f"  Splits: {split_counts}")


def generate_alignment_dataset(
    fasta_samples: list,
    num_samples: int,
    output_dir: Path,
    max_length: int = 300,
    train_ratio: float = 0.85,
    val_ratio: float = 0.15,
):
    """
    Generate alignment scoring dataset with FILE-LEVEL split to prevent data leakage.

    Creates pairs of sequences with known similarity scores.
    """
    print(f"\n{'='*60}")
    print("Generating ALIGNMENT dataset (FILE-LEVEL SPLIT)")
    print(f"{'='*60}")

    output_dir.mkdir(parents=True, exist_ok=True)

    def compute_similarity(seq1: str, seq2: str) -> float:
        """Compute simple sequence similarity."""
        min_len = min(len(seq1), len(seq2))
        max_len = max(len(seq1), len(seq2))

        if min_len == 0:
            return 0.0

        matches = sum(1 for i in range(min_len) if seq1[i] == seq2[i])

        # Penalize length difference
        length_penalty = min_len / max_len

        return (matches / min_len) * length_penalty

    # STEP 1: Split samples at SOURCE level to prevent data leakage
    random.shuffle(fasta_samples)
    n_train = int(len(fasta_samples) * train_ratio)

    train_sources = fasta_samples[:n_train]
    val_sources = fasta_samples[n_train:]

    print(f"  Source split: train={len(train_sources)}, val={len(val_sources)}")

    # STEP 2: Generate samples from each split independently
    def generate_samples_from_sources(sources, target_count, split_name):
        """Generate samples only from the given source samples."""
        samples = []
        source_idx = 0
        attempts = 0
        max_attempts = target_count * 3

        while len(samples) < target_count and attempts < max_attempts:
            # Pick a sequence from this split's sources only
            _, seq1 = sources[source_idx % len(sources)]

            # Truncate
            if len(seq1) > max_length:
                start = random.randint(0, len(seq1) - max_length)
                seq1 = seq1[start:start + max_length]

            # Generate pair based on desired similarity
            target_similarity = random.random()  # 0 to 1

            if target_similarity > 0.9:
                # High similarity: small mutations
                seq2 = mutate_sequence(seq1, mutation_rate=random.uniform(0.01, 0.05))
            elif target_similarity > 0.7:
                # Medium-high similarity
                seq2 = mutate_sequence(seq1, mutation_rate=random.uniform(0.05, 0.15))
            elif target_similarity > 0.5:
                # Medium similarity
                seq2 = mutate_sequence(seq1, mutation_rate=random.uniform(0.15, 0.30))
            elif target_similarity > 0.3:
                # Low similarity: different sequence FROM SAME SPLIT SOURCES
                _, seq2 = random.choice(sources)
                if len(seq2) > max_length:
                    start = random.randint(0, len(seq2) - max_length)
                    seq2 = seq2[start:start + max_length]
            else:
                # Very low similarity: different sequence FROM SAME SPLIT SOURCES
                _, seq2 = random.choice(sources)
                if len(seq2) > max_length:
                    start = random.randint(0, len(seq2) - max_length)
                    seq2 = seq2[start:start + max_length]
                seq2 = mutate_sequence(seq2, mutation_rate=0.3)

            # Compute actual similarity
            similarity = compute_similarity(seq1, seq2)

            # Encode sequences
            seq1_onehot = encode_sequence_onehot(seq1, max_length)
            seq2_onehot = encode_sequence_onehot(seq2, max_length)

            # Pad to max_length
            if seq1_onehot.shape[1] < max_length:
                pad = max_length - seq1_onehot.shape[1]
                seq1_onehot = np.pad(seq1_onehot, ((0, 0), (0, pad)))
            if seq2_onehot.shape[1] < max_length:
                pad = max_length - seq2_onehot.shape[1]
                seq2_onehot = np.pad(seq2_onehot, ((0, 0), (0, pad)))

            samples.append({
                'seq1': seq1_onehot,
                'seq2': seq2_onehot,
                'similarity': similarity,
                'len1': len(seq1),
                'len2': len(seq2),
            })

            source_idx += 1
            attempts += 1

        return samples

    # Calculate target counts per split
    n_train_samples = int(num_samples * train_ratio)
    n_val_samples = num_samples - n_train_samples

    for split in ["train", "val"]:
        split_dir = output_dir / split
        split_dir.mkdir(exist_ok=True)

    print(f"  Generating train samples ({n_train_samples})...")
    train_samples = generate_samples_from_sources(train_sources, n_train_samples, "train")

    print(f"  Generating val samples ({n_val_samples})...")
    val_samples = generate_samples_from_sources(val_sources, n_val_samples, "val")

    # STEP 3: Save samples
    split_data = {"train": train_samples, "val": val_samples}
    split_counts = {}

    sample_idx = 0
    for split_name, samples in split_data.items():
        split_counts[split_name] = len(samples)
        for sample in samples:
            sample_path = output_dir / split_name / f"sample_{sample_idx:06d}.npz"
            np.savez_compressed(
                sample_path,
                seq1=sample['seq1'],
                seq2=sample['seq2'],
                similarity=np.array([sample['similarity']], dtype=np.float32),
                len1=np.array([sample['len1']], dtype=np.int32),
                len2=np.array([sample['len2']], dtype=np.int32),
            )
            sample_idx += 1

    # Save metadata
    metadata = {
        "model": "alignment",
        "num_samples": sum(split_counts.values()),
        "splits": split_counts,
        "source_splits": {
            "train": len(train_sources),
            "val": len(val_sources),
        },
        "max_length": max_length,
        "split_method": "source-level (no data leakage)",
    }
    with open(output_dir / "metadata.json", "w") as f:
        json.dump(metadata, f, indent=2)

    print(f"  Saved {sum(split_counts.values())} samples to {output_dir}")
    print(f"  Splits: {split_counts}")


# =============================================================================
# Main
# =============================================================================

def parse_args():
    parser = argparse.ArgumentParser(
        description="Create training datasets for all ML models"
    )

    parser.add_argument(
        "--fastq-dir",
        type=str,
        default=str(DEFAULT_FASTQ_DIR),
        help="Directory containing FASTQ files",
    )
    parser.add_argument(
        "--fasta-dir",
        type=str,
        default=str(DEFAULT_FASTA_DIR),
        help="Directory containing FASTA files",
    )
    parser.add_argument(
        "--output-dir",
        type=str,
        default=str(DEFAULT_OUTPUT_DIR),
        help="Output directory for datasets",
    )
    parser.add_argument(
        "--models",
        nargs="+",
        choices=[
            "trimming", "heterozygote_training", "variants",
            "orf", "consensus", "alignment", "all"
        ],
        default=["all"],
        help="Models to generate datasets for",
    )
    parser.add_argument(
        "--samples-per-model",
        type=int,
        default=40000,
        help="Number of samples to generate per model",
    )
    parser.add_argument(
        "--max-source-samples",
        type=int,
        default=50000,
        help="Max source samples to load from FASTQ/FASTA",
    )

    return parser.parse_args()


def main():
    args = parse_args()

    fastq_dir = Path(args.fastq_dir)
    fasta_dir = Path(args.fasta_dir)
    output_dir = Path(args.output_dir)

    models = args.models
    if "all" in models:
        models = [
            "trimming", "heterozygote_training", "variants",
            "orf", "consensus", "alignment"
        ]

    print("=" * 60)
    print("Dataset Generation for ML Models")
    print("=" * 60)
    print(f"FASTQ source: {fastq_dir}")
    print(f"FASTA source: {fasta_dir}")
    print(f"Output: {output_dir}")
    print(f"Models: {models}")
    print(f"Samples per model: {args.samples_per_model}")

    # Load source data
    fastq_models = {"trimming", "heterozygote_training", "consensus"}
    fasta_models = {"variants", "orf", "alignment"}

    fastq_samples = []
    fasta_samples = []

    if any(m in fastq_models for m in models):
        print(f"\nLoading FASTQ samples from {fastq_dir}...")
        fastq_samples = load_all_fastq(fastq_dir, args.max_source_samples)
        print(f"  Loaded {len(fastq_samples)} FASTQ samples")

        if len(fastq_samples) == 0:
            print("ERROR: No FASTQ samples found!")
            return

    if any(m in fasta_models for m in models):
        print(f"\nLoading FASTA samples from {fasta_dir}...")
        fasta_samples = load_all_fasta(fasta_dir, args.max_source_samples)
        print(f"  Loaded {len(fasta_samples)} FASTA samples")

        if len(fasta_samples) == 0:
            print("ERROR: No FASTA samples found!")
            return

    # Generate datasets
    for model in models:
        if model == "trimming":
            generate_trimming_dataset(
                fastq_samples,
                args.samples_per_model,
                output_dir / "trimming",
            )

        elif model == "heterozygote_training":
            generate_heterozygote_dataset(
                fastq_samples,
                args.samples_per_model,
                output_dir / "heterozygote_training",
            )

        elif model == "variants":
            generate_variants_dataset(
                fasta_samples,
                args.samples_per_model,
                output_dir / "variants",
            )

        elif model == "orf":
            generate_orf_dataset(
                fasta_samples,
                args.samples_per_model,
                output_dir / "orf",
            )

        elif model == "consensus":
            generate_consensus_dataset(
                fastq_samples,
                args.samples_per_model,
                output_dir / "consensus",
            )

        elif model == "alignment":
            generate_alignment_dataset(
                fasta_samples,
                args.samples_per_model,
                output_dir / "alignment",
            )

    print("\n" + "=" * 60)
    print("Dataset generation complete!")
    print("=" * 60)
    print(f"Datasets saved to: {output_dir}")
    for model in models:
        print(f"  - {model}/")


if __name__ == "__main__":
    main()
