"""Local filesystem storage provider."""

import os
from pathlib import Path
from typing import AsyncIterator

import aiofiles
import aiofiles.os

from src.storage.base import BaseStorageProvider, StorageError


class LocalStorageProvider(BaseStorageProvider):
    """
    Storage provider for local filesystem.

    Stores files in a configurable base directory.
    """

    def __init__(self, base_path: str = "./data"):
        """
        Initialize local storage.

        Args:
            base_path: Base directory for storage
        """
        self._base_path = Path(base_path)

    @property
    def name(self) -> str:
        return "local"

    @property
    def base_path(self) -> Path:
        return self._base_path

    def _resolve_path(self, path: str) -> Path:
        """Resolve relative path to absolute path."""
        # Normalize and prevent directory traversal
        clean_path = Path(path).as_posix().lstrip("/")
        full_path = self._base_path / clean_path

        # Security check: ensure path is within base
        try:
            full_path.resolve().relative_to(self._base_path.resolve())
        except ValueError:
            raise StorageError(f"Invalid path: {path}", path)

        return full_path

    async def get(self, path: str) -> bytes:
        """Retrieve file from local storage."""
        full_path = self._resolve_path(path)

        if not full_path.exists():
            raise FileNotFoundError(f"File not found: {path}")

        try:
            async with aiofiles.open(full_path, "rb") as f:
                return await f.read()
        except Exception as e:
            raise StorageError(f"Failed to read file: {e}", path, e)

    async def put(self, path: str, data: bytes) -> str:
        """Store file to local storage."""
        full_path = self._resolve_path(path)

        try:
            # Create parent directories
            full_path.parent.mkdir(parents=True, exist_ok=True)

            async with aiofiles.open(full_path, "wb") as f:
                await f.write(data)

            return str(full_path)
        except Exception as e:
            raise StorageError(f"Failed to write file: {e}", path, e)

    async def delete(self, path: str) -> bool:
        """Delete file from local storage."""
        full_path = self._resolve_path(path)

        if not full_path.exists():
            return False

        try:
            await aiofiles.os.remove(full_path)
            return True
        except Exception as e:
            raise StorageError(f"Failed to delete file: {e}", path, e)

    async def exists(self, path: str) -> bool:
        """Check if file exists."""
        full_path = self._resolve_path(path)
        return full_path.exists()

    async def list(self, prefix: str = "") -> list[str]:
        """List files with given prefix."""
        if prefix:
            search_path = self._resolve_path(prefix)
        else:
            search_path = self._base_path

        if not search_path.exists():
            return []

        results = []

        if search_path.is_file():
            # Prefix is a file
            rel_path = search_path.relative_to(self._base_path)
            results.append(str(rel_path))
        else:
            # Prefix is a directory
            for item in search_path.rglob("*"):
                if item.is_file():
                    rel_path = item.relative_to(self._base_path)
                    results.append(str(rel_path))

        return sorted(results)

    async def get_stream(self, path: str, chunk_size: int = 8192) -> AsyncIterator[bytes]:
        """Stream file in chunks."""
        full_path = self._resolve_path(path)

        if not full_path.exists():
            raise FileNotFoundError(f"File not found: {path}")

        async with aiofiles.open(full_path, "rb") as f:
            while True:
                chunk = await f.read(chunk_size)
                if not chunk:
                    break
                yield chunk

    async def health_check(self) -> bool:
        """Check if base directory is accessible."""
        try:
            self._base_path.mkdir(parents=True, exist_ok=True)
            return self._base_path.exists() and os.access(self._base_path, os.W_OK)
        except Exception:
            return False

    async def get_size(self, path: str) -> int:
        """Get file size in bytes."""
        full_path = self._resolve_path(path)

        if not full_path.exists():
            raise FileNotFoundError(f"File not found: {path}")

        return full_path.stat().st_size
