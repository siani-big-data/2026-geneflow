"""Tests for Copilot and BLAST API endpoints."""

from unittest.mock import AsyncMock, MagicMock

import pytest
from fastapi.testclient import TestClient

from src.api import (
    app,
    set_blast_client,
    set_chat_handler,
    set_claude_configured,
    set_redis_health,
    set_service_status,
)
from src.models import ServiceStatus


@pytest.fixture
def api_client():
    """Create test client."""
    return TestClient(app)


@pytest.fixture
def running_service(api_client):
    """Setup service as running."""
    set_service_status(ServiceStatus.RUNNING)
    set_redis_health(True)
    set_claude_configured(True)
    yield api_client
    set_service_status(ServiceStatus.STOPPED)
    set_redis_health(False)
    set_claude_configured(False)


class TestCopilotEndpoints:
    """Tests for Copilot endpoints."""

    def test_copilot_status_not_initialized(self, running_service):
        """copilot/status returns not available when handler not set."""
        set_chat_handler(None)

        response = running_service.get("/copilot/status")

        assert response.status_code == 200
        data = response.json()
        assert data["available"] is False

    def test_copilot_status_available(self, running_service):
        """copilot/status returns available when configured."""
        mock_handler = MagicMock()
        mock_handler.is_available = True
        mock_handler.list_conversations.return_value = []
        set_chat_handler(mock_handler)

        response = running_service.get("/copilot/status")

        assert response.status_code == 200
        data = response.json()
        assert data["available"] is True
        assert data["configured"] is True
        assert data["activeConversations"] == 0

    def test_copilot_ask_not_initialized(self, running_service):
        """copilot/ask returns 503 when handler not set."""
        set_chat_handler(None)

        response = running_service.post(
            "/copilot/ask",
            json={"question": "What is this?"},
        )

        assert response.status_code == 503

    def test_copilot_ask_not_configured(self, running_service):
        """copilot/ask returns 503 when Claude not configured."""
        mock_handler = MagicMock()
        mock_handler.is_available = False
        set_chat_handler(mock_handler)

        response = running_service.post(
            "/copilot/ask",
            json={"question": "What is this?"},
        )

        assert response.status_code == 503

    def test_copilot_ask_success(self, running_service):
        """copilot/ask returns answer."""
        mock_handler = MagicMock()
        mock_handler.is_available = True
        mock_handler.ask = AsyncMock(return_value={
            "answer": "This is a test answer.",
            "conversationId": "conv-123",
            "messageCount": 2,
        })
        set_chat_handler(mock_handler)

        response = running_service.post(
            "/copilot/ask",
            json={"question": "What is this?"},
        )

        assert response.status_code == 200
        data = response.json()
        assert data["answer"] == "This is a test answer."
        assert data["conversationId"] == "conv-123"
        assert data["messageCount"] == 2

    def test_copilot_conversations_list(self, running_service):
        """copilot/conversations returns list."""
        mock_handler = MagicMock()
        mock_handler.list_conversations.return_value = [
            {"conversationId": "conv-1", "messageCount": 4},
            {"conversationId": "conv-2", "messageCount": 2},
        ]
        set_chat_handler(mock_handler)

        response = running_service.get("/copilot/conversations")

        assert response.status_code == 200
        data = response.json()
        assert len(data["conversations"]) == 2

    def test_copilot_delete_conversation(self, running_service):
        """copilot/conversations DELETE removes conversation."""
        mock_handler = MagicMock()
        mock_handler.delete_conversation.return_value = True
        set_chat_handler(mock_handler)

        response = running_service.delete("/copilot/conversations/conv-123")

        assert response.status_code == 200
        data = response.json()
        assert data["deleted"] is True

    def test_copilot_delete_conversation_not_found(self, running_service):
        """copilot/conversations DELETE returns 404 for unknown."""
        mock_handler = MagicMock()
        mock_handler.delete_conversation.return_value = False
        set_chat_handler(mock_handler)

        response = running_service.delete("/copilot/conversations/unknown")

        assert response.status_code == 404


