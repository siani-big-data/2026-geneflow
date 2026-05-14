"""Tests for AlignmentsHandler."""

from unittest.mock import AsyncMock, MagicMock

import pytest

from src.mounters.postgres.handlers.alignments import AlignmentsHandler


@pytest.fixture
def conn():
    c = MagicMock()
    c.execute = AsyncMock()
    return c


@pytest.fixture
def handler(conn):
    return AlignmentsHandler(conn)


class TestAlignmentsHandler:
    def test_event_mappings(self, handler):
        assert handler._event_mappings["AlignmentCreated"] == "insert_alignment"
        assert handler._event_mappings["AlignmentTraceAdded"] == "insert_alignment_trace"
        assert handler._event_mappings["AlignmentCompleted"] == "update_alignment"

    async def test_insert_alignment(self, handler, conn):
        await handler.insert_alignment(
            {
                "id": "a1",
                "name": "n",
                "study_id": "s1",
                "created_by": "u1",
                "type_id": 1,
            }
        )
        conn.execute.assert_awaited_once()
        args = conn.execute.await_args.args
        assert "INSERT INTO alignments.alignments" in args[0]

    async def test_insert_alignment_trace(self, handler, conn):
        await handler.insert_alignment_trace(
            {"alignment_id": "a1", "trace_id": "t1", "sequence_order": 0}
        )
        conn.execute.assert_awaited_once()
        args = conn.execute.await_args.args
        assert "INSERT INTO alignments.alignment_traces" in args[0]

    async def test_update_alignment_all_fields(self, handler, conn):
        await handler.update_alignment(
            {
                "id": "a1",
                "status_id": 3,
                "alignment_length": 100,
                "identity_percentage": 99.0,
                "consensus_sequence": "ATCG",
                "completed_at": "2024-01-01T00:00:00Z",
            }
        )
        conn.execute.assert_awaited_once()
        args = conn.execute.await_args.args
        assert "UPDATE alignments.alignments" in args[0]
        assert "status_id = $1" in args[0]

    async def test_update_alignment_partial(self, handler, conn):
        await handler.update_alignment({"id": "a1", "status_id": 2})
        conn.execute.assert_awaited_once()

    async def test_update_alignment_no_fields_skipped(self, handler, conn):
        await handler.update_alignment({"id": "a1"})
        conn.execute.assert_not_called()
