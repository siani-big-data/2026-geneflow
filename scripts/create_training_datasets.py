#!/usr/bin/env python3
"""Create training datasets from curated datalake.

Creates two datasets in datalake/dataset/:
1. taxonomy/ - For TaxonomyClassifier (6 levels)
2. quality_enhanced/  - For quality_enhanced prediction with synthetic quality_enhanced scores

Output format matches existing datasets:
- metadata.json with class labels, mappings, feature stats
- train/val/test folders with sample_XXXXXX.npz files

Usage:
    uv run python scripts/create_training_datasets.py
    uv run python scripts/create_training_datasets.py --taxonomy-only
    uv run python scripts/create_training_datasets.py --quality_enhanced-only
"""

import gzip
import json
import random
import sys
from collections import defaultdict
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).parent.parent))


# Nucleotide encoding
NUCLEOTIDE_MAP = {"A": 0, "T": 1, "C": 2, "G": 3, "N": 4}
DINUCLEOTIDES = [f"{a}{b}" for a in "ATCG" for b in "ATCG"]  # 16
TRINUCLEOTIDES = [f"{a}{b}{c}" for a in "ATCG" for b in "ATCG" for c in "ATCG"]  # 64

# Taxonomy levels
TAXONOMY_LEVELS = ["kingdom", "phylum", "class", "order", "family", "genus"]


def encode_sequence(sequence: str, max_length: int = 2000) -> np.ndarray:
    """Encode DNA sequence to integer array."""
    encoded = np.full(max_length, 4, dtype=np.int64)  # Pad with N (4)
    seq_upper = sequence.upper()
    for i, base in enumerate(seq_upper[:max_length]):
        encoded[i] = NUCLEOTIDE_MAP.get(base, 4)
    return encoded


