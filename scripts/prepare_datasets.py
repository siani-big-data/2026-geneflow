#!/usr/bin/env python3
"""Prepare datasets for model training from the curated datalake.

Creates train/val/test splits and generates dataset index files.

Usage:
    uv run python scripts/prepaREDACTED.py --stats        # Show dataset statistics
    uv run python scripts/prepaREDACTED.py --prepare      # Prepare all datasets
    uv run python scripts/prepaREDACTED.py --validate     # Validate datasets
"""

import argparse
import json
import random
import sys
from collections import defaultdict
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent.parent))


def collect_samples(data_dir: Path) -> list[dict]:
    """Collect all samples from the curated datalake."""
    samples = []

    json_files = list(data_dir.rglob("*.json"))
    print(f"Found {len(json_files)} metadata files")

    for json_path in json_files:
        try:
            with open(json_path) as f:
                metadata = json.load(f)

            # Check for corresponding fasta
            fasta_path = json_path.with_suffix(".fasta.gz")
            if not fasta_path.exists():
                continue

            taxonomy = metadata.get("taxonomy", {})
            if not taxonomy:
                continue

            # Get kingdom from path or taxonomy
            kingdom = taxonomy.get("kingdom", "")
            if not kingdom:
                # Try to extract from path
                parts = json_path.relative_to(data_dir).parts
                if parts:
                    kingdom = parts[0]

            samples.append({
                "path": str(json_path.relative_to(data_dir)),
                "fasta": str(fasta_path.relative_to(data_dir)),
                "taxon_id": metadata.get("taxon_id", 0),
                "species": metadata.get("species_name", ""),
                "sequence_count": metadata.get("sequence_count", 0),
                "taxonomy": taxonomy,
                "kingdom": kingdom.lower() if kingdom else "unknown",
                "phylum": taxonomy.get("phylum", "").lower() if taxonomy.get("phylum") else "unknown",
                "class": taxonomy.get("class", "").lower() if taxonomy.get("class") else "unknown",
                "order": taxonomy.get("order", "").lower() if taxonomy.get("order") else "unknown",
                "family": taxonomy.get("family", "").lower() if taxonomy.get("family") else "unknown",
                "genus": taxonomy.get("genus", "").lower() if taxonomy.get("genus") else "unknown",
            })
        except Exception as e:
            continue

    return samples


def compute_statistics(samples: list[dict]) -> dict:
    """Compute dataset statistics."""
    stats = {
        "total_samples": len(samples),
        "total_sequences": sum(s["sequence_count"] for s in samples),
        "by_kingdom": defaultdict(int),
        "by_phylum": defaultdict(int),
        "by_class": defaultdict(int),
        "by_order": defaultdict(int),
        "by_family": defaultdict(int),
        "by_genus": defaultdict(int),
    }

    for s in samples:
        stats["by_kingdom"][s["kingdom"]] += 1
        stats["by_phylum"][s["phylum"]] += 1
        stats["by_class"][s["class"]] += 1
        stats["by_order"][s["order"]] += 1
        stats["by_family"][s["family"]] += 1
        stats["by_genus"][s["genus"]] += 1

    return stats


def create_splits(
    samples: list[dict],
    train_ratio: float = 0.8,
    val_ratio: float = 0.1,
    test_ratio: float = 0.1,
    stratify_by: str = "kingdom",
    seed: int = 42,
) -> dict[str, list[dict]]:
    """Create train/val/test splits, stratified by taxonomy level."""
    random.seed(seed)

    # Group by stratification key
    groups = defaultdict(list)
    for s in samples:
        key = s.get(stratify_by, "unknown")
        groups[key].append(s)

    train, val, test = [], [], []

    for key, group_samples in groups.items():
        random.shuffle(group_samples)
        n = len(group_samples)

        n_train = int(n * train_ratio)
        n_val = int(n * val_ratio)

        train.extend(group_samples[:n_train])
        val.extend(group_samples[n_train:n_train + n_val])
        test.extend(group_samples[n_train + n_val:])

    # Shuffle each split
    random.shuffle(train)
    random.shuffle(val)
    random.shuffle(test)

    return {"train": train, "val": val, "test": test}


