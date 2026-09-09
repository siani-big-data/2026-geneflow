"""Tests for PostgresConnection."""

from unittest.mock import AsyncMock, MagicMock, patch

import pytest

from src.mounters.postgres.connection import PostgresConnection


@pytest.fixture
def conn():
    return PostgresConnection(dsn="postgresql://user:pass@localhost:5432/db")


@pytest.fixture
def mock_pool():
    pool = MagicMock()
    pool.execute = AsyncMock(return_value="OK")
    pool.executemany = AsyncMock(return_value=None)
    pool.fetch = AsyncMock(return_value=[{"a": 1}])
    pool.fetchrow = AsyncMock(return_value={"a": 1})
    pool.fetchval = AsyncMock(return_value=1)
    pool.close = AsyncMock()
    pool.acquire = MagicMock(return_value="acquired-context-manager")
    return pool


class TestPostgresConnection:
    async def test_connect_success(self, conn, mock_pool):
        with patch("asyncpg.create_pool", new=AsyncMock(return_value=mock_pool)) as mock_cp:
            await conn.connect()
            mock_cp.assert_awaited_once()
            assert conn._pool is mock_pool

    async def test_connect_failuREDACTED(self, conn):
        with patch("asyncpg.create_pool", new=AsyncMock(side_effect=RuntimeError("boom"))):
            with pytest.raises(RuntimeError):
                await conn.connect()

    async def test_close_when_pool_exists(self, conn, mock_pool):
        conn._pool = mock_pool
        await conn.close()
        mock_pool.close.assert_awaited_once()
        assert conn._pool is None

    async def test_close_when_no_pool(self, conn):
        # Should not raise even if pool is None
        await conn.close()
        assert conn._pool is None

    async def test_execute_no_pool_raises(self, conn):
        with pytest.raises(RuntimeError, match="not initialized"):
            await conn.execute("SELECT 1")

    async def test_execute_success(self, conn, mock_pool):
        conn._pool = mock_pool
        result = await conn.execute("SELECT $1", 1)
        assert result == "OK"
        mock_pool.execute.assert_awaited_once_with("SELECT $1", 1)

    async def test_executemany_no_pool_raises(self, conn):
        with pytest.raises(RuntimeError):
            await conn.executemany("Q", [])

    async def test_executemany_success(self, conn, mock_pool):
        conn._pool = mock_pool
        await conn.executemany("INSERT", [(1,), (2,)])
        mock_pool.executemany.assert_awaited_once()

    async def test_fetch_no_pool_raises(self, conn):
        with pytest.raises(RuntimeError):
            await conn.fetch("Q")

    async def test_fetch_success(self, conn, mock_pool):
        conn._pool = mock_pool
        res = await conn.fetch("SELECT *")
        assert res == [{"a": 1}]

    async def test_fetchrow_no_pool_raises(self, conn):
        with pytest.raises(RuntimeError):
            await conn.fetchrow("Q")

    async def test_fetchrow_success(self, conn, mock_pool):
        conn._pool = mock_pool
        res = await conn.fetchrow("SELECT *")
        assert res == {"a": 1}

    async def test_fetchval_no_pool_raises(self, conn):
        with pytest.raises(RuntimeError):
            await conn.fetchval("Q")

    async def test_fetchval_success(self, conn, mock_pool):
        conn._pool = mock_pool
        res = await conn.fetchval("SELECT 1")
        assert res == 1

    async def test_health_check_no_pool_returns_false(self, conn):
        result = await conn.health_check()
        assert result is False

    async def test_health_check_success(self, conn, mock_pool):
        conn._pool = mock_pool
        result = await conn.health_check()
        assert result is True
        mock_pool.fetchval.assert_awaited_once_with("SELECT 1")

    async def test_health_check_exception_returns_false(self, conn, mock_pool):
        mock_pool.fetchval = AsyncMock(side_effect=Exception("oops"))
        conn._pool = mock_pool
        result = await conn.health_check()
        assert result is False

    def test_get_connection_no_pool_raises(self, conn):
        with pytest.raises(RuntimeError):
            conn.get_connection()

    def test_get_connection_success(self, conn, mock_pool):
        conn._pool = mock_pool
        result = conn.get_connection()
        assert result == "acquired-context-manager"
        mock_pool.acquire.assert_called_once()
