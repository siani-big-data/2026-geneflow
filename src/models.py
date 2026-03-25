import json
from dataclasses import dataclass, field
from datetime import datetime
from enum import Enum
from typing import Optional


class EventCategory(str, Enum):
    """Event categories matching Redis streams."""

    USERS = "users"
    STUDIES = "studies"
    TRACES = "traces"
    ALIGNMENTS = "alignments"
    SUBSCRIPTIONS = "subscriptions"
    PLANS = "plans"
    AI = "ai"
    BLAST = "blast"
    SYSTEM = "system"


@dataclass
class EventBusMessage:
    """Events."""
    eventId: str
    type: str
    category: str
    timestamp: int
    data: str
    source: str = "unknown"
    version: str = "1.0"
    correlationId: Optional[str] = None

    @classmethod
    def from_redis(cls, data: dict) -> "EventBusMessage":
        return cls(
            eventId=data.get("eventId", ""),
            type=data.get("type", "Unknown"),
            category=data.get("category", "unknown"),
            timestamp=int(data.get("timestamp", 0)),
            data=data.get("data", "{}"),
            source=data.get("source", "unknown"),
            version=data.get("version", "1.0"),
            correlationId=data.get("correlationId"),
        )


@dataclass
class DatalakeEvent:
    """Normalized event."""
    eventId: str
    type: str
    category: str
    timestamp: datetime
    streamId: str
    data: dict
    receivedAt: datetime = field(default_factory=datetime.utcnow)

    def to_json_line(self) -> str:
        return json.dumps({
            "eventId": self.eventId,
            "type": self.type,
            "category": self.category,
            "timestamp": self.timestamp.isoformat(),
            "streamId": self.streamId,
            "data": self.data,
            "receivedAt": self.receivedAt.isoformat(),
        }, ensuREDACTED=False)

@dataclass
class RetryableEvent:
    """Retry event"""
    id: str
    category: str
    date: datetime
    eventLine: str
    retryCount: int = 0
    lastError: str = ""
    nextRetryAt: Optional[datetime] = None
    createdAt: datetime = field(default_factory=datetime.utcnow)

@dataclass
class DLQEvent:
    """Dead Letter Queue event."""
    eventId: str
    category: str
    date: str
    eventLine: str
    retryCount: int
    lastError: str
    createdAt: str
    movedToDlqAt: str

    def to_dict(self) -> dict:
        return {
            "eventId": self.eventId,
            "category": self.category,
            "date": self.date,
            "eventLine": self.eventLine,
            "retryCount": self.retryCount,
            "lastError": self.lastError,
            "createdAt": self.createdAt,
            "movedToDlqAt": self.movedToDlqAt,
        }