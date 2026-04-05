#!/usr/bin/env python3
"""
Create quality prediction dataset from real FASTQ files with data augmentation.

Usage:
    uv run python scripts/create_quality_dataset_fastq.py --input datalake/fastq_real --output datalake/datasets/quality_enhanced --samples 42000
"""

import argparse
import gzip
import json
import random
import sys
from pathlib import Path

import numpy as np

random.seed(42)
np.random.seed(42)


def read_fastq(fastq_path: Path, max_reads: int = None) -> list[dict]:
    """Read FASTQ file and return list of reads with sequence and quality."""
    reads = []

    opener = gzip.open if str(fastq_path).endswith('.gz') else open
    mode = 'rt' if str(fastq_path).endswith('.gz') else 'r'

    with opener(fastq_path, mode) as f:
        while True:
            header = f.readline()
            if not header:
                break
            seq = f.readline().strip().upper()
            plus = f.readline()
            qual_str = f.readline().strip()

            # Convert ASCII to Phred (offset 33)
            qualities = np.array([ord(c) - 33 for c in qual_str], dtype=np.int32)

            # Filter valid reads
            if len(seq) >= 100 and len(seq) == len(qualities):
                # Only keep reads with ACGT
                if all(c in 'ACGT' for c in seq):
                    reads.append({
                        'sequence': seq,
                        'qualities': qualities,
                    })

            if max_reads and len(reads) >= max_reads:
                break

    return reads


def augment_read(seq: str, qualities: np.ndarray, mutation_rate: float = 0.02, quality_noise: float = 3.0) -> tuple[str, np.ndarray]:
    """Apply data augmentation to a read.

    Augmentations:
    - Random point mutations
    - Random quality noise
    - Random subsequence (80-100% of original)
    """
    seq_list = list(seq)
    quals = qualities.copy().astype(np.float32)

    # 1. Point mutations
    for i in range(len(seq_list)):
        if random.random() < mutation_rate:
            original = seq_list[i]
            choices = [b for b in 'ACGT' if b != original]
            seq_list[i] = random.choice(choices)
            # Mutations often have lower quality
            quals[i] = max(2, quals[i] - random.uniform(5, 15))

    # 2. Quality noise
    noise = np.random.normal(0, quality_noise, len(quals))
    quals = np.clip(quals + noise, 2, 60)

    # 3. Random subsequence (80-100%)
    if random.random() < 0.5:
        keep_ratio = random.uniform(0.8, 1.0)
        new_len = int(len(seq_list) * keep_ratio)
        start = random.randint(0, len(seq_list) - new_len)
        seq_list = seq_list[start:start + new_len]
        quals = quals[start:start + new_len]

    return ''.join(seq_list), quals.astype(np.int32)


def compute_features(sequence: str) -> np.ndarray:
    """Compute features from DNA sequence (NO quality info).

    Features:
    - Length (normalized)
    - GC content
    - Dinucleotide frequencies (16)
    - Trinucleotide frequencies (64)
    - Homopolymer stats
    - Complexity metrics

    Total: ~90 features, padded to 101
    """
    n = len(sequence)
    features = []

    # 1. Basic stats
    features.append(n / 1000)  # Normalized length
    gc = (sequence.count('G') + sequence.count('C')) / n
    features.append(gc)

    # 2. Single nucleotide frequencies
    for base in 'ACGT':
        features.append(sequence.count(base) / n)

    # 3. Dinucleotide frequencies (16)
    dinucs = [''.join([a, b]) for a in 'ACGT' for b in 'ACGT']
    for di in dinucs:
        count = sum(1 for i in range(n-1) if sequence[i:i+2] == di)
        features.append(count / max(n-1, 1))

    # 4. Trinucleotide frequencies (64)
    trinucs = [''.join([a, b, c]) for a in 'ACGT' for b in 'ACGT' for c in 'ACGT']
    for tri in trinucs:
        count = sum(1 for i in range(n-2) if sequence[i:i+3] == tri)
        features.append(count / max(n-2, 1))

    # 5. Homopolymer features
    max_homopolymer = 1
    current_run = 1
    total_homopolymer = 0
    for i in range(1, n):
        if sequence[i] == sequence[i-1]:
            current_run += 1
        else:
            if current_run >= 3:
                total_homopolymer += current_run
            max_homopolymer = max(max_homopolymer, current_run)
            current_run = 1
    if current_run >= 3:
        total_homopolymer += current_run
    max_homopolymer = max(max_homopolymer, current_run)

    features.append(max_homopolymer / 10)  # Normalized
    features.append(total_homopolymer / n)

    # 6. Sequence complexity (unique k-mers / possible k-mers)
    for k in [3, 5]:
        kmers = set(sequence[i:i+k] for i in range(n-k+1))
        max_possible = min(4**k, n-k+1)
        features.append(len(kmers) / max_possible if max_possible > 0 else 0)

    # Pad to 101 features
    while len(features) < 101:
        features.append(0)

    return np.array(features[:101], dtype=np.float32)


