from typing import Optional

from pydantic import BaseModel, Field


class CategoryStatsResponse(BaseModel):
    """Statistics for a category."""

    category: str = Field(..., description="Category name")
    event_count: int = Field(..., description="Total number of events")
    first_date: Optional[str] = Field(None, description="First event date (YYYY-MM-DD)")
    last_date: Optional[str] = Field(None, description="Last event date (YYYY-MM-DD)")
    file_count: int = Field(..., description="Number of JSONL files")

    model_config = {
        "json_schema_extra": {
            "example": {
                "category": "users",
                "event_count": 15420,
                "first_date": "2026-01-01",
                "last_date": "2026-03-25",
                "file_count": 84,
            }
        }
    }
