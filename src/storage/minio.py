from contextlib import asynccontextmanager
from datetime import datetime, timedelta
from io import BytesIO
from typing import Optional

import structlog
from aiobotocore.session import get_session
from botocore.exceptions import ClientError

from src.storage.storage import StorageProvider

logger = structlog.get_logger()


class MinIOStorageProvider(StorageProvider):
    """
    Storage provider for MinIO / S3-compatible storage.

    Works with: MinIO, AWS S3, DigitalOcean Spaces, Cloudflare R2, etc.
    """

    def __init__(
        self,
        endpoint: str,
        access_key: str,
        secret_key: str,
        bucket: str = "geneflow-datalake",
        secure: bool = True,
    ):
        self.endpoint = endpoint
        self.access_key = access_key
        self.secret_key = secret_key
        self.bucket = bucket
        self.secure = secure
        self._session = get_session()

    @asynccontextmanager
    async def _get_client(self):
        """Get an S3 client from the session."""
        protocol = "https" if self.secure else "http"
        endpoint_url = f"{protocol}://{self.endpoint}"

        async with self._session.create_client(
            "s3",
            endpoint_url=endpoint_url,
            aws_access_key_id=self.access_key,
            aws_secret_access_key=self.secret_key,
        ) as client:
            yield client

    async def append_events_batch(
        self, category: str, date: datetime, event_lines: list[str]
    ) -> None:
        if not event_lines:
            return

        key = self._get_file_path(category, date)
        new_content = "\n".join(event_lines) + "\n"

        async with self._get_client() as client:
            # Try to get existing content
            existing_content = ""
            try:
                response = await client.get_object(Bucket=self.bucket, Key=key)
                async with response["Body"] as stream:
                    existing_content = (await stream.read()).decode("utf-8")
            except ClientError as e:
                if e.response["Error"]["Code"] != "NoSuchKey":
                    raise

            # Append and upload
            full_content = existing_content + new_content
            await client.put_object(
                Bucket=self.bucket,
                Key=key,
                Body=full_content.encode("utf-8"),
                ContentType="application/jsonl",
            )

        logger.debug(
            "minio_events_appended",
            category=category,
            date=date.strftime("%Y-%m-%d"),
            count=len(event_lines),
        )

    async def read_events(self, category: str, date: datetime) -> list[str]:
        key = self._get_file_path(category, date)

        async with self._get_client() as client:
            try:
                response = await client.get_object(Bucket=self.bucket, Key=key)
                async with response["Body"] as stream:
                    content = (await stream.read()).decode("utf-8")
                return [line for line in content.split("\n") if line.strip()]
            except ClientError as e:
                if e.response["Error"]["Code"] == "NoSuchKey":
                    return []
                raise

    async def read_events_range(
        self, category: str, start_date: datetime, end_date: datetime
    ) -> list[str]:
        all_events = []
        current = start_date

        while current <= end_date:
            events = await self.read_events(category, current)
            all_events.extend(events)
            current += timedelta(days=1)

        return all_events

    async def list_categories(self) -> list[str]:
        prefix = "events/"
        categories = set()

        async with self._get_client() as client:
            paginator = client.get_paginator("list_objects_v2")
            async for page in paginator.paginate(
                Bucket=self.bucket, Prefix=prefix, Delimiter="/"
            ):
                for common_prefix in page.get("CommonPrefixes", []):
                    # Extract category from "events/category/"
                    category = common_prefix["Prefix"].replace(prefix, "").rstrip("/")
                    if category:
                        categories.add(category)

        return sorted(categories)

    async def list_dates(self, category: str) -> list[datetime]:
        prefix = f"events/{category}/"
        dates = []

        async with self._get_client() as client:
            paginator = client.get_paginator("list_objects_v2")
            async for page in paginator.paginate(Bucket=self.bucket, Prefix=prefix):
                for obj in page.get("Contents", []):
                    key = obj["Key"]
                    # Extract date from "events/category/YYYY-MM-DD.jsonl"
                    filename = key.split("/")[-1]
                    if filename.endswith(".jsonl"):
                        date_str = filename.replace(".jsonl", "")
                        try:
                            dates.append(datetime.strptime(date_str, "%Y-%m-%d"))
                        except ValueError:
                            pass

        return sorted(dates)

    async def get_stats(self, category: str) -> dict:
        dates = await self.list_dates(category)

        if not dates:
            return {
                "category": category,
                "event_count": 0,
                "first_date": None,
                "last_date": None,
                "file_count": 0,
            }

        total_events = 0
        for date in dates:
            events = await self.read_events(category, date)
            total_events += len(events)

        return {
            "category": category,
            "event_count": total_events,
            "first_date": dates[0].strftime("%Y-%m-%d"),
            "last_date": dates[-1].strftime("%Y-%m-%d"),
            "file_count": len(dates),
        }

    async def health_check(self) -> bool:
        try:
            async with self._get_client() as client:
                await client.head_bucket(Bucket=self.bucket)
            return True
        except Exception as e:
            logger.error("minio_health_check_failed", error=str(e))
            return False

    async def close(self) -> None:
        pass
