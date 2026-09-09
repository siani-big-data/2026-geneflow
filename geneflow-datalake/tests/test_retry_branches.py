"""Branch coverage tests for src/retry/dlq.py and src/retry/retry_handler.py."""

import asyncio
import json
import tempfile
from datetime import datetime
from pathlib import Path

import pytest

from src.models import RetryableEvent
from src.retry import DeadLetterQueue, RetryHandler


@pytest.fixture
def temp_dir():
    with tempfile.TemporaryDirectory() as tmpdir:
        yield Path(tmpdir)


def make_retryable(category="users", event_id="evt-1"):
    return RetryableEvent(
        id=event_id,
        category=category,
        date=datetime(2026, 3, 25),
        eventLine='{"x": 1}',
        retryCount=0,
        lastError="boom",
    )


class TestDeadLetterQueueBranches:
    """Cover lines 71-76 (find_event) and branch 55->58 (get_events default date)."""

    @pytest.mark.asyncio
    async def test_get_events_default_date(self, temp_dir):
        """Cover branch 55->58: date=None defaults to today."""
        dlq = DeadLetterQueue(path=str(temp_dir / "dlq"))
        await dlq.initialize()

        today = datetime.utcnow().strftime("%Y-%m-%d")
        (temp_dir / "dlq" / f"{today}.jsonl").write_text(
            '{"eventId": "today-1"}\n', encoding="utf-8"
        )

        events = await dlq.get_events()  # No date param
        assert len(events) == 1
        assert events[0]["eventId"] == "today-1"

    @pytest.mark.asyncio
    async def test_find_event_returns_match(self, temp_dir):
        """Cover lines 71-75: iterate dlq files and find event by id."""
        dlq = DeadLetterQueue(path=str(temp_dir / "dlq"))
        await dlq.initialize()

        (temp_dir / "dlq" / "2026-03-24.jsonl").write_text(
            '{"eventId": "evt-A"}\n{"eventId": "evt-B"}\n', encoding="utf-8"
        )
        (temp_dir / "dlq" / "2026-03-25.jsonl").write_text(
            '{"eventId": "evt-C"}\n', encoding="utf-8"
        )

        found = await dlq.find_event("evt-C")
        assert found is not None
        assert found["eventId"] == "evt-C"

    @pytest.mark.asyncio
    async def test_find_event_not_found_returns_none(self, temp_dir):
        """Cover line 76: returns None when not found, iterating multiple files."""
        dlq = DeadLetterQueue(path=str(temp_dir / "dlq"))
        await dlq.initialize()

        (temp_dir / "dlq" / "2026-03-24.jsonl").write_text(
            '{"eventId": "evt-A"}\n', encoding="utf-8"
        )
        (temp_dir / "dlq" / "2026-03-25.jsonl").write_text(
            '{"eventId": "evt-B"}\n', encoding="utf-8"
        )

        found = await dlq.find_event("missing-id")
        assert found is None

    @pytest.mark.asyncio
    async def test_find_event_empty_directory(self, temp_dir):
        """Empty DLQ directory -> returns None."""
        dlq = DeadLetterQueue(path=str(temp_dir / "dlq"))
        await dlq.initialize()

        assert await dlq.find_event("anything") is None

    @pytest.mark.asyncio
    async def test_add_event_writes_jsonl_record(self, temp_dir):
        """Sanity for DLQ add - verifies file output structure."""
        dlq = DeadLetterQueue(path=str(temp_dir / "dlq"))
        await dlq.initialize()

        event = make_retryable()
        await dlq.add(event, "fail-error")

        files = list((temp_dir / "dlq").glob("*.jsonl"))
        assert len(files) == 1
        content = files[0].read_text(encoding="utf-8").strip()
        record = json.loads(content)
        assert record["eventId"] == "evt-1"
        assert record["lastError"] == "fail-error"


