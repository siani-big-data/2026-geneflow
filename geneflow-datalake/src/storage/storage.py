from abc import ABC, abstractmethod
from datetime import datetime


class StorageProvider(ABC):
    """Abstract interface for storage providers."""

    @abstractmethod
    async def append_events_batch(
        self, category: str, date: datetime, event_lines: list[str]
    ) -> None:
        """Append batch of events to a file."""
        pass

    @abstractmethod
    async def read_events(self, category: str, date: datetime) -> list[str]:
        """Read events from a single day."""
        pass

    @abstractmethod
    async def read_events_range(
        self, category: str, start_date: datetime, end_date: datetime
    ) -> list[str]:
        """Read events from a date range."""
        pass

    @abstractmethod
    async def list_categories(self) -> list[str]:
        """List categories with data."""
        pass

    @abstractmethod
    async def list_dates(self, category: str) -> list[datetime]:
        """List available dates for a category."""
        pass

    @abstractmethod
    async def get_stats(self, category: str) -> dict:
        """Get statistics for a category."""
        pass

    @abstractmethod
    async def health_check(self) -> bool:
        """Check storage health."""
        pass

    @abstractmethod
    async def close(self) -> None:
        """Close connections/resources."""
        pass

    def _get_file_path(self, category: str, date: datetime) -> str:
        """Helper: get JSONL file path."""
        date_str = date.strftime("%Y-%m-%d")
        return f"events/{category}/{date_str}.jsonl"
