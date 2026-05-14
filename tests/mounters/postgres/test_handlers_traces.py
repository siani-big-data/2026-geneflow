"""Tests for TracesHandler."""

from unittest.mock import AsyncMock, MagicMock

import pytest

from src.mounters.postgres.handlers.traces import TracesHandler


@pytest.fixture
def conn():
    c = MagicMock()
    c.execute = AsyncMock()
    return c


@pytest.fixture
def handler(conn):
    return TracesHandler(conn)


class TestTracesHandler:
    def test_event_mappings(self, handler):
        assert handler._event_mappings["TraceUploaded"] == "insert_trace"
        assert handler._event_mappings["TraceProcessed"] == "update_trace"
        assert handler._event_mappings["TraceDeleted"] == "soft_delete_trace"
        assert handler._event_mappings["AnnotationCreated"] == "insert_annotation"
        assert handler._event_mappings["AnnotationDeleted"] == "delete_annotation"

    async def test_insert_trace(self, handler, conn):
        await handler.insert_trace(
            {
                "id": "t1",
                "name": "T",
                "study_id": "s1",
                "uploaded_by": "u1",
                "file_name": "f.ab1",
                "format_id": 1,
                "size_bytes": 100,
            }
        )
        conn.execute.assert_awaited_once()
        args = conn.execute.await_args.args
        assert "INSERT INTO traces.traces" in args[0]

    async def test_update_trace_all_fields(self, handler, conn):
        await handler.update_trace(
            {
                "id": "t1",
                "status_id": 3,
                "total_bases": 100,
                "average_quality_score": 30.5,
                "processed_at": "2024-01-01T00:00:00Z",
            }
        )
        conn.execute.assert_awaited_once()
        args = conn.execute.await_args.args
        assert "UPDATE traces.traces" in args[0]
        assert "status_id = $1" in args[0]

    async def test_update_trace_partial(self, handler, conn):
        await handler.update_trace({"id": "t1", "status_id": 2})
        conn.execute.assert_awaited_once()

    async def test_update_trace_no_fields_skipped(self, handler, conn):
        await handler.update_trace({"id": "t1"})
        conn.execute.assert_not_called()

    async def test_soft_delete_trace_noop(self, handler, conn):
        await handler.soft_delete_trace({"id": "t1"})
        conn.execute.assert_not_called()

    async def test_insert_annotation(self, handler, conn):
        await handler.insert_annotation(
            {
                "id": "a1",
                "trace_id": "t1",
                "type_id": 1,
                "label": "L",
                "start_position": 0,
                "end_position": 10,
                "created_by": "u",
            }
        )
        conn.execute.assert_awaited_once()
        args = conn.execute.await_args.args
        assert "INSERT INTO traces.annotations" in args[0]

    async def test_delete_annotation_noop(self, handler, conn):
        await handler.delete_annotation({"id": "a1"})
        conn.execute.assert_not_called()