class TestRetryHandlerReplayBranches:
    """Cover lines 165-200: replay_dlq_event and replay_all_dlq paths."""

    @pytest.mark.asyncio
    async def test_replay_dlq_event_not_found(self, temp_dir):
        """Cover lines 165-168: event not found in DLQ -> return False."""
        called = []

        async def cb(c, d, lst):
            called.append(lst)

        handler = RetryHandler(dlq_path=str(temp_dir / "dlq"), check_interval=10.0)
        await handler.start(cb)
        try:
            result = await handler.replay_dlq_event("does-not-exist")
            assert result is False
            assert called == []
        finally:
            await handler.stop()

    @pytest.mark.asyncio
    async def test_replay_dlq_event_success(self, temp_dir):
        """Cover lines 170-176: event found, retry succeeds."""
        # Set up a DLQ file with an event
        dlq_dir = temp_dir / "dlq"
        dlq_dir.mkdir(parents=True)
        (dlq_dir / "2026-03-25.jsonl").write_text(
            '{"eventId": "ok-1", "category": "users", "date": "2026-03-25T00:00:00", '
            '"eventLine": "{\\"a\\":1}"}\n',
            encoding="utf-8",
        )

        called = []

        async def cb(c, d, lst):
            called.append((c, d, lst))

        handler = RetryHandler(dlq_path=str(dlq_dir), check_interval=10.0)
        await handler.start(cb)
        try:
            result = await handler.replay_dlq_event("ok-1")
            assert result is True
            assert len(called) == 1
            assert called[0][0] == "users"
        finally:
            await handler.stop()

    @pytest.mark.asyncio
    async def test_replay_dlq_event_failure(self, temp_dir):
        """Cover lines 177-180: replay callback raises -> returns False."""
        dlq_dir = temp_dir / "dlq"
        dlq_dir.mkdir(parents=True)
        (dlq_dir / "2026-03-25.jsonl").write_text(
            '{"eventId": "fail-1", "category": "users", "date": "2026-03-25T00:00:00", '
            '"eventLine": "{}"}\n',
            encoding="utf-8",
        )

        async def cb(c, d, lst):
            raise RuntimeError("nope")

        handler = RetryHandler(dlq_path=str(dlq_dir), check_interval=10.0)
        await handler.start(cb)
        try:
            result = await handler.replay_dlq_event("fail-1")
            assert result is False
        finally:
            await handler.stop()

    @pytest.mark.asyncio
    async def test_replay_all_dlq_with_date(self, temp_dir):
        """Cover lines 184-200: replay_all_dlq with date filter, mix succ/fail."""
        dlq_dir = temp_dir / "dlq"
        dlq_dir.mkdir(parents=True)
        target_date = datetime.utcnow()
        date_str = target_date.strftime("%Y-%m-%d")

        records = [
            {
                "eventId": "ok-1",
                "category": "users",
                "date": "2026-03-25T00:00:00",
                "eventLine": "{}",
            },
            {
                "eventId": "fail-1",
                "category": "traces",
                "date": "2026-03-25T00:00:00",
                "eventLine": "{}",
            },
        ]
        with (dlq_dir / f"{date_str}.jsonl").open("w", encoding="utf-8") as f:
            for r in records:
                f.write(json.dumps(r) + "\n")

        call_count = {"n": 0}

        async def cb(c, d, lst):
            call_count["n"] += 1
            if c == "traces":
                raise RuntimeError("simulated failure")

        handler = RetryHandler(dlq_path=str(dlq_dir), check_interval=10.0)
        await handler.start(cb)
        try:
            result = await handler.replay_all_dlq(target_date)
            assert result["total"] == 2
            assert result["succeeded"] == 1
            assert result["failed"] == 1
        finally:
            await handler.stop()

    @pytest.mark.asyncio
    async def test_replay_all_dlq_without_date_uses_all_events(self, temp_dir):
        """Cover branch 189->200: no date -> get_all_dlq_events path."""
        dlq_dir = temp_dir / "dlq"
        dlq_dir.mkdir(parents=True)

        record = {
            "eventId": "all-1",
            "category": "users",
            "date": "2026-03-25T00:00:00",
            "eventLine": "{}",
        }
        (dlq_dir / "2026-03-24.jsonl").write_text(json.dumps(record) + "\n", encoding="utf-8")

        succ = []

        async def cb(c, d, lst):
            succ.append(c)

        handler = RetryHandler(dlq_path=str(dlq_dir), check_interval=10.0)
        await handler.start(cb)
        try:
            result = await handler.replay_all_dlq()
            assert result["total"] == 1
            assert result["succeeded"] == 1
        finally:
            await handler.stop()


