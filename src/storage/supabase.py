from datetime import datetime, timedelta
from typing import Optional

import httpx
import structlog

from src.storage.storage import StorageProvider

logger = structlog.get_logger()


class SupabaseStorageProvider(StorageProvider):
    """
    Storage provider for Supabase Storage.

    Uses Supabase's REST API for file operations.
    """

    def __init__(
        self,
        url: str,
        key: str,
        bucket: str = "geneflow-datalake",
    ):
        self.url = url.rstrip("/")
        self.key = key
        self.bucket = bucket
        self._client: Optional[httpx.AsyncClient] = None

    async def _get_client(self) -> httpx.AsyncClient:
        """Get or create HTTP client."""
        if self._client is None:
            self._client = httpx.AsyncClient(
                base_url=f"{self.url}/storage/v1",
                headers={
                    "Authorization": f"Bearer {self.key}",
                    "apikey": self.key,
                },
                timeout=30.0,
            )
        return self._client

    async def append_events_batch(
        self, category: str, date: datetime, event_lines: list[str]
    ) -> None:
        if not event_lines:
            return

        path = self._get_file_path(category, date)
        new_content = "\n".join(event_lines) + "\n"

        client = await self._get_client()

        # Try to get existing content
        existing_content = ""
        response = await client.get(f"/object/{self.bucket}/{path}")
        if response.status_code == 200:
            existing_content = response.text

        # Upload combined content (upsert)
        full_content = existing_content + new_content
        response = await client.post(
            f"/object/{self.bucket}/{path}",
            content=full_content.encode("utf-8"),
            headers={
                "Content-Type": "application/jsonl",
                "x-upsert": "true",
            },
        )
        response.raise_for_status()

        logger.debug(
            "supabase_events_appended",
            category=category,
            date=date.strftime("%Y-%m-%d"),
            count=len(event_lines),
        )

    async def read_events(self, category: str, date: datetime) -> list[str]:
        path = self._get_file_path(category, date)
        client = await self._get_client()

        response = await client.get(f"/object/{self.bucket}/{path}")

        if response.status_code == 404:
            return []

        response.raise_for_status()
        content = response.text
        return [line for line in content.split("\n") if line.strip()]

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
        client = await self._get_client()
        categories = set()

        # List folders in events/
        response = await client.post(
            f"/object/list/{self.bucket}",
            json={"prefix": "events/", "limit": 1000},
        )

        if response.status_code == 404:
            return []

        response.raise_for_status()
        items = response.json()

        for item in items:
            name = item.get("name", "")
            # Folders end with /
            if name and item.get("id") is None:
                categories.add(name.rstrip("/"))

        return sorted(categories)

    async def list_dates(self, category: str) -> list[datetime]:
        client = await self._get_client()
        dates = []

        response = await client.post(
            f"/object/list/{self.bucket}",
            json={"prefix": f"events/{category}/", "limit": 1000},
        )

        if response.status_code == 404:
            return []

        response.raise_for_status()
        items = response.json()

        for item in items:
            name = item.get("name", "")
            if name.endswith(".jsonl"):
                date_str = name.replace(".jsonl", "")
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
            client = await self._get_client()
            response = await client.get(f"/bucket/{self.bucket}")
            return response.status_code == 200
        except Exception as e:
            logger.error("supabase_health_check_failed", error=str(e))
            return False

    async def close(self) -> None:
        if self._client:
            await self._client.aclose()
            self._client = None
