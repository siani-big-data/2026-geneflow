"""MinIO/S3 storage provider."""

import structlog
from minio import Minio
from minio.error import S3Error

from src.storage.base import BaseStorageProvider

logger = structlog.get_logger()


class MinIOStorageProvider(BaseStorageProvider):
    """Storage provider for MinIO/S3-compatible object storage."""

    def __init__(
        self,
        endpoint: str,
        access_key: str,
        secret_key: str,
        bucket: str,
        secure: bool = False,
    ):
        """
        Initialize MinIO storage provider.

        Args:
            endpoint: MinIO endpoint (e.g., 'localhost:9000')
            access_key: MinIO access key
            secret_key: MinIO secret key
            bucket: Bucket name to use
            secure: Whether to use HTTPS
        """
        self._client = Minio(
            endpoint,
            access_key=access_key,
            secret_key=secret_key,
            secure=secure,
        )
        self._bucket = bucket
        self._endpoint = endpoint

        logger.info(
            "minio_storage_initialized",
            endpoint=endpoint,
            bucket=bucket,
            secure=secure,
        )

    @property
    def name(self) -> str:
        """Provider name."""
        return f"minio:{self._endpoint}/{self._bucket}"

    async def get(self, path: str) -> bytes:
        """
        Get file content from MinIO.

        Args:
            path: Object path in bucket

        Returns:
            File content as bytes
        """
        try:
            response = self._client.get_object(self._bucket, path)
            data = response.read()
            response.close()
            response.release_conn()

            logger.debug("minio_get_success", path=path, size=len(data))
            return data

        except S3Error as e:
            logger.error("minio_get_failed", path=path, error=str(e))
            raise ValueError(f"Failed to get object from MinIO: {path}") from e

    async def put(self, path: str, data: bytes) -> str:
        """
        Put file content to MinIO.

        Args:
            path: Object path in bucket
            data: File content as bytes

        Returns:
            Final storage path
        """
        from io import BytesIO

        try:
            self._client.put_object(
                self._bucket,
                path,
                BytesIO(data),
                length=len(data),
            )
            logger.debug("minio_put_success", path=path, size=len(data))
            return path

        except S3Error as e:
            logger.error("minio_put_failed", path=path, error=str(e))
            raise ValueError(f"Failed to put object to MinIO: {path}") from e

    async def exists(self, path: str) -> bool:
        """
        Check if file exists in MinIO.

        Args:
            path: Object path in bucket

        Returns:
            True if exists, False otherwise
        """
        try:
            self._client.stat_object(self._bucket, path)
            return True
        except S3Error:
            return False

    async def delete(self, path: str) -> bool:
        """
        Delete file from MinIO.

        Args:
            path: Object path in bucket

        Returns:
            True if deleted, False if not found
        """
        try:
            if not await self.exists(path):
                return False

            self._client.remove_object(self._bucket, path)
            logger.debug("minio_delete_success", path=path)
            return True

        except S3Error as e:
            logger.error("minio_delete_failed", path=path, error=str(e))
            return False
