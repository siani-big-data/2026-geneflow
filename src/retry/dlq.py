"""Dead Letter Queue management."""

import json
from datetime import datetime
from pathlib import Path

import aiofiles
import structlog

from src.models import RetryableEvent
from src.utils.files import read_jsonl_file

logger = structlog.get_logger()


class DeadLetterQueue:
    """Dead Letter Queue for failed events."""

    def __init__(self, path: str = "./data/dlq"):
        self.path = Path(path)

    async def initialize(self) -> None:
        """Create DLQ directory."""
        self.path.mkdir(parents=True, exist_ok=True)

    async def add(self, event: RetryableEvent, error: str) -> None:
        """Add event to DLQ."""
        dlq_record = {
            "eventId": event.id,
            "category": event.category,
            "date": event.date.isoformat(),
            "eventLine": event.eventLine,
            "retryCount": event.retryCount,
            "lastError": error,
            "createdAt": event.createdAt.isoformat(),
            "movedToDlqAt": datetime.utcnow().isoformat(),
        }

        today = datetime.utcnow().strftime("%Y-%m-%d")
        dlq_file = self.path / f"{today}.jsonl"

        async with aiofiles.open(dlq_file, mode="a", encoding="utf-8") as f:
            await f.write(json.dumps(dlq_record, ensuREDACTED=False) + "\n")

        logger.error(
            "event_moved_to_dlq",
            event_id=event.id,
            category=event.category,
            retries=event.retryCount,
            error=error,
        )

    async def get_events(self, date: datetime | None = None) -> list[dict]:
        """Get events from DLQ for a specific date."""
        if date is None:
            date = datetime.utcnow()

        dlq_file = self.path / f"{date.strftime('%Y-%m-%d')}.jsonl"
        return await read_jsonl_file(dlq_file)

    async def get_all_events(self) -> list[dict]:
        """Get all events from DLQ."""
        all_events = []
        for dlq_file in sorted(self.path.glob("*.jsonl")):
            events = await read_jsonl_file(dlq_file)
            all_events.extend(events)
        return all_events

    async def find_event(self, event_id: str) -> dict | None:
        """Find a specific event by ID."""
        for dlq_file in self.path.glob("*.jsonl"):
            events = await read_jsonl_file(dlq_file)
            for event in events:
                if event.get("eventId") == event_id:
                    return event
        return None
