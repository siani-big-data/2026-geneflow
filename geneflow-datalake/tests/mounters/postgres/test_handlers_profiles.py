"""Tests for ProfilesHandler."""

from datetime import datetime
from unittest.mock import AsyncMock, MagicMock

import pytest

from src.mounters.postgres.handlers.profiles import ProfilesHandler


@pytest.fixture
def conn():
    c = MagicMock()
    c.execute = AsyncMock()
    return c


@pytest.fixture
def handler(conn):
    return ProfilesHandler(conn)


class TestProfilesHandler:
    def test_event_mappings(self, handler):
        assert handler._event_mappings["ProfileCreatedEvent"] == "handle_profile_created"
        assert handler._event_mappings["ProfileUpdatedEvent"] == "handle_profile_updated"
        assert handler._event_mappings["ProfilePhotoUpdatedEvent"] == "handle_photo_updated"

    async def test_handle_profile_created_snake_case_str_date(self, handler, conn):
        await handler.handle_profile_created(
            {
                "profile_id": "p1",
                "user_id": "u1",
                "first_name": "Alice",
                "occurred_at": "2024-01-01T00:00:00Z",
            }
        )
        conn.execute.assert_awaited_once()
        args = conn.execute.await_args.args
        assert "INSERT INTO profiles.profiles" in args[0]
        assert args[1] == "p1"
        assert args[2] == "u1"
        assert isinstance(args[4], datetime)

    async def test_handle_profile_created_pascal_case(self, handler, conn):
        await handler.handle_profile_created(
            {
                "ProfileId": "p1",
                "UserId": "u1",
                "FirstName": "Bob",
                "OccurredAt": "2024-01-01T00:00:00Z",
            }
        )
        conn.execute.assert_awaited_once()

    async def test_handle_profile_created_dict_value_objects(self, handler, conn):
        await handler.handle_profile_created(
            {
                "profile_id": {"Value": "p1-from-dict"},
                "user_id": {"Value": "u1-from-dict"},
                "first_name": "Carol",
                "occurred_at": "2024-01-01T00:00:00Z",
            }
        )
        args = conn.execute.await_args.args
        assert args[1] == "p1-from-dict"
        assert args[2] == "u1-from-dict"

    async def test_handle_profile_created_dict_no_value_falls_back_to_str(self, handler, conn):
        # dict without "Value" key falls back to str(dict)
        payload = {
            "profile_id": {"NotValue": "x"},
            "user_id": {"NotValue": "y"},
            "first_name": "X",
            "occurred_at": None,
        }
        await handler.handle_profile_created(payload)
        args = conn.execute.await_args.args
        assert isinstance(args[1], str)
        assert isinstance(args[2], str)

    async def test_handle_profile_created_datetime_passthrough(self, handler, conn):
        dt = datetime(2024, 1, 1)
        await handler.handle_profile_created(
            {
                "profile_id": "p1",
                "user_id": "u1",
                "first_name": "X",
                "occurred_at": dt,
            }
        )
        args = conn.execute.await_args.args
        assert args[4] == dt

    async def test_handle_profile_updated(self, handler, conn):
        await handler.handle_profile_updated({"profile_id": "p1"})
        conn.execute.assert_awaited_once()
        args = conn.execute.await_args.args
        assert "UPDATE profiles.profiles" in args[0]
        assert args[1] == "p1"

    async def test_handle_profile_updated_dict_id(self, handler, conn):
        await handler.handle_profile_updated({"ProfileId": {"Value": "p1"}})
        conn.execute.assert_awaited_once()
        args = conn.execute.await_args.args
        assert args[1] == "p1"

    async def test_handle_photo_updated(self, handler, conn):
        await handler.handle_photo_updated({"profile_id": "p1", "photo_url": "https://x"})
        conn.execute.assert_awaited_once()
        args = conn.execute.await_args.args
        assert "UPDATE profiles.profiles" in args[0]
        assert args[1] == "p1"
        assert args[2] == "https://x"

    async def test_handle_photo_updated_pascal(self, handler, conn):
        await handler.handle_photo_updated({"ProfileId": {"Value": "p2"}, "PhotoUrl": "https://y"})
        args = conn.execute.await_args.args
        assert args[1] == "p2"
        assert args[2] == "https://y"

    async def test_truncate(self, handler, conn):
        await handler.truncate()
        conn.execute.assert_awaited_once()
        args = conn.execute.await_args.args
        assert "TRUNCATE" in args[0]
