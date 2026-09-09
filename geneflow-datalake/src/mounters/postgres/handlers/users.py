"""Users event handler for PostgreSQL."""

from src.mounters.postgres.handlers.base import BaseHandler
from src.mounters.postgres.repositories import UserRepository
from src.mounters.postgres.transformers import PayloadTransformer

ROLE_MAP = {
    "User": 1,
    "user": 1,
    1: 1,
    "Admin": 2,
    "admin": 2,
    2: 2,
    "SuperAdmin": 3,
    "superadmin": 3,
    3: 3,
}


class UsersHandler(BaseHandler):
    """Handler for user-related events from Identity bounded context."""

    def __init__(self, connection):
        super().__init__(connection)
        self._repo = UserRepository(connection)
        self._transformer = PayloadTransformer()
        self._event_mappings = {
            "UserRegisteredEvent": "handle_user_registered",
            "UserRegisteredViaOAuthEvent": "handle_user_registered_oauth",
            "UserEmailVerifiedEvent": "handle_email_verified",
            "UserPasswordChangedEvent": "handle_password_changed",
            "PasswordResetRequestedEvent": "handle_password_reset_requested",
            "UserTwoFactorEnabledEvent": "handle_2fa_enabled",
            "UserTwoFactorDisabledEvent": "handle_2fa_disabled",
            "TwoFactorCodeGeneratedEvent": "handle_2fa_code_generated",
            "UserLockedOutEvent": "handle_user_locked_out",
            "UserRoleAddedEvent": "handle_role_added",
            "UserRoleRemovedEvent": "handle_role_removed",
            "UserDeactivatedEvent": "handle_user_deactivated",
            "ExternalLoginLinkedEvent": "handle_external_login_linked",
        }

    async def handle_user_registered(self, payload: dict) -> None:
        """Handle UserRegisteredEvent."""
        user_id = self._transformer.get_string(payload, "user_id", "UserId")
        email = self._transformer.get_string(payload, "email", "Email")
        username = self._transformer.get_string(payload, "username", "Username")
        occurred_at = self._transformer.get_datetime(payload, "occurred_at", "OccurredAt")

        await self._repo.create_user(user_id, email, username, False, occurred_at)
        await self._repo.add_role(user_id, 1, occurred_at)

    async def handle_user_registered_oauth(self, payload: dict) -> None:
        """Handle UserRegisteredViaOAuthEvent."""
        user_id = self._transformer.get_string(payload, "user_id", "UserId")
        email = self._transformer.get_string(payload, "email", "Email")
        username = self._transformer.get_string(payload, "username", "Username")
        provider = self._transformer.get_string(payload, "provider", "Provider")
        provider_key = self._transformer.get_string(payload, "provider_key", "ProviderKey")
        occurred_at = self._transformer.get_datetime(payload, "occurred_at", "OccurredAt")

        await self._repo.create_user(user_id, email, username, True, occurred_at)
        await self._repo.add_role(user_id, 1, occurred_at)
        await self._repo.link_external_login(user_id, provider, provider_key, occurred_at)

    async def handle_email_verified(self, payload: dict) -> None:
        """Handle UserEmailVerifiedEvent."""
        user_id = self._transformer.get_string(payload, "user_id", "UserId")
        await self._repo.verify_email(user_id)

    async def handle_password_changed(self, payload: dict) -> None:
        """Handle UserPasswordChangedEvent."""
        user_id = self._transformer.get_string(payload, "user_id", "UserId")
        await self._repo.update_timestamp(user_id)

    async def handle_password_reset_requested(self, payload: dict) -> None:
        """Handle PasswordResetRequestedEvent - notification only."""
        pass

    async def handle_2fa_enabled(self, payload: dict) -> None:
        """Handle UserTwoFactorEnabledEvent."""
        user_id = self._transformer.get_string(payload, "user_id", "UserId")
        await self._repo.set_two_factor(user_id, True)

    async def handle_2fa_disabled(self, payload: dict) -> None:
        """Handle UserTwoFactorDisabledEvent."""
        user_id = self._transformer.get_string(payload, "user_id", "UserId")
        await self._repo.set_two_factor(user_id, False)

    async def handle_2fa_code_generated(self, payload: dict) -> None:
        """Handle TwoFactorCodeGeneratedEvent - notification only."""
        pass

    async def handle_user_locked_out(self, payload: dict) -> None:
        """Handle UserLockedOutEvent."""
        user_id = self._transformer.get_string(payload, "user_id", "UserId")
        lockout_end = self._transformer.get_datetime(payload, "lockout_end", "LockoutEnd")
        failed_attempts = self._transformer.get_int(payload, "failed_attempts", "FailedAttempts")
        await self._repo.set_lockout(user_id, lockout_end, failed_attempts)

    async def handle_role_added(self, payload: dict) -> None:
        """Handle UserRoleAddedEvent."""
        user_id = self._transformer.get_string(payload, "user_id", "UserId")
        role = self._transformer.get_field(payload, "role", "Role")
        role_id = ROLE_MAP.get(role, 1)
        await self._repo.add_role(user_id, role_id, None)

    async def handle_role_removed(self, payload: dict) -> None:
        """Handle UserRoleRemovedEvent."""
        user_id = self._transformer.get_string(payload, "user_id", "UserId")
        role = self._transformer.get_field(payload, "role", "Role")
        role_id = ROLE_MAP.get(role, 1)
        await self._repo.remove_role(user_id, role_id)

    async def handle_user_deactivated(self, payload: dict) -> None:
        """Handle UserDeactivatedEvent."""
        user_id = self._transformer.get_string(payload, "user_id", "UserId")
        await self._repo.deactivate(user_id)

    async def handle_external_login_linked(self, payload: dict) -> None:
        """Handle ExternalLoginLinkedEvent."""
        user_id = self._transformer.get_string(payload, "user_id", "UserId")
        provider = self._transformer.get_string(payload, "provider", "Provider")
        provider_key = self._transformer.get_string(payload, "provider_key", "ProviderKey")
        await self._repo.link_external_login(user_id, provider, provider_key, None)
