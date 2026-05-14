"""Tests for EventBusPublisher."""

from unittest.mock import AsyncMock

import pytest

from src.config import Settings
from src.events.events import WorkerStarted, WorkerStopped
from src.events.publisher import EventBusPublisher


@pytest.fixture
def enabled_settings() -> Settings:
    return Settings(eventbus_enabled=True, eventbus_stream_prefix="evt")


@pytest.fixture
def disabled_settings() -> Settings:
    return Settings(eventbus_enabled=False)


@pytest.fixture
def mock_redis() -> AsyncMock:
    redis = AsyncMock()
    redis.xadd = AsyncMock(return_value="1-0")
    redis.ping = AsyncMock(return_value=True)
    return redis


@pytest.fixture
def event() -> WorkerStarted:
    return WorkerStarted(
        workerName="w", workerId="id-1", enabledWorkers=["trace"]
    )


class TestEventBusPublisher:
    @pytest.mark.asyncio
    async def test_publish_returns_message_id(self, mock_redis, enabled_settings, event):
        publisher = EventBusPublisher(mock_redis, enabled_settings)

        message_id = await publisher.publish(event)

        assert message_id == "1-0"
        mock_redis.xadd.assert_called_once()
        stream, payload = mock_redis.xadd.call_args[0]
        assert stream.startswith("evt:")
        assert event.category in stream
        assert "data" in payload

    @pytest.mark.asyncio
    async def test_publish_disabled_returns_none(self, mock_redis, disabled_settings, event):
        publisher = EventBusPublisher(mock_redis, disabled_settings)

        result = await publisher.publish(event)

        assert result is None
        mock_redis.xadd.assert_not_called()

    @pytest.mark.asyncio
    async def test_publish_propagates_redis_error(self, mock_redis, enabled_settings, event):
        mock_redis.xadd.side_effect = RuntimeError("redis down")
        publisher = EventBusPublisher(mock_redis, enabled_settings)

        with pytest.raises(RuntimeError, match="redis down"):
            await publisher.publish(event)

    @pytest.mark.asyncio
    async def test_publish_batch_returns_ids(self, mock_redis, enabled_settings):
        publisher = EventBusPublisher(mock_redis, enabled_settings)
        events = [
            WorkerStarted(workerName="a", workerId="1", enabledWorkers=[]),
            WorkerStopped(workerName="a", workerId="1", reason="bye", jobsProcessed=2),
        ]

        ids = await publisher.publish_batch(events)

        assert len(ids) == 2
        assert mock_redis.xadd.await_count == 2

    @pytest.mark.asyncio
    async def test_publish_batch_skips_when_disabled(self, mock_redis, disabled_settings, event):
        publisher = EventBusPublisher(mock_redis, disabled_settings)

        ids = await publisher.publish_batch([event, event])

        assert ids == []
        mock_redis.xadd.assert_not_called()

    @pytest.mark.asyncio
    async def test_health_check_true(self, mock_redis, enabled_settings):
        publisher = EventBusPublisher(mock_redis, enabled_settings)
        assert await publisher.health_check() is True

    @pytest.mark.asyncio
    async def test_health_check_false_on_exception(self, mock_redis, enabled_settings):
        mock_redis.ping.side_effect = ConnectionError("nope")
        publisher = EventBusPublisher(mock_redis, enabled_settings)

        assert await publisher.health_check() is False
