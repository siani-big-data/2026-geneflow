from pydantic import BaseModel, Field


class AvailableCategoriesResponse(BaseModel):
    """All valid event categories."""

    categories: list[str] = Field(..., description="Valid category names from enum")
    count: int = Field(..., description="Number of categories")

    model_config = {
        "json_schema_extra": {
            "example": {
                "categories": [
                    "users",
                    "studies",
                    "traces",
                    "alignments",
                    "subscriptions",
                    "plans",
                    "ai",
                    "blast",
                    "system",
                ],
                "count": 9,
            }
        }
    }
