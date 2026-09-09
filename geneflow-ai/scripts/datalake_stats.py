#!/usr/bin/env python3
"""Show datalake statistics.

Usage:
    python scripts/datalake_stats.py
    python scripts/datalake_stats.py --raw          # Solo datalake/raw
    python scripts/datalake_stats.py --augmented    # Solo datalake/augmented
    python scripts/datalake_stats.py --curated      # Solo datalake/curated
    python scripts/datalake_stats.py --harvest      # Solo estado de descarga
    python scripts/datalake_stats.py --json         # Output en JSON
"""

import argparse
import json
import sys
from collections import defaultdict
from pathlib import Path

# Add project root to path
project_root = Path(__file__).parent.parent
sys.path.insert(0, str(project_root))


def get_datalake_stats(data_dir: Path) -> dict:
    """Get statistics from a datalake directory."""
    if not data_dir.exists():
        return None

    kingdom_counts = defaultdict(lambda: {
        'species': 0,
        'sequences': 0,
        'seq_per_species': [],
        'total_length': 0,
    })

    for json_file in data_dir.rglob('*.json'):
        parts = json_file.relative_to(data_dir).parts
        kingdom = parts[0] if parts else 'unknown'
        try:
            with open(json_file) as f:
                meta = json.load(f)
                seq_count = meta.get('sequence_count', 0)
                total_length = meta.get('total_length', 0)
                kingdom_counts[kingdom]['species'] += 1
                kingdom_counts[kingdom]['sequences'] += seq_count
                kingdom_counts[kingdom]['seq_per_species'].append(seq_count)
                kingdom_counts[kingdom]['total_length'] += total_length
        except Exception:
            pass

    # Calculate totals
    total_species = sum(k['species'] for k in kingdom_counts.values())
    total_sequences = sum(k['sequences'] for k in kingdom_counts.values())
    total_length = sum(k['total_length'] for k in kingdom_counts.values())

    # Distribution
    all_counts = []
    for k in kingdom_counts.values():
        all_counts.extend(k['seq_per_species'])

    distribution = {}
    ranges = [(1, 1), (2, 5), (6, 10), (11, 20), (21, 50), (51, 100), (101, 1000)]
    for lo, hi in ranges:
        count = sum(1 for c in all_counts if lo <= c <= hi)
        key = str(lo) if lo == hi else f"{lo}-{hi}"
        distribution[key] = count

    # Build result
    result = {
        'total_species': total_species,
        'total_sequences': total_sequences,
        'total_length_bp': total_length,
        'avg_seq_per_species': (
            round(total_sequences / total_species, 1) if total_species > 0 else 0
        ),
        'avg_length_bp': (
            round(total_length / total_sequences, 0) if total_sequences > 0 else 0
        ),
        'kingdoms': {},
        'distribution': distribution,
    }

    for kingdom in ['animalia', 'plantae', 'fungi', 'protista', 'monera']:
        if kingdom in kingdom_counts:
            k = kingdom_counts[kingdom]
            result['kingdoms'][kingdom] = {
                'species': k['species'],
                'sequences': k['sequences'],
                'avg_seq_per_species': (
                    round(k['sequences'] / k['species'], 1) if k['species'] > 0 else 0
                ),
                'percent_sequences': (
                    round(k['sequences'] / total_sequences * 100, 1)
                    if total_sequences > 0 else 0
                ),
            }

    return result


def get_harvest_stats() -> dict:
    """Get harvest state statistics."""
    harvest_file = project_root / 'harvest_state.json'
    if not harvest_file.exists():
        return None

    try:
        from src.ml.datasets.downloader import HarvestState
        state = HarvestState.load(harvest_file)

        result = {
            'created': state.created_at,
            'updated': state.updated_at,
            'target_sequences_per_species': state.target_sequences_per_species,
            'kingdoms': {},
            'total_species': 0,
            'total_complete': 0,
            'total_sequences': 0,
        }

        for kingdom, data in state.kingdoms.items():
            species_count = len(data.species)
            complete_count = sum(1 for s in data.species.values() if s.completed)
            seq_count = sum(len(s.accessions) for s in data.species.values())

            result['kingdoms'][kingdom] = {
                'species': species_count,
                'complete': complete_count,
                'sequences': seq_count,
            }
            result['total_species'] += species_count
            result['total_complete'] += complete_count
            result['total_sequences'] += seq_count

        return result

    except Exception as e:
        return {'error': str(e)}


