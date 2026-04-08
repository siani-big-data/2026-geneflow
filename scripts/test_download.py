"""Test download script with full taxonomic classification."""

import asyncio
import json
from datetime import datetime, timezone

import httpx

from src.external.ncbi_taxonomy import KNOWN_TAXONOMY, ncbi_taxonomy
from src.storage import minio_storage

# Sample AB1 files from different organisms
SAMPLE_FILES = [
    # Human samples (ABI instruments)
    {
        "url": "https://raw.githubusercontent.com/biopython/biopython/master/Tests/Abi/3730.ab1",
        "name": "human_3730_sample1",
        "instrument": "ABI 3730",
        "organism": "Homo sapiens",
    },
    {
        "url": "https://raw.githubusercontent.com/biopython/biopython/master/Tests/Abi/310.ab1",
        "name": "human_310_sample1",
        "instrument": "ABI 310",
        "organism": "Homo sapiens",
    },
    {
        "url": "https://raw.githubusercontent.com/biopython/biopython/master/Tests/Abi/3100.ab1",
        "name": "human_3100_sample1",
        "instrument": "ABI 3100",
        "organism": "Homo sapiens",
    },
    # We'll simulate other organisms by reusing files but with different metadata
    # In production, these would be real files from each organism
    {
        "url": "https://raw.githubusercontent.com/biopython/biopython/master/Tests/Abi/3730.ab1",
        "name": "mouse_sample1",
        "instrument": "ABI 3730",
        "organism": "Mus musculus",
    },
    {
        "url": "https://raw.githubusercontent.com/biopython/biopython/master/Tests/Abi/310.ab1",
        "name": "ecoli_sample1",
        "instrument": "ABI 310",
        "organism": "Escherichia coli",
    },
    {
        "url": "https://raw.githubusercontent.com/biopython/biopython/master/Tests/Abi/3100.ab1",
        "name": "yeast_sample1",
        "instrument": "ABI 3100",
        "organism": "Saccharomyces cerevisiae",
    },
    {
        "url": "https://raw.githubusercontent.com/biopython/biopython/master/Tests/Abi/3730.ab1",
        "name": "arabidopsis_sample1",
        "instrument": "ABI 3730",
        "organism": "Arabidopsis thaliana",
    },
    {
        "url": "https://raw.githubusercontent.com/biopython/biopython/master/Tests/Abi/310.ab1",
        "name": "drosophila_sample1",
        "instrument": "ABI 310",
        "organism": "Drosophila melanogaster",
    },
    {
        "url": "https://raw.githubusercontent.com/biopython/biopython/master/Tests/Abi/3100.ab1",
        "name": "zebrafish_sample1",
        "instrument": "ABI 3100",
        "organism": "Danio rerio",
    },
]


async def clear_bucket():
    """Clear all objects from bucket."""
    print("Limpiando bucket existente...")
    objects = minio_storage.list_objects()
    for obj in objects:
        minio_storage.delete_object(obj)
    print(f"  Eliminados {len(objects)} objetos")


