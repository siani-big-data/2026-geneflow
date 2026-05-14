"""Event bus publisher for GeneFlow Analysis Worker."""

import json
import structlog
from redis.asyncio import Redis

from src.config import Settings
from src.events.events import BaseEvent

logger = structlog.get_logger()


class EventBusPublisher:
    """Publishes events to Redis Streams."""

    def __init__(self, redis: Redis, settings: Settings):
        self._redis = redis
        self._settings = settings
        self._stream_prefix = settings.eventbus_stream_prefix
        self._max_len = settings.eventbus_max_stream_length
        self._enabled = settings.eventbus_enabled

    async def publish(self, event: BaseEvent) -> str | None:
        """
        Publish an event to the appropriate Redis stream.

        Returns the stream message ID if successful, None if disabled.
        """
        if not self._enabled:
            logger.debug("event_bus_disabled", event_type=event.type)
            return None

        stream_name = f"{self._stream_prefix}:{event.category}"
        event_data = event.to_dict()

        try:
            message_id = await self._redis.xadd(
                stream_name,
                {"data": json.dumps(event_data)},
                maxlen=self._max_len,
                approximate=True,
            )

            logger.info(
                "event_published",
                event_id=event.eventId,
                event_type=event.type,
                category=event.category,
                stream=stream_name,
                message_id=message_id,
            )

            return message_id

        except Exception as e:
            logger.error(
                "event_publish_failed",
                event_id=event.eventId,
                event_type=event.type,
                error=str(e),
            )
            raise

    async def publish_batch(self, events: list[BaseEvent]) -> list[str]:
        """Publish multiple events."""
        message_ids = []
        for event in events:
            msg_id = await self.publish(event)
            if msg_id:
                message_ids.append(msg_id)
        return message_ids

    async def health_check(self) -> bool:
        """Check if Redis connection is healthy."""
        try:
            await self._redis.ping()
            return True
        except Exception:
            return False
