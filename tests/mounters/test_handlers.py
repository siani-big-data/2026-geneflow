"""Tests for PostgreSQL event handlers."""

from unittest.mock import AsyncMock, MagicMock

import pytest

from src.mounters.postgres.handlers import (
    AlignmentsHandler,
    BillingHandler,
    StudiesHandler,
    TracesHandler,
    UsersHandler,
)


@pytest.fixture
def mock_connection():
    """Create a mock PostgreSQL connection."""
    conn = MagicMock()
    conn.execute = AsyncMock()
    conn.fetch = AsyncMock(return_value=[])
    return conn


class TestUsersHandler:
    """Tests for UsersHandler."""

    @pytest.fixture
    def handler(self, mock_connection):
        return UsersHandler(mock_connection)

    async def test_insert_user(self, handler, mock_connection):
        """Test inserting a new user."""
        payload = {
            "id": "user-123",
            "email": "test@example.com",
            "username": "testuser",
            "password_hash": "hashed_password",
            "is_active": True,
            "email_verified": False,
            "created_at": "2026-03-26T10:00:00Z",
        }

        await handler.insert(payload)

        mock_connection.execute.assert_called_once()
        call_args = mock_connection.execute.call_args
        assert "INSERT INTO identity.users" in call_args[0][0]
        assert call_args[0][1] == "user-123"
        assert call_args[0][2] == "test@example.com"

    async def test_update_user(self, handler, mock_connection):
        """Test updating a user."""
        payload = {
            "id": "user-123",
            "email": "newemail@example.com",
            "updated_at": "2026-03-26T12:00:00Z",
        }

        await handler.update(payload)

        mock_connection.execute.assert_called_once()
        call_args = mock_connection.execute.call_args
        assert "UPDATE identity.users" in call_args[0][0]
        assert "email = $1" in call_args[0][0]

    async def test_soft_delete_user(self, handler, mock_connection):
        """Test soft deleting a user."""
        payload = {
            "id": "user-123",
            "deleted_at": "2026-03-26T14:00:00Z",
            "deleted_by": "admin-456",
        }

        await handler.soft_delete(payload)

        mock_connection.execute.assert_called_once()
        call_args = mock_connection.execute.call_args
        assert "is_deleted = TRUE" in call_args[0][0]

    async def test_event_routing(self, handler):
        """Test event type to handler method mapping."""
        assert handler._event_mappings["UserRegistered"] == "insert"
        assert handler._event_mappings["UserUpdated"] == "update"
        assert handler._event_mappings["UserDeleted"] == "soft_delete"


class TestStudiesHandler:
    """Tests for StudiesHandler."""

    @pytest.fixture
    def handler(self, mock_connection):
        return StudiesHandler(mock_connection)

    async def test_insert_study(self, handler, mock_connection):
        """Test inserting a new study."""
        payload = {
            "id": "study-123",
            "title": "Test Study",
            "description": "A test study",
            "owner_id": "user-456",
            "created_at": "2026-03-26T10:00:00Z",
        }

        await handler.insert_study(payload)

        mock_connection.execute.assert_called_once()
        call_args = mock_connection.execute.call_args
        assert "INSERT INTO studies.studies" in call_args[0][0]

    async def test_insert_member(self, handler, mock_connection):
        """Test adding a member to a study."""
        payload = {
            "study_id": "study-123",
            "user_id": "user-456",
            "role_id": 2,
            "invited_by": "user-001",
        }

        await handler.insert_member(payload)

        mock_connection.execute.assert_called_once()
        call_args = mock_connection.execute.call_args
        assert "INSERT INTO studies.members" in call_args[0][0]

    async def test_delete_member(self, handler, mock_connection):
        """Test removing a member from a study."""
        payload = {
            "study_id": "study-123",
            "user_id": "user-456",
        }

        await handler.delete_member(payload)

        mock_connection.execute.assert_called_once()
        call_args = mock_connection.execute.call_args
        assert "DELETE FROM studies.members" in call_args[0][0]

    async def test_insert_invitation(self, handler, mock_connection):
        """Test creating an invitation."""
        payload = {
            "id": "inv-123",
            "study_id": "study-123",
            "email": "invite@example.com",
            "token": "abc123token",
            "invited_by": "user-001",
            "expires_at": "2026-04-01T00:00:00Z",
        }

        await handler.insert_invitation(payload)

        mock_connection.execute.assert_called_once()
        call_args = mock_connection.execute.call_args
        assert "INSERT INTO studies.invitations" in call_args[0][0]


