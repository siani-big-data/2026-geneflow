#!/usr/bin/env python3
"""Reorganize datalake from flat structure to taxonomic hierarchy.

Moves species from:
  datalake/raw/kingdom/species_name/
To:
  datalake/raw/kingdom/phylum/class/order/family/genus/species_name/

Usage:
    uv run python scripts/reorganize_datalake.py --dry-run  # Preview changes
    uv run python scripts/reorganize_datalake.py            # Execute reorganization
"""

import argparse
import json
import shutil
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent.parent))

from src.ml.datasets.downloader import ENAClient
from src.ml.datasets.downloader.parallel_downloader import sanitize_path_component


def get_species_dirs(raw_dir: Path) -> list[tuple[Path, str, int]]:
    """Find all species directories that need reorganization.

    Returns list of (dir_path, kingdom, taxon_id) for flat directories.
    """
    species_to_move = []

    for kingdom_dir in raw_dir.iterdir():
        if not kingdom_dir.is_dir():
            continue
        kingdom = kingdom_dir.name

        for species_dir in kingdom_dir.iterdir():
            if not species_dir.is_dir():
                continue

            # Check if this is a flat structure (species directly under kingdom)
            # by looking for .fasta.gz files
            fasta_files = list(species_dir.glob("*.fasta.gz"))
            if fasta_files:
                # This is a species dir - check if it's flat (parent is kingdom)
                if species_dir.parent.name == kingdom:
                    # Get taxon_id from json file
                    json_files = list(species_dir.glob("*.json"))
                    if json_files:
                        try:
                            with open(json_files[0]) as f:
                                meta = json.load(f)
                                taxon_id = meta.get("taxon_id", 0)
                                if taxon_id:
                                    species_to_move.append((species_dir, kingdom, taxon_id))
                        except Exception:
                            pass

    return species_to_move


def reorganize_species(
    species_dir: Path,
    kingdom: str,
    taxon_id: int,
    raw_dir: Path,
    client: ENAClient,
    dry_run: bool = True,
) -> tuple[bool, str]:
    """Reorganize a single species directory.

    Returns (success, message).
    """
    # Get taxonomy
    taxonomy = client.get_taxonomy(taxon_id)

    if not taxonomy:
        return False, f"No taxonomy found for taxon_id {taxon_id}"

    # Build new path
    path_parts = [sanitize_path_component(kingdom, 20)]

    for rank in ["phylum", "class", "order", "family", "genus"]:
        value = taxonomy.get(rank, "")
        if value and value.lower() != "unknown":
            path_parts.append(sanitize_path_component(value, 40))

    # Add species name (current directory name)
    species_name = species_dir.name
    path_parts.append(species_name)

    new_path = raw_dir / "/".join(path_parts)

    # Check if already in correct location
    if species_dir == new_path:
        return True, "Already in correct location"

    # Check if destination exists
    if new_path.exists():
        return False, f"Destination already exists: {new_path}"

    if dry_run:
        return True, f"Would move to: {new_path.relative_to(raw_dir)}"

    # Move the directory
    try:
        new_path.parent.mkdir(parents=True, exist_ok=True)
        shutil.move(str(species_dir), str(new_path))

        # Update metadata with taxonomy
        json_files = list(new_path.glob("*.json"))
        if json_files:
            with open(json_files[0]) as f:
                meta = json.load(f)
            meta["taxonomy"] = taxonomy
            meta["taxonomy"]["kingdom"] = kingdom
            with open(json_files[0], "w") as f:
                json.dump(meta, f, indent=2)

        return True, f"Moved to: {new_path.relative_to(raw_dir)}"
    except Exception as e:
        return False, f"Move failed: {e}"


def cleanup_empty_dirs(raw_dir: Path):
    """Remove empty directories after reorganization."""
    for kingdom_dir in raw_dir.iterdir():
        if not kingdom_dir.is_dir():
            continue

        # Remove empty subdirectories
        for subdir in list(kingdom_dir.rglob("*")):
            if subdir.is_dir() and not any(subdir.iterdir()):
                subdir.rmdir()


def main():
    parser = argparse.ArgumentParser(description="Reorganize datalake to taxonomic hierarchy")
    parser.add_argument("--datalake", type=Path, default=Path("datalake"),
                       help="Datalake directory (default: datalake)")
    parser.add_argument("--dry-run", action="stoREDACTED",
                       help="Preview changes without moving files")
    parser.add_argument("--kingdom", help="Only reorganize this kingdom")
    parser.add_argument("--limit", type=int, help="Limit number of species to process")
    args = parser.parse_args()

    raw_dir = args.datalake / "raw"

    if not raw_dir.exists():
        print(f"Error: {raw_dir} does not exist")
        return 1

    print("=" * 60)
    print("DATALAKE REORGANIZATION")
    print("=" * 60)
    print(f"Datalake: {args.datalake}")
    print(f"Mode: {'DRY RUN' if args.dry_run else 'EXECUTE'}")
    print()

    # Find species to move
    print("Scanning for flat directories...")
    species_list = get_species_dirs(raw_dir)

    if args.kingdom:
        species_list = [(d, k, t) for d, k, t in species_list if k == args.kingdom]

    if args.limit:
        species_list = species_list[:args.limit]

    print(f"Found {len(species_list)} species to reorganize")
    print()

    if not species_list:
        print("Nothing to reorganize!")
        return 0

    # Initialize ENA client for taxonomy lookups
    client = ENAClient(requests_per_second=10)

    # Process species
    success_count = 0
    fail_count = 0

    for i, (species_dir, kingdom, taxon_id) in enumerate(species_list):
        success, message = reorganize_species(
            species_dir, kingdom, taxon_id, raw_dir, client, args.dry_run
        )

        status = "OK" if success else "FAIL"
        print(f"[{i+1}/{len(species_list)}] {status} {species_dir.name}: {message}")

        if success:
            success_count += 1
        else:
            fail_count += 1

        # Progress every 100
        if (i + 1) % 100 == 0:
            print(
                f"\n--- Progress: {i+1}/{len(species_list)} "
                f"({success_count} OK, {fail_count} failed) ---\n"
            )

    # Cleanup empty directories
    if not args.dry_run:
        print("\nCleaning up empty directories...")
        cleanup_empty_dirs(raw_dir)

    # Summary
    print()
    print("=" * 60)
    print("SUMMARY")
    print("=" * 60)
    print(f"Total processed: {len(species_list)}")
    print(f"Successful: {success_count}")
    print(f"Failed: {fail_count}")

    if args.dry_run:
        print("\n[DRY RUN - No changes made. Run without --dry-run to execute]")

    return 0


if __name__ == "__main__":
    sys.exit(main())
