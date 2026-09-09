"""Tests for StudiesHandler."""

from unittest.mock import AsyncMock, MagicMock

import pytest

from src.mounters.postgres.handlers.studies import StudiesHandler


@pytest.fixture
def conn():
    c = MagicMock()
    c.execute = AsyncMock()
    return c


@pytest.fixture
def handler(conn):
    return StudiesHandler(conn)


class TestStudiesHandler:
    def test_event_mappings(self, handler):
        assert handler._event_mappings["StudyCreated"] == "insert_study"
        assert handler._event_mappings["StudyUpdated"] == "update_study"
        assert handler._event_mappings["StudyDeleted"] == "soft_delete_study"
        assert handler._event_mappings["MemberAdded"] == "insert_member"
        assert handler._event_mappings["MemberRemoved"] == "delete_member"
        assert handler._event_mappings["InvitationCreated"] == "insert_invitation"

    async def test_insert_study(self, handler, conn):
        await handler.insert_study(
            {
                "id": "s1",
                "title": "T",
                "description": "D",
                "owner_id": "u1",
                "created_at": "2024-01-01T00:00:00Z",
            }
        )
        conn.execute.assert_awaited_once()
        args = conn.execute.await_args.args
        assert "INSERT INTO studies.studies" in args[0]

    async def test_update_study_noop(self, handler, conn):
        await handler.update_study({"id": "s1"})
        conn.execute.assert_not_called()

    async def test_soft_delete_study_noop(self, handler, conn):
        await handler.soft_delete_study({"id": "s1"})
        conn.execute.assert_not_called()

    async def test_insert_member(self, handler, conn):
        await handler.insert_member(
            {"study_id": "s1", "user_id": "u1", "role_id": 2, "invited_by": "u0"}
        )
        conn.execute.assert_awaited_once()
        args = conn.execute.await_args.args
        assert "INSERT INTO studies.members" in args[0]

    async def test_delete_member(self, handler, conn):
        await handler.delete_member({"study_id": "s1", "user_id": "u1"})
        conn.execute.assert_awaited_once()
        args = conn.execute.await_args.args
        assert "DELETE FROM studies.members" in args[0]

    async def test_insert_invitation(self, handler, conn):
        await handler.insert_invitation(
            {
                "id": "i1",
                "study_id": "s1",
                "email": "a@b",
                "token": "t",
                "invited_by": "u",
                "expires_at": "2024-12-31T00:00:00Z",
            }
        )
        conn.execute.assert_awaited_once()
        args = conn.execute.await_args.args
        assert "INSERT INTO studies.invitations" in args[0]
