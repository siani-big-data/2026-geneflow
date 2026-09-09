"""Event bus consumer for GeneFlow AI service."""

import asyncio
import json
from typing import Any, Awaitable, Callable, Optional

import structlog
from redis.asyncio import Redis

from src.config import Settings

logger = structlog.get_logger()


class EventBusConsumer:
    """Consumes events from Redis Streams."""

    def __init__(
        self,
        redis: Redis,
        settings: Settings,
        on_trace_processed: Optional[Callable[[dict], Awaitable[None]]] = None,
        on_alignment_completed: Optional[Callable[[dict], Awaitable[None]]] = None,
    ):
        self._redis = redis
        self._settings = settings
        self._stream_prefix = settings.eventbus_stream_prefix
        self._consumer_group = settings.redis_consumer_group
        self._consumer_name = settings.redis_consumer_name
        self._block_ms = settings.redis_block_ms
        self._categories = settings.subscribed_categories

        # Event handlers
        self._on_trace_processed = on_trace_processed
        self._on_alignment_completed = on_alignment_completed

        self._running = False
        self._events_received = 0
        self._events_processed = 0
        self._errors = 0

    @property
    def metrics(self) -> dict[str, Any]:
        """Get consumer metrics."""
        return {
            "eventsReceived": self._events_received,
            "eventsProcessed": self._events_processed,
            "errors": self._errors,
            "running": self._running,
        }

    async def start(self) -> None:
        """Start consuming events."""
        logger.info(
            "consumer_starting",
            categories=self._categories,
            consumer_group=self._consumer_group,
            consumer_name=self._consumer_name,
        )

        await self._ensuREDACTED()
        self._running = True
        await self._consume_loop()

    async def stop(self) -> None:
        """Stop consuming events."""
        logger.info("consumer_stopping", metrics=self.metrics)
        self._running = False

    async def _ensuREDACTED(self) -> None:
        """Create consumer groups for subscribed categories."""
        for category in self._categories:
            stream_name = f"{self._stream_prefix}:{category}"

            try:
                await self._redis.xgroup_create(
                    stream_name,
                    self._consumer_group,
                    id="0",
                    mkstream=True,
                )
                logger.info(
                    "consumer_group_created",
                    stream=stream_name,
                    group=self._consumer_group,
                )
            except Exception as e:
                if "BUSYGROUP" in str(e):
                    logger.debug("consumer_group_exists", stream=stream_name)
                else:
                    raise

    async def _consume_loop(self) -> None:
        """Main loop that consumes events."""
        streams = {f"{self._stream_prefix}:{cat}": ">" for cat in self._categories}

        while self._running:
            try:
                messages = await self._redis.xreadgroup(
                    groupname=self._consumer_group,
                    consumername=self._consumer_name,
                    streams=streams,
                    count=10,
                    block=self._block_ms,
                )

                if not messages:
                    continue

                for stream_name, entries in messages:
                    for msg_id, data in entries:
                        await self._process_event(stream_name, msg_id, data)

            except asyncio.CancelledError:
                logger.info("consumer_cancelled")
                break
            except Exception as e:
                logger.error("consumer_error", error=str(e))
                self._errors += 1
                await asyncio.sleep(5)

    async def _process_event(
        self,
        stream_name: bytes | str,
        msg_id: bytes | str,
        data: dict,
    ) -> None:
        """Process a single event from the stream."""
        self._events_received += 1

        # Decode if bytes
        if isinstance(stream_name, bytes):
            stream_name = stream_name.decode()
        if isinstance(msg_id, bytes):
            msg_id = msg_id.decode()

        try:
            # Parse event data
            raw_data = data.get(b"data") or data.get("data", "{}")
            if isinstance(raw_data, bytes):
                raw_data = raw_data.decode()

            event_dict = self._parse_event_data(raw_data)
            event_type = event_dict.get("type", "")

            logger.debug(
                "event_received",
                event_type=event_type,
                stream=stream_name,
                msg_id=msg_id,
            )

            # Route to appropriate handler
            if event_type == "TraceProcessed" and self._on_trace_processed:
                event_data = self._extract_event_data(event_dict)
                await self._on_trace_processed(event_data)
                self._events_processed += 1

            elif event_type == "AlignmentCompleted" and self._on_alignment_completed:
                event_data = self._extract_event_data(event_dict)
                await self._on_alignment_completed(event_data)
                self._events_processed += 1

            # Acknowledge message
            await self._redis.xack(
                stream_name,
                self._consumer_group,
                msg_id,
            )

        except Exception as e:
            self._errors += 1
            logger.error(
                "event_processing_failed",
                stream=stream_name,
                msg_id=msg_id,
                error=str(e),
            )
            # Acknowledge to prevent infinite retries
            await self._redis.xack(
                stream_name,
                self._consumer_group,
                msg_id,
            )

    def _parse_event_data(self, raw_data: str) -> dict:
        """Parse event data from string."""
        # Handle double-encoded JSON or dict string
        if raw_data.startswith("{"):
            try:
                return json.loads(raw_data)
            except json.JSONDecodeError:
                # Try eval for dict-like strings
                import ast

                return ast.literal_eval(raw_data)
        return {}

    def _extract_event_data(self, event_dict: dict) -> dict:
        """Extract the data payload from an event."""
        data = event_dict.get("data", {})
        if isinstance(data, str):
            try:
                return json.loads(data)
            except json.JSONDecodeError:
                return {}
        return data
