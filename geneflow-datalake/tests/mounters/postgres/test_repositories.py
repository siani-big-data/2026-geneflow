"""Tests for BaseRepository and UserRepository."""

from datetime import datetime
from unittest.mock import AsyncMock, MagicMock

import pytest

from src.mounters.postgres.repositories.base import BaseRepository
from src.mounters.postgres.repositories.user_repository import UserRepository


@pytest.fixture
def conn():
    c = MagicMock()
    c.execute = AsyncMock(return_value="OK")
    c.fetchrow = AsyncMock(return_value={"id": 1})
    c.fetch = AsyncMock(return_value=[{"id": 1}, {"id": 2}])
    return c


class TestBaseRepository:
    async def test_execute_delegates(self, conn):
        repo = BaseRepository(conn)
        await repo.execute("Q", 1, 2)
        conn.execute.assert_awaited_once_with("Q", 1, 2)

    async def test_fetch_one_delegates(self, conn):
        repo = BaseRepository(conn)
        result = await repo.fetch_one("Q", 1)
        assert result == {"id": 1}
        conn.fetchrow.assert_awaited_once_with("Q", 1)

    async def test_fetch_all_delegates(self, conn):
        repo = BaseRepository(conn)
        result = await repo.fetch_all("Q")
        assert result == [{"id": 1}, {"id": 2}]
        conn.fetch.assert_awaited_once_with("Q")


class TestUserRepository:
    @pytest.fixture
    def repo(self, conn):
        return UserRepository(conn)

    async def test_create_user(self, repo, conn):
        dt = datetime(2024, 1, 1)
        await repo.create_user("u1", "e@x", "name", True, dt)
        conn.execute.assert_awaited_once()
        args = conn.execute.await_args.args
        assert "INSERT INTO identity.users" in args[0]
        assert args[1] == "u1"
        assert args[2] == "e@x"
        assert args[3] == "name"
        assert args[4] is True
        assert args[5] == dt

    async def test_add_role(self, repo, conn):
        dt = datetime(2024, 1, 1)
        await repo.add_role("u1", 2, dt)
        args = conn.execute.await_args.args
        assert "INSERT INTO identity.user_roles" in args[0]
        assert args[1] == "u1"
        assert args[2] == 2
        assert args[3] == dt

    async def test_remove_role(self, repo, conn):
        await repo.remove_role("u1", 2)
        args = conn.execute.await_args.args
        assert "DELETE FROM identity.user_roles" in args[0]
        assert args[1] == "u1"
        assert args[2] == 2

    async def test_verify_email(self, repo, conn):
        await repo.verify_email("u1")
        args = conn.execute.await_args.args
        assert "email_verified = TRUE" in args[0]
        assert args[1] == "u1"

    async def test_update_timestamp(self, repo, conn):
        await repo.update_timestamp("u1")
        args = conn.execute.await_args.args
        assert "updated_at = CURRENT_TIMESTAMP" in args[0]
        assert args[1] == "u1"

    async def test_set_two_factor_enabled(self, repo, conn):
        await repo.set_two_factor("u1", True)
        args = conn.execute.await_args.args
        assert "two_factor_enabled" in args[0]
        assert args[1] == "u1"
        assert args[2] is True

    async def test_set_two_factor_disabled(self, repo, conn):
        await repo.set_two_factor("u1", False)
        args = conn.execute.await_args.args
        assert args[2] is False

    async def test_set_lockout(self, repo, conn):
        dt = datetime(2024, 1, 1)
        await repo.set_lockout("u1", dt, 3)
        args = conn.execute.await_args.args
        assert "lockout_end" in args[0]
        assert args[1] == "u1"
        assert args[2] == dt
        assert args[3] == 3

    async def test_deactivate(self, repo, conn):
        await repo.deactivate("u1")
        args = conn.execute.await_args.args
        assert "is_active = FALSE" in args[0]
        assert args[1] == "u1"

    async def test_link_external_login(self, repo, conn):
        dt = datetime(2024, 1, 1)
        await repo.link_external_login("u1", "google", "key1", dt)
        args = conn.execute.await_args.args
        assert "INSERT INTO identity.external_logins" in args[0]
        assert args[1] == "u1"
        assert args[2] == "google"
        assert args[3] == "key1"
        assert args[4] == dt
