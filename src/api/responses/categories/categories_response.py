from pydantic import BaseModel, Field


class CategoriesResponse(BaseModel):
    """List of available categories."""

    categories: list[str] = Field(..., description="Event category names")

    model_config = {
        "json_schema_extra": {
            "example": {"categories": ["users", "traces", "studies", "alignments"]}
        }
    }