def encode_sequence(sequence: str, max_length: int) -> np.ndarray:
    """Encode sequence as integer array (A=0, C=1, G=2, T=3, pad=4)."""
    mapping = {'A': 0, 'C': 1, 'G': 2, 'T': 3}
    encoded = np.full(max_length, 4, dtype=np.int64)  # 4 = padding
    for i, base in enumerate(sequence[:max_length]):
        encoded[i] = mapping.get(base, 4)
    return encoded


def generate_samples_from_reads(reads: list, target_count: int, max_length: int, augment_ratio: int) -> list:
    """Generate augmented samples from a list of reads."""
    samples = []
    read_idx = 0

    while len(samples) < target_count:
        read = reads[read_idx % len(reads)]

        # Add original
        if len(samples) < target_count:
            seq = read['sequence'][:max_length]
            quals = read['qualities'][:max_length]
            samples.append({
                'sequence': seq,
                'qualities': quals,
                'augmented': False,
            })

        # Add augmented versions
        for _ in range(augment_ratio):
            if len(samples) >= target_count:
                break

            aug_seq, aug_quals = augment_read(
                read['sequence'][:max_length],
                read['qualities'][:max_length],
                mutation_rate=random.uniform(0.01, 0.05),
                quality_noise=random.uniform(2, 5),
            )

            if len(aug_seq) >= 100:
                samples.append({
                    'sequence': aug_seq,
                    'qualities': aug_quals,
                    'augmented': True,
                })

        read_idx += 1

    return samples


