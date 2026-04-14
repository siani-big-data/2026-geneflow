"""Tests for retry handler module."""

import asyncio
from datetime import datetime
from pathlib import Path

import pytest

from src.retry import RetryHandler


@pytest.mark.asyncio
async def test_add_failed_event(retry_handler: RetryHandler):
    await retry_handler.add_failed_event(
        category="users",
        date=datetime(2026, 3, 25),
        event_line='{"test": 1}',
        error="Storage timeout",
    )

    assert retry_handler.queue_size == 1


@pytest.mark.asyncio
async def test_retry_succeeds(retry_handler: RetryHandler):
    await retry_handler.add_failed_event(
        category="users",
        date=datetime(2026, 3, 25),
        event_line='{"test": 1}',
        error="Temporary error",
    )

    # Wait for retry loop to process
    await asyncio.sleep(0.8)

    # Should have been retried successfully
    assert retry_handler.queue_size == 0
    assert retry_handler.metrics["retries_succeeded"] == 1


@pytest.mark.asyncio
async def test_backoff_uses_exponential_delay():
    """Test that backoff delays increase exponentially."""
    from src.retry.backoff import ExponentialBackoff

    backoff = ExponentialBackoff(base_delay=1.0, max_delay=300.0)

    delay_0 = backoff.calculate_delay(0)
    delay_1 = backoff.calculate_delay(1)
    delay_2 = backoff.calculate_delay(2)

    assert delay_0 == 1.0
    assert delay_1 == 2.0
    assert delay_2 == 4.0


@pytest.mark.asyncio
async def test_max_retries_to_dlq(temp_dir: Path):
    failures = 0

    async def failing_callback(cat, date, line):
        nonlocal failures
        failures += 1
        raise Exception("Always fails")

    handler = RetryHandler(
        max_retries=2,
        base_delay=0.05,
        max_delay=0.1,
        dlq_path=str(temp_dir / "dlq"),
        check_interval=0.05,
    )

    await handler.start(failing_callback)

    await handler.add_failed_event(
        category="users",
        date=datetime(2026, 3, 25),
        event_line='{"test": 1}',
        error="Initial error",
    )

    # Wait for retries to exhaust
    await asyncio.sleep(1.0)

    assert handler.queue_size == 0
    assert handler.metrics["events_to_dlq"] == 1

    # Check DLQ file exists
    dlq_files = list((temp_dir / "dlq").glob("*.jsonl"))
    assert len(dlq_files) == 1

    await handler.stop()


@pytest.mark.asyncio
async def test_get_dlq_events(temp_dir: Path):
    async def noop_callback(cat, date, line):
        pass

    handler = RetryHandler(
        dlq_path=str(temp_dir / "dlq"),
        check_interval=0.1,
    )
    await handler.start(noop_callback)

    # Manually create DLQ file
    dlq_path = temp_dir / "dlq"
    dlq_path.mkdir(parents=True, exist_ok=True)

    today = datetime.utcnow().strftime("%Y-%m-%d")
    dlq_file = dlq_path / f"{today}.jsonl"
    dlq_file.write_text('{"eventId": "test-1", "category": "users", "eventLine": "{}", "date": "2026-03-25"}\n')

    events = await handler.get_dlq_events()

    assert len(events) == 1
    assert events[0]["eventId"] == "test-1"

    await handler.stop()


@pytest.mark.asyncio
async def test_get_all_dlq_events(temp_dir: Path):
    async def noop_callback(cat, date, line):
        pass

    handler = RetryHandler(
        dlq_path=str(temp_dir / "dlq"),
        check_interval=0.1,
    )
    await handler.start(noop_callback)

    # Manually create DLQ files
    dlq_path = temp_dir / "dlq"
    dlq_path.mkdir(parents=True, exist_ok=True)

    (dlq_path / "2026-03-24.jsonl").write_text('{"eventId": "old-1", "category": "users", "eventLine": "{}", "date": "2026-03-24"}\n')
    (dlq_path / "2026-03-25.jsonl").write_text('{"eventId": "new-1", "category": "users", "eventLine": "{}", "date": "2026-03-25"}\n')

    events = await handler.get_all_dlq_events()

    assert len(events) == 2

    await handler.stop()


@pytest.mark.asyncio
async def test_metrics(retry_handler: RetryHandler):
    metrics = retry_handler.metrics

    assert "queue_size" in metrics
    assert "retries_attempted" in metrics
    assert "retries_succeeded" in metrics
    assert "events_to_dlq" in metrics


@pytest.mark.asyncio
async def test_retry_handler_stop_moves_pending_to_dlq(temp_dir: Path):
    async def noop_callback(cat, date, line):
        pass

    handler = RetryHandler(
        dlq_path=str(temp_dir / "dlq"),
        check_interval=10.0,  # Long interval to prevent retry during test
    )
    await handler.start(noop_callback)

    await handler.add_failed_event(
        category="users",
        date=datetime(2026, 3, 25),
        event_line='{"test": 1}',
        error="Test error",
    )

    assert handler.queue_size == 1

    await handler.stop()

    assert handler.queue_size == 0
    assert handler.metrics["events_to_dlq"] == 1