class TestBlastEndpoints:
    """Tests for BLAST endpoints."""

    def test_blast_metrics_not_initialized(self, running_service):
        """blast/metrics returns not configured when client not set."""
        set_blast_client(None)

        response = running_service.get("/blast/metrics")

        assert response.status_code == 200
        data = response.json()
        assert data["configured"] is False

    def test_blast_metrics_configured(self, running_service):
        """blast/metrics returns metrics when configured."""
        mock_client = MagicMock()
        mock_client.is_configured = True
        mock_client.metrics = {
            "jobsSubmitted": 5,
            "jobsCompleted": 3,
            "errors": 0,
            "configured": True,
        }
        set_blast_client(mock_client)

        response = running_service.get("/blast/metrics")

        assert response.status_code == 200
        data = response.json()
        assert data["jobsSubmitted"] == 5
        assert data["configured"] is True

    def test_blast_submit_not_configured(self, running_service):
        """blast/submit returns 503 when not configured."""
        mock_client = MagicMock()
        mock_client.is_configured = False
        set_blast_client(mock_client)

        response = running_service.post(
            "/blast/submit",
            json={"sequence": "ATCGATCG"},
        )

        assert response.status_code == 503

    def test_blast_submit_success(self, running_service):
        """blast/submit returns job info."""
        mock_client = MagicMock()
        mock_client.is_configured = True
        mock_client.submit_search = AsyncMock(return_value=MagicMock(
            to_dict=lambda: {
                "rid": "ABC123",
                "status": "WAITING",
                "program": "blastn",
                "database": "nt",
            }
        ))
        set_blast_client(mock_client)

        response = running_service.post(
            "/blast/submit",
            json={"sequence": "ATCGATCG"},
        )

        assert response.status_code == 200
        data = response.json()
        assert data["rid"] == "ABC123"
        assert data["status"] == "WAITING"

    def test_blast_status(self, running_service):
        """blast/status returns job status."""
        mock_client = MagicMock()
        mock_client.is_configured = True
        mock_client.check_status = AsyncMock(return_value="READY")
        set_blast_client(mock_client)

        response = running_service.get("/blast/status/ABC123")

        assert response.status_code == 200
        data = response.json()
        assert data["rid"] == "ABC123"
        assert data["status"] == "READY"

    def test_blast_results(self, running_service):
        """blast/results returns search results."""
        mock_client = MagicMock()
        mock_client.is_configured = True
        mock_client.get_results = AsyncMock(return_value=MagicMock(
            to_dict=lambda: {
                "rid": "ABC123",
                "hits": [],
                "hitCount": 0,
                "queryLength": 500,
            }
        ))
        set_blast_client(mock_client)

        response = running_service.get("/blast/results/ABC123")

        assert response.status_code == 200
        data = response.json()
        assert data["rid"] == "ABC123"
        assert data["hitCount"] == 0

    def test_blast_search_full(self, running_service):
        """blast/search submits and waits for results."""
        mock_hit = MagicMock()
        mock_hit.accession = "NC_001234"
        mock_hit.organism = "Homo sapiens"

        mock_result = MagicMock()
        mock_result.rid = "ABC123"
        mock_result.hitCount = 1
        mock_result.topHit = mock_hit

        mock_client = MagicMock()
        mock_client.is_configured = True
        mock_client.search = AsyncMock(return_value=mock_result)
        set_blast_client(mock_client)

        response = running_service.post(
            "/blast/search",
            json={"sequence": "ATCGATCG", "program": "blastn", "database": "nt"},
        )

        assert response.status_code == 200
        data = response.json()
        assert data["rid"] == "ABC123"
        assert data["status"] == "completed"
        assert data["hitCount"] == 1
        assert data["topHitAccession"] == "NC_001234"
        assert data["topHitOrganism"] == "Homo sapiens"
