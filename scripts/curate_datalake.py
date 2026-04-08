#!/usr/bin/env python3
"""Curate datalake by combining raw and augmented data.

Creates a single curated layer with one fasta.gz and one json per species,
combining raw sequences with augmented ones.

Usage:
    python scripts/curate_datalake.py
    python scripts/curate_datalake.py --max-species 1000  # Test with 1000 species
    python scripts/curate_datalake.py --dry-run           # Preview without writing
"""

import argparse
import gzip
import json
import logging
import sys
from datetime import datetime, timezone
from pathlib import Path

# Add project root to path
project_root = Path(__file__).parent.parent
sys.path.insert(0, str(project_root))

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
)
logger = logging.getLogger(__name__)


def read_fasta_sequences(fasta_path: Path) -> list[tuple[str, str]]:
    """Read sequences from a fasta.gz file.

    Returns:
        List of (header, sequence) tuples
    """
    sequences = []
    try:
        with gzip.open(fasta_path, "rt") as f:
            current_header = ""
            current_seq = []

            for line in f:
                line = line.strip()
                if line.startswith(">"):
                    if current_seq:
                        sequences.append((current_header, "".join(current_seq)))
                    current_header = line[1:]
                    current_seq = []
                else:
                    current_seq.append(line)

            if current_seq:
                sequences.append((current_header, "".join(current_seq)))

    except Exception as e:
        logger.warning(f"Error reading {fasta_path}: {e}")

    return sequences


def write_fasta_sequences(fasta_path: Path, sequences: list[tuple[str, str]]):
    """Write sequences to a fasta.gz file."""
    fasta_path.parent.mkdir(parents=True, exist_ok=True)

    with gzip.open(fasta_path, "wt") as f:
        for header, seq in sequences:
            f.write(f">{header}\n")
            # Write sequence in 80-char lines
            for i in range(0, len(seq), 80):
                f.write(seq[i:i+80] + "\n")


def curate_species(
    raw_fasta: Path,
    raw_json: Path,
    aug_fasta: Path | None,
    aug_json: Path | None,
    out_fasta: Path,
    out_json: Path,
    dry_run: bool = False,
) -> dict:
    """Curate a single species by combining raw and augmented data.

    Returns:
        Stats dict with counts
    """
    stats = {
        'raw_sequences': 0,
        'augmented_sequences': 0,
        'total_sequences': 0,
    }

    # Read raw sequences
    raw_sequences = read_fasta_sequences(raw_fasta)
    stats['raw_sequences'] = len(raw_sequences)

    # Read raw metadata
    raw_meta = {}
    if raw_json.exists():
        try:
            with open(raw_json) as f:
                raw_meta = json.load(f)
        except Exception:
            pass

    # Read augmented sequences if available
    aug_sequences = []
    if aug_fasta and aug_fasta.exists():
        all_aug = read_fasta_sequences(aug_fasta)
        # Only take augmented ones (marked with [augmented:])
        aug_sequences = [(h, s) for h, s in all_aug if "[augmented:" in h]
        stats['augmented_sequences'] = len(aug_sequences)

    # Combine sequences
    all_sequences = raw_sequences + aug_sequences
    stats['total_sequences'] = len(all_sequences)

    if dry_run:
        return stats

    # Write combined fasta
    write_fasta_sequences(out_fasta, all_sequences)

    # Write combined metadata
    out_meta = raw_meta.copy()
    out_meta['sequence_count'] = len(all_sequences)
    out_meta['raw_count'] = len(raw_sequences)
    out_meta['augmented_count'] = len(aug_sequences)
    out_meta['total_length'] = sum(len(s) for _, s in all_sequences)
    out_meta['curated_at'] = datetime.now(timezone.utc).isoformat()

    out_json.parent.mkdir(parents=True, exist_ok=True)
    with open(out_json, "w") as f:
        json.dump(out_meta, f, indent=2)

    return stats


