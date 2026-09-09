from dataclasses import dataclass, field
from datetime import datetime


@dataclass
class RetryableEvent:
    """Event pending retry with exponential backoff metadata."""

    id: str
    category: str
    date: datetime
    eventLine: str
    retryCount: int
    lastError: str
    nextRetryAt: datetime | None = None
    createdAt: datetime = field(default_factory=datetime.utcnow)
