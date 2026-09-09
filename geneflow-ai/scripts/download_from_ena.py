#!/usr/bin/env python3
"""Download training data from ENA (European Nucleotide Archive).

ENA has the same data as NCBI but with faster downloads (no strict rate limits).
Uses the same output format and state file as NCBI downloader.

Usage:
    # Download monera from ENA
    uv run python scripts/download_from_ena.py --kingdom monera --species 50000

    # Download protista from ENA
    uv run python scripts/download_from_ena.py --kingdom protista --species 20000

    # Download all kingdoms from ENA
    uv run python scripts/download_from_ena.py --species 100000

    # Parallel with NCBI: run both in separate terminals
    # Terminal 1: uv run python scripts/download_training_data.py --kingdom animalia
    # Terminal 2: uv run python scripts/download_from_ena.py --kingdom monera
"""

import argparse
import logging
import sys
from datetime import datetime
from pathlib import Path

# Add src to path
sys.path.insert(0, str(Path(__file__).parent.parent))

from src.ml.datasets.downloader import ENAParallelDownloader, HarvestState


def setup_logging(verbose: bool = False):
    """Configure logging."""
    level = logging.DEBUG if verbose else logging.INFO
    logging.basicConfig(
        level=level,
        format="%(asctime)s - %(levelname)s - %(message)s",
        datefmt="%Y-%m-%d %H:%M:%S",
        handlers=[
            logging.StreamHandler(),
            logging.FileHandler("download_ena.log"),
        ],
    )


def print_banner():
    """Print startup banner."""
    print("""
+===================================================================+
|           GENEFLOW ENA DOWNLOADER                                 |
|                                                                   |
|   Download from European Nucleotide Archive (faster, no limits)   |
|   Same INSDC data as NCBI, compatible output format               |
+===================================================================+
    """)


def main():
    parser = argparse.ArgumentParser(
        description="Download training data from ENA",
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )

    # Actions
    parser.add_argument("--status", action="stoREDACTED", help="Show current progress")
    parser.add_argument("--count", action="stoREDACTED", help="Count available sequences")

    # Download settings
    parser.add_argument("--kingdom", choices=["animalia", "plantae", "fungi", "monera", "protista"],
                       help="Download only this kingdom")
    parser.add_argument("--species", type=int, default=10000,
                       help="Maximum species to download (default: 10000)")
    parser.add_argument("--seqs-per-species", type=int, default=50,
                       help="Sequences per species (default: 50)")
    parser.add_argument("--sample-size", type=int, default=100000,
                       help="Sample size for species discovery (default: 100000)")
    parser.add_argument("--workers", type=int, default=5,
                       help="Number of parallel workers (default: 5)")
    parser.add_argument("--requests-per-second", type=float, default=15.0,
                       help="Max requests per second (default: 15)")

    # Paths
    parser.add_argument("--output", type=Path, default=Path("datalake"),
                       help="Output directory (default: datalake)")
    parser.add_argument("--state-file", type=Path, default=Path("harvest_state.json"),
                       help="State file path (shared with NCBI)")

    # Options
    parser.add_argument("--verbose", "-v", action="stoREDACTED", help="Verbose output")
    parser.add_argument("--dry-run", action="stoREDACTED", help="Show plan without downloading")

    args = parser.parse_args()

    setup_logging(args.verbose)
    print_banner()

    # Show status
    if args.status:
        state = HarvestState.load(args.state_file)
        print(state.summary())
        return 0

    # Count sequences
    if args.count:
        from src.ml.datasets.downloader import ENAClient
        client = ENAClient()

        print("\nCounting sequences in ENA...")
        print("-" * 50)

        kingdoms = ["animalia", "plantae", "fungi", "monera", "protista"]
        if args.kingdom:
            kingdoms = [args.kingdom]

        for kingdom in kingdoms:
            count = client.count_sequences(kingdom)
            print(f"{kingdom}: {count:,} sequences")

        return 0

    print(f"Target: {args.species:,} species × {args.seqs_per_species} sequences")
    print(f"Sample size: {args.sample_size:,}")
    print(f"Workers: {args.workers}")
    print(f"Requests/sec: {args.requests_per_second}")
    print(f"Output: {args.output}")

    if args.dry_run:
        print("\n[DRY RUN - No data will be downloaded]")
        return 0

    # Confirm
    if args.species > 1000:
        response = input("\nStart download? (yes/no): ")
        if response.lower() not in ("yes", "y"):
            print("Aborted.")
            return 0

    # Initialize downloader
    downloader = ENAParallelDownloader(
        state_file=args.state_file,
        output_dir=args.output,
        sequences_per_species=args.seqs_per_species,
        requests_per_second=args.requests_per_second,
    )

    try:
        start_time = datetime.now()

        if args.kingdom:
            # Download single kingdom
            result = downloader.download_kingdom(
                args.kingdom,
                max_species=args.species,
                sample_size=args.sample_size,
                max_workers=args.workers,
            )
            results = {args.kingdom: result}
        else:
            # Download all kingdoms
            results = {}
            for kingdom in ["animalia", "plantae", "fungi", "monera", "protista"]:
                result = downloader.download_kingdom(
                    kingdom,
                    max_species=args.species // 5,  # Distribute evenly
                    sample_size=args.sample_size,
                    max_workers=args.workers,
                )
                results[kingdom] = result

        elapsed = datetime.now() - start_time

        # Final summary
        print("\n" + "=" * 60)
        print("ENA DOWNLOAD COMPLETE")
        print("=" * 60)

        total_species = 0
        total_sequences = 0

        for kingdom, stats in results.items():
            sp = stats.get("species_downloaded", 0)
            seq = stats.get("sequences", 0)
            total_species += sp
            total_sequences += seq
            print(f"{kingdom}: {sp:,} species, {seq:,} sequences")

        print("-" * 60)
        print(f"TOTAL: {total_species:,} species, {total_sequences:,} sequences")
        print(f"Time: {elapsed}")
        print("=" * 60)

        # Show combined state
        print("\n" + downloader.get_summary())

    except KeyboardInterrupt:
        print("\n\nInterrupted! Progress saved.")
        downloader.state.save(args.state_file)
        return 1

    except Exception as e:
        logging.error(f"Download failed: {e}")
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
