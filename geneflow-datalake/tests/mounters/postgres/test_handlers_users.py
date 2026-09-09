"""Tests for UsersHandler."""

from unittest.mock import AsyncMock, MagicMock

import pytest

from src.mounters.postgres.handlers.users import ROLE_MAP, UsersHandler


@pytest.fixture
def conn():
    c = MagicMock()
    c.execute = AsyncMock()
    return c


@pytest.fixture
def handler(conn):
    return UsersHandler(conn)


class TestUsersHandler:
    def test_event_mappings(self, handler):
        assert handler._event_mappings["UserRegisteredEvent"] == "handle_user_registered"
        assert (
            handler._event_mappings["UserRegisteredViaOAuthEvent"] == "handle_user_registered_oauth"
        )
        assert handler._event_mappings["UserEmailVerifiedEvent"] == "handle_email_verified"
        assert handler._event_mappings["UserPasswordChangedEvent"] == "handle_password_changed"
        assert (
            handler._event_mappings["PasswordResetRequestedEvent"]
            == "handle_password_reset_requested"
        )
        assert handler._event_mappings["UserTwoFactorEnabledEvent"] == "handle_2fa_enabled"
        assert handler._event_mappings["UserTwoFactorDisabledEvent"] == "handle_2fa_disabled"
        assert handler._event_mappings["TwoFactorCodeGeneratedEvent"] == "handle_2fa_code_generated"
        assert handler._event_mappings["UserLockedOutEvent"] == "handle_user_locked_out"
        assert handler._event_mappings["UserRoleAddedEvent"] == "handle_role_added"
        assert handler._event_mappings["UserRoleRemovedEvent"] == "handle_role_removed"
        assert handler._event_mappings["UserDeactivatedEvent"] == "handle_user_deactivated"
        assert handler._event_mappings["ExternalLoginLinkedEvent"] == "handle_external_login_linked"

    async def test_handle_user_registered_snake_case(self, handler, conn):
        await handler.handle_user_registered(
            {
                "user_id": "u1",
                "email": "e@x",
                "username": "name",
                "occurred_at": "2024-01-01T00:00:00Z",
            }
        )
        # Two calls: create_user + add_role
        assert conn.execute.await_count == 2

    async def test_handle_user_registered_pascal_case(self, handler, conn):
        await handler.handle_user_registered(
            {
                "UserId": "u1",
                "Email": "e@x",
                "Username": "name",
                "OccurredAt": "2024-01-01T00:00:00Z",
            }
        )
        assert conn.execute.await_count == 2

    async def test_handle_user_registered_oauth(self, handler, conn):
        await handler.handle_user_registered_oauth(
            {
                "user_id": "u1",
                "email": "e@x",
                "username": "name",
                "provider": "google",
                "provider_key": "abc",
                "occurred_at": "2024-01-01T00:00:00Z",
            }
        )
        # Three calls: create_user + add_role + link_external_login
        assert conn.execute.await_count == 3

    async def test_handle_email_verified(self, handler, conn):
        await handler.handle_email_verified({"user_id": "u1"})
        conn.execute.assert_awaited_once()

    async def test_handle_password_changed(self, handler, conn):
        await handler.handle_password_changed({"user_id": "u1"})
        conn.execute.assert_awaited_once()

    async def test_handle_password_reset_requested_noop(self, handler, conn):
        await handler.handle_password_reset_requested({"user_id": "u1"})
        conn.execute.assert_not_called()

    async def test_handle_2fa_enabled(self, handler, conn):
        await handler.handle_2fa_enabled({"user_id": "u1"})
        conn.execute.assert_awaited_once()

    async def test_handle_2fa_disabled(self, handler, conn):
        await handler.handle_2fa_disabled({"user_id": "u1"})
        conn.execute.assert_awaited_once()

    async def test_handle_2fa_code_generated_noop(self, handler, conn):
        await handler.handle_2fa_code_generated({"user_id": "u1"})
        conn.execute.assert_not_called()

    async def test_handle_user_locked_out(self, handler, conn):
        await handler.handle_user_locked_out(
            {
                "user_id": "u1",
                "lockout_end": "2024-12-31T00:00:00Z",
                "failed_attempts": 5,
            }
        )
        conn.execute.assert_awaited_once()

    async def test_handle_role_added_known_role(self, handler, conn):
        await handler.handle_role_added({"user_id": "u1", "role": "Admin"})
        conn.execute.assert_awaited_once()

    async def test_handle_role_added_unknown_role_defaults(self, handler, conn):
        await handler.handle_role_added({"user_id": "u1", "role": "Unknown"})
        # default role 1 should be used
        conn.execute.assert_awaited_once()
        args = conn.execute.await_args.args
        # The second arg should be role_id (1)
        assert 1 in args

    async def test_handle_role_removed(self, handler, conn):
        await handler.handle_role_removed({"user_id": "u1", "role": "SuperAdmin"})
        conn.execute.assert_awaited_once()

    async def test_handle_user_deactivated(self, handler, conn):
        await handler.handle_user_deactivated({"user_id": "u1"})
        conn.execute.assert_awaited_once()

    async def test_handle_external_login_linked(self, handler, conn):
        await handler.handle_external_login_linked(
            {"user_id": "u1", "provider": "google", "provider_key": "k"}
        )
        conn.execute.assert_awaited_once()

    def test_role_map_values(self):
        assert ROLE_MAP["User"] == 1
        assert ROLE_MAP["Admin"] == 2
        assert ROLE_MAP["SuperAdmin"] == 3
        assert ROLE_MAP[1] == 1
