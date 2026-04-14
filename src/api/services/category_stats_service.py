"""Service for category statistics and listing."""

from dataclasses import dataclass
from datetime import datetime

from src.constants import DATE_FORMAT
from src.models import EventCategory
from src.storage import StorageProvider


@dataclass
class CategoryStats:
    """Statistics for a category."""

    category: str
    event_count: int
    first_date: str | None
    last_date: str | None
    file_count: int


@dataclass
class CategoryDates:
    """Available dates for a category."""

    category: str
    dates: list[str]
    count: int


class CategoryStatsService:
    """Service for category statistics and listing."""

    def __init__(self, storage: StorageProvider):
        self._storage = storage

    async def list_categories_with_data(self) -> list[str]:
        """List categories that have stored data."""
        return await self._storage.list_categories()

    def list_available_categories(self) -> list[str]:
        """List all valid event categories from enum."""
        return [c.value for c in EventCategory]

    async def get_stats(self, category: str) -> CategoryStats:
        """Get statistics for a category."""
        stats = await self._storage.get_stats(category)
        return CategoryStats(
            category=category,
            event_count=stats.get("event_count", 0),
            first_date=stats.get("first_date"),
            last_date=stats.get("last_date"),
            file_count=stats.get("file_count", 0),
        )

    async def get_dates(self, category: str) -> CategoryDates:
        """Get available dates for a category."""
        dates = await self._storage.list_dates(category)
        formatted = [d.strftime(DATE_FORMAT) for d in dates]
        return CategoryDates(
            category=category,
            dates=formatted,
            count=len(formatted),
        )
