from typing import Optional

from pydantic import BaseModel, Field


class HealthResponse(BaseModel):
    """Service health status."""

    status: str = Field(..., description="Service status", examples=["healthy", "degraded"])
    storage_healthy: bool = Field(..., description="Storage provider health")
    consumer_metrics: Optional[dict] = Field(
        None, description="Redis consumer metrics (events processed, etc.)"
    )

    model_config = {
        "json_schema_extra": {
            "example": {
                "status": "healthy",
                "storage_healthy": True,
                "consumer_metrics": {"events_processed": 1234, "events_per_second": 45.2},
            }
        }
    }
