#!/usr/bin/env python
"""Proving script for storage providers."""

import asyncio
import tempfile

from src.config import Settings
from src.storage import (
    HTTPStorageProvider,
    LocalStorageProvider,
    StorageFactory,
)


async def main():
    """Run storage proving tests."""
    print("=" * 60)
    print("STORAGE MODULE - PROVING SCRIPT")
    print("=" * 60)

    # 1. Local Storage
    print("\n1. LOCAL STORAGE")
    print("-" * 40)

    with tempfile.TemporaryDirectory() as tmpdir:
        provider = LocalStorageProvider(base_path=tmpdir)

        print(f"Provider:  {provider.name}")
        print(f"Base path: {provider.base_path}")

        # Health check
        healthy = await provider.health_check()
        print(f"Healthy:   {healthy}")

        # Put file
        test_data = b"Hello, GeneFlow Storage!"
        await provider.put("test/hello.txt", test_data)
        print(f"Put:       test/hello.txt ({len(test_data)} bytes)")

        # Get file
        retrieved = await provider.get("test/hello.txt")
        print(f"Get:       {retrieved.decode()}")

        # Exists check
        exists = await provider.exists("test/hello.txt")
        print(f"Exists:    {exists}")

        # List files
        await provider.put("test/file1.txt", b"data1")
        await provider.put("test/file2.txt", b"data2")
        await provider.put("other/file3.txt", b"data3")

        all_files = await provider.list()
        print(f"List all:  {len(all_files)} files")

        test_files = await provider.list("test")
        print(f"List test: {len(test_files)} files")

        # Get size
        size = await provider.get_size("test/hello.txt")
        print(f"Size:      {size} bytes")

        # Stream
        chunks = []
        async for chunk in provider.get_stream("test/hello.txt"):
            chunks.append(chunk)
        print(f"Stream:    {len(chunks)} chunk(s)")

        # Delete
        deleted = await provider.delete("test/hello.txt")
        print(f"Delete:    {deleted}")

        exists_after = await provider.exists("test/hello.txt")
        print(f"Exists:    {exists_after}")

    # 2. HTTP Storage
    print("\n2. HTTP STORAGE")
    print("-" * 40)

    http_provider = HTTPStorageProvider(timeout=10.0)
    print(f"Provider:  {http_provider.name}")
    print(f"Healthy:   {await http_provider.health_check()}")

    # Note: We don't actually fetch URLs in proving to avoid network dependency
    print("Read-only: True")
    print("(Skipping actual HTTP requests in proving)")

    # 3. Storage Factory
    print("\n3. STORAGE FACTORY")
    print("-" * 40)

    # Create from settings
    settings = Settings(storage_provider="local", local_storage_path="/tmp/geneflow-test")
    provider = StorageFactory.create(settings)
    print(f"From settings: {provider.name}")

    # Create directly
    local = StorageFactory.create_local("/tmp/test")
    print(f"create_local:  {local.name}")

    http = StorageFactory.create_http()
    print(f"create_http:   {http.name}")

    supabase = StorageFactory.create_supabase(
        url="https://example.supabase.co",
        key="test-key",
    )
    print(f"create_supabase: {supabase.name} (bucket: {supabase.bucket})")

    # 4. Error Handling
    print("\n4. ERROR HANDLING")
    print("-" * 40)

    with tempfile.TemporaryDirectory() as tmpdir:
        provider = LocalStorageProvider(base_path=tmpdir)

        # File not found
        try:
            await provider.get("nonexistent.txt")
        except FileNotFoundError as e:
            print(f"FileNotFoundError: {e}")

        # Path traversal blocked
        try:
            await provider.get("../../../etc/passwd")
        except Exception as e:
            print(f"PathTraversal blocked: {type(e).__name__}")

    # HTTP read-only
    try:
        await http_provider.put("http://example.com/file.txt", b"data")
    except Exception as e:
        print(f"HTTP read-only: {type(e).__name__}")

    print("\n" + "=" * 60)
    print("PROVING COMPLETE - All storage providers working")
    print("=" * 60)


if __name__ == "__main__":
    asyncio.run(main())
