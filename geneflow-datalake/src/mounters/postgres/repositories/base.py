"""Base repository for PostgreSQL operations."""

from src.mounters.postgres.connection import PostgresConnection


class BaseRepository:
    """Base class for PostgreSQL repositories."""

    def __init__(self, connection: PostgresConnection):
        self._connection = connection

    async def execute(self, query: str, *args) -> None:
        """Execute a query."""
        await self._connection.execute(query, *args)

    async def fetch_one(self, query: str, *args) -> dict | None:
        """Fetch a single row."""
        return await self._connection.fetchrow(query, *args)

    async def fetch_all(self, query: str, *args) -> list[dict]:
        """Fetch all rows."""
        return await self._connection.fetch(query, *args)