class TestTracesHandler:
    """Tests for TracesHandler."""

    @pytest.fixture
    def handler(self, mock_connection):
        return TracesHandler(mock_connection)

    async def test_insert_trace(self, handler, mock_connection):
        """Test inserting trace metadata."""
        payload = {
            "id": "trace-123",
            "name": "Sample Trace",
            "study_id": "study-456",
            "uploaded_by": "user-789",
            "file_name": "sample.ab1",
            "format_id": 1,
            "size_bytes": 50000,
        }

        await handler.insert_trace(payload)

        mock_connection.execute.assert_called_once()
        call_args = mock_connection.execute.call_args
        assert "INSERT INTO traces.traces" in call_args[0][0]

    async def test_update_trace_after_processing(self, handler, mock_connection):
        """Test updating trace with processing results."""
        payload = {
            "id": "trace-123",
            "status_id": 3,  # Completed
            "total_bases": 850,
            "average_quality_score": 35.5,
            "processed_at": "2026-03-26T11:00:00Z",
        }

        await handler.update_trace(payload)

        mock_connection.execute.assert_called_once()
        call_args = mock_connection.execute.call_args
        assert "UPDATE traces.traces" in call_args[0][0]
        assert "status_id" in call_args[0][0]

    async def test_insert_annotation(self, handler, mock_connection):
        """Test inserting an annotation."""
        payload = {
            "id": "ann-123",
            "trace_id": "trace-456",
            "type_id": 1,
            "label": "Primer",
            "start_position": 10,
            "end_position": 30,
            "created_by": "user-789",
        }

        await handler.insert_annotation(payload)

        mock_connection.execute.assert_called_once()
        call_args = mock_connection.execute.call_args
        assert "INSERT INTO traces.annotations" in call_args[0][0]


class TestAlignmentsHandler:
    """Tests for AlignmentsHandler."""

    @pytest.fixture
    def handler(self, mock_connection):
        return AlignmentsHandler(mock_connection)

    async def test_insert_alignment(self, handler, mock_connection):
        """Test inserting an alignment."""
        payload = {
            "id": "align-123",
            "name": "Test Alignment",
            "study_id": "study-456",
            "created_by": "user-789",
            "type_id": 1,  # Pairwise
        }

        await handler.insert_alignment(payload)

        mock_connection.execute.assert_called_once()
        call_args = mock_connection.execute.call_args
        assert "INSERT INTO alignments.alignments" in call_args[0][0]

    async def test_insert_alignment_trace(self, handler, mock_connection):
        """Test adding a trace to an alignment."""
        payload = {
            "alignment_id": "align-123",
            "trace_id": "trace-456",
            "sequence_order": 0,
        }

        await handler.insert_alignment_trace(payload)

        mock_connection.execute.assert_called_once()
        call_args = mock_connection.execute.call_args
        assert "INSERT INTO alignments.alignment_traces" in call_args[0][0]

    async def test_update_alignment_completed(self, handler, mock_connection):
        """Test updating alignment with results."""
        payload = {
            "id": "align-123",
            "status_id": 3,  # Completed
            "alignment_length": 850,
            "identity_percentage": 98.5,
            "consensus_sequence": "ATCGATCG...",
            "completed_at": "2026-03-26T12:00:00Z",
        }

        await handler.update_alignment(payload)

        mock_connection.execute.assert_called_once()
        call_args = mock_connection.execute.call_args
        assert "UPDATE alignments.alignments" in call_args[0][0]


class TestBillingHandler:
    """Tests for BillingHandler."""

    @pytest.fixture
    def handler(self, mock_connection):
        return BillingHandler(mock_connection)

    async def test_insert_plan(self, handler, mock_connection):
        """Test inserting a plan."""
        payload = {
            "id": "plan-123",
            "name": "Pro",
            "monthly_price": 29.99,
            "annual_price": 299.99,
            "max_studies": 100,
        }

        await handler.insert_plan(payload)

        mock_connection.execute.assert_called_once()
        call_args = mock_connection.execute.call_args
        assert "INSERT INTO billing.plans" in call_args[0][0]

    async def test_insert_subscription(self, handler, mock_connection):
        """Test inserting a subscription."""
        payload = {
            "id": "sub-123",
            "user_id": "user-456",
            "plan_id": "plan-789",
            "plan_name": "Pro",
            "period_start": "2026-03-01T00:00:00Z",
            "period_end": "2026-04-01T00:00:00Z",
        }

        await handler.insert_subscription(payload)

        mock_connection.execute.assert_called_once()
        call_args = mock_connection.execute.call_args
        assert "INSERT INTO billing.subscriptions" in call_args[0][0]

    async def test_update_subscription_cancelled(self, handler, mock_connection):
        """Test cancelling a subscription."""
        payload = {
            "id": "sub-123",
            "status": 2,  # Cancelled
            "cancellation_reason": "User requested",
            "cancelled_at": "2026-03-26T15:00:00Z",
        }

        await handler.update_subscription(payload)

        mock_connection.execute.assert_called_once()
        call_args = mock_connection.execute.call_args
        assert "UPDATE billing.subscriptions" in call_args[0][0]
