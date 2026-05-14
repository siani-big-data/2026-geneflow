"""Profiles event handler for PostgreSQL.

Handles all Profile domain events from the backend:
- Profile creation (automatic on user registration)
- Profile updates (basic info, research identifiers)
- Profile photo updates
"""

from datetime import datetime

from src.mounters.postgres.handlers.base import BaseHandler


class ProfilesHandler(BaseHandler):
    """Handler for profile-related events from Profiles bounded context."""

    def __init__(self, connection):
        super().__init__(connection)
        self._event_mappings = {
            "ProfileCreatedEvent": "handle_profile_created",
            "ProfileUpdatedEvent": "handle_profile_updated",
            "ProfilePhotoUpdatedEvent": "handle_photo_updated",
        }

    async def handle_profile_created(self, payload: dict) -> None:
        """Handle ProfileCreatedEvent - Insert new profile."""
        profile_id = payload.get("profile_id") or payload.get("ProfileId")
        user_id = payload.get("user_id") or payload.get("UserId")
        first_name = payload.get("first_name") or payload.get("FirstName")
        occurred_at = payload.get("occurred_at") or payload.get("OccurredAt")

        if isinstance(profile_id, dict):
            profile_id = profile_id.get("Value") or str(profile_id)
        if isinstance(user_id, dict):
            user_id = user_id.get("Value") or str(user_id)

        if isinstance(occurred_at, str):
            occurred_at = datetime.fromisoformat(occurred_at.replace("Z", "+00:00"))

        await self._connection.execute(
            """
            INSERT INTO profiles.profiles (id, user_id, first_name, is_complete, created_at)
            VALUES ($1, $2, $3, FALSE, $4)
            ON CONFLICT (id) DO NOTHING
            """,
            profile_id,
            user_id,
            first_name,
            occurred_at,
        )

    async def handle_profile_updated(self, payload: dict) -> None:
        """Handle ProfileUpdatedEvent - Update profile information.

        Note: This event only contains the ProfileId. The actual data must be
        fetched from the read model or included in the event payload extension.
        For now, we just update the timestamp to mark it as modified.

        In a full implementation, you would either:
        1. Include all updated fields in the event
        2. Fetch the current state from the API
        3. Use a snapshot-based approach
        """
        profile_id = payload.get("profile_id") or payload.get("ProfileId")

        if isinstance(profile_id, dict):
            profile_id = profile_id.get("Value") or str(profile_id)

        await self._connection.execute(
            """
            UPDATE profiles.profiles
            SET updated_at = CURRENT_TIMESTAMP
            WHERE id = $1
            """,
            profile_id,
        )

    async def handle_photo_updated(self, payload: dict) -> None:
        """Handle ProfilePhotoUpdatedEvent - Update profile photo URLs."""
        profile_id = payload.get("profile_id") or payload.get("ProfileId")
        photo_url = payload.get("photo_url") or payload.get("PhotoUrl")

        if isinstance(profile_id, dict):
            profile_id = profile_id.get("Value") or str(profile_id)

        await self._connection.execute(
            """
            UPDATE profiles.profiles
            SET photo_url = $2, updated_at = CURRENT_TIMESTAMP
            WHERE id = $1
            """,
            profile_id,
            photo_url,
        )

    async def truncate(self) -> None:
        """Truncate all profile tables for rebuild."""
        await self._connection.execute("TRUNCATE TABLE profiles.profiles CASCADE")
