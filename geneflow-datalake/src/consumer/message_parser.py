"""Message parsing from Redis streams."""

import json
from datetime import datetime
from typing import Any

from src.models import DatalakeEvent, EventBusMessage


class MessageParser:
    """Parses messages from Redis streams into domain events."""

    def parse_redis_message(self, data: dict) -> EventBusMessage:
        """Parse raw Redis message into EventBusMessage."""
        return EventBusMessage.from_redis(data)

    def parse_timestamp(self, timestamp_ms: int) -> datetime:
        """Parse millisecond timestamp to datetime."""
        try:
            return datetime.fromtimestamp(timestamp_ms / 1000)
        except (ValueError, TypeError):
            return datetime.utcnow()

    def parse_event_data(self, data: Any) -> dict:
        """Parse event data, handling string or dict."""
        if isinstance(data, str):
            try:
                return json.loads(data)
            except json.JSONDecodeError:
                return {"raw": data}
        return data if isinstance(data, dict) else {"raw": str(data)}

    def create_datalake_event(
        self,
        message: EventBusMessage,
        category: str,
        stream_id: str,
    ) -> DatalakeEvent:
        """Create normalized DatalakeEvent from parsed message."""
        timestamp = self.parse_timestamp(message.timestamp)
        event_data = self.parse_event_data(message.data)

        return DatalakeEvent(
            eventId=message.eventId,
            type=message.type,
            category=category,
            timestamp=timestamp,
            streamId=stream_id,
            data=event_data,
        )
