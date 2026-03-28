"""Tests for chat handler."""

from unittest.mock import AsyncMock, MagicMock

import pytest

from src.config import Settings
from src.copilot.chat import ChatHandler, Conversation, Message
from src.copilot.client import ClaudeClient
from src.models import AnalysisResult, AnalysisStatus


@pytest.fixture
def mock_client():
    """Create a mock Claude client."""
    client = MagicMock(spec=ClaudeClient)
    client.is_configured = True
    client.create_message = AsyncMock(return_value="Test response")
    return client


@pytest.fixture
def unconfigured_client():
    """Create an unconfigured mock client."""
    client = MagicMock(spec=ClaudeClient)
    client.is_configured = False
    return client


@pytest.fixture
def settings():
    """Create test settings."""
    return Settings()


@pytest.fixture
def sample_analysis():
    """Create a sample analysis result."""
    return AnalysisResult(
        traceId="trace-123",
        studyId="study-456",
        analysisId="analysis-789",
        status=AnalysisStatus.COMPLETED,
        overallConfidence=0.95,
    )


class TestMessage:
    """Tests for Message dataclass."""

    def test_message_creation(self):
        """Message is created with role and content."""
        msg = Message(role="user", content="Hello")

        assert msg.role == "user"
        assert msg.content == "Hello"
        assert msg.timestamp is not None


class TestConversation:
    """Tests for Conversation dataclass."""

    def test_conversation_creation(self):
        """Conversation is created with defaults."""
        conv = Conversation()

        assert conv.conversationId
        assert conv.messages == []
        assert conv.createdAt is not None

    def test_add_message(self):
        """add_message adds a message to the conversation."""
        conv = Conversation()
        conv.add_message("user", "Hello")
        conv.add_message("assistant", "Hi there!")

        assert len(conv.messages) == 2
        assert conv.messages[0].role == "user"
        assert conv.messages[1].role == "assistant"

    def test_get_messages_for_api(self):
        """get_messages_for_api returns formatted messages."""
        conv = Conversation()
        conv.add_message("user", "Hello")
        conv.add_message("assistant", "Hi!")

        api_messages = conv.get_messages_for_api()

        assert api_messages == [
            {"role": "user", "content": "Hello"},
            {"role": "assistant", "content": "Hi!"},
        ]

    def test_to_dict(self):
        """to_dict serializes conversation metadata."""
        conv = Conversation(
            conversationId="conv-123",
            traceId="trace-456",
            analysisId="analysis-789",
        )
        conv.add_message("user", "Test")

        data = conv.to_dict()

        assert data["conversationId"] == "conv-123"
        assert data["traceId"] == "trace-456"
        assert data["analysisId"] == "analysis-789"
        assert data["messageCount"] == 1
        assert "createdAt" in data


class TestChatHandler:
    """Tests for ChatHandler."""

    def test_is_available_when_configured(self, mock_client, settings):
        """is_available returns True when client is configured."""
        handler = ChatHandler(mock_client, settings)

        assert handler.is_available is True

    def test_is_available_when_not_configured(self, unconfigured_client, settings):
        """is_available returns False when client is not configured."""
        handler = ChatHandler(unconfigured_client, settings)

        assert handler.is_available is False

    def test_list_conversations_empty(self, mock_client, settings):
        """list_conversations returns empty list initially."""
        handler = ChatHandler(mock_client, settings)

        assert handler.list_conversations() == []

    @pytest.mark.asyncio
    async def test_ask_creates_conversation(self, mock_client, settings):
        """ask creates a new conversation."""
        handler = ChatHandler(mock_client, settings)

        result = await handler.ask("What is this?")

        assert "conversationId" in result
        assert result["answer"] == "Test response"
        assert len(handler.list_conversations()) == 1

    @pytest.mark.asyncio
    async def test_ask_continues_conversation(self, mock_client, settings):
        """ask continues existing conversation."""
        handler = ChatHandler(mock_client, settings)

        result1 = await handler.ask("First question")
        conv_id = result1["conversationId"]

        result2 = await handler.ask("Second question", conversation_id=conv_id)

        assert result2["conversationId"] == conv_id
        assert result2["messageCount"] == 4  # 2 user + 2 assistant

    @pytest.mark.asyncio
    async def test_ask_with_analysis_context(
        self, mock_client, settings, sample_analysis
    ):
        """ask uses analysis result as context."""
        handler = ChatHandler(mock_client, settings)

        result = await handler.ask(
            "Explain this",
            analysis_result=sample_analysis,
        )

        assert result["answer"] == "Test response"
        conv = handler.get_conversation(result["conversationId"])
        assert conv.traceId == "trace-123"
        assert conv.analysisId == "analysis-789"

    @pytest.mark.asyncio
    async def test_ask_not_configured(self, unconfigured_client, settings):
        """ask returns error when not configured."""
        handler = ChatHandler(unconfigured_client, settings)

        result = await handler.ask("Hello?")

        assert result["error"] is True
        assert "no configurado" in result["answer"].lower()

    def test_get_conversation(self, mock_client, settings):
        """get_conversation returns conversation by ID."""
        handler = ChatHandler(mock_client, settings)
        conv = Conversation(conversationId="test-123")
        handler._conversations["test-123"] = conv

        result = handler.get_conversation("test-123")

        assert result is conv

    def test_get_conversation_not_found(self, mock_client, settings):
        """get_conversation returns None for unknown ID."""
        handler = ChatHandler(mock_client, settings)

        result = handler.get_conversation("unknown")

        assert result is None

    def test_delete_conversation(self, mock_client, settings):
        """delete_conversation removes conversation."""
        handler = ChatHandler(mock_client, settings)
        handler._conversations["test-123"] = Conversation(conversationId="test-123")

        result = handler.delete_conversation("test-123")

        assert result is True
        assert "test-123" not in handler._conversations

    def test_delete_conversation_not_found(self, mock_client, settings):
        """delete_conversation returns False for unknown ID."""
        handler = ChatHandler(mock_client, settings)

        result = handler.delete_conversation("unknown")

        assert result is False

    @pytest.mark.asyncio
    async def test_interpret_analysis(self, mock_client, settings, sample_analysis):
        """interpret_analysis returns summary and recommendations."""
        handler = ChatHandler(mock_client, settings)

        result = await handler.interpret_analysis(sample_analysis)

        assert "summary" in result
        assert "recommendations" in result
        assert result["summary"] == "Test response"

    @pytest.mark.asyncio
    async def test_interpret_analysis_not_configured(
        self, unconfigured_client, settings, sample_analysis
    ):
        """interpret_analysis returns error when not configured."""
        handler = ChatHandler(unconfigured_client, settings)

        result = await handler.interpret_analysis(sample_analysis)

        assert result["error"] is True
        assert result["recommendations"] == []
