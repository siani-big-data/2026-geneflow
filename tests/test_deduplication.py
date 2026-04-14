import asyncio
from datetime import datetime, timedelta

import pytest

from src.consumer import EventDeduplicator


@pytest.mark.asyncio
async def test_not_duplicate_initially(deduplicator: EventDeduplicator):
    result = await deduplicator.is_duplicate("event-123")

    assert result is False


@pytest.mark.asyncio
async def test_duplicate_after_mark_seen(deduplicator: EventDeduplicator):
    await deduplicator.mark_seen("event-123")

    result = await deduplicator.is_duplicate("event-123")

    assert result is True


@pytest.mark.asyncio
async def test_different_events_not_duplicate(deduplicator: EventDeduplicator):
    await deduplicator.mark_seen("event-123")

    result = await deduplicator.is_duplicate("event-456")

    assert result is False


@pytest.mark.asyncio
async def test_size_tracking(deduplicator: EventDeduplicator):
    assert deduplicator.size == 0

    await deduplicator.mark_seen("event-1")
    await deduplicator.mark_seen("event-2")
    await deduplicator.mark_seen("event-3")

    assert deduplicator.size == 3


@pytest.mark.asyncio
async def test_cleanup_expired():
    dedup = EventDeduplicator(
        ttl_hours=0,  # Immediate expiry
        max_size=100,
        cleanup_interval=60.0,
    )
    await dedup.start()

    await dedup.mark_seen("event-123")
    assert dedup.size == 1

    # Manually trigger cleanup
    dedup._seen["event-123"] = datetime.utcnow() - timedelta(hours=1)
    await dedup._cleanup()

    assert dedup.size == 0
    await dedup.stop()


@pytest.mark.asyncio
async def test_cleanup_on_max_size_exceeded():
    dedup = EventDeduplicator(
        ttl_hours=1,  # 1 hour TTL
        max_size=2,
        cleanup_interval=60.0,
    )
    await dedup.start()

    # Mark old events as expired (older than TTL)
    dedup._seen["old-1"] = datetime.utcnow() - timedelta(hours=2)
    dedup._seen["old-2"] = datetime.utcnow() - timedelta(hours=2)

    # This should trigger cleanup since we exceed max_size
    await dedup.mark_seen("new-1")

    # Old expired events should be cleaned
    assert "old-1" not in dedup._seen
    assert "old-2" not in dedup._seen
    assert "new-1" in dedup._seen

    await dedup.stop()


@pytest.mark.asyncio
async def test_concurrent_access(deduplicator: EventDeduplicator):
    # Test thread safety with concurrent operations
    async def mark_many(prefix: str):
        for i in range(100):
            await deduplicator.mark_seen(f"{prefix}-{i}")

    await asyncio.gather(
        mark_many("a"),
        mark_many("b"),
        mark_many("c"),
    )

    assert deduplicator.size == 300
