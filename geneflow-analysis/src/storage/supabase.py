"""Supabase storage provider."""

from typing import AsyncIterator

import httpx

from src.storage.base import BaseStorageProvider, StorageError


class SupabaseStorageProvider(BaseStorageProvider):
    """
    Storage provider for Supabase Storage.

    Uses Supabase Storage API for cloud file storage.
    """

    def __init__(
        self,
        url: str,
        key: str,
        bucket: str = "geneflow-traces",
        timeout: float = 30.0,
    ):
        """
        Initialize Supabase storage.

        Args:
            url: Supabase project URL
            key: Supabase API key (service role or anon)
            bucket: Storage bucket name
            timeout: Request timeout in seconds
        """
        self._url = url.rstrip("/")
        self._key = key
        self._bucket = bucket
        self._timeout = timeout
        self._storage_url = f"{self._url}/storage/v1"

    @property
    def name(self) -> str:
        return "supabase"

    @property
    def bucket(self) -> str:
        return self._bucket

    def _get_headers(self) -> dict[str, str]:
        """Get authorization headers."""
        return {
            "Authorization": f"Bearer {self._key}",
            "apikey": self._key,
        }

    def _get_object_url(self, path: str) -> str:
        """Get full URL for object."""
        clean_path = path.lstrip("/")
        return f"{self._storage_url}/object/{self._bucket}/{clean_path}"

    async def get(self, path: str) -> bytes:
        """Download file from Supabase storage."""
        url = self._get_object_url(path)

        try:
            async with httpx.AsyncClient(timeout=self._timeout) as client:
                response = await client.get(url, headers=self._get_headers())

                if response.status_code == 404:
                    raise FileNotFoundError(f"File not found: {path}")

                response.raise_for_status()
                return response.content

        except httpx.HTTPStatusError as e:
            raise StorageError(
                f"Supabase error {e.response.status_code}: {e.response.text}",
                path,
                e,
            )
        except httpx.RequestError as e:
            raise StorageError(f"Request failed: {e}", path, e)

    async def put(
        self, path: str, data: bytes, content_type: str = "application/octet-stream"
    ) -> str:
        """Upload file to Supabase storage."""
        url = self._get_object_url(path)

        headers = self._get_headers()
        headers["Content-Type"] = content_type

        try:
            async with httpx.AsyncClient(timeout=self._timeout) as client:
                response = await client.post(url, content=data, headers=headers)

                if response.status_code == 400 and "already exists" in response.text.lower():
                    response = await client.put(url, content=data, headers=headers)

                response.raise_for_status()

                return f"{self._storage_url}/object/public/{self._bucket}/{path.lstrip('/')}"

        except httpx.HTTPStatusError as e:
            raise StorageError(
                f"Supabase error {e.response.status_code}: {e.response.text}",
                path,
                e,
            )
        except httpx.RequestError as e:
            raise StorageError(f"Upload failed: {e}", path, e)

    async def delete(self, path: str) -> bool:
        """Delete file from Supabase storage."""
        url = self._get_object_url(path)

        try:
            async with httpx.AsyncClient(timeout=self._timeout) as client:
                response = await client.delete(url, headers=self._get_headers())

                if response.status_code == 404:
                    return False

                response.raise_for_status()
                return True

        except httpx.HTTPStatusError as e:
            raise StorageError(
                f"Supabase error {e.response.status_code}: {e.response.text}",
                path,
                e,
            )
        except httpx.RequestError as e:
            raise StorageError(f"Delete failed: {e}", path, e)

    async def exists(self, path: str) -> bool:
        """Check if file exists in Supabase storage."""
        url = self._get_object_url(path)

        try:
            async with httpx.AsyncClient(timeout=self._timeout) as client:
                response = await client.head(url, headers=self._get_headers())
                return response.status_code == 200
        except Exception:
            return False

    async def list(self, prefix: str = "") -> list[str]:
        """List files in bucket with prefix."""
        url = f"{self._storage_url}/object/list/{self._bucket}"

        body = {
            "prefix": prefix,
            "limit": 1000,
        }

        try:
            async with httpx.AsyncClient(timeout=self._timeout) as client:
                response = await client.post(
                    url,
                    json=body,
                    headers=self._get_headers(),
                )
                response.raise_for_status()

                items = response.json()
                return [item["name"] for item in items if item.get("name")]

        except httpx.HTTPStatusError as e:
            raise StorageError(
                f"Supabase error {e.response.status_code}: {e.response.text}",
                prefix,
                e,
            )
        except httpx.RequestError as e:
            raise StorageError(f"List failed: {e}", prefix, e)

    async def get_stream(self, path: str, chunk_size: int = 8192) -> AsyncIterator[bytes]:
        """Stream file from Supabase storage."""
        url = self._get_object_url(path)

        try:
            async with httpx.AsyncClient(timeout=self._timeout) as client:
                async with client.stream("GET", url, headers=self._get_headers()) as response:
                    if response.status_code == 404:
                        raise FileNotFoundError(f"File not found: {path}")

                    response.raise_for_status()

                    async for chunk in response.aiter_bytes(chunk_size):
                        yield chunk

        except httpx.HTTPStatusError as e:
            raise StorageError(
                f"Supabase error {e.response.status_code}",
                path,
                e,
            )
        except httpx.RequestError as e:
            raise StorageError(f"Stream failed: {e}", path, e)

    async def health_check(self) -> bool:
        """Check if Supabase storage is accessible."""
        url = f"{self._storage_url}/bucket/{self._bucket}"

        try:
            async with httpx.AsyncClient(timeout=self._timeout) as client:
                response = await client.get(url, headers=self._get_headers())
                return response.status_code == 200
        except Exception:
            return False

    async def get_public_url(self, path: str) -> str:
        """Get public URL for a file."""
        clean_path = path.lstrip("/")
        return f"{self._storage_url}/object/public/{self._bucket}/{clean_path}"

    async def get_signed_url(self, path: str, expires_in: int = 3600) -> str:
        """
        Get a signed URL for temporary access.

        Args:
            path: File path
            expires_in: Expiration time in seconds

        Returns:
            Signed URL
        """
        url = f"{self._storage_url}/object/sign/{self._bucket}/{path.lstrip('/')}"

        body = {"expiresIn": expires_in}

        try:
            async with httpx.AsyncClient(timeout=self._timeout) as client:
                response = await client.post(
                    url,
                    json=body,
                    headers=self._get_headers(),
                )
                response.raise_for_status()

                data = response.json()
                return f"{self._url}{data['signedURL']}"

        except httpx.HTTPStatusError as e:
            raise StorageError(
                f"Failed to create signed URL: {e.response.text}",
                path,
                e,
            )
        except httpx.RequestError as e:
            raise StorageError(f"Request failed: {e}", path, e)