async def download_and_store():
    print("=" * 70)
    print("DESCARGA CON CLASIFICACION TAXONOMICA COMPLETA")
    print("=" * 70)
    print()

    # Clear existing data
    await clear_bucket()
    print()

    results = []
    taxonomy_index = {}

    async with httpx.AsyncClient(timeout=60.0, follow_redirects=True) as client:
        for sample in SAMPLE_FILES:
            organism = sample["organism"]
            print(f"Procesando: {sample['name']} ({organism})")

            # Get taxonomy (use known taxonomy for speed)
            if organism in KNOWN_TAXONOMY:
                tax_info = KNOWN_TAXONOMY[organism]
                print(
                    f"  Taxonomia: {tax_info.superkingdom} > ... > "
                    f"{tax_info.genus} > {tax_info.species}"
                )
            else:
                tax_info = await ncbi_taxonomy.get_taxonomy(organism)
                print("  Taxonomia obtenida de NCBI")

            # Build taxonomic path
            tax_path = tax_info.get_path()
            print(f"  Path: {tax_path}")

            try:
                response = await client.get(sample["url"])
                if response.status_code != 200:
                    print(f"  ERROR: HTTP {response.status_code}")
                    continue

                data = response.content

                # Validate AB1
                if data[:4] != b"ABIF":
                    print("  SKIP: No es archivo AB1 valido")
                    continue

                # Upload trace file with taxonomic path
                trace_path = f"raw/sanger/{tax_path}/{sample['name']}.ab1"
                minio_storage.upload_bytes(data, trace_path, "application/octet-stream")

                # Create comprehensive metadata
                metadata = {
                    "traceId": sample["name"],
                    "instrument": sample["instrument"],
                    "sourceUrl": sample["url"],
                    "fileSize": len(data),
                    "downloadedAt": datetime.now(timezone.utc).isoformat(),
                    "taxonomy": tax_info.to_dict(),
                    "storagePath": trace_path,
                }

                metadata_path = f"metadata/{sample['name']}.json"
                minio_storage.upload_bytes(
                    json.dumps(metadata, indent=2).encode(),
                    metadata_path,
                    "application/json",
                )

                # Track by taxonomy for index
                superkingdom = tax_info.superkingdom
                if superkingdom not in taxonomy_index:
                    taxonomy_index[superkingdom] = {}

                species = tax_info.species
                if species not in taxonomy_index[superkingdom]:
                    taxonomy_index[superkingdom][species] = []

                taxonomy_index[superkingdom][species].append({
                    "name": sample["name"],
                    "path": trace_path,
                    "size": len(data),
                })

                results.append({
                    "name": sample["name"],
                    "organism": organism,
                    "trace_path": trace_path,
                    "metadata_path": metadata_path,
                    "size": len(data),
                    "taxonomy_path": tax_path,
                })

                print(f"  OK: {len(data):,} bytes")

            except Exception as e:
                print(f"  ERROR: {e}")

    # Create manifest with taxonomy summary
    manifest = {
        "version": "1.0",
        "createdAt": datetime.now(timezone.utc).isoformat(),
        "totalFiles": len(results),
        "totalBytes": sum(r["size"] for r in results),
        "taxonomySummary": {
            domain: {
                species: len(files)
                for species, files in species_dict.items()
            }
            for domain, species_dict in taxonomy_index.items()
        },
        "files": results,
    }

    minio_storage.upload_bytes(
        json.dumps(manifest, indent=2).encode(),
        "index/manifest.json",
        "application/json",
    )

    # Create taxonomy index
    minio_storage.upload_bytes(
        json.dumps(taxonomy_index, indent=2).encode(),
        "index/taxonomy_index.json",
        "application/json",
    )

    print()
    print("=" * 70)
    print("ESTRUCTURA FINAL EN MINIO")
    print("=" * 70)
    print()
    print(f"Bucket: {minio_storage.bucket}")
    print()

    # Show structure using simple ASCII
    objects = sorted(minio_storage.list_objects())

    current_prefix = ""
    for obj in objects:
        parts = obj.split("/")
        len(parts) - 1

        # Show path hierarchy
        for i, part in enumerate(parts):
            prefix = "  " * i
            if i == len(parts) - 1:
                # File
                print(f"{prefix}+-- {part}")
            elif "/".join(parts[:i+1]) != current_prefix:
                # New directory
                print(f"{prefix}+-- {part}/")
                current_prefix = "/".join(parts[:i+1])

    print()
    print("=" * 70)
    print("RESUMEN TAXONOMICO")
    print("=" * 70)
    print()

    for domain, species_dict in taxonomy_index.items():
        print(f"[{domain}]")
        for species, files in species_dict.items():
            total_size = sum(f["size"] for f in files)
            print(f"  {species}: {len(files)} archivos ({total_size:,} bytes)")
    print()

    print("=" * 70)
    print("ESTADISTICAS")
    print("=" * 70)
    print(f"Total archivos: {len(results)}")
    print(f"Total bytes: {sum(r['size'] for r in results):,}")
    print(f"Dominios: {len(taxonomy_index)}")
    print(f"Especies: {sum(len(sp) for sp in taxonomy_index.values())}")
    print()
    print("Indices creados:")
    print("  - index/manifest.json (lista completa de archivos)")
    print("  - index/taxonomy_index.json (indice por taxonomia)")

    await ncbi_taxonomy.close()


if __name__ == "__main__":
    asyncio.run(download_and_store())
