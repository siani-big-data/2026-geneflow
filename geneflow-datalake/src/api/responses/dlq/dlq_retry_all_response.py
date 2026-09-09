from pydantic import BaseModel, Field


class DLQRetryAllResponse(BaseModel):
    """Result of retrying all DLQ events."""

    succeeded: int = Field(..., description="Number of successful retries")
    failed: int = Field(..., description="Number of failed retries")
    total: int = Field(..., description="Total events processed")