class TestRetryLoopBranches:
    """Cover lines 118-119: retry loop error path, plus branch 98->-96."""

    @pytest.mark.asyncio
    async def test_retry_loop_handles_exception_in_loop(self, temp_dir):
        """Cover lines 118-119: generic Exception inside _retry_loop is logged but loop continues."""

        async def cb(c, d, lst):
            pass

        handler = RetryHandler(
            dlq_path=str(temp_dir / "dlq"),
            check_interval=0.05,
        )

        # Patch _lock to throw to trigger the except Exception path
        original_lock = handler._lock

        class BadLock:
            entered = 0

            async def __aenter__(self):
                BadLock.entered += 1
                if BadLock.entered == 1:
                    raise RuntimeError("lock failed")
                # Forward to real lock thereafter
                await original_lock.__aenter__()
                return self

            async def __aexit__(self, exc_type, exc, tb):
                if BadLock.entered > 1:
                    await original_lock.__aexit__(exc_type, exc, tb)

        handler._lock = BadLock()
        await handler.start(cb)

        # Let the loop iterate a few times
        await asyncio.sleep(0.2)

        # Restore so stop() works cleanly
        handler._lock = original_lock
        await handler.stop()

        # If we got here, the exception was handled in-loop without crashing


class TestRetryEventBranches:
    """Cover branches 166->167 / 166->170: retry_event success/failure split."""

    @pytest.mark.asyncio
    async def test_retry_event_succeeds_path(self, temp_dir):
        """Successful retry -> increments retries_succeeded."""

        async def cb(c, d, lst):
            pass  # success

        handler = RetryHandler(dlq_path=str(temp_dir / "dlq"), check_interval=10.0)
        await handler.start(cb)
        try:
            event = make_retryable()
            await handler._retry_event(event)
            assert handler._retries_succeeded == 1
        finally:
            await handler.stop()

    @pytest.mark.asyncio
    async def test_retry_event_fail_under_max_requeues(self, temp_dir):
        """Failure under max_retries -> event added back to queue with nextRetryAt."""

        async def cb(c, d, lst):
            raise RuntimeError("fail")

        handler = RetryHandler(
            max_retries=5,
            dlq_path=str(temp_dir / "dlq"),
            check_interval=10.0,
            base_delay=10.0,
            max_delay=20.0,
        )
        await handler.start(cb)
        try:
            event = make_retryable()
            await handler._retry_event(event)
            assert handler._retries_attempted == 1
            assert handler._retries_succeeded == 0
            # Re-queued, not DLQ'd
            assert handler.queue_size == 1
            assert handler._events_to_dlq == 0
        finally:
            # Stop will move queue to DLQ; that's fine
            await handler.stop()

    @pytest.mark.asyncio
    async def test_retry_event_max_exceeded_goes_to_dlq(self, temp_dir):
        """Cover lines 130-132 (DLQ branch): retryCount >= max_retries -> DLQ."""

        async def cb(c, d, lst):
            raise RuntimeError("fail")

        handler = RetryHandler(
            max_retries=2,
            dlq_path=str(temp_dir / "dlq"),
            check_interval=10.0,
        )
        await handler.start(cb)
        try:
            event = make_retryable()
            event.retryCount = 1  # one more failure = max
            await handler._retry_event(event)
            assert handler._events_to_dlq == 1
            assert handler.queue_size == 0
        finally:
            await handler.stop()
