#!/usr/bin/env python3
"""Generate synthetic traces from existing datalake sequences.

This script creates synthetic chromatogram data from FASTA sequences,
useful for training QualityPredictor without real .ab1 files.

Usage:
    # Generate traces for all species
    python scripts/generate_synthetic_traces.py

    # Limit to 1000 species
    python scripts/generate_synthetic_traces.py --max-species 1000

    # Custom output directory
    python scripts/generate_synthetic_traces.py --output datalake/synthetic_traces

    # More variations per sequence
    python scripts/generate_synthetic_traces.py --variations 5
"""

import argparse
import logging
import sys
from pathlib import Path

# Add project root to path
project_root = Path(__file__).parent.parent
sys.path.insert(0, str(project_root))

from src.ml.datasets.augmentation import TraceGenerator, TraceGeneratorConfig

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
)
logger = logging.getLogger(__name__)


def main():
    parser = argparse.ArgumentParser(
        description="Generate synthetic traces from datalake sequences",
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
        default=Path("datalake/synthetic_traces"),
        help="Output directory for synthetic traces (default: datalake/synthetic_traces)",
    )
    parser.add_argument(
        "--max-species",
        type=int,
        default=None,
        help="Maximum species to process (default: all)",
    )
    parser.add_argument(
        "--max-seq-length",
        type=int,
        default=1000,
        help="Maximum sequence length (default: 1000)",
    )
    parser.add_argument(
        "--min-seq-length",
        type=int,
        default=100,
        help="Minimum sequence length (default: 100)",
    )
    parser.add_argument(
        "--variations",
        type=int,
        default=3,
        help="Variations per sequence (default: 3)",
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

    # Configure generator
    config = TraceGeneratorConfig(
        variations_per_sequence=args.variations,
    )

    generator = TraceGenerator(config=config, seed=args.seed)

    print(f"\n{'='*60}")
    print("Synthetic Trace Generation")
    print(f"{'='*60}")
    print(f"Input: {args.input}")
    print(f"Output: {args.output}")
    print(f"Max species: {args.max_species or 'all'}")
    print(f"Sequence length: {args.min_seq_length}-{args.max_seq_length}")
    print(f"Variations per sequence: {args.variations}")
    print(f"{'='*60}\n")

    # Generate traces
    stats = generator.process_datalake(
        raw_dir=args.input,
        output_dir=args.output,
        max_species=args.max_species,
        max_seq_length=args.max_seq_length,
        min_seq_length=args.min_seq_length,
    )

    print(f"\n{'='*60}")
    print("Generation Complete")
    print(f"{'='*60}")
    print(f"Species processed: {stats['total_species']:,}")
    print(f"Traces generated: {stats['total_traces']:,}")
    print(f"Mean quality: {stats['mean_quality']:.1f}")
    print(f"Quality std: {stats['quality_std']:.1f}")
    print(f"Output: {args.output}")
    print(f"{'='*60}\n")


if __name__ == "__main__":
    main()
