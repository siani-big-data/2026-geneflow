#!/usr/bin/env python3
"""
Download AB1 trace files from NCBI Trace Archive.

Downloads Sanger sequencing traces (AB1 format) for training heterozygote_training detection.

Usage:
    uv run python scripts/download_ab1_traces.py --count 500
    uv run python scripts/download_ab1_traces.py --organism "Homo sapiens" --count 1000
"""

import argparse
import asyncio
import gzip
from pathlib import Path

import httpx

TRACE_BASE_URL = "https://trace.ncbi.nlm.nih.gov/Traces"


async def download_trace(
    client: httpx.AsyncClient,
    tid: str,
    output_dir: Path,
    semaphore: asyncio.Semaphore,
) -> bool:
    """Download a single trace by TID."""
    async with semaphore:
        try:
            url = f"{TRACE_BASE_URL}/trace.fcgi?cmd=raw&val={tid}"
            response = await client.get(url)

            if response.status_code != 200:
                return False

            data = response.content

            # Decompress if gzipped
            if data[:2] == b'\x1f\x8b':
                data = gzip.decompress(data)

            # Validate AB1 magic number
            if data[:4] != b'ABIF':
                return False

            # Save
            output_path = output_dir / f"{tid}.ab1"
            output_path.write_bytes(data)
            return True

        except Exception:
            return False


async def download_traces(
    output_dir: Path,
    start_tid: int,
    count: int,
    max_concurrent: int = 10,
) -> dict:
    """Download multiple traces."""

    output_dir.mkdir(parents=True, exist_ok=True)

    semaphore = asyncio.Semaphore(max_concurrent)
    stats = {"downloaded": 0, "failed": 0, "total": count}

    async with httpx.AsyncClient(timeout=60.0, follow_redirects=True) as client:
        # Try sequential TIDs - NCBI Trace Archive has billions of traces
        # Human traces are often in specific ranges

        # Known ranges with human Sanger traces:
        # 2274376821+ has some human traces
        # We'll try a batch approach

        batch_size = 100
        current_tid = start_tid
        downloaded = 0
        failed_streak = 0

        while downloaded < count and failed_streak < 500:
            # Create batch of tasks
            tasks = []
            tids = []

            for i in range(min(batch_size, count - downloaded)):
                tid = str(current_tid + i)
                tids.append(tid)
                tasks.append(download_trace(client, tid, output_dir, semaphore))

            results = await asyncio.gather(*tasks)

            batch_success = 0
            for tid, success in zip(tids, results):
                if success:
                    downloaded += 1
                    batch_success += 1
                    failed_streak = 0
                    if downloaded % 50 == 0:
                        print(f"  Downloaded {downloaded}/{count}...")
                else:
                    stats["failed"] += 1
                    failed_streak += 1

            current_tid += batch_size

            if batch_success == 0:
                # Try jumping to a different range
                current_tid += 10000

    stats["downloaded"] = downloaded
    return stats


def main():
    parser = argparse.ArgumentParser(description="Download AB1 traces from NCBI")

    parser.add_argument(
        "--output-dir",
        type=str,
        default="datalake/ab1",
        help="Output directory for AB1 files",
    )
    parser.add_argument(
        "--count",
        type=int,
        default=500,
        help="Number of traces to download",
    )
    parser.add_argument(
        "--start-tid",
        type=int,
        default=2274376821,
        help="Starting TID to search from",
    )
    parser.add_argument(
        "--max-concurrent",
        type=int,
        default=10,
        help="Maximum concurrent downloads",
    )

    args = parser.parse_args()

    print("=" * 70)
    print("NCBI TRACE ARCHIVE DOWNLOADER")
    print("=" * 70)
    print(f"Output: {args.output_dir}")
    print(f"Target: {args.count} AB1 files")
    print(f"Starting TID: {args.start_tid}")
    print()

    output_dir = Path(args.output_dir)

    stats = asyncio.run(download_traces(
        output_dir,
        args.start_tid,
        args.count,
        args.max_concurrent,
    ))

    print()
    print("=" * 70)
    print("DOWNLOAD COMPLETE")
    print("=" * 70)
    print(f"Downloaded: {stats['downloaded']} AB1 files")
    print(f"Failed: {stats['failed']}")
    print(f"Location: {output_dir}")

    # List some files
    ab1_files = list(output_dir.glob("*.ab1"))
    if ab1_files:
        print(f"\nSample files:")
        for f in ab1_files[:5]:
            print(f"  - {f.name} ({f.stat().st_size / 1024:.1f} KB)")


if __name__ == "__main__":
    main()