def save_dataset_index(
    splits: dict[str, list[dict]],
    output_dir: Path,
    dataset_name: str,
):
    """Save dataset index files."""
    output_dir.mkdir(parents=True, exist_ok=True)

    for split_name, samples in splits.items():
        output_file = output_dir / f"{dataset_name}_{split_name}.json"

        with open(output_file, "w") as f:
            json.dump({
                "name": dataset_name,
                "split": split_name,
                "count": len(samples),
                "samples": samples,
            }, f, indent=2)

        print(f"  Saved {output_file.name}: {len(samples)} samples")


def print_statistics(stats: dict):
    """Print dataset statistics."""
    print("=" * 60)
    print("DATASET STATISTICS")
    print("=" * 60)
    print(f"Total species: {stats['total_samples']:,}")
    print(f"Total sequences: {stats['total_sequences']:,}")
    print()

    print("By Kingdom:")
    for k, v in sorted(stats["by_kingdom"].items(), key=lambda x: -x[1]):
        print(f"  {k}: {v:,}")
    print()

    print(f"Unique phyla: {len(stats['by_phylum'])}")
    print(f"Unique classes: {len(stats['by_class'])}")
    print(f"Unique orders: {len(stats['by_order'])}")
    print(f"Unique families: {len(stats['by_family'])}")
    print(f"Unique genera: {len(stats['by_genus'])}")


def main():
    parser = argparse.ArgumentParser(description="Prepare datasets for training")
    parser.add_argument("--datalake", type=Path, default=Path("datalake/curated"),
                       help="Curated datalake directory")
    parser.add_argument("--output", type=Path, default=Path("datasets"),
                       help="Output directory for dataset indexes")
    parser.add_argument("--stats", action="stoREDACTED", help="Show statistics only")
    parser.add_argument("--prepare", action="stoREDACTED", help="Prepare all datasets")
    parser.add_argument("--validate", action="stoREDACTED", help="Validate datasets")
    parser.add_argument("--seed", type=int, default=42, help="Random seed")
    args = parser.parse_args()

    if not args.datalake.exists():
        print(f"Error: {args.datalake} does not exist")
        return 1

    print(f"Loading samples from {args.datalake}...")
    samples = collect_samples(args.datalake)

    if not samples:
        print("No samples found!")
        return 1

    stats = compute_statistics(samples)

    if args.stats or (not args.prepare and not args.validate):
        print_statistics(stats)
        return 0

    if args.prepare:
        print()
        print("=" * 60)
        print("PREPARING DATASETS")
        print("=" * 60)

        # 1. Kingdom classification dataset
        print("\n[1] Kingdom Classification Dataset")
        kingdom_splits = create_splits(samples, stratify_by="kingdom", seed=args.seed)
        save_dataset_index(kingdom_splits, args.output, "taxonomy_kingdom")

        # 2. Hierarchical taxonomy dataset (all levels)
        print("\n[2] Hierarchical Taxonomy Dataset")
        hier_splits = create_splits(samples, stratify_by="phylum", seed=args.seed)
        save_dataset_index(hier_splits, args.output, "taxonomy_hierarchical")

        # 3. Per-kingdom datasets for finer classification
        print("\n[3] Per-Kingdom Datasets")
        for kingdom in ["animalia", "plantae", "fungi", "monera", "protista"]:
            kingdom_samples = [s for s in samples if s["kingdom"] == kingdom]
            if len(kingdom_samples) < 100:
                print(f"  Skipping {kingdom}: only {len(kingdom_samples)} samples")
                continue

            splits = create_splits(kingdom_samples, stratify_by="phylum", seed=args.seed)
            save_dataset_index(splits, args.output, f"taxonomy_{kingdom}")

        # 4. Summary
        print()
        print("=" * 60)
        print("DATASETS CREATED")
        print("=" * 60)

        for f in sorted(args.output.glob("*.json")):
            with open(f) as fp:
                data = json.load(fp)
            print(f"  {f.name}: {data['count']:,} samples")

        print()
        print(f"Output directory: {args.output}")

    if args.validate:
        print()
        print("=" * 60)
        print("VALIDATING DATASETS")
        print("=" * 60)

        # Check that all referenced files exist
        missing = 0
        for s in samples[:1000]:  # Check first 1000
            fasta = args.datalake / s["fasta"]
            if not fasta.exists():
                print(f"  Missing: {s['fasta']}")
                missing += 1

        if missing == 0:
            print("  All files validated successfully!")
        else:
            print(f"  {missing} files missing")

    return 0


if __name__ == "__main__":
    sys.exit(main())