def print_stats(stats: dict, title: str):
    """Print statistics in a nice format."""
    print()
    print("=" * 60)
    print(title)
    print("=" * 60)
    print(f"Total especies: {stats['total_species']:,}")
    print(f"Total secuencias: {stats['total_sequences']:,}")
    print(f"Promedio seq/especie: {stats['avg_seq_per_species']}")

    if stats.get('total_length_bp'):
        length_mb = stats['total_length_bp'] / 1_000_000
        print(f"Total longitud: {length_mb:,.1f} Mbp")
        print(f"Longitud promedio: {stats['avg_length_bp']:,.0f} bp")

    print()
    print("Por reino:")
    print(f"{'Reino':<12} {'Especies':>10} {'Secuencias':>12} {'Seq/Sp':>8} {'%':>7}")
    print("-" * 51)

    for kingdom in ['animalia', 'plantae', 'fungi', 'protista', 'monera']:
        if kingdom in stats['kingdoms']:
            k = stats['kingdoms'][kingdom]
            print(f"{kingdom:<12} {k['species']:>10,} {k['sequences']:>12,} "
                  f"{k['avg_seq_per_species']:>8.1f} {k['percent_sequences']:>6.1f}%")

    if stats.get('distribution'):
        print()
        print("Distribucion de secuencias por especie:")
        for key, count in stats['distribution'].items():
            pct = count / stats['total_species'] * 100 if stats['total_species'] > 0 else 0
            print(f"  {key:>8} seq: {count:>10,} especies ({pct:>5.1f}%)")

    print("=" * 60)


def print_harvest_stats(stats: dict):
    """Print harvest state statistics."""
    print()
    print("=" * 60)
    print("HARVEST STATE (descarga en progreso)")
    print("=" * 60)

    if 'error' in stats:
        print(f"Error: {stats['error']}")
        return

    print(f"Creado: {stats['created']}")
    print(f"Actualizado: {stats['updated']}")
    print(f"Target seq/especie: {stats['target_sequences_per_species']}")
    print()
    print(f"{'Reino':<12} {'Especies':>10} {'Completas':>10} {'Secuencias':>12}")
    print("-" * 46)

    for kingdom in ['animalia', 'plantae', 'fungi', 'protista', 'monera']:
        if kingdom in stats['kingdoms']:
            k = stats['kingdoms'][kingdom]
            print(f"{kingdom:<12} {k['species']:>10,} {k['complete']:>10,} {k['sequences']:>12,}")

    print("-" * 46)
    print(f"{'TOTAL':<12} {stats['total_species']:>10,} {stats['total_complete']:>10,} "
          f"{stats['total_sequences']:>12,}")
    print("=" * 60)


def main():
    parser = argparse.ArgumentParser(
        description="Show datalake statistics",
    )
    parser.add_argument(
        "--raw",
        action="stoREDACTED",
        help="Show only datalake/raw statistics",
    )
    parser.add_argument(
        "--augmented",
        action="stoREDACTED",
        help="Show only datalake/augmented statistics",
    )
    parser.add_argument(
        "--curated",
        action="stoREDACTED",
        help="Show only datalake/curated statistics",
    )
    parser.add_argument(
        "--harvest",
        action="stoREDACTED",
        help="Show only harvest state (download progress)",
    )
    parser.add_argument(
        "--json",
        action="stoREDACTED",
        help="Output in JSON format",
    )
    parser.add_argument(
        "--input",
        type=Path,
        default=None,
        help="Custom input directory",
    )

    args = parser.parse_args()

    results = {}

    # Determine what to show
    show_all = not (args.raw or args.augmented or args.curated or args.harvest)

    # Custom input
    if args.input:
        stats = get_datalake_stats(args.input)
        if stats:
            results['custom'] = stats
            if not args.json:
                print_stats(stats, f"DATALAKE: {args.input}")
        else:
            print(f"No data found in {args.input}")
        return

    # Raw datalake
    if show_all or args.raw:
        raw_dir = project_root / 'datalake' / 'raw'
        stats = get_datalake_stats(raw_dir)
        if stats:
            results['raw'] = stats
            if not args.json:
                print_stats(stats, "DATALAKE RAW")

    # Augmented datalake
    if show_all or args.augmented:
        aug_dir = project_root / 'datalake' / 'augmented'
        stats = get_datalake_stats(aug_dir)
        if stats:
            results['augmented'] = stats
            if not args.json:
                print_stats(stats, "DATALAKE AUGMENTED")

    # Curated datalake
    if show_all or args.curated:
        cur_dir = project_root / 'datalake' / 'curated'
        stats = get_datalake_stats(cur_dir)
        if stats:
            results['curated'] = stats
            if not args.json:
                print_stats(stats, "DATALAKE CURATED")

    # Harvest state
    if show_all or args.harvest:
        stats = get_harvest_stats()
        if stats:
            results['harvest'] = stats
            if not args.json:
                print_harvest_stats(stats)

    # JSON output
    if args.json:
        print(json.dumps(results, indent=2, default=str))

    # Summary if showing all
    if show_all and not args.json and 'raw' in results:
        print()
        print("Comandos utiles:")
        print("  uv run python scripts/datalake_stats.py --raw")
        print("  uv run python scripts/datalake_stats.py --harvest")
        print("  uv run python scripts/datalake_stats.py --json > stats.json")


if __name__ == "__main__":
    main()
