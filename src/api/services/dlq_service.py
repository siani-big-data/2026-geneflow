"""Service for Dead Letter Queue operations."""

from dataclasses import dataclass
from datetime import datetime

from src.constants import DATE_FORMAT
from src.consumer import RetryHandler


@dataclass
class DLQResult:
    """Result of DLQ query."""

    events: list[dict]
    count: int
    date: str | None = None


@dataclass
class RetryResult:
    """Result of single retry operation."""

    success: bool
    event_id: str
    message: str


@dataclass
class RetryAllResult:
    """Result of retry all operation."""

    succeeded: int
    failed: int
    total: int


class DLQService:
    """Service for Dead Letter Queue operations."""

    def __init__(self, retry_handler: RetryHandler):
        self._retry_handler = retry_handler

    async def get_events(self, date: str | None = None) -> DLQResult:
        """Get DLQ events for a specific date or today."""
        if date:
            target = datetime.strptime(date, DATE_FORMAT)
            events = await self._retry_handler.get_dlq_events(target)
        else:
            events = await self._retry_handler.get_dlq_events()

        return DLQResult(events=events, count=len(events), date=date)

    async def get_all_events(self) -> DLQResult:
        """Get all DLQ events across all dates."""
        events = await self._retry_handler.get_all_dlq_events()
        return DLQResult(events=events, count=len(events))

    async def retry_event(self, event_id: str) -> RetryResult:
        """Retry a specific event from DLQ."""
        success = await self._retry_handler.replay_dlq_event(event_id)
        return RetryResult(
            success=success,
            event_id=event_id,
            message="Event replayed successfully" if success else "Failed to replay event",
        )

    async def retry_all(self, date: str | None = None) -> RetryAllResult:
        """Retry all events from DLQ."""
        target = datetime.strptime(date, DATE_FORMAT) if date else None
        result = await self._retry_handler.replay_all_dlq(target)
        return RetryAllResult(**result)
