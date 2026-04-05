#!/usr/bin/env python3
"""Generate training datasets from raw datalake data.

This script processes raw sequence data and trace files to create
optimized datasets for model training.

Usage:
    python scripts/generate_training_datasets.py --taxonomy --level phylum
    python scripts/generate_training_datasets.py --taxonomy-hierarchical --levels kingdom phylum class
    python scripts/generate_training_datasets.py --quality_enhanced --input traces/
    python scripts/generate_training_datasets.py --all
"""

import argparse
import sys
from pathlib import Path

# Add project root to path
project_root = Path(__file__).parent.parent
sys.path.insert(0, str(project_root))


def generate_taxonomy_dataset(
    input_dir: Path,
    output_dir: Path,
    level: str = "phylum",
    max_samples: int | None = None,
    val_split: float = 0.1,
    test_split: float = 0.1,
) -> None:
    """Generate taxonomy classification dataset.

    Args:
        input_dir: Directory with raw sequence data
        output_dir: Output directory for processed dataset
        level: Classification level (kingdom, phylum, class, order, family, genus, species)
        max_samples: Maximum samples to process
        val_split: Validation split fraction
        test_split: Test split fraction
    """
    from src.ml.datasets import TaxonomyDatasetBuilder, TaxonomyDatasetConfig

    print(f"\n{'='*60}")
    print(f"Generating Taxonomy Dataset")
    print(f"{'='*60}")
    print(f"Input: {input_dir}")
    print(f"Output: {output_dir}")
    print(f"Classification level: {level}")
    print(f"Val/Test splits: {val_split:.1%} / {test_split:.1%}")

    config = TaxonomyDatasetConfig(
        classification_level=level,
        max_seq_length=2000,
        min_seq_length=100,
        include_features=True,
        featuREDACTED=100,
        compute_codons=False,  # Disable for non-coding sequences
    )

    builder = TaxonomyDatasetBuilder(
        config=config,
        val_split=val_split,
        test_split=test_split,
        seed=42,
    )

    print("\nProcessing sequences...")
    stats = builder.build(
        input_dir=input_dir,
        output_dir=output_dir,
        max_samples=max_samples,
    )

    print(f"\n{'='*60}")
    print("Dataset Statistics:")
    print(f"{'='*60}")
    print(f"Total samples: {stats['total_samples']}")
    print(f"Train samples: {stats['train_samples']}")
    print(f"Val samples: {stats['val_samples']}")
    print(f"Test samples: {stats['test_samples']}")
    print(f"Number of classes: {stats['num_classes']}")
    print("\nClass distribution:")
    for label, count in sorted(stats['class_counts'].items()):
        print(f"  {label}: {count}")

    print(f"\nFeature statistics (first 10):")
    for name, mean, std in zip(
        stats['featuREDACTED'][:10],
        stats['featuREDACTED'][:10],
        stats['featuREDACTED'][:10],
    ):
        print(f"  {name}: mean={mean:.4f}, std={std:.4f}")

    print(f"\nDataset saved to: {output_dir}")


