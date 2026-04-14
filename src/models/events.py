import json
from dataclasses import dataclass
from datetime import datetime
from enum import Enum
from typing import Any


class EventCategory(str, Enum):
    """Event categories."""

    USERS = "users"
    STUDIES = "studies"
    TRACES = "traces"
    ALIGNMENTS = "alignments"
    SUBSCRIPTIONS = "subscriptions"
    PLANS = "plans"
    AI = "ai"
    BLAST = "blast"
    SYSTEM = "system"
    PROFILES = "profiles"
    BILLING = "billing"
    PAYMENTS = "payments"

    @classmethod
    def from_string(cls, value: str) -> "EventCategory":
        try:
            return cls(value.lower())
        except ValueError:
            raise ValueError(f"Unknown category: {value}")


@dataclass
class EventBusMessage:
    """Message from Redis event bus."""

    eventId: str
    type: str
    timestamp: int
    data: Any

    @classmethod
    def from_redis(cls, data: dict) -> "EventBusMessage":
        """Parse message from Redis stream data."""
        return cls(
            eventId=data.get("eventId", ""),
            type=data.get("type", ""),
            timestamp=int(data.get("timestamp", 0)),
            data=data.get("data", {}),
        )


@dataclass
class DatalakeEvent:
    """Normalized event for storage in datalake."""

    eventId: str
    type: str
    category: str
    timestamp: datetime
    streamId: str
    data: dict[str, Any]

    @property
    def date_partition(self) -> str:
        """Date partition YYYY-MM-DD."""
        return self.timestamp.strftime("%Y-%m-%d")

    def to_dict(self) -> dict[str, Any]:
        """Convert to dictionary."""
        return {
            "eventId": self.eventId,
            "type": self.type,
            "category": self.category,
            "timestamp": self.timestamp.isoformat(),
            "streamId": self.streamId,
            "data": self.data,
        }

    def to_json_line(self) -> str:
        """Convert to JSON line for JSONL storage."""
        return json.dumps(self.to_dict(), ensuREDACTED=False)
