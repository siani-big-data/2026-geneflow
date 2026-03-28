"""Tests for storage providers."""

import pytest
import tempfile
import os
from pathlib import Path

from src.storage import (
    LocalStorageProvider,
    HTTPStorageProvider,
    SupabaseStorageProvider,
    StorageFactory,
)
from src.storage.base import StorageError
from src.config import Settings


class TestLocalStorageProvider:
    """Tests for LocalStorageProvider."""

    @pytest.fixture
    def temp_dir(self):
        """Create temporary directory for tests."""
        with tempfile.TemporaryDirectory() as tmpdir:
            yield tmpdir

    @pytest.fixture
    def provider(self, temp_dir):
        """Create provider with temp directory."""
        return LocalStorageProvider(base_path=temp_dir)

    @pytest.mark.asyncio
    async def test_put_and_get(self, provider):
        """Test storing and retrieving data."""
        data = b"Hello, World!"
        path = "test/file.txt"

        await provider.put(path, data)
        result = await provider.get(path)

        assert result == data

    @pytest.mark.asyncio
    async def test_get_not_found(self, provider):
        """Test getting non-existent file."""
        with pytest.raises(FileNotFoundError):
            await provider.get("nonexistent.txt")

    @pytest.mark.asyncio
    async def test_exists(self, provider):
        """Test existence check."""
        path = "test/exists.txt"

        assert not await provider.exists(path)

        await provider.put(path, b"data")

        assert await provider.exists(path)

    @pytest.mark.asyncio
    async def test_delete(self, provider):
        """Test file deletion."""
        path = "test/delete.txt"
        await provider.put(path, b"data")

        assert await provider.exists(path)

        result = await provider.delete(path)

        assert result is True
        assert not await provider.exists(path)

    @pytest.mark.asyncio
    async def test_delete_not_found(self, provider):
        """Test deleting non-existent file."""
        result = await provider.delete("nonexistent.txt")
        assert result is False

    @pytest.mark.asyncio
    async def test_list(self, provider):
        """Test listing files."""
        await provider.put("dir1/file1.txt", b"data1")
        await provider.put("dir1/file2.txt", b"data2")
        await provider.put("dir2/file3.txt", b"data3")

        # List all
        all_files = await provider.list()
        assert len(all_files) == 3

        # List with prefix
        dir1_files = await provider.list("dir1")
        assert len(dir1_files) == 2

    @pytest.mark.asyncio
    async def test_creates_parent_directories(self, provider):
        """Test that put creates parent directories."""
        path = "deep/nested/path/file.txt"
        await provider.put(path, b"data")

        assert await provider.exists(path)

    @pytest.mark.asyncio
    async def test_path_traversal_blocked(self, provider):
        """Test that path traversal is blocked."""
        with pytest.raises(StorageError):
            await provider.get("../../../etc/passwd")

    @pytest.mark.asyncio
    async def test_health_check(self, provider):
        """Test health check."""
        result = await provider.health_check()
        assert result is True

    @pytest.mark.asyncio
    async def test_get_stream(self, provider):
        """Test streaming file."""
        data = b"Hello, streaming world!"
        path = "stream.txt"
        await provider.put(path, data)

        chunks = []
        async for chunk in provider.get_stream(path):
            chunks.append(chunk)

        assert b"".join(chunks) == data

    @pytest.mark.asyncio
    async def test_get_size(self, provider):
        """Test getting file size."""
        data = b"12345"
        path = "size.txt"
        await provider.put(path, data)

        size = await provider.get_size(path)
        assert size == 5


class TestHTTPStorageProvider:
    """Tests for HTTPStorageProvider."""

    @pytest.fixture
    def provider(self):
        return HTTPStorageProvider()

    def test_name(self, provider):
        """Test provider name."""
        assert provider.name == "http"

    @pytest.mark.asyncio
    async def test_invalid_url_scheme(self, provider):
        """Test that invalid URL schemes are rejected."""
        with pytest.raises(StorageError):
            await provider.get("ftp://example.com/file.txt")

    @pytest.mark.asyncio
    async def test_invalid_url_no_host(self, provider):
        """Test that URLs without host are rejected."""
        with pytest.raises(StorageError):
            await provider.get("http:///path")

    @pytest.mark.asyncio
    async def test_put_raises(self, provider):
        """Test that put raises (read-only)."""
        with pytest.raises(StorageError, match="read-only"):
            await provider.put("http://example.com/file.txt", b"data")

    @pytest.mark.asyncio
    async def test_delete_raises(self, provider):
        """Test that delete raises (read-only)."""
        with pytest.raises(StorageError, match="read-only"):
            await provider.delete("http://example.com/file.txt")

    @pytest.mark.asyncio
    async def test_health_check(self, provider):
        """Test health check always returns True."""
        result = await provider.health_check()
        assert result is True


class TestSupabaseStorageProvider:
    """Tests for SupabaseStorageProvider."""

    @pytest.fixture
    def provider(self):
        return SupabaseStorageProvider(
            url="https://test.supabase.co",
            key="test-key",
            bucket="test-bucket",
        )

    def test_name(self, provider):
        """Test provider name."""
        assert provider.name == "supabase"

    def test_bucket(self, provider):
        """Test bucket property."""
        assert provider.bucket == "test-bucket"

    @pytest.mark.asyncio
    async def test_get_public_url(self, provider):
        """Test public URL generation."""
        url = await provider.get_public_url("path/to/file.txt")

        assert "test.supabase.co" in url
        assert "test-bucket" in url
        assert "file.txt" in url


class TestStorageFactory:
    """Tests for StorageFactory."""

    def test_create_local(self):
        """Test creating local storage."""
        settings = Settings(storage_provider="local", local_storage_path="/tmp/test")
        provider = StorageFactory.create(settings)

        assert isinstance(provider, LocalStorageProvider)

    def test_create_http(self):
        """Test creating HTTP storage."""
        settings = Settings(storage_provider="http")
        provider = StorageFactory.create(settings)

        assert isinstance(provider, HTTPStorageProvider)

    def test_create_supabase_requires_credentials(self):
        """Test that Supabase requires credentials."""
        settings = Settings(
            storage_provider="supabase",
            supabase_url="",
            supabase_key="",
        )

        with pytest.raises(ValueError, match="requires"):
            StorageFactory.create(settings)

    def test_create_supabase_with_credentials(self):
        """Test creating Supabase storage with credentials."""
        settings = Settings(
            storage_provider="supabase",
            supabase_url="https://test.supabase.co",
            supabase_key="test-key",
            supabase_bucket="test-bucket",
        )
        provider = StorageFactory.create(settings)

        assert isinstance(provider, SupabaseStorageProvider)

    def test_create_unknown_raises(self):
        """Test that unknown provider raises via pydantic validation."""
        from pydantic import ValidationError

        with pytest.raises(ValidationError):
            Settings(storage_provider="unknown")

    def test_create_local_helper(self):
        """Test create_local helper."""
        provider = StorageFactory.create_local("/tmp/test")
        assert isinstance(provider, LocalStorageProvider)

    def test_create_http_helper(self):
        """Test create_http helper."""
        provider = StorageFactory.create_http(timeout=60.0)
        assert isinstance(provider, HTTPStorageProvider)

    def test_create_supabase_helper(self):
        """Test create_supabase helper."""
        provider = StorageFactory.create_supabase(
            url="https://test.supabase.co",
            key="test-key",
        )
        assert isinstance(provider, SupabaseStorageProvider)
