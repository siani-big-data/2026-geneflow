#!/usr/bin/env python3
"""
Download massive training dataset from NCBI.

Usage:
    # View current progress
    uv run python scripts/download_training_data.py --status

    # Ultra minimal test (5 species, ~15 sequences)
    uv run python scripts/download_training_data.py --profile nano

    # Quick test (100 species, ~5K sequences)
    uv run python scripts/download_training_data.py --profile micro

    # Medium dataset (10K species, ~500K sequences)
    uv run python scripts/download_training_data.py --profile basic

    # Large dataset (50K species, ~2.5M sequences)
    uv run python scripts/download_training_data.py --profile large

    # Full dataset (100K species, ~5M sequences)
    uv run python scripts/download_training_data.py --profile full

    # Custom
    uv run python scripts/download_training_data.py --species 20000 --seqs-per-species 100

    # Continue interrupted download
    uv run python scripts/download_training_data.py --continue
"""

import argparse
import logging
import os
import sys
from datetime import datetime
from pathlib import Path

# Add src to path
sys.path.insert(0, str(Path(__file__).parent.parent))

from src.ml.datasets.downloader import HarvestState, ParallelDownloader

# Profiles
PROFILES = {
    "nano": {
        "name": "Nano Test",
        "species": 5,
        "seqs_per_species": 3,
        "estimated_sequences": 15,
        "estimated_time": "30 sec",
        "estimated_storage": "100 KB",
    },
    "micro": {
        "name": "Micro Test",
        "species": 100,
        "seqs_per_species": 50,
        "estimated_sequences": 5_000,
        "estimated_time": "5-10 min",
        "estimated_storage": "50 MB",
    },
    "small": {
        "name": "Small",
        "species": 1_000,
        "seqs_per_species": 50,
        "estimated_sequences": 50_000,
        "estimated_time": "1-2 hours",
        "estimated_storage": "500 MB",
    },
    "basic": {
        "name": "Basic",
        "species": 10_000,
        "seqs_per_species": 50,
        "estimated_sequences": 500_000,
        "estimated_time": "6-12 hours",
        "estimated_storage": "5 GB",
    },
    "medium": {
        "name": "Medium",
        "species": 25_000,
        "seqs_per_species": 50,
        "estimated_sequences": 1_250_000,
        "estimated_time": "1-2 days",
        "estimated_storage": "12 GB",
    },
    "large": {
        "name": "Large",
        "species": 50_000,
        "seqs_per_species": 50,
        "estimated_sequences": 2_500_000,
        "estimated_time": "2-4 days",
        "estimated_storage": "25 GB",
    },
    "full": {
        "name": "Full",
        "species": 100_000,
        "seqs_per_species": 50,
        "estimated_sequences": 5_000_000,
        "estimated_time": "1 week",
        "estimated_storage": "50 GB",
    },
}


def setup_logging(verbose: bool = False):
    """Configure logging."""
    level = logging.DEBUG if verbose else logging.INFO
    logging.basicConfig(
        level=level,
        format="%(asctime)s - %(levelname)s - %(message)s",
        datefmt="%Y-%m-%d %H:%M:%S",
        handlers=[
            logging.StreamHandler(),
            logging.FileHandler("download_training.log"),
        ],
    )


def print_banner():
    """Print startup banner."""
    print("""
+===================================================================+
|           GENEFLOW TRAINING DATA DOWNLOADER                       |
|                                                                   |
|   Download millions of sequences from NCBI with full taxonomy     |
+===================================================================+
    """)


def print_profiles():
    """Print available profiles."""
    print("\nAVAILABLE PROFILES:")
    print("=" * 70)
    print(f"{'Profile':<10} {'Species':>10} {'Sequences':>12} {'Storage':>10} {'Time':>15}")
    print("-" * 70)
    for key, p in PROFILES.items():
        print(
            f"{key:<10} {p['species']:>10,} {p['estimated_sequences']:>12,} "
            f"{p['estimated_storage']:>10} {p['estimated_time']:>15}"
        )
    print("=" * 70)


def get_api_keys() -> tuple[str, list[str]]:
    """Get NCBI credentials from environment."""
    email = os.getenv("NCBI_EMAIL", "")

    # Try to load from .env file
    env_file = Path(".env")
    if env_file.exists():
        with open(env_file) as f:
            for line in f:
                line = line.strip()
                if line and not line.startswith("#") and "=" in line:
                    key, value = line.split("=", 1)
                    os.environ[key.strip()] = value.strip()
        email = os.getenv("NCBI_EMAIL", email)

    # Collect API keys
    api_keys = []
    for i in range(1, 10):
        key = os.getenv(f"NCBI_API_KEY_{i}", "")
        if key:
            api_keys.append(key)

    # Add main key if exists
    main_key = os.getenv("NCBI_API_KEY", "")
    if main_key and main_key not in api_keys:
        api_keys.insert(0, main_key)

    return email, api_keys


