"""HTTP storage provider for fetching remote files."""

from typing import AsyncIterator
from urllib.parse import urlparse

import httpx

from src.storage.base import BaseStorageProvider, StorageError


class HTTPStorageProvider(BaseStorageProvider):
    """
    Storage provider for HTTP/HTTPS resources.

    Read-only provider for fetching files from URLs.
    """

    def __init__(
        self,
        timeout: float = 30.0,
        max_size: int = 100 * 1024 * 1024,  # 100MB default max
        headers: dict[str, str] | None = None,
    ):
        """
        Initialize HTTP storage.

        Args:
            timeout: Request timeout in seconds
            max_size: Maximum file size to download
            headers: Default headers for requests
        """
        self._timeout = timeout
        self._max_size = max_size
        self._headers = headers or {}

    @property
    def name(self) -> str:
        return "http"

    def _validate_url(self, url: str) -> None:
        """Validate URL format."""
        parsed = urlparse(url)

        if parsed.scheme not in ("http", "https"):
            raise StorageError(f"Invalid URL scheme: {parsed.scheme}", url)

        if not parsed.netloc:
            raise StorageError("Invalid URL: missing host", url)

    async def get(self, path: str) -> bytes:
        """Fetch file from URL."""
        self._validate_url(path)

        try:
            async with httpx.AsyncClient(timeout=self._timeout) as client:
                response = await client.get(path, headers=self._headers)
                response.raise_for_status()

                # Check content length
                content_length = response.headers.get("content-length")
                if content_length and int(content_length) > self._max_size:
                    raise StorageError(
                        f"File too large: {content_length} bytes (max: {self._max_size})",
                        path,
                    )

                return response.content

        except httpx.HTTPStatusError as e:
            if e.response.status_code == 404:
                raise FileNotFoundError(f"URL not found: {path}")
            raise StorageError(f"HTTP error {e.response.status_code}", path, e)
        except httpx.RequestError as e:
            raise StorageError(f"Request failed: {e}", path, e)

    async def put(self, path: str, data: bytes) -> str:
        """
        HTTP storage is read-only.

        Raises:
            StorageError: Always, as HTTP is read-only
        """
        raise StorageError("HTTP storage is read-only", path)

    async def delete(self, path: str) -> bool:
        """
        HTTP storage is read-only.

        Raises:
            StorageError: Always, as HTTP is read-only
        """
        raise StorageError("HTTP storage is read-only", path)

    async def exists(self, path: str) -> bool:
        """Check if URL exists using HEAD request."""
        self._validate_url(path)

        try:
            async with httpx.AsyncClient(timeout=self._timeout) as client:
                response = await client.head(path, headers=self._headers)
                return response.status_code == 200
        except Exception:
            return False

    async def get_stream(self, path: str, chunk_size: int = 8192) -> AsyncIterator[bytes]:
        """Stream file from URL."""
        self._validate_url(path)

        try:
            async with httpx.AsyncClient(timeout=self._timeout) as client:
                async with client.stream("GET", path, headers=self._headers) as response:
                    response.raise_for_status()

                    async for chunk in response.aiter_bytes(chunk_size):
                        yield chunk

        except httpx.HTTPStatusError as e:
            if e.response.status_code == 404:
                raise FileNotFoundError(f"URL not found: {path}")
            raise StorageError(f"HTTP error {e.response.status_code}", path, e)
        except httpx.RequestError as e:
            raise StorageError(f"Request failed: {e}", path, e)

    async def health_check(self) -> bool:
        """HTTP provider is always available."""
        return True

    async def get_metadata(self, path: str) -> dict:
        """Get metadata from URL using HEAD request."""
        self._validate_url(path)

        try:
            async with httpx.AsyncClient(timeout=self._timeout) as client:
                response = await client.head(path, headers=self._headers)
                response.raise_for_status()

                return {
                    "content_type": response.headers.get("content-type"),
                    "content_length": response.headers.get("content-length"),
                    "last_modified": response.headers.get("last-modified"),
                    "etag": response.headers.get("etag"),
                }
        except Exception as e:
            raise StorageError(f"Failed to get metadata: {e}", path, e)
