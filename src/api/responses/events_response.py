from typing import Optional

from pydantic import BaseModel, Field


class EventsResponse(BaseModel):
    """Query results for events."""

    category: str = Field(..., description="Category name")
    events: list[dict] = Field(..., description="List of events")
    count: int = Field(..., description="Total count (before pagination)")
    date: Optional[str] = Field(None, description="Queried date")
    start_date: Optional[str] = Field(None, description="Range start date")
    end_date: Optional[str] = Field(None, description="Range end date")

    model_config = {
        "json_schema_extra": {
            "example": {
                "category": "users",
                "events": [
                    {
                        "eventId": "550e8400-e29b-41d4-a716-446655440000",
                        "type": "UserRegistered",
                        "timestamp": "2026-03-25T10:30:00.000Z",
                        "data": {"userId": "user-123", "email": "scientist@lab.org"},
                    }
                ],
                "count": 1,
                "date": "2026-03-25",
            }
        }
    }
