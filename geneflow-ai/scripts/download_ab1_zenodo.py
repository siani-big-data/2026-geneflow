#!/usr/bin/env python3
"""
Download AB1 trace files from Zenodo datasets.

Downloads Sanger sequencing traces (AB1 format) from public Zenodo repositories.

Usage:
    uv run python scripts/download_ab1_zenodo.py
    uv run python scripts/download_ab1_zenodo.py --output-dir datalake/ab1
"""

import argparse
import asyncio
import zipfile
from io import BytesIO
from pathlib import Path

import httpx

# Known Zenodo records with AB1 files
ZENODO_RECORDS = [
    {
        "id": "13756482",
        "name": "SETBP1 iPSC Sanger sequencing",
        "description": "70+ AB1 files from gene-edited iPSC clones",
    },
    {
        "id": "15945185",
        "name": "CRISPR Cane Toad Tyrosinase",
        "description": "40+ AB1 files from CRISPR-Cas9 edited cane toad",
    },
    {
        "id": "17172684",
        "name": "16S rRNA Halotolerant Bacteria",
        "description": "6 AB1 files from bacterial isolates",
    },
    {
        "id": "17807424",
        "name": "Schistosoma ITS Genotyping",
        "description": "AB1 chromatograms archive",
    },
    {
        "id": "14001236",
        "name": "Mouse Prex2 Mutation",
        "description": "2 AB1 files for sequence verification",
    },
    {
        "id": "5620073",
        "name": "Gentianinae Plastid Genome",
        "description": "3 AB1 files",
    },
]


async def get_record_files(client: httpx.AsyncClient, record_id: str) -> list[dict]:
    """Get list of files from a Zenodo record."""
    url = f"https://zenodo.org/api/records/{record_id}"
    try:
        response = await client.get(url)
        if response.status_code != 200:
            return []
        data = response.json()
        return data.get("files", [])
    except Exception as e:
        print(f"  Error getting record {record_id}: {e}")
        return []


async def download_file(
    client: httpx.AsyncClient,
    url: str,
    output_path: Path,
    semaphore: asyncio.Semaphore,
) -> bool:
    """Download a single file."""
    async with semaphore:
        try:
            response = await client.get(url, follow_redirects=True)
            if response.status_code != 200:
                return False
            output_path.write_bytes(response.content)
            return True
        except Exception as e:
            print(f"  Error downloading {url}: {e}")
            return False


async def extract_ab1_from_zip(
    client: httpx.AsyncClient,
    url: str,
    output_dir: Path,
    semaphore: asyncio.Semaphore,
) -> int:
    """Download a ZIP and extract AB1 files."""
    async with semaphore:
        try:
            response = await client.get(url, follow_redirects=True)
            if response.status_code != 200:
                return 0

            count = 0
            with zipfile.ZipFile(BytesIO(response.content)) as zf:
                for name in zf.namelist():
                    if name.lower().endswith('.ab1'):
                        # Extract with flat filename
                        filename = Path(name).name
                        output_path = output_dir / filename
                        if not output_path.exists():
                            output_path.write_bytes(zf.read(name))
                            count += 1
            return count
        except Exception as e:
            print(f"  Error extracting from {url}: {e}")
            return 0


async def download_zenodo_record(
    client: httpx.AsyncClient,
    record: dict,
    output_dir: Path,
    semaphore: asyncio.Semaphore,
) -> dict:
    """Download all AB1 files from a Zenodo record."""
    record_id = record["id"]
    print(f"\n[{record['name']}] (zenodo.org/records/{record_id})")
    print(f"  {record['description']}")

    files = await get_record_files(client, record_id)
    if not files:
        print("  No files found or access restricted")
        return {"downloaded": 0, "skipped": 0}

    downloaded = 0
    skipped = 0

    for file_info in files:
        filename = file_info.get("key", "")
        file_url = file_info.get("links", {}).get("self", "")

        if not file_url:
            continue

        # Handle AB1 files directly
        if filename.lower().endswith('.ab1'):
            output_path = output_dir / filename
            if output_path.exists():
                skipped += 1
                continue

            print(f"  Downloading {filename}...")
            if await download_file(client, file_url, output_path, semaphore):
                downloaded += 1

        # Handle ZIP files that may contain AB1
        elif filename.lower().endswith('.zip'):
            print(f"  Extracting from {filename}...")
            count = await extract_ab1_from_zip(client, file_url, output_dir, semaphore)
            downloaded += count
            if count > 0:
                print(f"    Extracted {count} AB1 files")

    print(f"  Result: {downloaded} downloaded, {skipped} skipped")
    return {"downloaded": downloaded, "skipped": skipped}


async def main_async(output_dir: Path, max_concurrent: int = 5):
    """Main async function."""
    output_dir.mkdir(parents=True, exist_ok=True)
    semaphore = asyncio.Semaphore(max_concurrent)

    total_downloaded = 0
    total_skipped = 0

    async with httpx.AsyncClient(timeout=120.0) as client:
        for record in ZENODO_RECORDS:
            result = await download_zenodo_record(client, record, output_dir, semaphore)
            total_downloaded += result["downloaded"]
            total_skipped += result["skipped"]

    return {"downloaded": total_downloaded, "skipped": total_skipped}


def main():
    parser = argparse.ArgumentParser(description="Download AB1 files from Zenodo")
    parser.add_argument(
        "--output-dir",
        type=str,
        default="datalake/ab1",
        help="Output directory for AB1 files",
    )
    parser.add_argument(
        "--max-concurrent",
        type=int,
        default=5,
        help="Maximum concurrent downloads",
    )

    args = parser.parse_args()

    print("=" * 70)
    print("ZENODO AB1 DOWNLOADER")
    print("=" * 70)
    print(f"Output: {args.output_dir}")
    print(f"Sources: {len(ZENODO_RECORDS)} Zenodo records")

    output_dir = Path(args.output_dir)

    # Count existing files
    existing = len(list(output_dir.glob("*.ab1"))) if output_dir.exists() else 0
    print(f"Existing AB1 files: {existing}")

    stats = asyncio.run(main_async(output_dir, args.max_concurrent))

    # Final count
    final_count = len(list(output_dir.glob("*.ab1")))

    print()
    print("=" * 70)
    print("DOWNLOAD COMPLETE")
    print("=" * 70)
    print(f"Downloaded: {stats['downloaded']} new AB1 files")
    print(f"Skipped: {stats['skipped']} (already exist)")
    print(f"Total AB1 files: {final_count}")
    print(f"Location: {output_dir}")


if __name__ == "__main__":
    main()
