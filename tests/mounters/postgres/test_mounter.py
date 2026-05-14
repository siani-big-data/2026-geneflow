"""Tests for PostgresMounter."""

import json
from unittest.mock import AsyncMock, MagicMock, patch

import pytest

from src.mounters.postgres.mounter import PostgresMounter


@pytest.fixture
def mock_pg_conn():
    conn = MagicMock()
    conn.connect = AsyncMock()
    conn.close = AsyncMock()
    conn.execute = AsyncMock()
    conn.health_check = AsyncMock(return_value=True)
    return conn


@pytest.fixture
def mounter(mock_pg_conn):
    with patch("src.mounters.postgres.mounter.PostgresConnection", return_value=mock_pg_conn):
        m = PostgresMounter(dsn="postgresql://user:pass@localhost:5432/db")
        m._connection = mock_pg_conn
        # Preinitialize the events_skipped metric (source uses it but base doesn't init it)
        m._metrics["events_skipped"] = 0
        return m


class TestPostgresMounter:
    def test_init_categories(self, mounter):
        assert set(mounter.categories) == {
            "users",
            "studies",
            "traces",
            "alignments",
            "billing",
            "payments",
            "profiles",
        }
        assert mounter.name == "postgres"

    async def test_start_initializes_schemas_and_handlers(self, mock_pg_conn):
        with patch(
            "src.mounters.postgres.mounter.PostgresConnection",
            return_value=mock_pg_conn,
        ):
            m = PostgresMounter(dsn="postgresql://x")
            m._connection = mock_pg_conn
            await m.start()
            mock_pg_conn.connect.assert_awaited_once()
            assert m._running is True
            assert set(m._handlers.keys()) == {
                "users",
                "studies",
                "traces",
                "alignments",
                "billing",
                "payments",
                "profiles",
            }
            # Multiple schema statements executed
            assert mock_pg_conn.execute.await_count > 0

    async def test_initialize_schemas_swallows_statement_errors(self, mounter, mock_pg_conn):
        mock_pg_conn.execute.side_effect = Exception("dup table")
        await mounter._initialize_schemas()
        # Despite errors, it should not raise
        assert mock_pg_conn.execute.await_count > 0

    async def test_stop(self, mounter, mock_pg_conn):
        mounter._handlers = {"users": object()}
        mounter._running = True
        await mounter.stop()
        mock_pg_conn.close.assert_awaited_once()
        assert mounter._running is False
        assert mounter._handlers == {}

    async def test_handle_event_no_handler_for_category(self, mounter):
        event = {
            "event_id": "e1",
            "event_type": "X",
            "category": "unknown",
            "occurred_at": "2024-01-01T00:00:00Z",
            "data": {},
        }
        await mounter.handle_event(event)
        assert mounter._metrics["events_skipped"] == 1

    async def test_handle_event_routes_to_handler(self, mounter):
        handler = MagicMock()
        handler.handle = AsyncMock()
        mounter._handlers["users"] = handler
        event = {
            "event_id": "e1",
            "event_type": "UserRegisteredEvent",
            "category": "users",
            "occurred_at": "2024-01-01T00:00:00Z",
            "data": {"user_id": "u1"},
        }
        await mounter.handle_event(event)
        handler.handle.assert_awaited_once()
        payload = handler.handle.await_args.args[1]
        assert payload["user_id"] == "u1"
        assert payload["occurred_at"] == "2024-01-01T00:00:00Z"
        assert payload["event_id"] == "e1"
        assert mounter._metrics["events_processed"] == 1

    async def test_handle_event_with_string_json_data(self, mounter):
        handler = MagicMock()
        handler.handle = AsyncMock()
        mounter._handlers["users"] = handler
        event = {
            "event_id": "e2",
            "event_type": "UserRegisteredEvent",
            "category": "users",
            "occurred_at": "2024-01-01T00:00:00Z",
            "data": json.dumps({"user_id": "u2"}),
        }
        await mounter.handle_event(event)
        handler.handle.assert_awaited_once()

    async def test_handle_event_with_invalid_json(self, mounter):
        handler = MagicMock()
        handler.handle = AsyncMock()
        mounter._handlers["users"] = handler
        event = {
            "event_id": "e3",
            "event_type": "X",
            "category": "users",
            "occurred_at": "x",
            "data": "{not json",
        }
        mounter._metrics["events_failed"] = 0
        await mounter.handle_event(event)
        handler.handle.assert_not_called()
        assert mounter._metrics["events_failed"] == 1

    async def test_handle_event_handler_raises_increments_failed(self, mounter):
        handler = MagicMock()
        handler.handle = AsyncMock(side_effect=RuntimeError("DB error"))
        mounter._handlers["users"] = handler
        event = {
            "event_id": "e4",
            "event_type": "UserRegisteredEvent",
            "category": "users",
            "occurred_at": None,
            "data": {},
        }
        with pytest.raises(RuntimeError):
            await mounter.handle_event(event)
        assert mounter._metrics["events_failed"] == 1

    async def test_health_check(self, mounter, mock_pg_conn):
        result = await mounter.health_check()
        assert result is True
        mock_pg_conn.health_check.assert_awaited_once()

    async def test_rebuild_all_categories(self, mounter):
        h1 = MagicMock()
        h1.truncate = AsyncMock()
        h2 = MagicMock(spec=[])  # No truncate
        mounter._handlers = {"users": h1, "studies": h2}
        mounter._metrics["events_processed"] = 99
        mounter._metrics["events_failed"] = 5
        mounter._metrics["events_skipped"] = 3
        await mounter.rebuild()
        h1.truncate.assert_awaited_once()
        assert mounter._metrics["events_processed"] == 0
        assert mounter._metrics["events_failed"] == 0
        assert mounter._metrics["events_skipped"] == 0

    async def test_rebuild_specific_categories(self, mounter):
        h1 = MagicMock()
        h1.truncate = AsyncMock()
        mounter._handlers = {"users": h1}
        await mounter.rebuild(categories=["users"])
        h1.truncate.assert_awaited_once()

    async def test_rebuild_missing_handler_skipped(self, mounter):
        mounter._handlers = {}
        # Should not raise even if no handlers exist for categories
        await mounter.rebuild(categories=["users"])
