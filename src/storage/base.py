"""Base storage provider interface."""

from abc import ABC, abstractmethod
from typing import AsyncIterator


class BaseStorageProvider(ABC):
    """Abstract base class for storage providers."""

    @property
    @abstractmethod
    def name(self) -> str:
        """Provider name."""
        pass

    @abstractmethod
    async def get(self, path: str) -> bytes:
        """
        Retrieve data from storage.

        Args:
            path: Storage path or identifier

        Returns:
            Raw bytes data

        Raises:
            FileNotFoundError: If path doesn't exist
            StorageError: For other storage errors
        """
        pass

    @abstractmethod
    async def put(self, path: str, data: bytes) -> str:
        """
        Store data.

        Args:
            path: Storage path or identifier
            data: Raw bytes to store

        Returns:
            Final storage path/URL

        Raises:
            StorageError: If storage fails
        """
        pass

    @abstractmethod
    async def delete(self, path: str) -> bool:
        """
        Delete data from storage.

        Args:
            path: Storage path or identifier

        Returns:
            True if deleted, False if not found
        """
        pass

    @abstractmethod
    async def exists(self, path: str) -> bool:
        """
        Check if path exists.

        Args:
            path: Storage path or identifier

        Returns:
            True if exists
        """
        pass

    async def list(self, prefix: str = "") -> list[str]:
        """
        List paths with given prefix.

        Args:
            prefix: Path prefix to filter

        Returns:
            List of matching paths
        """
        raise NotImplementedError("List not supported by this provider")

    async def get_stream(self, path: str) -> AsyncIterator[bytes]:
        """
        Stream data from storage.

        Args:
            path: Storage path

        Yields:
            Chunks of data
        """
        # Default implementation: get all and yield
        data = await self.get(path)
        yield data

    async def health_check(self) -> bool:
        """Check if storage is accessible."""
        return True


class StorageError(Exception):
    """Base exception for storage errors."""

    def __init__(self, message: str, path: str = "", cause: Exception | None = None):
        super().__init__(message)
        self.path = path
        self.cause = cause