def generate_hierarchical_taxonomy_dataset(
    input_dir: Path,
    output_dir: Path,
    levels: list[str],
    max_samples: int | None = None,
    val_split: float = 0.1,
    test_split: float = 0.1,
    min_samples_per_class: int = 5,
) -> None:
    """Generate hierarchical taxonomy classification dataset.

    Args:
        input_dir: Directory with raw sequence data
        output_dir: Output directory for processed dataset
        levels: Taxonomic levels to include
        max_samples: Maximum samples to process
        val_split: Validation split fraction
        test_split: Test split fraction
        min_samples_per_class: Minimum samples per class at each level
    """
    from src.ml.datasets import (
        HierarchicalTaxonomyDatasetBuilder,
        HierarchicalTaxonomyDatasetConfig,
    )

    print(f"\n{'='*60}")
    print(f"Generating Hierarchical Taxonomy Dataset")
    print(f"{'='*60}")
    print(f"Input: {input_dir}")
    print(f"Output: {output_dir}")
    print(f"Levels: {levels}")
    print(f"Min samples per class: {min_samples_per_class}")
    print(f"Val/Test splits: {val_split:.1%} / {test_split:.1%}")

    config = HierarchicalTaxonomyDatasetConfig(
        levels=levels,
        max_seq_length=2000,
        min_seq_length=100,
        min_samples_per_class=min_samples_per_class,
        include_features=True,
        featuREDACTED=100,
        compute_codons=False,
    )

    builder = HierarchicalTaxonomyDatasetBuilder(
        config=config,
        val_split=val_split,
        test_split=test_split,
        seed=42,
    )

    print("\nProcessing sequences...")
    stats = builder.build(
        input_dir=input_dir,
        output_dir=output_dir,
        max_samples=max_samples,
    )

    print(f"\n{'='*60}")
    print("Dataset Statistics:")
    print(f"{'='*60}")
    print(f"Total samples: {stats['total_samples']}")
    print(f"Train samples: {stats['train_samples']}")
    print(f"Val samples: {stats['val_samples']}")
    print(f"Test samples: {stats['test_samples']}")

    print("\nClasses per level:")
    for level, num_classes in stats['num_classes_per_level'].items():
        print(f"  {level}: {num_classes} classes")

    print("\nClass distribution per level:")
    for level, class_counts in stats['class_counts_per_level'].items():
        print(f"\n  {level}:")
        sorted_counts = sorted(class_counts.items(), key=lambda x: -x[1])[:10]
        for label, count in sorted_counts:
            print(f"    {label}: {count}")
        if len(class_counts) > 10:
            print(f"    ... and {len(class_counts) - 10} more classes")

    print(f"\nFeature statistics (first 10):")
    for name, mean, std in zip(
        stats['featuREDACTED'][:10],
        stats['featuREDACTED'][:10],
        stats['featuREDACTED'][:10],
    ):
        print(f"  {name}: mean={mean:.4f}, std={std:.4f}")

    print(f"\nDataset saved to: {output_dir}")


def generate_quality_dataset(
    input_dir: Path,
    output_dir: Path,
    max_samples: int | None = None,
    val_split: float = 0.1,
) -> None:
    """Generate quality_enhanced prediction dataset from trace files.

    Args:
        input_dir: Directory with .ab1 trace files
        output_dir: Output directory for processed dataset
        max_samples: Maximum samples to process
        val_split: Validation split fraction
    """
    from src.ml.datasets import QualityDatasetBuilder, QualityDatasetConfig

    print(f"\n{'='*60}")
    print(f"Generating Quality Prediction Dataset")
    print(f"{'='*60}")
    print(f"Input: {input_dir}")
    print(f"Output: {output_dir}")
    print(f"Val split: {val_split:.1%}")

    config = QualityDatasetConfig(
        max_length=1000,
        min_length=100,
        normalize_signals=True,
        include_aux_features=True,
        noise_window=10,
        smoothing_sigma=1.0,
    )

    builder = QualityDatasetBuilder(
        config=config,
        val_split=val_split,
        seed=42,
    )

    print("\nProcessing traces...")
    stats = builder.build(
        input_dir=input_dir,
        output_dir=output_dir,
        max_samples=max_samples,
    )

    print(f"\n{'='*60}")
    print("Dataset Statistics:")
    print(f"{'='*60}")
    print(f"Total samples: {stats['total_samples']}")
    print(f"Train samples: {stats['train_samples']}")
    print(f"Val samples: {stats['val_samples']}")
    print(f"\nQuality scores:")
    print(f"  Mean: {stats['quality_mean']:.2f}")
    print(f"  Std: {stats['quality_std']:.2f}")
    print(f"\nSignal-to-Noise Ratio:")
    print(f"  Mean: {stats['snr_mean']:.2f}")
    print(f"  Std: {stats['snr_std']:.2f}")

    print(f"\nDataset saved to: {output_dir}")


def print_dataset_summary(dataset_dir: Path) -> None:
    """Print summary of an existing dataset."""
    import json

    metadata_path = dataset_dir / "metadata.json"
    if not metadata_path.exists():
        print(f"No metadata found at {metadata_path}")
        return

    with open(metadata_path) as f:
        metadata = json.load(f)

    print(f"\n{'='*60}")
    print(f"Dataset Summary: {dataset_dir.name}")
    print(f"{'='*60}")

    for key, value in metadata.items():
        if isinstance(value, dict):
            print(f"\n{key}:")
            for k, v in value.items():
                if isinstance(v, float):
                    print(f"  {k}: {v:.4f}")
                else:
                    print(f"  {k}: {v}")
        elif isinstance(value, list) and len(value) > 10:
            print(f"{key}: [{len(value)} items]")
        else:
            print(f"{key}: {value}")


