from datetime import datetime, timedelta
from pathlib import Path

import aiofiles

from src.storage.storage import StorageProvider


class LocalStorageProvider(StorageProvider):
    """Storage usando sistema de archivos local."""

    def __init__(self, base_path: str = "./data/datalake"):
        self.base_path = Path(base_path)

    async def append_events_batch(
        self, category: str, date: datetime, event_lines: list[str]
    ) -> None:
        if not event_lines:
            return

        file_path = self._get_full_path(category, date)
        file_path.parent.mkdir(parents=True, exist_ok=True)

        content = "\n".join(event_lines) + "\n"
        async with aiofiles.open(file_path, mode="a", encoding="utf-8") as f:
            await f.write(content)

    async def read_events(self, category: str, date: datetime) -> list[str]:
        file_path = self._get_full_path(category, date)

        if not file_path.exists():
            return []

        async with aiofiles.open(file_path, mode="r", encoding="utf-8") as f:
            content = await f.read()

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
        events_path = self.base_path / "events"
        if not events_path.exists():
            return []

        return sorted([p.name for p in events_path.iterdir() if p.is_dir()])

    async def list_dates(self, category: str) -> list[datetime]:
        category_path = self.base_path / "events" / category
        if not category_path.exists():
            return []

        dates = []
        for file_path in category_path.glob("*.jsonl"):
            try:
                dates.append(datetime.strptime(file_path.stem, "%Y-%m-%d"))
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
            self.base_path.mkdir(parents=True, exist_ok=True)
            return True
        except Exception:
            return False

    async def close(self) -> None:
        pass

    def _get_full_path(self, category: str, date: datetime) -> Path:
        relative = self._get_file_path(category, date)
        return self.base_path / relative