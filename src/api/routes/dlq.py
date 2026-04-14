from datetime import datetime
from typing import Callable, Optional

from fastapi import APIRouter, Depends, Query

from src.api.responses import DLQResponse, DLQRetryAllResponse, DLQRetryResponse
from src.constants import DATE_FORMAT
from src.retry import RetryHandler

router = APIRouter(prefix="/dlq", tags=["DLQ"])


def setup_dlq_routes(
    router: APIRouter,
    retry_handler: RetryHandler,
    verify_api_key: Callable,
) -> None:
    """Configure DLQ routes."""

    @router.get(
        "",
        response_model=DLQResponse,
        summary="Get failed events",
        description="Get events from the Dead Letter Queue for a specific date (default: today).",
    )
    async def get_dlq(
        date: Optional[str] = Query(None, description="Date YYYY-MM-DD, default today"),
        _: None = Depends(verify_api_key),
    ):
        if date:
            target = datetime.strptime(date, DATE_FORMAT)
            events = await retry_handler.get_dlq_events(target)
        else:
            events = await retry_handler.get_dlq_events()

        return DLQResponse(events=events, count=len(events), date=date)

    @router.get(
        "/all",
        response_model=DLQResponse,
        summary="Get all failed events",
        description="Get all events from the Dead Letter Queue across all dates.",
    )
    async def get_all_dlq(_: None = Depends(verify_api_key)):
        events = await retry_handler.get_all_dlq_events()
        return DLQResponse(events=events, count=len(events))

    @router.post(
        "/retry/{event_id}",
        response_model=DLQRetryResponse,
        summary="Retry a failed event",
        description="Attempt to reprocess a specific event from the DLQ.",
    )
    async def retry_dlq_event(event_id: str, _: None = Depends(verify_api_key)):
        success = await retry_handler.replay_dlq_event(event_id)
        return DLQRetryResponse(
            success=success,
            event_id=event_id,
            message="Event replayed successfully" if success else "Failed to replay event",
        )

    @router.post(
        "/retry-all",
        response_model=DLQRetryAllResponse,
        summary="Retry all failed events",
        description="Attempt to reprocess all events in the DLQ.",
    )
    async def retry_all_dlq(
        date: Optional[str] = Query(None, description="Filter by date (YYYY-MM-DD)"),
        _: None = Depends(verify_api_key),
    ):
        target = datetime.strptime(date, DATE_FORMAT) if date else None
        result = await retry_handler.replay_all_dlq(target)
        return DLQRetryAllResponse(**result)
