"""Tests for Claude API client."""

from unittest.mock import AsyncMock, MagicMock, patch

import pytest

from src.config import Settings
from src.copilot.client import ClaudeClient


@pytest.fixture
def settings_without_api_key():
    """Settings without API key."""
    return Settings(claude_api_key="")


@pytest.fixture
def settings_with_api_key():
    """Settings with API key."""
    return Settings(claude_api_key="test-api-key")


class TestClaudeClient:
    """Tests for ClaudeClient."""

    def test_not_configured_without_api_key(self, settings_without_api_key):
        """Client is not configured without API key."""
        client = ClaudeClient(settings_without_api_key)
        assert client.is_configured is False

    def test_configured_with_api_key(self, settings_with_api_key):
        """Client is configured with API key."""
        with patch("src.copilot.client.AsyncAnthropic"):
            client = ClaudeClient(settings_with_api_key)
            assert client.is_configured is True

    def test_metrics_initial(self, settings_without_api_key):
        """Initial metrics are zero."""
        client = ClaudeClient(settings_without_api_key)
        metrics = client.metrics

        assert metrics["requestsMade"] == 0
        assert metrics["tokensUsed"] == 0
        assert metrics["errors"] == 0
        assert metrics["configured"] is False

    @pytest.mark.asyncio
    async def test_create_message_not_configured(self, settings_without_api_key):
        """create_message raises when not configured."""
        client = ClaudeClient(settings_without_api_key)

        with pytest.raises(RuntimeError, match="not configured"):
            await client.create_message(
                system_prompt="Test system",
                user_message="Test message",
            )

    @pytest.mark.asyncio
    async def test_create_message_success(self, settings_with_api_key):
        """create_message returns response text."""
        with patch("src.copilot.client.AsyncAnthropic") as mock_anthropic:
            # Setup mock
            mock_client = MagicMock()
            mock_anthropic.return_value = mock_client

            mock_response = MagicMock()
            mock_response.content = [MagicMock(text="Test response")]
            mock_response.usage.input_tokens = 10
            mock_response.usage.output_tokens = 20

            mock_client.messages.create = AsyncMock(return_value=mock_response)

            client = ClaudeClient(settings_with_api_key)
            result = await client.create_message(
                system_prompt="Test system",
                user_message="Test message",
            )

            assert result == "Test response"
            assert client.metrics["requestsMade"] == 1
            assert client.metrics["tokensUsed"] == 30

    @pytest.mark.asyncio
    async def test_create_message_error_increments_counter(self, settings_with_api_key):
        """create_message increments error counter on failure."""
        with patch("src.copilot.client.AsyncAnthropic") as mock_anthropic:
            mock_client = MagicMock()
            mock_anthropic.return_value = mock_client
            mock_client.messages.create = AsyncMock(side_effect=Exception("API error"))

            client = ClaudeClient(settings_with_api_key)

            with pytest.raises(Exception, match="API error"):
                await client.create_message(
                    system_prompt="Test",
                    user_message="Test",
                )

            assert client.metrics["errors"] == 1