def curate_datalake(
    raw_dir: Path,
    aug_dir: Path,
    out_dir: Path,
    max_species: int | None = None,
    dry_run: bool = False,
) -> dict:
    """Curate entire datalake.

    Args:
        raw_dir: Path to datalake/raw
        aug_dir: Path to datalake/augmented
        out_dir: Path to datalake/curated
        max_species: Maximum species to process
        dry_run: Preview without writing

    Returns:
        Statistics dictionary
    """
    # Find all raw fasta files
    raw_fastas = list(raw_dir.rglob("*.fasta.gz"))
    if max_species:
        raw_fastas = raw_fastas[:max_species]

    logger.info(f"Processing {len(raw_fastas)} species...")

    stats = {
        'total_species': 0,
        'species_with_augmentation': 0,
        'total_raw_sequences': 0,
        'total_augmented_sequences': 0,
        'total_sequences': 0,
    }

    for i, raw_fasta in enumerate(raw_fastas):
        try:
            # Get relative path
            rel_path = raw_fasta.relative_to(raw_dir)

            # Build paths
            raw_json = raw_fasta.with_suffix("").with_suffix(".json")
            aug_fasta = aug_dir / rel_path
            aug_json = (
                aug_fasta.with_suffix("").with_suffix(".json")
                if aug_fasta.exists() else None
            )
            out_fasta = out_dir / rel_path
            out_json = out_fasta.with_suffix("").with_suffix(".json")

            # Curate species
            species_stats = curate_species(
                raw_fasta=raw_fasta,
                raw_json=raw_json,
                aug_fasta=aug_fasta if aug_fasta.exists() else None,
                aug_json=aug_json,
                out_fasta=out_fasta,
                out_json=out_json,
                dry_run=dry_run,
            )

            stats['total_species'] += 1
            stats['total_raw_sequences'] += species_stats['raw_sequences']
            stats['total_augmented_sequences'] += species_stats['augmented_sequences']
            stats['total_sequences'] += species_stats['total_sequences']

            if species_stats['augmented_sequences'] > 0:
                stats['species_with_augmentation'] += 1

            if (i + 1) % 1000 == 0:
                logger.info(f"Processed {i + 1}/{len(raw_fastas)} species...")

        except Exception as e:
            logger.warning(f"Error processing {raw_fasta}: {e}")

    return stats


def main():
    parser = argparse.ArgumentParser(
        description="Curate datalake by combining raw and augmented data",
    )
    parser.add_argument(
        "--raw-dir",
        type=Path,
        default=Path("datalake/raw"),
        help="Raw data directory (default: datalake/raw)",
    )
    parser.add_argument(
        "--aug-dir",
        type=Path,
        default=Path("datalake/augmented"),
        help="Augmented data directory (default: datalake/augmented)",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=Path("datalake/curated"),
        help="Output directory (default: datalake/curated)",
    )
    parser.add_argument(
        "--max-species",
        type=int,
        default=None,
        help="Maximum species to process (default: all)",
    )
    parser.add_argument(
        "--dry-run",
        action="stoREDACTED",
        help="Preview without writing files",
    )

    args = parser.parse_args()

    # Check directories
    if not args.raw_dir.exists():
        logger.error(f"Raw directory does not exist: {args.raw_dir}")
        sys.exit(1)

    print(f"\n{'='*60}")
    print("Curate Datalake")
    print(f"{'='*60}")
    print(f"Raw: {args.raw_dir}")
    print(f"Augmented: {args.aug_dir}")
    print(f"Output: {args.output}")
    print(f"Max species: {args.max_species or 'all'}")
    print(f"Dry run: {args.dry_run}")
    print(f"{'='*60}\n")

    # Run curation
    stats = curate_datalake(
        raw_dir=args.raw_dir,
        aug_dir=args.aug_dir,
        out_dir=args.output,
        max_species=args.max_species,
        dry_run=args.dry_run,
    )

    print(f"\n{'='*60}")
    print("Curation Complete" + (" (DRY RUN)" if args.dry_run else ""))
    print(f"{'='*60}")
    print(f"Species processed: {stats['total_species']:,}")
    print(f"Species with augmentation: {stats['species_with_augmentation']:,}")
    print(f"Raw sequences: {stats['total_raw_sequences']:,}")
    print(f"Augmented sequences: {stats['total_augmented_sequences']:,}")
    print(f"Total sequences: {stats['total_sequences']:,}")
    if not args.dry_run:
        print(f"Output: {args.output}")
    print(f"{'='*60}\n")


if __name__ == "__main__":
    main()
