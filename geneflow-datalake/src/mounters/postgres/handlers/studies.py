"""Studies event handler for PostgreSQL."""

from src.mounters.postgres.handlers.base import BaseHandler


class StudiesHandler(BaseHandler):
    """Handler for study-related events."""

    def __init__(self, connection):
        super().__init__(connection)
        self._event_mappings = {
            "StudyCreated": "insert_study",
            "StudyUpdated": "update_study",
            "StudyDeleted": "soft_delete_study",
            "MemberAdded": "insert_member",
            "MemberRemoved": "delete_member",
            "InvitationCreated": "insert_invitation",
        }

    async def insert_study(self, payload: dict) -> None:
        """Insert a new study."""
        await self._connection.execute(
            """INSERT INTO studies.studies (id, title, description, owner_id, created_at)
            VALUES ($1, $2, $3, $4, $5)""",
            payload.get("id"),
            payload.get("title"),
            payload.get("description"),
            payload.get("owner_id"),
            payload.get("created_at"),
        )

    async def update_study(self, payload: dict) -> None:
        """Update an existing study."""
        pass

    async def soft_delete_study(self, payload: dict) -> None:
        """Soft delete a study."""
        pass

    async def insert_member(self, payload: dict) -> None:
        """Add a member to a study."""
        await self._connection.execute(
            """INSERT INTO studies.members (study_id, user_id, role_id, invited_by)
            VALUES ($1, $2, $3, $4)""",
            payload.get("study_id"),
            payload.get("user_id"),
            payload.get("role_id"),
            payload.get("invited_by"),
        )

    async def delete_member(self, payload: dict) -> None:
        """Remove a member from a study."""
        await self._connection.execute(
            "DELETE FROM studies.members WHERE study_id = $1 AND user_id = $2",
            payload.get("study_id"),
            payload.get("user_id"),
        )

    async def insert_invitation(self, payload: dict) -> None:
        """Create a study invitation."""
        await self._connection.execute(
            """INSERT INTO studies.invitations (id, study_id, email, token, invited_by, expires_at)
            VALUES ($1, $2, $3, $4, $5, $6)""",
            payload.get("id"),
            payload.get("study_id"),
            payload.get("email"),
            payload.get("token"),
            payload.get("invited_by"),
            payload.get("expires_at"),
        )
