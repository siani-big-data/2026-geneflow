"""Tests for MounterEngine."""

import json
import pytest
import tempfile
from datetime import datetime
from pathlib import Path
from unittest.mock import AsyncMock, MagicMock

from src.mounters.base import BaseMounter, MounterMode
from src.mounters.engine import MounterEngine


class MockMounter(BaseMounter):
    """Mock mounter for testing."""

    def __init__(self, name: str, categories: list[str]):
        super().__init__(name=name, categories=categories)
        self.events_handled = []
        self.started = False
        self.stopped = False
        self.rebuilt = False

    async def start(self) -> None:
        self.started = True
        self._running = True

    async def stop(self) -> None:
        self.stopped = True
        self._running = False

    async def handle_event(self, event: dict) -> None:
        self.events_handled.append(event)

    async def health_check(self) -> bool:
        return True

    async def rebuild(self) -> None:
        self.rebuilt = True


@pytest.fixture
def temp_datalake():
    """Create a temporary datalake directory with test events."""
    with tempfile.TemporaryDirectory() as tmpdir:
        datalake_path = Path(tmpdir)
        events_path = datalake_path / "events"

        # Create users events
        users_path = events_path / "users"
        users_path.mkdir(parents=True)

        events_file = users_path / "2026-03-25.jsonl"
        events = [
            {"category": "users", "type": "UserRegistered", "payload": {"id": "user-1"}},
            {"category": "users", "type": "UserUpdated", "payload": {"id": "user-1"}},
        ]
        with open(events_file, "w") as f:
            for event in events:
                f.write(json.dumps(event) + "\n")

        # Create traces events
        traces_path = events_path / "traces"
        traces_path.mkdir(parents=True)

        traces_file = traces_path / "2026-03-25.jsonl"
        events = [
            {"category": "traces", "type": "TraceUploaded", "payload": {"id": "trace-1"}},
        ]
        with open(traces_file, "w") as f:
            for event in events:
                f.write(json.dumps(event) + "\n")

        yield datalake_path


class TestMounterEngine:
    """Tests for MounterEngine class."""

    def test_register_mounter(self):
        """Test registering a mounter."""
        engine = MounterEngine()
        mounter = MockMounter("test", ["users"])

        engine.register(mounter)

        assert len(engine.mounters) == 1
        assert engine.mounters[0].name == "test"

    def test_get_mounter(self):
        """Test getting a mounter by name."""
        engine = MounterEngine()
        mounter = MockMounter("postgres", ["users", "studies"])
        engine.register(mounter)

        result = engine.get_mounter("postgres")
        assert result is mounter

        result = engine.get_mounter("nonexistent")
        assert result is None

    async def test_start_mounters(self):
        """Test starting all mounters."""
        engine = MounterEngine()
        mounter1 = MockMounter("m1", ["users"])
        mounter2 = MockMounter("m2", ["traces"])
        engine.register(mounter1)
        engine.register(mounter2)

        await engine.start()

        assert mounter1.started is True
        assert mounter2.started is True

    async def test_stop_mounters(self):
        """Test stopping all mounters."""
        engine = MounterEngine()
        mounter = MockMounter("test", ["users"])
        engine.register(mounter)

        await engine.start()
        await engine.stop()

        assert mounter.stopped is True

    async def test_replay_mode(self, temp_datalake):
        """Test REPLAY mode processes events."""
        engine = MounterEngine(datalake_path=str(temp_datalake))

        users_mounter = MockMounter("users", ["users"])
        traces_mounter = MockMounter("traces", ["traces"])
        engine.register(users_mounter)
        engine.register(traces_mounter)

        result = await engine.run(mode=MounterMode.REPLAY)

        # Users mounter should have 2 events
        assert len(users_mounter.events_handled) == 2
        assert users_mounter.events_handled[0]["type"] == "UserRegistered"

        # Traces mounter should have 1 event
        assert len(traces_mounter.events_handled) == 1
        assert traces_mounter.events_handled[0]["type"] == "TraceUploaded"

        assert result["events_processed"] == 3
        assert result["events_failed"] == 0

    async def test_replay_with_date_filter(self, temp_datalake):
        """Test REPLAY mode with date range."""
        engine = MounterEngine(datalake_path=str(temp_datalake))

        mounter = MockMounter("test", ["users"])
        engine.register(mounter)

        # Filter to future date - should find nothing
        result = await engine.run(
            mode=MounterMode.REPLAY,
            from_date=datetime(2026, 4, 1),
        )

        assert len(mounter.events_handled) == 0
        assert result["events_processed"] == 0

    async def test_replay_with_category_filter(self, temp_datalake):
        """Test REPLAY mode with category filter."""
        engine = MounterEngine(datalake_path=str(temp_datalake))

        mounter = MockMounter("all", ["users", "traces"])
        engine.register(mounter)

        # Only process users
        result = await engine.run(
            mode=MounterMode.REPLAY,
            categories=["users"],
        )

        # Should only have user events
        assert len(mounter.events_handled) == 2
        assert all(e["category"] == "users" for e in mounter.events_handled)

    async def test_rebuild_mode(self, temp_datalake):
        """Test REBUILD mode drops and rebuilds."""
        engine = MounterEngine(datalake_path=str(temp_datalake))

        mounter = MockMounter("test", ["users"])
        engine.register(mounter)

        result = await engine.run(mode=MounterMode.REBUILD)

        # Rebuild should have been called
        assert mounter.rebuilt is True

        # Events should have been replayed
        assert len(mounter.events_handled) == 2

    async def test_health_check(self):
        """Test health check aggregation."""
        engine = MounterEngine()

        healthy_mounter = MockMounter("healthy", ["users"])
        unhealthy_mounter = MockMounter("unhealthy", ["traces"])
        unhealthy_mounter.health_check = AsyncMock(return_value=False)

        engine.register(healthy_mounter)
        engine.register(unhealthy_mounter)

        await engine.start()
        health = await engine.health_check()

        assert health["healthy"] is True
        assert health["unhealthy"] is False

    async def test_get_metrics(self):
        """Test metrics aggregation."""
        engine = MounterEngine()

        mounter = MockMounter("test", ["users"])
        engine.register(mounter)

        metrics = engine.get_metrics()

        assert "test" in metrics
        assert "events_processed" in metrics["test"]

    async def test_empty_datalake(self):
        """Test handling empty datalake."""
        with tempfile.TemporaryDirectory() as tmpdir:
            engine = MounterEngine(datalake_path=tmpdir)
            mounter = MockMounter("test", ["users"])
            engine.register(mounter)

            result = await engine.run(mode=MounterMode.REPLAY)

            assert result["events_processed"] == 0
            assert len(mounter.events_handled) == 0

    def test_parse_date_from_filename(self):
        """Test date parsing from filename."""
        engine = MounterEngine()

        date = engine._parse_date_from_filename("2026-03-25.jsonl")
        assert date.year == 2026
        assert date.month == 3
        assert date.day == 25

        date = engine._parse_date_from_filename("invalid.jsonl")
        assert date is None


class TestMounterMode:
    """Tests for MounterMode enum."""

    def test_mode_values(self):
        """Test mode enum values."""
        assert MounterMode.REPLAY.value == "replay"
        assert MounterMode.LIVE.value == "live"
        assert MounterMode.REBUILD.value == "rebuild"
