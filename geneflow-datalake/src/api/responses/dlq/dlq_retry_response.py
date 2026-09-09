from typing import Optional

from pydantic import BaseModel, Field


class DLQRetryResponse(BaseModel):
    """Result of retrying a DLQ event."""

    success: bool = Field(..., description="Whether retry succeeded")
    event_id: Optional[str] = Field(None, description="Event ID")
    message: str = Field(..., description="Result message")
