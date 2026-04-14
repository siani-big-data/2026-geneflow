from typing import Optional

from pydantic import BaseModel, Field


class DLQResponse(BaseModel):
    """Dead Letter Queue contents."""

    events: list[dict] = Field(..., description="Failed events in DLQ")
    count: int = Field(..., description="Number of failed events")
    date: Optional[str] = Field(None, description="Queried date")

    model_config = {
        "json_schema_extra": {
            "example": {
                "events": [
                    {
                        "eventId": "failed-event-123",
                        "category": "users",
                        "lastError": "Storage timeout",
                        "retryCount": 5,
                        "movedToDlqAt": "2026-03-25T10:35:00.000Z",
                    }
                ],
                "count": 1,
                "date": "2026-03-25",
            }
        }
    }
