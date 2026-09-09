"""Service for querying events from storage."""

import json
from dataclasses import dataclass
from datetime import datetime

from src.constants import DATE_FORMAT
from src.storage import StorageProvider


@dataclass
class EventsQueryResult:
    """Result of an events query."""

    events: list[dict]
    total_count: int
    category: str
    date: str | None = None
    start_date: str | None = None
    end_date: str | None = None


class EventsQueryService:
    """Service for querying and filtering events."""

    def __init__(self, storage: StorageProvider):
        self._storage = storage

    async def query_events(
        self,
        category: str,
        date: str | None = None,
        start_date: str | None = None,
        end_date: str | None = None,
        event_type: str | None = None,
        limit: int = 1000,
        offset: int = 0,
    ) -> EventsQueryResult:
        """Query events with optional filtering and pagination."""
        lines = await self._fetch_lines(category, date, start_date, end_date)
        resolved_date = date

        if not date and not start_date:
            resolved_date = datetime.utcnow().strftime(DATE_FORMAT)

        events = self._parse_and_filter(lines, event_type)
        total = len(events)
        paginated = events[offset : offset + limit]

        return EventsQueryResult(
            events=paginated,
            total_count=total,
            category=category,
            date=resolved_date,
            start_date=start_date,
            end_date=end_date,
        )

    async def _fetch_lines(
        self,
        category: str,
        date: str | None,
        start_date: str | None,
        end_date: str | None,
    ) -> list[str]:
        """Fetch event lines from storage."""
        if date:
            target = datetime.strptime(date, DATE_FORMAT)
            return await self._storage.read_events(category, target)

        if start_date and end_date:
            start = datetime.strptime(start_date, DATE_FORMAT)
            end = datetime.strptime(end_date, DATE_FORMAT)
            return await self._storage.read_events_range(category, start, end)

        target = datetime.utcnow()
        return await self._storage.read_events(category, target)

    def _parse_and_filter(
        self,
        lines: list[str],
        event_type: str | None,
    ) -> list[dict]:
        """Parse JSON lines and filter by event type."""
        events = []
        for line in lines:
            try:
                event = json.loads(line)
                if event_type and event.get("type") != event_type:
                    continue
                events.append(event)
            except json.JSONDecodeError:
                continue
        return events
