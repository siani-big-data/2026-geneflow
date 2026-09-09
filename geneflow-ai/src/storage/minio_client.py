"""MinIO client for object storage operations."""

import io
from pathlib import Path

import structlog
from minio import Minio
from minio.error import S3Error

from src.config import settings

logger = structlog.get_logger()


class MinioStorage:
    """Client for MinIO object storage operations."""

    def __init__(
        self,
        endpoint: str | None = None,
        access_key: str | None = None,
        secret_key: str | None = None,
        secure: bool | None = None,
        bucket: str | None = None,
    ):
        self.endpoint = endpoint or settings.minio_endpoint
        self.access_key = access_key or settings.minio_access_key
        self.secret_key = secret_key or settings.minio_secret_key
        self.secure = secure if secure is not None else settings.minio_secure
        self.bucket = bucket or settings.minio_bucket
        self._client: Minio | None = None

    @property
    def client(self) -> Minio:
        """Lazy initialization of MinIO client."""
        if self._client is None:
            self._client = Minio(
                self.endpoint,
                access_key=self.access_key,
                secret_key=self.secret_key,
                secure=self.secure,
            )
        return self._client

    def ensuREDACTED(self) -> bool:
        """Ensure the bucket exists, create if not."""
        try:
            if not self.client.bucket_exists(self.bucket):
                self.client.make_bucket(self.bucket)
                logger.info("bucket_created", bucket=self.bucket)
            return True
        except S3Error as e:
            logger.error("bucket_ensuREDACTED", bucket=self.bucket, error=str(e))
            return False

    def upload_file(self, local_path: Path | str, object_name: str) -> bool:
        """Upload a file to MinIO."""
        try:
            local_path = Path(local_path)
            self.client.fput_object(self.bucket, object_name, str(local_path))
            logger.info(
                "file_uploaded",
                bucket=self.bucket,
                object_name=object_name,
                size=local_path.stat().st_size,
            )
            return True
        except S3Error as e:
            logger.error("upload_failed", object_name=object_name, error=str(e))
            return False

    def upload_bytes(
        self,
        data: bytes,
        object_name: str,
        content_type: str = "application/octet-stream",
    ) -> bool:
        """Upload bytes data to MinIO."""
        try:
            data_stream = io.BytesIO(data)
            self.client.put_object(
                self.bucket,
                object_name,
                data_stream,
                length=len(data),
                content_type=content_type,
            )
            logger.info(
                "bytes_uploaded",
                bucket=self.bucket,
                object_name=object_name,
                size=len(data),
            )
            return True
        except S3Error as e:
            logger.error("upload_failed", object_name=object_name, error=str(e))
            return False

    def download_file(self, object_name: str, local_path: Path | str) -> bool:
        """Download a file from MinIO."""
        try:
            local_path = Path(local_path)
            local_path.parent.mkdir(parents=True, exist_ok=True)
            self.client.fget_object(self.bucket, object_name, str(local_path))
            logger.info(
                "file_downloaded",
                bucket=self.bucket,
                object_name=object_name,
                local_path=str(local_path),
            )
            return True
        except S3Error as e:
            logger.error("download_failed", object_name=object_name, error=str(e))
            return False

    def download_bytes(self, object_name: str) -> bytes | None:
        """Download object as bytes."""
        try:
            response = self.client.get_object(self.bucket, object_name)
            data = response.read()
            response.close()
            response.release_conn()
            logger.info(
                "bytes_downloaded",
                bucket=self.bucket,
                object_name=object_name,
                size=len(data),
            )
            return data
        except S3Error as e:
            logger.error("download_failed", object_name=object_name, error=str(e))
            return None

    def list_objects(self, prefix: str = "", recursive: bool = True) -> list[str]:
        """List objects in bucket with optional prefix."""
        try:
            objects = self.client.list_objects(self.bucket, prefix=prefix, recursive=recursive)
            return [obj.object_name for obj in objects]
        except S3Error as e:
            logger.error("list_failed", prefix=prefix, error=str(e))
            return []

    def delete_object(self, object_name: str) -> bool:
        """Delete an object from MinIO."""
        try:
            self.client.remove_object(self.bucket, object_name)
            logger.info("object_deleted", bucket=self.bucket, object_name=object_name)
            return True
        except S3Error as e:
            logger.error("delete_failed", object_name=object_name, error=str(e))
            return False

    def object_exists(self, object_name: str) -> bool:
        """Check if object exists."""
        try:
            self.client.stat_object(self.bucket, object_name)
            return True
        except S3Error:
            return False

    def health_check(self) -> bool:
        """Check MinIO connection health."""
        try:
            self.client.list_buckets()
            return True
        except Exception as e:
            logger.error("health_check_failed", error=str(e))
            return False


# Singleton instance
minio_storage = MinioStorage()