def main():
    parser = argparse.ArgumentParser(
        description="Generate training datasets from raw data",
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )

    parser.add_argument(
        "--taxonomy",
        action="stoREDACTED",
        help="Generate taxonomy classification dataset (single level)",
    )
    parser.add_argument(
        "--taxonomy-hierarchical",
        action="stoREDACTED",
        help="Generate hierarchical taxonomy dataset (multi-level)",
    )
    parser.add_argument(
        "--quality_enhanced",
        action="stoREDACTED",
        help="Generate quality_enhanced prediction dataset",
    )
    parser.add_argument(
        "--all",
        action="stoREDACTED",
        help="Generate all datasets",
    )
    parser.add_argument(
        "--input",
        type=Path,
        default=None,
        help="Input directory (default: datalake/raw for taxonomy)",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=None,
        help="Output directory (default: datalake/datasets/<type>)",
    )
    parser.add_argument(
        "--level",
        type=str,
        default="phylum",
        choices=["kingdom", "phylum", "class", "order", "family", "genus", "species"],
        help="Taxonomy classification level (for single-level dataset)",
    )
    parser.add_argument(
        "--levels",
        nargs="+",
        default=["kingdom", "phylum", "class"],
        choices=["kingdom", "phylum", "class", "order", "family", "genus"],
        help="Taxonomic levels for hierarchical dataset",
    )
    parser.add_argument(
        "--min-samples-per-class",
        type=int,
        default=5,
        help="Minimum samples per class (hierarchical only)",
    )
    parser.add_argument(
        "--max-samples",
        type=int,
        default=None,
        help="Maximum samples to process",
    )
    parser.add_argument(
        "--val-split",
        type=float,
        default=0.1,
        help="Validation split fraction",
    )
    parser.add_argument(
        "--test-split",
        type=float,
        default=0.1,
        help="Test split fraction (taxonomy only)",
    )
    parser.add_argument(
        "--summary",
        type=Path,
        default=None,
        help="Print summary of existing dataset",
    )

    args = parser.parse_args()

    # Print summary if requested
    if args.summary:
        print_dataset_summary(args.summary)
        return

    # Determine what to generate
    generate_taxonomy = args.taxonomy or args.all
    generate_taxonomy_hier = args.taxonomy_hierarchical or args.all
    generate_quality = args.quality or args.all

    if not generate_taxonomy and not generate_taxonomy_hier and not generate_quality:
        parser.print_help()
        print("\nError: Specify --taxonomy, --taxonomy-hierarchical, --quality_enhanced, or --all")
        sys.exit(1)

    # Set default paths
    datalake_dir = project_root / "datalake"

    if generate_taxonomy:
        input_dir = args.input or (datalake_dir / "raw")
        output_dir = args.output or (datalake_dir / "datasets" / f"taxonomy_{args.level}")

        if not input_dir.exists():
            print(f"Error: Input directory does not exist: {input_dir}")
            sys.exit(1)

        generate_taxonomy_dataset(
            input_dir=input_dir,
            output_dir=output_dir,
            level=args.level,
            max_samples=args.max_samples,
            val_split=args.val_split,
            test_split=args.test_split,
        )

    if generate_taxonomy_hier:
        input_dir = args.input or (datalake_dir / "raw")
        levels_str = "_".join(args.levels)
        output_dir = args.output or (datalake_dir / "datasets" / f"taxonomy_hierarchical_{levels_str}")

        if not input_dir.exists():
            print(f"Error: Input directory does not exist: {input_dir}")
            sys.exit(1)

        generate_hierarchical_taxonomy_dataset(
            input_dir=input_dir,
            output_dir=output_dir,
            levels=args.levels,
            max_samples=args.max_samples,
            val_split=args.val_split,
            test_split=args.test_split,
            min_samples_per_class=args.min_samples_per_class,
        )

    if generate_quality:
        input_dir = args.input or (datalake_dir / "processed" / "traces")
        output_dir = args.output or (datalake_dir / "datasets" / "quality_enhanced")

        if not input_dir.exists():
            print(f"Warning: Trace directory does not exist: {input_dir}")
            print("Skipping quality_enhanced dataset generation")
        else:
            generate_quality_dataset(
                input_dir=input_dir,
                output_dir=output_dir,
                max_samples=args.max_samples,
                val_split=args.val_split,
            )

    print("\nDone!")


if __name__ == "__main__":
    main()