def main():
    parser = argparse.ArgumentParser(description="Create quality dataset from FASTQ")
    parser.add_argument("--input", type=str, default="datalake/fastq_real", help="FASTQ directory")
    parser.add_argument("--output", type=str, default="datalake/datasets/quality_enhanced", help="Output directory")
    parser.add_argument("--samples", type=int, default=42000, help="Total samples to generate")
    parser.add_argument("--max-length", type=int, default=500, help="Max sequence length")
    parser.add_argument("--val-split", type=float, default=0.1, help="Validation split")
    parser.add_argument("--test-split", type=float, default=0.1, help="Test split")
    parser.add_argument("--augment-ratio", type=int, default=10, help="Augmentations per original read")
    args = parser.parse_args()

    input_dir = Path(args.input)
    output_dir = Path(args.output)

    print("=" * 70)
    print("QUALITY DATASET GENERATOR (from real FASTQ)")
    print("=" * 70)

    # STEP 1: Split FILES first (not samples) to prevent data leakage
    all_files = sorted(input_dir.glob("*.fastq*"))
    random.shuffle(all_files)

    n_val_files = max(1, int(len(all_files) * args.val_split))
    n_test_files = max(1, int(len(all_files) * args.test_split))
    n_train_files = len(all_files) - n_val_files - n_test_files

    train_files = all_files[:n_train_files]
    val_files = all_files[n_train_files:n_train_files + n_val_files]
    test_files = all_files[n_train_files + n_val_files:]

    print(f"\nFile-level split (NO DATA LEAKAGE):")
    print(f"  Train files: {len(train_files)}")
    print(f"  Val files: {len(val_files)}")
    print(f"  Test files: {len(test_files)}")

    # STEP 2: Load reads from each file group
    def load_reads_from_files(file_list):
        reads = []
        for fq in file_list:
            file_reads = read_fastq(fq, max_reads=50000)
            reads.extend(file_reads)
        return reads

    print(f"\nLoading reads...")
    train_reads = load_reads_from_files(train_files)
    val_reads = load_reads_from_files(val_files)
    test_reads = load_reads_from_files(test_files)

    print(f"  Train reads: {len(train_reads):,}")
    print(f"  Val reads: {len(val_reads):,}")
    print(f"  Test reads: {len(test_reads):,}")

    if len(train_reads) == 0:
        print("ERROR: No reads found in train files!")
        return 1

    # STEP 3: Generate samples from each group (no cross-contamination)
    # Use DESIRED sample ratios, not file ratios
    n_train = int(args.samples * (1 - args.val_split - args.test_split))  # 80%
    n_val = int(args.samples * args.val_split)  # 10%
    n_test = args.samples - n_train - n_val  # 10%

    print(f"\nGenerating samples (file-level isolated)...")
    print(f"  Target: train={n_train:,}, val={n_val:,}, test={n_test:,}")

    train_samples = generate_samples_from_reads(train_reads, n_train, args.max_length, args.augment_ratio)
    val_samples = generate_samples_from_reads(val_reads, n_val, args.max_length, args.augment_ratio)
    test_samples = generate_samples_from_reads(test_reads, n_test, args.max_length, args.augment_ratio)

    # Shuffle within each split
    random.shuffle(train_samples)
    random.shuffle(val_samples)
    random.shuffle(test_samples)

    splits = {
        'train': train_samples,
        'val': val_samples,
        'test': test_samples,
    }

    print(f"\nGenerated: train={len(train_samples):,} | val={len(val_samples):,} | test={len(test_samples):,}")

    # Compute statistics
    all_samples = train_samples + val_samples + test_samples
    all_quals = []
    for s in all_samples:
        all_quals.extend(s['qualities'].tolist())
    all_quals = np.array(all_quals)

    mean_quals = [np.mean(s['qualities']) for s in all_samples]

    print(f"\nQuality statistics:")
    print(f"  Per-base: mean={all_quals.mean():.1f}, std={all_quals.std():.1f}, range=[{all_quals.min()}, {all_quals.max()}]")
    print(f"  Per-sequence mean: mean={np.mean(mean_quals):.1f}, std={np.std(mean_quals):.1f}")

    # Save datasets
    print(f"\nSaving to {output_dir}...")

    for split_name, split_samples in splits.items():
        split_dir = output_dir / split_name
        split_dir.mkdir(parents=True, exist_ok=True)

        # Clear old files
        for old_file in split_dir.glob("sample_*.npz"):
            old_file.unlink()

        for idx, sample in enumerate(split_samples):
            seq = sample['sequence']
            quals = sample['qualities']

            # Encode sequence
            encoded_seq = encode_sequence(seq, args.max_length)

            # Pad qualities
            padded_quals = np.zeros(args.max_length, dtype=np.int64)
            padded_quals[:len(quals)] = quals

            # Compute features (NO quality info!)
            features = compute_features(seq)

            # Save
            npz_path = split_dir / f"sample_{idx:06d}.npz"
            np.savez(
                npz_path,
                sequence=encoded_seq,
                qualities=padded_quals,
                features=features,
                length=np.int64(len(seq)),
                mean_quality=np.float32(np.mean(quals)),
            )

        print(f"  {split_name}: {len(split_samples):,} samples")

    # Save metadata
    metadata = {
        "total_samples": len(all_samples),
        "train_samples": len(splits['train']),
        "val_samples": len(splits['val']),
        "test_samples": len(splits['test']),
        "train_files": len(train_files),
        "val_files": len(val_files),
        "test_files": len(test_files),
        "task": "quality_prediction",
        "quality_range": [int(all_quals.min()), int(all_quals.max())],
        "mean_quality": float(np.mean(mean_quals)),
        "std_quality": float(np.std(mean_quals)),
        "config": {
            "max_seq_length": args.max_length,
            "min_seq_length": 100,
            "synthetic_qualities": False,
            "source": "real_fastq_augmented",
            "augment_ratio": args.augment_ratio,
            "split_method": "file-level (no data leakage)",
        }
    }

    with open(output_dir / "metadata.json", "w") as f:
        json.dump(metadata, f, indent=2)

    print("\n" + "=" * 70)
    print("DONE!")
    print("=" * 70)
    print(f"\nDataset saved to: {output_dir}")
    print(f"Features: 101 (sequence-only, NO quality leakage)")
    print(f"Target: mean_quality per sequence")

    return 0


if __name__ == "__main__":
    sys.exit(main())
