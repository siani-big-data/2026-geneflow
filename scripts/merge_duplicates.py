#!/usr/bin/env python3
"""Merge duplicate species from flat structure into hierarchical structure.

When a species exists in both flat (kingdom/species) and hierarchical
(kingdom/.../species) structure, merge the sequences and remove the flat copy.
"""

import gzip
import json
import shutil
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent.parent))

from src.ml.datasets.downloader import ENAClient
from src.ml.datasets.downloader.parallel_downloader import sanitize_path_component


def find_duplicates(raw_dir: Path) -> list[tuple[Path, Path]]:
    """Find species that exist in both flat and hierarchical structure."""
    client = ENAClient(requests_per_second=10)
    duplicates = []

    for kingdom_dir in raw_dir.iterdir():
        if not kingdom_dir.is_dir():
            continue
        kingdom = kingdom_dir.name

        for item in kingdom_dir.iterdir():
            if not item.is_dir():
                continue

            # Check if this is flat (has fasta files directly)
            fastas = list(item.glob("*.fasta.gz"))
            if not fastas:
                continue

            # Get taxon_id
            jsons = list(item.glob("*.json"))
            if not jsons:
                continue

            try:
                with open(jsons[0]) as f:
                    meta = json.load(f)
                taxon_id = meta.get("taxon_id", 0)
                if not taxon_id:
                    continue
            except Exception:
                continue

            # Get taxonomy and build expected hierarchical path
            taxonomy = client.get_taxonomy(taxon_id)
            if not taxonomy:
                continue

            path_parts = [sanitize_path_component(kingdom, 20)]
            for rank in ["phylum", "class", "order", "family", "genus"]:
                value = taxonomy.get(rank, "")
                if value and value.lower() != "unknown":
                    path_parts.append(sanitize_path_component(value, 40))
            path_parts.append(item.name)

            hierarchical_path = raw_dir / "/".join(path_parts)

            if hierarchical_path.exists() and hierarchical_path != item:
                duplicates.append((item, hierarchical_path))

    return duplicates


def merge_species(flat_dir: Path, hier_dir: Path, dry_run: bool = False) -> str:
    """Merge sequences from flat dir into hierarchical dir."""
    # Find fasta files
    flat_fastas = list(flat_dir.glob("*.fasta.gz"))
    hier_fastas = list(hier_dir.glob("*.fasta.gz"))

    if not flat_fastas or not hier_fastas:
        return "Missing fasta files"

    flat_fasta = flat_fastas[0]
    hier_fasta = hier_fastas[0]

    # Read existing accessions from hierarchical
    hier_accessions = set()
    hier_sequences = []

    try:
        with gzip.open(hier_fasta, "rt") as f:
            current_header = ""
            current_seq = []
            for line in f:
                line = line.rstrip()
                if line.startswith(">"):
                    if current_seq:
                        acc = current_header.split()[0]
                        hier_accessions.add(acc)
                        hier_sequences.append((current_header, "".join(current_seq)))
                    current_header = line[1:]
                    current_seq = []
                else:
                    current_seq.append(line)
            if current_seq:
                acc = current_header.split()[0]
                hier_accessions.add(acc)
                hier_sequences.append((current_header, "".join(current_seq)))
    except Exception as e:
        return f"Error reading hierarchical fasta: {e}"

    # Read new sequences from flat
    new_sequences = []
    try:
        with gzip.open(flat_fasta, "rt") as f:
            current_header = ""
            current_seq = []
            for line in f:
                line = line.rstrip()
                if line.startswith(">"):
                    if current_seq:
                        acc = current_header.split()[0]
                        if acc not in hier_accessions:
                            new_sequences.append((current_header, "".join(current_seq)))
                    current_header = line[1:]
                    current_seq = []
                else:
                    current_seq.append(line)
            if current_seq:
                acc = current_header.split()[0]
                if acc not in hier_accessions:
                    new_sequences.append((current_header, "".join(current_seq)))
    except Exception as e:
        return f"Error reading flat fasta: {e}"

    if not new_sequences:
        # No new sequences, just delete flat
        if not dry_run:
            shutil.rmtree(flat_dir)
        return "No new sequences, deleted flat dir"

    if dry_run:
        return f"Would merge {len(new_sequences)} new sequences"

    # Write merged fasta
    try:
        with gzip.open(hier_fasta, "wt") as f:
            # Write existing
            for header, seq in hier_sequences:
                f.write(f">{header}\n")
                for i in range(0, len(seq), 80):
                    f.write(seq[i:i+80] + "\n")
            # Write new
            for header, seq in new_sequences:
                f.write(f">{header}\n")
                for i in range(0, len(seq), 80):
                    f.write(seq[i:i+80] + "\n")

        # Update metadata
        hier_jsons = list(hier_dir.glob("*.json"))
        if hier_jsons:
            with open(hier_jsons[0]) as f:
                meta = json.load(f)

            meta["sequence_count"] = len(hier_sequences) + len(new_sequences)
            new_accessions = [h.split()[0] for h, _ in new_sequences]
            meta["accessions"] = list(set(meta.get("accessions", []) + new_accessions))
            meta["sources"] = list(set(meta.get("sources", []) + ["ENA"]))

            with open(hier_jsons[0], "w") as f:
                json.dump(meta, f, indent=2)

        # Delete flat dir
        shutil.rmtree(flat_dir)

        return f"Merged {len(new_sequences)} sequences"

    except Exception as e:
        return f"Merge error: {e}"


def main():
    import argparse

    parser = argparse.ArgumentParser(description="Merge duplicate species")
    parser.add_argument("--datalake", type=Path, default=Path("datalake"))
    parser.add_argument("--dry-run", action="stoREDACTED")
    args = parser.parse_args()

    raw_dir = args.datalake / "raw"

    print("=" * 60)
    print("MERGE DUPLICATES")
    print("=" * 60)
    print(f"Mode: {'DRY RUN' if args.dry_run else 'EXECUTE'}")
    print()

    print("Finding duplicates...")
    duplicates = find_duplicates(raw_dir)
    print(f"Found {len(duplicates)} duplicates")
    print()

    merged = 0
    deleted = 0
    errors = 0

    for i, (flat, hier) in enumerate(duplicates):
        result = merge_species(flat, hier, args.dry_run)
        status = "OK" if "error" not in result.lower() else "FAIL"

        if "merged" in result.lower():
            merged += 1
        elif "deleted" in result.lower() or "no new" in result.lower():
            deleted += 1
        else:
            errors += 1

        print(f"[{i+1}/{len(duplicates)}] {status} {flat.name}: {result}")

    print()
    print("=" * 60)
    print("SUMMARY")
    print("=" * 60)
    print(f"Total duplicates: {len(duplicates)}")
    print(f"Merged (new sequences): {merged}")
    print(f"Deleted (no new sequences): {deleted}")
    print(f"Errors: {errors}")

    if args.dry_run:
        print("\n[DRY RUN - No changes made]")


if __name__ == "__main__":
    main()
