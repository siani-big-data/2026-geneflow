from typing import Optional

from pydantic import BaseModel, Field


class ReplayResponse(BaseModel):
    """Replay results for system reconstruction."""

    category: str = Field(..., description="Category name")
    events: list[dict] = Field(..., description="Events sorted chronologically")
    count: int = Field(..., description="Total event count")
    first_date: Optional[str] = Field(None, description="First event date")
    last_date: Optional[str] = Field(None, description="Last event date")
