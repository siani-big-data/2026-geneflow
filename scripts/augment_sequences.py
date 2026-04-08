#!/usr/bin/env python3
"""Augment sequences for species with few samples.

Generates additional sequences by applying biologically-plausible
transformations (mutations, indels, reverse complement, subsequences).

Usage:
    # Augment to 10 sequences per species (default)
    python scripts/augment_sequences.py

    # Augment to 20 sequences per species
    python scripts/augment_sequences.py --target 20

    # Process only 1000 species
    python scripts/augment_sequences.py --max-species 1000

    # Custom mutation rate
    python scripts/augment_sequences.py --mutation-rate 0.03
"""

import argparse
import logging
import sys
from pathlib import Path

# Add project root to path
project_root = Path(__file__).parent.parent
sys.path.insert(0, str(project_root))

from src.ml.datasets.augmentation import AugmentationConfig, SequenceAugmenter  # noqa: E402

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
)
logger = logging.getLogger(__name__)


def main():
    parser = argparse.ArgumentParser(
        description="Augment sequences for species with few samples",
    )
    parser.add_argument(
        "--input",
        type=Path,
        default=Path("datalake/raw"),
        help="Input directory with FASTA files (default: datalake/raw)",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=Path("datalake/augmented"),
        help="Output directory (default: datalake/augmented)",
    )
    parser.add_argument(
        "--target",
        type=int,
        default=10,
        help="Target sequences per species (default: 10)",
    )
    parser.add_argument(
        "--max-species",
        type=int,
        default=None,
        help="Maximum species to process (default: all)",
    )
    parser.add_argument(
        "--mutation-rate",
        type=float,
        default=0.02,
        help="Mutation rate (default: 0.02 = 2%%)",
    )
    parser.add_argument(
        "--max-aug-per-seq",
        type=int,
        default=5,
        help="Maximum augmentations per original sequence (default: 5)",
    )
    parser.add_argument(
        "--seed",
        type=int,
        default=42,
        help="Random seed (default: 42)",
    )

    args = parser.parse_args()

    # Check input directory
    if not args.input.exists():
        logger.error(f"Input directory does not exist: {args.input}")
        sys.exit(1)

    # Configure augmenter
    config = AugmentationConfig(
        min_samples_per_species=args.target,
        max_augmented_per_original=args.max_aug_per_seq,
        mutation_rate=args.mutation_rate,
        seed=args.seed,
    )

    augmenter = SequenceAugmenter(config)

    print(f"\n{'='*60}")
    print("Sequence Augmentation")
    print(f"{'='*60}")
    print(f"Input: {args.input}")
    print(f"Output: {args.output}")
    print(f"Target per species: {args.target}")
    print(f"Max species: {args.max_species or 'all'}")
    print(f"Mutation rate: {args.mutation_rate:.1%}")
    print(f"{'='*60}\n")

    # Run augmentation
    stats = augmenter.augment_datalake(
        raw_dir=args.input,
        output_dir=args.output,
        target_per_species=args.target,
        max_species=args.max_species,
    )

    print(f"\n{'='*60}")
    print("Augmentation Complete")
    print(f"{'='*60}")
    print(f"Species processed: {stats['total_species']:,}")
    print(f"Species augmented: {stats['species_augmented']:,}")
    print(f"Original sequences: {stats['original_sequences']:,}")
    print(f"New sequences: {stats['augmented_sequences']:,}")
    print(f"Total sequences: {stats['total_sequences']:,}")
    print(f"Output: {args.output}")
    print(f"{'='*60}\n")


if __name__ == "__main__":
    main()
