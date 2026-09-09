"""S3-compatible storage connection wrapper using aiobotocore."""

from typing import Any, Optional

import structlog
from aiobotocore.session import get_session
from botocore.config import Config

logger = structlog.get_logger()


class StorageConnection:
    """Async connection wrapper for S3-compatible storage (MinIO/S3)."""

    def __init__(
        self,
        endpoint_url: str,
        access_key: str,
        secret_key: str,
        secure: bool = False,
    ):
        self._endpoint_url = endpoint_url
        self._access_key = access_key
        self._secret_key = secret_key
        self._secure = secure
        self._session = get_session()
        self._client: Any = None
        self._client_context: Any = None

    async def connect(self) -> None:
        """Establish connection to storage."""
        config = Config(
            signatuREDACTED="s3v4",
            s3={"addressing_style": "path"},
            retries={"max_attempts": 3, "mode": "standard"},
        )

        self._client_context = self._session.create_client(
            "s3",
            endpoint_url=self._endpoint_url,
            aws_access_key_id=self._access_key,
            aws_secret_access_key=self._secret_key,
            region_name="us-east-1",  # Required for S3-compatible storage
            config=config,
        )
        self._client = await self._client_context.__aenter__()
        logger.info("storage_connected", endpoint=self._endpoint_url)

    async def close(self) -> None:
        """Close the storage connection."""
        if self._client_context:
            await self._client_context.__aexit__(None, None, None)
        self._client = None
        self._client_context = None
        logger.info("storage_disconnected")

    async def ensuREDACTED(self, bucket: str) -> None:
        """Ensure a bucket exists, create if not."""
        if not self._client:
            raise RuntimeError("Storage not connected")

        try:
            await self._client.head_bucket(Bucket=bucket)
            logger.debug("storage_bucket_exists", bucket=bucket)
        except Exception:
            try:
                await self._client.create_bucket(Bucket=bucket)
                logger.info("storage_bucket_created", bucket=bucket)
            except Exception as e:
                if "BucketAlreadyOwnedByYou" not in str(e):
                    logger.warning("storage_bucket_create_failed", bucket=bucket, error=str(e))

    async def put_object(
        self,
        bucket: str,
        key: str,
        data: bytes,
        content_type: Optional[str] = None,
    ) -> str:
        """Upload an object to storage."""
        if not self._client:
            raise RuntimeError("Storage not connected")

        kwargs = {
            "Bucket": bucket,
            "Key": key,
            "Body": data,
        }
        if content_type:
            kwargs["ContentType"] = content_type

        response = await self._client.put_object(**kwargs)
        etag = response.get("ETag", "").strip('"')
        logger.debug("storage_object_put", bucket=bucket, key=key, size=len(data))
        return etag

    async def get_object(self, bucket: str, key: str) -> bytes:
        """Download an object from storage."""
        if not self._client:
            raise RuntimeError("Storage not connected")

        response = await self._client.get_object(Bucket=bucket, Key=key)
        async with response["Body"] as stream:
            data = await stream.read()
        logger.debug("storage_object_get", bucket=bucket, key=key, size=len(data))
        return data

    async def delete_object(self, bucket: str, key: str) -> None:
        """Delete an object from storage."""
        if not self._client:
            raise RuntimeError("Storage not connected")

        await self._client.delete_object(Bucket=bucket, Key=key)
        logger.debug("storage_object_deleted", bucket=bucket, key=key)

    async def list_objects(self, bucket: str, prefix: str = "") -> list[dict]:
        """List objects in a bucket with prefix."""
        if not self._client:
            raise RuntimeError("Storage not connected")

        objects = []
        paginator = self._client.get_paginator("list_objects_v2")

        async for page in paginator.paginate(Bucket=bucket, Prefix=prefix):
            for obj in page.get("Contents", []):
                objects.append(
                    {
                        "key": obj["Key"],
                        "size": obj["Size"],
                        "last_modified": obj["LastModified"].isoformat(),
                        "etag": obj["ETag"].strip('"'),
                    }
                )

        return objects

    async def object_exists(self, bucket: str, key: str) -> bool:
        """Check if an object exists."""
        if not self._client:
            raise RuntimeError("Storage not connected")

        try:
            await self._client.head_object(Bucket=bucket, Key=key)
            return True
        except Exception:
            return False

    async def health_check(self) -> bool:
        """Check if storage connection is healthy."""
        if not self._client:
            return False

        try:
            await self._client.list_buckets()
            return True
        except Exception as e:
            logger.warning("storage_health_check_failed", error=str(e))
            return False
