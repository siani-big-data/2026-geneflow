"""Users event handler for PostgreSQL.

Handles all Identity domain events from the backend:
- User registration (standard and OAuth)
- Email verification
- Password changes
- Two-factor authentication
- Account lockout
- Role management
- External login linking
- Account deactivation
"""

from datetime import datetime

from src.mounters.postgres.handlers.base import BaseHandler


class UsersHandler(BaseHandler):
    """Handler for user-related events from Identity bounded context."""

    def __init__(self, connection):
        super().__init__(connection)
        self._event_mappings = {
            # Registration
            "UserRegisteredEvent": "handle_user_registered",
            "UserRegisteredViaOAuthEvent": "handle_user_registered_oauth",

            # Email verification
            "UserEmailVerifiedEvent": "handle_email_verified",

            # Password
            "UserPasswordChangedEvent": "handle_password_changed",
            "PasswordResetRequestedEvent": "handle_password_reset_requested",

            # Two-factor authentication
            "UserTwoFactorEnabledEvent": "handle_2fa_enabled",
            "UserTwoFactorDisabledEvent": "handle_2fa_disabled",
            "TwoFactorCodeGeneratedEvent": "handle_2fa_code_generated",

            # Account lockout
            "UserLockedOutEvent": "handle_user_locked_out",

            # Roles
            "UserRoleAddedEvent": "handle_role_added",
            "UserRoleRemovedEvent": "handle_role_removed",

            # Account status
            "UserDeactivatedEvent": "handle_user_deactivated",

            # External logins (OAuth)
            "ExternalLoginLinkedEvent": "handle_external_login_linked",
        }

    # =========================================================================
    # REGISTRATION EVENTS
    # =========================================================================

    async def handle_user_registered(self, payload: dict) -> None:
        """Handle UserRegisteredEvent - Insert new user with standard registration."""
        user_id = payload.get("user_id") or payload.get("UserId")
        email = payload.get("email") or payload.get("Email")
        username = payload.get("username") or payload.get("Username")
        occurred_at = payload.get("occurred_at") or payload.get("OccurredAt")

        # Parse timestamp if string
        if isinstance(occurred_at, str):
            occurred_at = datetime.fromisoformat(occurred_at.replace("Z", "+00:00"))

        await self._connection.execute(
            """
            INSERT INTO identity.users (id, email, username, is_active, email_verified, created_at)
            VALUES ($1, $2, $3, TRUE, FALSE, $4)
            ON CONFLICT (id) DO NOTHING
            """,
            user_id,
            email,
            username,
            occurred_at,
        )

        # Add default 'User' role (role_id = 1)
        await self._connection.execute(
            """
            INSERT INTO identity.user_roles (user_id, role_id, assigned_at)
            VALUES ($1, 1, $2)
            ON CONFLICT (user_id, role_id) DO NOTHING
            """,
            user_id,
            occurred_at,
        )

    async def handle_user_registered_oauth(self, payload: dict) -> None:
        """Handle UserRegisteredViaOAuthEvent - Insert new user from OAuth provider."""
        user_id = payload.get("user_id") or payload.get("UserId")
        email = payload.get("email") or payload.get("Email")
        username = payload.get("username") or payload.get("Username")
        provider = payload.get("provider") or payload.get("Provider")
        provider_key = payload.get("provider_key") or payload.get("ProviderKey")
        occurred_at = payload.get("occurred_at") or payload.get("OccurredAt")

        if isinstance(occurred_at, str):
            occurred_at = datetime.fromisoformat(occurred_at.replace("Z", "+00:00"))

        # OAuth users are automatically verified
        await self._connection.execute(
            """
            INSERT INTO identity.users (id, email, username, is_active, email_verified, created_at)
            VALUES ($1, $2, $3, TRUE, TRUE, $4)
            ON CONFLICT (id) DO NOTHING
            """,
            user_id,
            email,
            username,
            occurred_at,
        )

        # Add default 'User' role
        await self._connection.execute(
            """
            INSERT INTO identity.user_roles (user_id, role_id, assigned_at)
            VALUES ($1, 1, $2)
            ON CONFLICT (user_id, role_id) DO NOTHING
            """,
            user_id,
            occurred_at,
        )

        # Link external login
        await self._connection.execute(
            """
            INSERT INTO identity.external_logins (user_id, provider, provider_key, linked_at)
            VALUES ($1, $2, $3, $4)
            ON CONFLICT (provider, provider_key) DO NOTHING
            """,
            user_id,
            provider,
            provider_key,
            occurred_at,
        )

    # =========================================================================
    # EMAIL VERIFICATION
    # =========================================================================

    async def handle_email_verified(self, payload: dict) -> None:
        """Handle UserEmailVerifiedEvent - Mark email as verified."""
        user_id = payload.get("user_id") or payload.get("UserId")

        await self._connection.execute(
            """
            UPDATE identity.users
            SET email_verified = TRUE, updated_at = CURRENT_TIMESTAMP
            WHERE id = $1
            """,
            user_id,
        )

    # =========================================================================
    # PASSWORD EVENTS
    # =========================================================================

    async def handle_password_changed(self, payload: dict) -> None:
        """Handle UserPasswordChangedEvent - Update timestamp (password hash not stored in events)."""
        user_id = payload.get("user_id") or payload.get("UserId")

        await self._connection.execute(
            """
            UPDATE identity.users
            SET updated_at = CURRENT_TIMESTAMP
            WHERE id = $1
            """,
            user_id,
        )

    async def handle_password_reset_requested(self, payload: dict) -> None:
        """Handle PasswordResetRequestedEvent - No database update needed, just for audit/notifications."""
        # This event is primarily for email notifications, not data projection
        pass

    # =========================================================================
    # TWO-FACTOR AUTHENTICATION
    # =========================================================================

    async def handle_2fa_enabled(self, payload: dict) -> None:
        """Handle UserTwoFactorEnabledEvent - Enable 2FA for user."""
        user_id = payload.get("user_id") or payload.get("UserId")

        await self._connection.execute(
            """
            UPDATE identity.users
            SET two_factor_enabled = TRUE, updated_at = CURRENT_TIMESTAMP
            WHERE id = $1
            """,
            user_id,
        )

    async def handle_2fa_disabled(self, payload: dict) -> None:
        """Handle UserTwoFactorDisabledEvent - Disable 2FA for user."""
        user_id = payload.get("user_id") or payload.get("UserId")

        await self._connection.execute(
            """
            UPDATE identity.users
            SET two_factor_enabled = FALSE, updated_at = CURRENT_TIMESTAMP
            WHERE id = $1
            """,
            user_id,
        )

    async def handle_2fa_code_generated(self, payload: dict) -> None:
        """Handle TwoFactorCodeGeneratedEvent - No storage needed, for notifications only."""
        # This event is for sending the 2FA code via email, not for data projection
        pass

    # =========================================================================
    # ACCOUNT LOCKOUT
    # =========================================================================

    async def handle_user_locked_out(self, payload: dict) -> None:
        """Handle UserLockedOutEvent - Lock user account temporarily."""
        user_id = payload.get("user_id") or payload.get("UserId")
        lockout_end = payload.get("lockout_end") or payload.get("LockoutEnd")
        failed_attempts = payload.get("failed_attempts") or payload.get("FailedAttempts")

        if isinstance(lockout_end, str):
            lockout_end = datetime.fromisoformat(lockout_end.replace("Z", "+00:00"))

        await self._connection.execute(
            """
            UPDATE identity.users
            SET lockout_end = $2, failed_login_attempts = $3, updated_at = CURRENT_TIMESTAMP
            WHERE id = $1
            """,
            user_id,
            lockout_end,
            failed_attempts,
        )

    # =========================================================================
    # ROLE MANAGEMENT
    # =========================================================================

    async def handle_role_added(self, payload: dict) -> None:
        """Handle UserRoleAddedEvent - Add role to user."""
        user_id = payload.get("user_id") or payload.get("UserId")
        role = payload.get("role") or payload.get("Role")

        # Convert role name to ID if needed
        role_id = self._get_role_id(role)

        await self._connection.execute(
            """
            INSERT INTO identity.user_roles (user_id, role_id, assigned_at)
            VALUES ($1, $2, CURRENT_TIMESTAMP)
            ON CONFLICT (user_id, role_id) DO NOTHING
            """,
            user_id,
            role_id,
        )

    async def handle_role_removed(self, payload: dict) -> None:
        """Handle UserRoleRemovedEvent - Remove role from user."""
        user_id = payload.get("user_id") or payload.get("UserId")
        role = payload.get("role") or payload.get("Role")

        role_id = self._get_role_id(role)

        await self._connection.execute(
            """
            DELETE FROM identity.user_roles
            WHERE user_id = $1 AND role_id = $2
            """,
            user_id,
            role_id,
        )

    def _get_role_id(self, role) -> int:
        """Convert role name or value to role ID."""
        role_map = {
            "User": 1, "user": 1, 1: 1,
            "Admin": 2, "admin": 2, 2: 2,
            "SuperAdmin": 3, "superadmin": 3, 3: 3,
        }
        return role_map.get(role, 1)  # Default to User role

    # =========================================================================
    # ACCOUNT STATUS
    # =========================================================================

    async def handle_user_deactivated(self, payload: dict) -> None:
        """Handle UserDeactivatedEvent - Deactivate user account."""
        user_id = payload.get("user_id") or payload.get("UserId")

        await self._connection.execute(
            """
            UPDATE identity.users
            SET is_active = FALSE, updated_at = CURRENT_TIMESTAMP
            WHERE id = $1
            """,
            user_id,
        )

    # =========================================================================
    # EXTERNAL LOGINS (OAuth)
    # =========================================================================

    async def handle_external_login_linked(self, payload: dict) -> None:
        """Handle ExternalLoginLinkedEvent - Link OAuth provider to existing user."""
        user_id = payload.get("user_id") or payload.get("UserId")
        provider = payload.get("provider") or payload.get("Provider")
        provider_key = payload.get("provider_key") or payload.get("ProviderKey")

        await self._connection.execute(
            """
            INSERT INTO identity.external_logins (user_id, provider, provider_key, linked_at)
            VALUES ($1, $2, $3, CURRENT_TIMESTAMP)
            ON CONFLICT (provider, provider_key) DO UPDATE SET user_id = $1
            """,
            user_id,
            provider,
            provider_key,
        )
