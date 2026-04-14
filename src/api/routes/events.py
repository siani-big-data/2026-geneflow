import json
from datetime import datetime
from typing import Callable, Optional

from fastapi import APIRouter, Depends, HTTPException, Query

from src.api.responses import EventsResponse
from src.constants import API_DEFAULT_PAGE_LIMIT, API_MAX_PAGE_LIMIT, DATE_FORMAT
from src.storage import StorageProvider

router = APIRouter(tags=["Events"])


def setup_events_routes(
    router: APIRouter,
    storage: StorageProvider,
    verify_api_key: Callable,
) -> None:
    """Configure event routes."""

    @router.get(
        "/events/{category}",
        response_model=EventsResponse,
        summary="Query events",
        description="Query events by category with optional date range, type filter, and pagination.",
    )
    async def get_events(
        category: str,
        date: Optional[str] = Query(None, description="Date YYYY-MM-DD"),
        start_date: Optional[str] = Query(None),
        end_date: Optional[str] = Query(None),
        event_type: Optional[str] = Query(None),
        limit: int = Query(API_DEFAULT_PAGE_LIMIT, ge=1, le=API_MAX_PAGE_LIMIT),
        offset: int = Query(0, ge=0),
        _: None = Depends(verify_api_key),
    ):
        try:
            if date:
                target = datetime.strptime(date, DATE_FORMAT)
                lines = await storage.read_events(category, target)
            elif start_date and end_date:
                start = datetime.strptime(start_date, DATE_FORMAT)
                end = datetime.strptime(end_date, DATE_FORMAT)
                lines = await storage.read_events_range(category, start, end)
            else:
                target = datetime.utcnow()
                lines = await storage.read_events(category, target)
                date = target.strftime(DATE_FORMAT)

            events = []
            for line in lines:
                try:
                    event = json.loads(line)
                    if event_type and event.get("type") != event_type:
                        continue
                    events.append(event)
                except json.JSONDecodeError:
                    continue

            total = len(events)
            events = events[offset : offset + limit]

            return EventsResponse(
                category=category,
                events=events,
                count=total,
                date=date,
                start_date=start_date,
                end_date=end_date,
            )

        except ValueError as e:
            raise HTTPException(400, f"Invalid date format: {e}")
