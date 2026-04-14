"""User repository for PostgreSQL operations."""

from datetime import datetime

from src.mounters.postgres.repositories.base import BaseRepository


class UserRepository(BaseRepository):
    """Repository for user-related database operations."""

    async def create_user(
        self,
        user_id: str,
        email: str,
        username: str,
        email_verified: bool,
        created_at: datetime,
    ) -> None:
        """Create a new user."""
        await self.execute(
            """
            INSERT INTO identity.users (id, email, username, is_active, email_verified, created_at)
            VALUES ($1, $2, $3, TRUE, $4, $5)
            ON CONFLICT (id) DO NOTHING
            """,
            user_id,
            email,
            username,
            email_verified,
            created_at,
        )

    async def add_role(self, user_id: str, role_id: int, assigned_at: datetime) -> None:
        """Add a role to a user."""
        await self.execute(
            """
            INSERT INTO identity.user_roles (user_id, role_id, assigned_at)
            VALUES ($1, $2, $3)
            ON CONFLICT (user_id, role_id) DO NOTHING
            """,
            user_id,
            role_id,
            assigned_at,
        )

    async def remove_role(self, user_id: str, role_id: int) -> None:
        """Remove a role from a user."""
        await self.execute(
            """
            DELETE FROM identity.user_roles
            WHERE user_id = $1 AND role_id = $2
            """,
            user_id,
            role_id,
        )

    async def verify_email(self, user_id: str) -> None:
        """Mark user email as verified."""
        await self.execute(
            """
            UPDATE identity.users
            SET email_verified = TRUE, updated_at = CURRENT_TIMESTAMP
            WHERE id = $1
            """,
            user_id,
        )

    async def update_timestamp(self, user_id: str) -> None:
        """Update user's updated_at timestamp."""
        await self.execute(
            """
            UPDATE identity.users
            SET updated_at = CURRENT_TIMESTAMP
            WHERE id = $1
            """,
            user_id,
        )

    async def set_two_factor(self, user_id: str, enabled: bool) -> None:
        """Enable or disable two-factor authentication."""
        await self.execute(
            """
            UPDATE identity.users
            SET two_factor_enabled = $2, updated_at = CURRENT_TIMESTAMP
            WHERE id = $1
            """,
            user_id,
            enabled,
        )

    async def set_lockout(
        self,
        user_id: str,
        lockout_end: datetime,
        failed_attempts: int,
    ) -> None:
        """Set account lockout."""
        await self.execute(
            """
            UPDATE identity.users
            SET lockout_end = $2, failed_login_attempts = $3, updated_at = CURRENT_TIMESTAMP
            WHERE id = $1
            """,
            user_id,
            lockout_end,
            failed_attempts,
        )

    async def deactivate(self, user_id: str) -> None:
        """Deactivate user account."""
        await self.execute(
            """
            UPDATE identity.users
            SET is_active = FALSE, updated_at = CURRENT_TIMESTAMP
            WHERE id = $1
            """,
            user_id,
        )

    async def link_external_login(
        self,
        user_id: str,
        provider: str,
        provider_key: str,
        linked_at: datetime,
    ) -> None:
        """Link an external login provider."""
        await self.execute(
            """
            INSERT INTO identity.external_logins (user_id, provider, provider_key, linked_at)
            VALUES ($1, $2, $3, $4)
            ON CONFLICT (provider, provider_key) DO UPDATE SET user_id = $1
            """,
            user_id,
            provider,
            provider_key,
            linked_at,
        )
