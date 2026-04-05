#!/usr/bin/env python3
"""
Download real AB1 traces from public GitHub repos.

Usage:
    uv run python scripts/download_traces.py --output datalake/traces
"""

import argparse
import asyncio
import aiohttp
import sys
from pathlib import Path

# Verified public AB1 files
AB1_URLS = [
    # Biopython test files (verified)
    "https://raw.githubusercontent.com/biopython/biopython/master/Tests/Abi/310.ab1",
    "https://raw.githubusercontent.com/biopython/biopython/master/Tests/Abi/3100.ab1",
    "https://raw.githubusercontent.com/biopython/biopython/master/Tests/Abi/3730.ab1",
    "https://raw.githubusercontent.com/biopython/biopython/master/Tests/Abi/A6_1-DB3.ab1",
    "https://raw.githubusercontent.com/biopython/biopython/master/Tests/Abi/empty.ab1",
    "https://raw.githubusercontent.com/biopython/biopython/master/Tests/Abi/no_smpl1.ab1",
    "https://raw.githubusercontent.com/biopython/biopython/master/Tests/Abi/nonascii_encoding.ab1",
    # abifpy test files
    "https://raw.githubusercontent.com/bow/abifpy/master/tests/data/3100.ab1",
    "https://raw.githubusercontent.com/bow/abifpy/master/tests/data/3730.ab1",
    # PyABI test files
    "https://raw.githubusercontent.com/sanger-sequence/pyabi/main/tests/data/sample1.ab1",
    "https://raw.githubusercontent.com/sanger-sequence/pyabi/main/tests/data/sample2.ab1",
    # DNABarcodes examples
    "https://raw.githubusercontent.com/xrobin/DNABarcodeCompat/master/inst/extdata/example.ab1",
]


async def download_file(session: aiohttp.ClientSession, url: str, output_path: Path) -> bool:
    """Download a single AB1 file."""
    try:
        async with session.get(url, timeout=aiohttp.ClientTimeout(total=30), allow_redirects=True) as resp:
            if resp.status == 200:
                content = await resp.read()
                if len(content) > 100 and content[:4] == b"ABIF":
                    output_path.write_bytes(content)
                    return True
            return False
    except Exception as e:
        return False


async def main_async(args):
    output_dir = Path(args.output)
    output_dir.mkdir(parents=True, exist_ok=True)

    print("=" * 70)
    print("AB1 TRACE DOWNLOADER")
    print("=" * 70)
    print(f"Output: {output_dir}")

    downloaded = 0
    skipped = 0
    failed = 0

    async with aiohttp.ClientSession() as session:
        for i, url in enumerate(AB1_URLS):
            filename = url.split("/")[-1]
            # Make unique names if duplicate
            output_path = output_dir / filename
            if output_path.exists():
                # Try with prefix
                repo = url.split("/")[4]
                output_path = output_dir / f"{repo}_{filename}"

            if output_path.exists():
                print(f"[{i+1:2d}/{len(AB1_URLS)}] {output_path.name} - exists")
                skipped += 1
                continue

            print(f"[{i+1:2d}/{len(AB1_URLS)}] {output_path.name}...", end=" ", flush=True)
            success = await download_file(session, url, output_path)

            if success:
                size_kb = output_path.stat().st_size / 1024
                print(f"OK ({size_kb:.1f} KB)")
                downloaded += 1
            else:
                print("FAILED")
                failed += 1

    # Count total
    all_files = list(output_dir.glob("*.ab1"))
    valid_files = [f for f in all_files if f.stat().st_size > 1000]  # Filter tiny/empty

    print("\n" + "=" * 70)
    print(f"Downloaded: {downloaded} | Skipped: {skipped} | Failed: {failed}")
    print(f"Valid AB1 files: {len(valid_files)}")

    if valid_files:
        print("\nFiles:")
        for f in sorted(valid_files):
            size_kb = f.stat().st_size / 1024
            print(f"  {f.name} ({size_kb:.1f} KB)")

    return 0


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=str, default="datalake/traces")
    return asyncio.run(main_async(parser.parse_args()))


if __name__ == "__main__":
    sys.exit(main())