def main():
    parser = argparse.ArgumentParser(
        description="Download training data from NCBI",
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )

    # Actions
    parser.add_argument("--status", action="stoREDACTED", help="Show current progress")
    parser.add_argument("--profiles", action="stoREDACTED", help="List available profiles")

    # Profile selection
    parser.add_argument("--profile", choices=list(PROFILES.keys()), default="basic",
                       help="Download profile (default: basic)")

    # Custom settings
    parser.add_argument("--species", type=int, help="Total species to download")
    parser.add_argument("--seqs-per-species", type=int,
                       help="Sequences per species (default: from profile)")
    parser.add_argument("--kingdom", choices=["animalia", "plantae", "fungi", "monera", "protista"],
                       help="Download only this kingdom (monera = bacteria + archaea)")
    parser.add_argument("--sample-size", type=int,
                       help="Sample size for species discovery (default: auto, 200k-500k)")

    # Paths
    parser.add_argument("--output", type=Path, default=Path("datalake"),
                       help="Output directory (default: datalake)")
    parser.add_argument("--state-file", type=Path, default=Path("harvest_state.json"),
                       help="State file path")

    # Options
    parser.add_argument("--continue", dest="continue_download", action="stoREDACTED",
                       help="Continue from previous state")
    parser.add_argument("--fresh", action="stoREDACTED",
                       help="Start fresh, ignore previous state")
    parser.add_argument("--verbose", "-v", action="stoREDACTED",
                       help="Verbose output")
    parser.add_argument("--dry-run", action="stoREDACTED",
                       help="Show plan without downloading")
    parser.add_argument("--minio", action="stoREDACTED",
                       help="Upload to MinIO after downloading")
    parser.add_argument("--minio-prefix", type=str, default="training/raw",
                       help="MinIO object prefix (default: training/raw)")

    args = parser.parse_args()

    setup_logging(args.verbose)
    print_banner()

    # Show profiles
    if args.profiles:
        print_profiles()
        return 0

    # Get credentials
    email, api_keys = get_api_keys()

    if not email:
        print("ERROR: NCBI_EMAIL is required.")
        print("Set it in .env file: NCBI_EMAIL=your.email@example.com")
        return 1

    print(f"Email: {email}")
    print(f"API Keys: {len(api_keys)} ({len(api_keys) * 10} req/s)")

    # Show status
    if args.status:
        state = HarvestState.load(args.state_file)
        print(state.summary())
        return 0

    # Get profile settings
    profile = PROFILES[args.profile]
    total_species = args.species or profile["species"]
    seqs_per_species = args.seqs_per_species or profile.get("seqs_per_species", 50)

    print(f"\nProfile: {profile['name']}")
    total_seqs = total_species * seqs_per_species
    print(
        f"Target: {total_species:,} species × {seqs_per_species} sequences = "
        f"{total_seqs:,} total"
    )
    print(f"Estimated storage: {profile['estimated_storage']}")
    print(f"Estimated time: {profile['estimated_time']}")
    print(f"Output: {args.output}")
    if args.minio:
        print(f"MinIO upload: enabled (prefix: {args.minio_prefix})")

    if args.dry_run:
        print("\n[DRY RUN - No data will be downloaded]")
        return 0

    # Confirm
    if total_species > 1000:
        response = input("\nStart download? (yes/no): ")
        if response.lower() not in ("yes", "y"):
            print("Aborted.")
            return 0

    # Initialize downloader
    downloader = ParallelDownloader(
        email=email,
        api_keys=api_keys,
        state_file=args.state_file,
        output_dir=args.output,
        sequences_per_species=seqs_per_species,
        upload_to_minio=args.minio,
        minio_prefix=args.minio_prefix,
    )

    # Handle fresh start
    if args.fresh and args.state_file.exists():
        args.state_file.unlink()
        downloader.state = HarvestState()
        print("\n[FRESH START - Previous state cleared]")

    try:
        start_time = datetime.now()

        if args.kingdom:
            # Download single kingdom
            result = downloader.download_kingdom(
                args.kingdom,
                total_species,
                sample_size=args.sample_size,
            )
            results = {args.kingdom: result}
        else:
            # Download all kingdoms
            results = downloader.download_all(total_species)

        elapsed = datetime.now() - start_time

        # Final summary
        print("\n" + "=" * 60)
        print("DOWNLOAD COMPLETE")
        print("=" * 60)
        print(downloader.get_summary())
        print(f"\nElapsed time: {elapsed}")
        print(f"Output directory: {args.output}")

        # Save final results
        import json
        with open("download_results.json", "w") as f:
            json.dump({
                "completed_at": datetime.now().isoformat(),
                "elapsed_seconds": elapsed.total_seconds(),
                "results": results,
            }, f, indent=2)

    except KeyboardInterrupt:
        print("\n\nInterrupted. State has been saved.")
        print("Run with --continue to resume.")
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
