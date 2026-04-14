from typing import Callable, Optional

from fastapi import APIRouter, Depends, HTTPException, Query

from src.api.responses import EventsResponse
from src.api.services import EventsQueryService
from src.constants import API_DEFAULT_PAGE_LIMIT, API_MAX_PAGE_LIMIT

router = APIRouter(tags=["Events"])


def setup_events_routes(
    router: APIRouter,
    events_service: EventsQueryService,
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
            result = await events_service.query_events(
                category=category,
                date=date,
                start_date=start_date,
                end_date=end_date,
                event_type=event_type,
                limit=limit,
                offset=offset,
            )

            return EventsResponse(
                category=result.category,
                events=result.events,
                count=result.total_count,
                date=result.date,
                start_date=result.start_date,
                end_date=result.end_date,
            )

        except ValueError as e:
            raise HTTPException(400, f"Invalid date format: {e}")
