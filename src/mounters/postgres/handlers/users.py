"""Users event handler for PostgreSQL."""

from src.mounters.postgres.handlers.base import BaseHandler


class UsersHandler(BaseHandler):
    """Handler for user-related events."""

    def __init__(self, connection):
        super().__init__(connection)
        self._event_mappings = {
            "UserRegistered": "insert",
            "UserUpdated": "update",
            "UserDeleted": "soft_delete",
        }

    async def insert(self, payload: dict) -> None:
        """Insert a new user."""
        await self._connection.execute(
            """INSERT INTO identity.users (id, email, username, password_hash, is_active, email_verified, created_at)
            VALUES ($1, $2, $3, $4, $5, $6, $7)""",
            payload.get("id"),
            payload.get("email"),
            payload.get("username"),
            payload.get("password_hash"),
            payload.get("is_active"),
            payload.get("email_verified"),
            payload.get("created_at"),
        )

    async def update(self, payload: dict) -> None:
        """Update an existing user."""
        user_id = payload.get("id")
        updates = []
        values = []
        idx = 1

        for key in ["email", "username", "is_active", "email_verified", "updated_at"]:
            if key in payload:
                updates.append(f"{key} = ${idx}")
                values.append(payload[key])
                idx += 1

        if updates:
            values.append(user_id)
            query = f"UPDATE identity.users SET {', '.join(updates)} WHERE id = ${idx}"
            await self._connection.execute(query, *values)

    async def soft_delete(self, payload: dict) -> None:
        """Soft delete a user."""
        await self._connection.execute(
            "UPDATE identity.users SET is_deleted = TRUE, deleted_at = $1, deleted_by = $2 WHERE id = $3",
            payload.get("deleted_at"),
            payload.get("deleted_by"),
            payload.get("id"),
        )
