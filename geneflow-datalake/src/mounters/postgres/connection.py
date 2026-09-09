"""PostgreSQL async connection wrapper using asyncpg."""

import asyncpg
import structlog

logger = structlog.get_logger()


class PostgresConnection:
    """Async connection wrapper for PostgreSQL using asyncpg."""

    def __init__(self, dsn: str):
        self._dsn = dsn
        self._pool: asyncpg.Pool | None = None

    async def connect(self) -> None:
        """Establish connection pool to PostgreSQL."""
        try:
            self._pool = await asyncpg.create_pool(
                dsn=self._dsn,
                min_size=2,
                max_size=10,
                command_timeout=60,
            )
            logger.info("postgres_connected", dsn=self._dsn[:30] + "...")
        except Exception as e:
            logger.error("postgres_connection_failed", error=str(e))
            raise

    async def close(self) -> None:
        """Close the connection pool."""
        if self._pool:
            await self._pool.close()
            self._pool = None
        logger.info("postgres_disconnected")

    async def execute(self, query: str, *args) -> str:
        """Execute a query."""
        if not self._pool:
            raise RuntimeError("PostgreSQL pool not initialized")
        return await self._pool.execute(query, *args)

    async def executemany(self, query: str, args: list) -> None:
        """Execute a query for multiple rows."""
        if not self._pool:
            raise RuntimeError("PostgreSQL pool not initialized")
        await self._pool.executemany(query, args)

    async def fetch(self, query: str, *args) -> list:
        """Fetch results from a query."""
        if not self._pool:
            raise RuntimeError("PostgreSQL pool not initialized")
        return await self._pool.fetch(query, *args)

    async def fetchrow(self, query: str, *args):
        """Fetch a single row from a query."""
        if not self._pool:
            raise RuntimeError("PostgreSQL pool not initialized")
        return await self._pool.fetchrow(query, *args)

    async def fetchval(self, query: str, *args):
        """Fetch a single value from a query."""
        if not self._pool:
            raise RuntimeError("PostgreSQL pool not initialized")
        return await self._pool.fetchval(query, *args)

    async def health_check(self) -> bool:
        """Check if PostgreSQL connection is healthy."""
        if not self._pool:
            return False
        try:
            await self._pool.fetchval("SELECT 1")
            return True
        except Exception as e:
            logger.error("postgres_health_check_failed", error=str(e))
            return False

    def get_connection(self):
        """Get a raw connection for transaction support."""
        if not self._pool:
            raise RuntimeError("PostgreSQL pool not initialized")
        return self._pool.acquire()