def compute_features(sequence: str) -> np.ndarray:
    """Compute 101 features from DNA sequence.

    21 basic features + 16 dinucleotide freqs + 64 trinucleotide freqs = 101
    """
    seq = sequence.upper()
    n = len(seq)

    if n == 0:
        return np.zeros(101, dtype=np.float32)

    # Count nucleotides
    counts = {base: seq.count(base) for base in "ATCGN"}
    total_atcg = counts["A"] + counts["T"] + counts["C"] + counts["G"]

    if total_atcg == 0:
        total_atcg = 1  # Avoid division by zero

    # Basic features (21)
    gc_content = (counts["G"] + counts["C"]) / total_atcg
    at_content = (counts["A"] + counts["T"]) / total_atcg
    purine_content = (counts["A"] + counts["G"]) / total_atcg
    pyrimidine_content = (counts["T"] + counts["C"]) / total_atcg

    freq_a = counts["A"] / total_atcg
    freq_t = counts["T"] / total_atcg
    freq_c = counts["C"] / total_atcg
    freq_g = counts["G"] / total_atcg

    # GC skew and AT skew
    gc_sum = counts["G"] + counts["C"]
    at_sum = counts["A"] + counts["T"]
    gc_skew = (counts["G"] - counts["C"]) / gc_sum if gc_sum > 0 else 0
    at_skew = (counts["A"] - counts["T"]) / at_sum if at_sum > 0 else 0

    # Sequence entropy
    probs = np.array([freq_a, freq_t, freq_c, freq_g])
    probs = probs[probs > 0]
    entropy = -np.sum(probs * np.log2(probs)) if len(probs) > 0 else 0

    # Linguistic complexity (approximate)
    kmers = set()
    for k in [1, 2, 3]:
        for i in range(len(seq) - k + 1):
            kmers.add(seq[i:i+k])
    max_possible = sum(min(4**k, len(seq) - k + 1) for k in [1, 2, 3])
    linguistic_complexity = len(kmers) / max_possible if max_possible > 0 else 0

    # Compression ratio (simplified - unique k-mers / total)
    kmers_3 = [seq[i:i+3] for i in range(len(seq) - 2)]
    compression_ratio = len(set(kmers_3)) / len(kmers_3) if kmers_3 else 0

    # Max homopolymer lengths
    max_homopolymer = {"A": 1, "T": 1, "C": 1, "G": 1}
    current_base = ""
    current_length = 0
    for base in seq:
        if base == current_base:
            current_length += 1
        else:
            if current_base in max_homopolymer:
                max_homopolymer[current_base] = max(max_homopolymer[current_base], current_length)
            current_base = base
            current_length = 1
    if current_base in max_homopolymer:
        max_homopolymer[current_base] = max(max_homopolymer[current_base], current_length)

    # Local GC content statistics (sliding window)
    window_size = 100
    gc_values = []
    for i in range(0, len(seq) - window_size + 1, window_size // 2):
        window = seq[i:i+window_size]
        gc = (window.count("G") + window.count("C")) / len(window)
        gc_values.append(gc)

    if gc_values:
        gc_mean = np.mean(gc_values)
        gc_std = np.std(gc_values)
        gc_min = np.min(gc_values)
        gc_max = np.max(gc_values)
    else:
        gc_mean = gc_content
        gc_std = 0
        gc_min = gc_content
        gc_max = gc_content

    basic_features = [
        gc_content, at_content, purine_content, pyrimidine_content,
        freq_a, freq_t, freq_c, freq_g,
        gc_skew, at_skew,
        entropy, linguistic_complexity, compression_ratio,
        max_homopolymer["A"], max_homopolymer["T"], max_homopolymer["C"], max_homopolymer["G"],
        gc_mean, gc_std, gc_min, gc_max
    ]

    # Dinucleotide frequencies (16)
    dinuc_counts = {di: 0 for di in DINUCLEOTIDES}
    for i in range(len(seq) - 1):
        di = seq[i:i+2]
        if di in dinuc_counts:
            dinuc_counts[di] += 1
    total_di = sum(dinuc_counts.values())
    dinuc_freqs = [dinuc_counts[di] / total_di if total_di > 0 else 0 for di in DINUCLEOTIDES]

    # Trinucleotide frequencies (64)
    trinuc_counts = {tri: 0 for tri in TRINUCLEOTIDES}
    for i in range(len(seq) - 2):
        tri = seq[i:i+3]
        if tri in trinuc_counts:
            trinuc_counts[tri] += 1
    total_tri = sum(trinuc_counts.values())
    trinuc_freqs = [trinuc_counts[tri] / total_tri if total_tri > 0 else 0 for tri in TRINUCLEOTIDES]

    features = np.array(basic_features + dinuc_freqs + trinuc_freqs, dtype=np.float32)
    return features


def generate_synthetic_quality(sequence: str, seed: int | None = None) -> np.ndarray:
    """Generate synthetic Phred quality_enhanced scores for a sequence.

    Simulates realistic quality_enhanced patterns:
    - Lower quality_enhanced at sequence ends
    - Quality drops in homopolymer regions
    - Lower quality_enhanced in GC-extreme regions
    - Random noise to simulate real sequencing

    Returns:
        Array of Phred quality_enhanced scores (0-40)
    """
    if seed is not None:
        np.random.seed(seed)

    n = len(sequence)
    qualities = np.zeros(n)

    # Base quality_enhanced: good quality_enhanced in the middle, drops at ends
    position_factor = np.ones(n)
    ramp_length = min(50, n // 4)

    if ramp_length > 0:
        position_factor[:ramp_length] = np.linspace(0.6, 1.0, ramp_length)
        position_factor[-ramp_length:] = np.linspace(1.0, 0.7, ramp_length)

    # Base quality_enhanced (Phred 30-38 for good reads)
    base_quality = np.random.normal(34, 2, n)

    # Homopolymer penalty
    homopolymer_penalty = np.zeros(n)
    i = 0
    while i < n:
        j = i
        while j < n and sequence[j] == sequence[i]:
            j += 1
        run_length = j - i
        if run_length >= 4:
            penalty = min((run_length - 3) * 2, 10)
            homopolymer_penalty[i:j] = penalty
        i = j

    # GC content penalty (local)
    gc_penalty = np.zeros(n)
    window = 20
    for i in range(n):
        start = max(0, i - window // 2)
        end = min(n, i + window // 2)
        local_seq = sequence[start:end]
        gc = (local_seq.count('G') + local_seq.count('C')) / len(local_seq) if local_seq else 0.5
        if gc < 0.2 or gc > 0.8:
            gc_penalty[i] = 5 * abs(gc - 0.5)

    # Combine factors
    qualities = (
        base_quality * position_factor
        - homopolymer_penalty
        - gc_penalty
        + np.random.normal(0, 1.5, n)
    )

    # Clamp to valid Phred range
    qualities = np.clip(qualities, 2, 40).astype(np.int64)
    return qualities


def read_fasta_sequences(fasta_path: Path, max_sequences: int = 10) -> list[dict]:
    """Read sequences from a gzipped FASTA file."""
    sequences = []

    try:
        with gzip.open(fasta_path, "rt") as f:
            current_header = ""
            current_seq = []

            for line in f:
                line = line.strip()
                if line.startswith(">"):
                    if current_seq:
                        sequences.append({
                            "header": current_header,
                            "sequence": "".join(current_seq),
                        })
                    current_header = line[1:]
                    current_seq = []
                else:
                    current_seq.append(line)

            if current_seq:
                sequences.append({
                    "header": current_header,
                    "sequence": "".join(current_seq),
                })
    except Exception:
        return []

    # Limit and shuffle
    if len(sequences) > max_sequences:
        sequences = random.sample(sequences, max_sequences)

    return sequences


def collect_taxonomy_samples(
    curated_dir: Path,
    max_seq_length: int = 2000,
    min_seq_length: int = 100,
    max_sequences_per_species: int = 5,
) -> tuple[list[dict], dict]:
    """Collect samples for taxonomy dataset."""
    print("Collecting taxonomy samples...")

    json_files = list(curated_dir.rglob("*.json"))
    print(f"Found {len(json_files)} species metadata files")

    samples = []
    class_counts = {level: defaultdict(int) for level in TAXONOMY_LEVELS}

    for i, json_path in enumerate(json_files):
        try:
            with open(json_path) as f:
                metadata = json.load(f)

            taxonomy = metadata.get("taxonomy", {})
            if not taxonomy:
                continue

            # Check all taxonomy levels exist
            levels_data = {}
            for level in TAXONOMY_LEVELS:
                value = taxonomy.get(level, "")
                if value:
                    levels_data[level] = value.lower()
                else:
                    levels_data[level] = "unknown"

            # Skip if kingdom is unknown
            if levels_data["kingdom"] == "unknown":
                continue

            # Read sequences
            fasta_path = json_path.with_suffix(".fasta.gz")
            if not fasta_path.exists():
                continue

            seqs = read_fasta_sequences(fasta_path, max_sequences_per_species)

            for seq_data in seqs:
                seq = seq_data["sequence"]
                seq_len = len(seq)

                if seq_len < min_seq_length or seq_len > max_seq_length * 2:
                    continue

                sample = {
                    "sequence": seq[:max_seq_length],
                    "length": min(seq_len, max_seq_length),
                    "taxon_id": metadata.get("taxon_id", 0),
                    "species": metadata.get("species_name", ""),
                }

                for level in TAXONOMY_LEVELS:
                    sample[level] = levels_data[level]
                    class_counts[level][levels_data[level]] += 1

                samples.append(sample)

        except Exception:
            continue

        if (i + 1) % 5000 == 0:
            print(f"  Processed {i+1}/{len(json_files)}, {len(samples)} samples")

    print(f"Total samples: {len(samples)}")
    return samples, class_counts


def collect_quality_samples(
    curated_dir: Path,
    max_seq_length: int = 2000,
    min_seq_length: int = 100,
    max_sequences_per_species: int = 3,
    max_species: int = 15000,
) -> list[dict]:
    """Collect samples for quality_enhanced dataset with synthetic qualities."""
    print("Collecting quality_enhanced samples...")

    json_files = list(curated_dir.rglob("*.json"))

    # Limit species
    if len(json_files) > max_species:
        json_files = random.sample(json_files, max_species)

    print(f"Processing {len(json_files)} species...")

    samples = []
    seed_counter = 0

    for i, json_path in enumerate(json_files):
        try:
            with open(json_path) as f:
                metadata = json.load(f)

            fasta_path = json_path.with_suffix(".fasta.gz")
            if not fasta_path.exists():
                continue

            seqs = read_fasta_sequences(fasta_path, max_sequences_per_species)

            for seq_data in seqs:
                seq = seq_data["sequence"]
                seq_len = len(seq)

                if seq_len < min_seq_length or seq_len > max_seq_length:
                    continue

                # Generate synthetic quality_enhanced
                qualities = generate_synthetic_quality(seq, seed=seed_counter)
                seed_counter += 1

                samples.append({
                    "sequence": seq,
                    "qualities": qualities,
                    "length": seq_len,
                    "mean_quality": float(qualities.mean()),
                    "taxon_id": metadata.get("taxon_id", 0),
                })

        except Exception:
            continue

        if (i + 1) % 2000 == 0:
            print(f"  Processed {i+1}/{len(json_files)}, {len(samples)} samples")

    print(f"Total samples: {len(samples)}")
    return samples


def filter_raREDACTED(samples: list[dict], class_counts: dict, min_samples: int = 10) -> tuple[list[dict], dict]:
    """Filter out samples with rare classes (< min_samples)."""
    valid_classes = {}
    for level in TAXONOMY_LEVELS:
        valid_classes[level] = {
            cls for cls, count in class_counts[level].items()
            if count >= min_samples and cls != "unknown"
        }

    # Filter samples
    filtered = []
    for sample in samples:
        valid = True
        for level in TAXONOMY_LEVELS:
            if sample[level] not in valid_classes[level]:
                valid = False
                break
        if valid:
            filtered.append(sample)

    # Recount
    new_counts = {level: defaultdict(int) for level in TAXONOMY_LEVELS}
    for sample in filtered:
        for level in TAXONOMY_LEVELS:
            new_counts[level][sample[level]] += 1

    print(f"Filtered: {len(samples)} -> {len(filtered)} samples")
    return filtered, new_counts


def build_label_mappings(class_counts: dict) -> tuple[dict, dict, dict]:
    """Build class label lists and mappings."""
    class_labels = {}
    class_to_idx = {}
    num_classes = {}

    for level in TAXONOMY_LEVELS:
        # Sort by count descending
        sorted_classes = sorted(class_counts[level].items(), key=lambda x: -x[1])
        labels = [cls for cls, _ in sorted_classes if cls != "unknown"]

        class_labels[level] = labels
        class_to_idx[level] = {cls: idx for idx, cls in enumerate(labels)}
        num_classes[level] = len(labels)

    return class_labels, class_to_idx, num_classes


def create_splits(samples: list[dict], train_ratio: float = 0.8, val_ratio: float = 0.1) -> dict:
    """Create train/val/test splits."""
    random.shuffle(samples)
    n = len(samples)

    n_train = int(n * train_ratio)
    n_val = int(n * val_ratio)

    return {
        "train": samples[:n_train],
        "val": samples[n_train:n_train + n_val],
        "test": samples[n_train + n_val:],
    }


def save_taxonomy_dataset(
    samples: list[dict],
    class_counts: dict,
    output_dir: Path,
    max_seq_length: int = 2000,
):
    """Save taxonomy dataset in NPZ format."""
    print(f"\nSaving taxonomy dataset to {output_dir}")

    # Filter rare classes
    samples, class_counts = filter_raREDACTED(samples, class_counts, min_samples=10)

    # Build mappings
    class_labels, class_to_idx, num_classes = build_label_mappings(class_counts)

    # Print stats
    print("\nClasses per level:")
    for level in TAXONOMY_LEVELS:
        print(f"  {level}: {num_classes[level]}")

    # Create splits
    splits = create_splits(samples)

    # Compute feature statistics from training set
    print("\nComputing feature statistics...")
    train_features = []
    for sample in splits["train"][:5000]:  # Sample for stats
        features = compute_features(sample["sequence"])
        train_features.append(features)

    train_features = np.array(train_features)
    featuREDACTED = train_features.mean(axis=0).tolist()
    featuREDACTED = train_features.std(axis=0).tolist()

    # Feature names
    featuREDACTED = [
        "gc_content", "at_content", "purine_content", "pyrimidine_content",
        "freq_a", "freq_t", "freq_c", "freq_g",
        "gc_skew", "at_skew",
        "sequence_entropy", "linguistic_complexity", "compression_ratio",
        "max_homopolymer_a", "max_homopolymer_t", "max_homopolymer_c", "max_homopolymer_g",
        "gc_mean", "gc_std", "gc_min", "gc_max"
    ] + [f"dinuc_{di}" for di in DINUCLEOTIDES] + [f"trinuc_{tri}" for tri in TRINUCLEOTIDES]

    # Create output dirs
    output_dir.mkdir(parents=True, exist_ok=True)

    # Save metadata
    metadata = {
        "total_samples": len(samples),
        "train_samples": len(splits["train"]),
        "val_samples": len(splits["val"]),
        "test_samples": len(splits["test"]),
        "levels": TAXONOMY_LEVELS,
        "num_classes_per_level": num_classes,
        "class_labels_per_level": class_labels,
        "class_to_idx_per_level": class_to_idx,
        "class_counts_per_level": {level: dict(class_counts[level]) for level in TAXONOMY_LEVELS},
        "featuREDACTED": featuREDACTED,
        "featuREDACTED": featuREDACTED,
        "featuREDACTED": featuREDACTED,
        "num_features": 101,
        "config": {
            "levels": TAXONOMY_LEVELS,
            "max_seq_length": max_seq_length,
            "min_seq_length": 100,
            "min_samples_per_class": 10,
            "include_features": True,
            "compute_codons": False,
        }
    }

    with open(output_dir / "metadata.json", "w") as f:
        json.dump(metadata, f, indent=2)
    print(f"Saved metadata.json")

    # Save splits
    for split_name, split_samples in splits.items():
        split_dir = output_dir / split_name
        split_dir.mkdir(exist_ok=True)

        print(f"Saving {split_name}: {len(split_samples)} samples...")

        for idx, sample in enumerate(split_samples):
            # Encode sequence
            encoded_seq = encode_sequence(sample["sequence"], max_seq_length)

            # Compute features
            features = compute_features(sample["sequence"])

            # Get labels for each level
            labels = {}
            for level in TAXONOMY_LEVELS:
                cls = sample[level]
                if cls in class_to_idx[level]:
                    labels[f"label_{level}"] = np.int64(class_to_idx[level][cls])
                else:
                    labels[f"label_{level}"] = np.int64(-1)  # Unknown

            # Save NPZ
            npz_path = split_dir / f"sample_{idx:06d}.npz"
            np.savez(
                npz_path,
                sequence=encoded_seq,
                features=features,
                length=np.int64(sample["length"]),
                **labels
            )

            if (idx + 1) % 5000 == 0:
                print(f"  {idx + 1}/{len(split_samples)}")

        print(f"  Saved {len(split_samples)} files to {split_dir}")


def save_quality_dataset(
    samples: list[dict],
    output_dir: Path,
    max_seq_length: int = 2000,
):
    """Save quality_enhanced dataset in NPZ format."""
    print(f"\nSaving quality_enhanced dataset to {output_dir}")

    # Create splits
    splits = create_splits(samples)

    # Compute quality_enhanced statistics from training set
    mean_quals = [s["mean_quality"] for s in splits["train"]]
    print(f"Mean quality_enhanced: {np.mean(mean_quals):.1f}")
    print(f"Length range: {min(s['length'] for s in samples)} - {max(s['length'] for s in samples)}")

    # Create output dirs
    output_dir.mkdir(parents=True, exist_ok=True)

    # Save metadata
    metadata = {
        "total_samples": len(samples),
        "train_samples": len(splits["train"]),
        "val_samples": len(splits["val"]),
        "test_samples": len(splits["test"]),
        "task": "quality_prediction",
        "quality_range": [2, 40],
        "mean_quality": float(np.mean(mean_quals)),
        "std_quality": float(np.std(mean_quals)),
        "config": {
            "max_seq_length": max_seq_length,
            "min_seq_length": 100,
            "synthetic_qualities": True,
        }
    }

    with open(output_dir / "metadata.json", "w") as f:
        json.dump(metadata, f, indent=2)
    print(f"Saved metadata.json")

    # Save splits
    for split_name, split_samples in splits.items():
        split_dir = output_dir / split_name
        split_dir.mkdir(exist_ok=True)

        print(f"Saving {split_name}: {len(split_samples)} samples...")

        for idx, sample in enumerate(split_samples):
            seq = sample["sequence"]
            seq_len = sample["length"]

            # Encode sequence
            encoded_seq = encode_sequence(seq, max_seq_length)

            # Pad qualities to max_length
            qualities = sample["qualities"]
            padded_quals = np.full(max_seq_length, 0, dtype=np.int64)
            padded_quals[:len(qualities)] = qualities

            # Compute features
            features = compute_features(seq)

            # Save NPZ
            npz_path = split_dir / f"sample_{idx:06d}.npz"
            np.savez(
                npz_path,
                sequence=encoded_seq,
                qualities=padded_quals,
                features=features,
                length=np.int64(seq_len),
                mean_quality=np.float32(sample["mean_quality"]),
            )

            if (idx + 1) % 5000 == 0:
                print(f"  {idx + 1}/{len(split_samples)}")

        print(f"  Saved {len(split_samples)} files to {split_dir}")


def main():
    import argparse

    parser = argparse.ArgumentParser(description="Create training datasets")
    parser.add_argument("--curated", type=Path, default=Path("datalake/curated"),
                       help="Curated datalake directory")
    parser.add_argument("--output", type=Path, default=Path("datalake/datasets"),
                       help="Output directory")
    parser.add_argument("--taxonomy-only", action="stoREDACTED", help="Only create taxonomy dataset")
    parser.add_argument("--quality-only", action="stoREDACTED", help="Only create quality_enhanced dataset")
    parser.add_argument("--seed", type=int, default=42, help="Random seed")
    parser.add_argument("--max-seq-length", type=int, default=2000, help="Max sequence length")
    args = parser.parse_args()

    random.seed(args.seed)
    np.random.seed(args.seed)

    if not args.curated.exists():
        print(f"Error: {args.curated} does not exist")
        return 1

    # Create taxonomy dataset
    if not args.quality_only:
        print("=" * 60)
        print("CREATING TAXONOMY DATASET (Enhanced Hierarchical)")
        print("=" * 60)

        samples, class_counts = collect_taxonomy_samples(
            args.curated,
            max_seq_length=args.max_seq_length,
            max_sequences_per_species=5,
        )

        if samples:
            save_taxonomy_dataset(
                samples,
                class_counts,
                args.output / "taxonomy",
                max_seq_length=args.max_seq_length,
            )
        else:
            print("No taxonomy samples collected!")

    # Create quality_enhanced dataset
    if not args.taxonomy_only:
        print("\n" + "=" * 60)
        print("CREATING QUALITY DATASET")
        print("=" * 60)

        samples = collect_quality_samples(
            args.curated,
            max_seq_length=args.max_seq_length,
            max_sequences_per_species=3,
            max_species=15000,
        )

        if samples:
            save_quality_dataset(
                samples,
                args.output / "quality_enhanced",
                max_seq_length=args.max_seq_length,
            )
        else:
            print("No quality_enhanced samples collected!")

    print("\n" + "=" * 60)
    print("DATASETS CREATED")
    print("=" * 60)
    print(f"Output: {args.output}")

    for subdir in sorted(args.output.iterdir()):
        if subdir.is_dir() and (subdir / "metadata.json").exists():
            with open(subdir / "metadata.json") as f:
                meta = json.load(f)
            print(f"  {subdir.name}/: {meta.get('total_samples', 0):,} samples")

    return 0


if __name__ == "__main__":
    sys.exit(main())
