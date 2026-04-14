from pydantic import BaseModel, Field


class CategoryDatesResponse(BaseModel):
    """Available dates for a category."""

    category: str = Field(..., description="Category name")
    dates: list[str] = Field(..., description="Available dates (YYYY-MM-DD)")
    count: int = Field(..., description="Number of dates")

    model_config = {
        "json_schema_extra": {
            "example": {
                "category": "users",
                "dates": ["2026-03-23", "2026-03-24", "2026-03-25"],
                "count": 3,
            }
        }
    }
