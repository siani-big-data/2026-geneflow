from dataclasses import dataclass
from datetime import datetime
from enum import Enum
from typing import Any


class EventCategory(str, Enum):
    """Event categories"""
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

    @classmethod
    def from_string(cls, value: str) -> "EventCategory":
        try:
            return cls(value.lower())
        except ValueError:
            raise ValueError(f"Categoría desconocida: {value}")


@dataclass(frozen=True)
class DatalakeEvent:
    """Inmutable event for Datalake."""
    event_id: str
    event_type: str
    category: EventCategory
    occurred_at: datetime
    correlation_id: str | None
    payload: dict[str, Any]

    @property
    def date_partition(self) -> str:
        """Partición de fecha YYYY-MM-DD."""
        return self.occurred_at.strftime("%Y-%m-%d")

    def to_dict(self) -> dict[str, Any]:
        return {
            "eventId": self.event_id,
            "type": self.event_type,
            "category": self.category.value,
            "occurredAt": self.occurred_at.isoformat(),
            "correlationId": self.correlation_id,
            "payload": self.payload,
        }