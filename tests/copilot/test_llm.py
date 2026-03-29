"""Tests for LLM providers."""

from unittest.mock import AsyncMock, MagicMock, patch

import pytest

from src.copilot.llm import (
    LLMProvider,
    LLMResponse,
    Message,
    create_llm_client,
    get_llm_client,
)
from src.copilot.llm.claude import ClaudeClient
from src.copilot.llm.deepseek import DeepSeekClient
from src.copilot.llm.factory import (
    _clients,
    get_best_available,
    get_cheapest_available,
    get_default_provider,
    list_available_providers,
    register_client,
    set_default_provider,
)


class TestLLMResponse:
    """Tests for LLMResponse dataclass."""

    def test_basic_response(self):
        response = LLMResponse(
            content="Hello, world!",
            input_tokens=10,
            output_tokens=5,
            model="test-model",
        )
        assert response.content == "Hello, world!"
        assert response.total_tokens == 15
        assert not response.has_tool_calls

    def test_response_with_tool_calls(self):
        from src.copilot.llm.base import ToolCall

        response = LLMResponse(
            content="",
            tool_calls=[
                ToolCall(id="1", name="get_weather", arguments={"city": "Madrid"})
            ],
            input_tokens=20,
            output_tokens=10,
        )
        assert response.has_tool_calls
        assert len(response.tool_calls) == 1
        assert response.tool_calls[0].name == "get_weather"


class TestDeepSeekClient:
    """Tests for DeepSeek client."""

    def test_not_available_without_key(self):
        client = DeepSeekClient(api_key="")
        assert not client.is_available

    def test_available_with_key(self):
        client = DeepSeekClient(api_key="test-key")
        assert client.is_available
        assert client.provider == LLMProvider.DEEPSEEK
        assert client.model_name == "deepseek-chat"

    def test_custom_model(self):
        client = DeepSeekClient(api_key="test-key", model="deepseek-coder")
        assert client.model_name == "deepseek-coder"

    def test_metrics_initial(self):
        client = DeepSeekClient(api_key="test-key")
        metrics = client.get_metrics()
        assert metrics["provider"] == "deepseek"
        assert metrics["requestsMade"] == 0
        assert metrics["totalTokens"] == 0

    @pytest.mark.asyncio
    async def test_chat_not_configured(self):
        client = DeepSeekClient(api_key="")
        with pytest.raises(ValueError, match="not configured"):
            await client.chat(messages=[Message(role="user", content="Hello")])

    @pytest.mark.asyncio
    async def test_chat_success(self):
        client = DeepSeekClient(api_key="test-key")

        mock_response = MagicMock()
        mock_response.status_code = 200
        mock_response.json.return_value = {
            "choices": [
                {
                    "message": {"content": "Hello! How can I help?"},
                    "finish_reason": "stop",
                }
            ],
            "usage": {"prompt_tokens": 10, "completion_tokens": 8},
            "model": "deepseek-chat",
        }
        mock_response.raise_for_status = MagicMock()

        with patch.object(client._client, "post", new_callable=AsyncMock) as mock_post:
            mock_post.return_value = mock_response

            response = await client.chat(
                messages=[Message(role="user", content="Hello")],
                system_prompt="You are helpful.",
            )

            assert response.content == "Hello! How can I help?"
            assert response.input_tokens == 10
            assert response.output_tokens == 8
            assert not response.has_tool_calls

    @pytest.mark.asyncio
    async def test_chat_with_tools(self):
        client = DeepSeekClient(api_key="test-key")

        mock_response = MagicMock()
        mock_response.status_code = 200
        mock_response.json.return_value = {
            "choices": [
                {
                    "message": {
                        "content": None,
                        "tool_calls": [
                            {
                                "id": "call_123",
                                "function": {
                                    "name": "get_quality",
                                    "arguments": '{"trace_id": "abc"}',
                                },
                            }
                        ],
                    },
                    "finish_reason": "tool_calls",
                }
            ],
            "usage": {"prompt_tokens": 15, "completion_tokens": 12},
            "model": "deepseek-chat",
        }
        mock_response.raise_for_status = MagicMock()

        with patch.object(client._client, "post", new_callable=AsyncMock) as mock_post:
            mock_post.return_value = mock_response

            tools = [
                {
                    "type": "function",
                    "function": {
                        "name": "get_quality",
                        "parameters": {"type": "object"},
                    },
                }
            ]

            response = await client.chat(
                messages=[Message(role="user", content="Check quality")],
                tools=tools,
            )

            assert response.has_tool_calls
            assert response.tool_calls[0].name == "get_quality"
            assert response.tool_calls[0].arguments == {"trace_id": "abc"}


class TestClaudeClient:
    """Tests for Claude client."""

    def test_not_available_without_key(self):
        client = ClaudeClient(api_key="")
        assert not client.is_available

    def test_available_with_key(self):
        client = ClaudeClient(api_key="test-key")
        assert client.is_available
        assert client.provider == LLMProvider.CLAUDE
        assert "claude" in client.model_name

    def test_metrics_initial(self):
        client = ClaudeClient(api_key="test-key")
        metrics = client.get_metrics()
        assert metrics["provider"] == "claude"
        assert metrics["requestsMade"] == 0


class TestFactory:
    """Tests for LLM factory."""

    def setup_method(self):
        """Clear clients before each test."""
        _clients.clear()

    def test_create_deepseek_client(self):
        client = create_llm_client(LLMProvider.DEEPSEEK, api_key="test-key")
        assert isinstance(client, DeepSeekClient)

    def test_create_claude_client(self):
        client = create_llm_client(LLMProvider.CLAUDE, api_key="test-key")
        assert isinstance(client, ClaudeClient)

    def test_create_unsupported_provider(self):
        with pytest.raises(ValueError, match="Unsupported"):
            create_llm_client(LLMProvider.OPENAI, api_key="test-key")

    def test_register_and_get_client(self):
        client = DeepSeekClient(api_key="test-key")
        register_client(client)

        retrieved = get_llm_client(LLMProvider.DEEPSEEK)
        assert retrieved is client

    def test_default_provider(self):
        set_default_provider(LLMProvider.DEEPSEEK)
        assert get_default_provider() == LLMProvider.DEEPSEEK

        # Reset
        set_default_provider(LLMProvider.CLAUDE)

    def test_list_available_providers(self):
        client1 = DeepSeekClient(api_key="key1")
        client2 = ClaudeClient(api_key="key2")
        register_client(client1)
        register_client(client2)

        providers = list_available_providers()
        assert len(providers) == 2
        provider_names = [p["provider"] for p in providers]
        assert "deepseek" in provider_names
        assert "claude" in provider_names

    def test_get_cheapest_available(self):
        # Register both
        deepseek = DeepSeekClient(api_key="key1")
        claude = ClaudeClient(api_key="key2")
        register_client(deepseek)
        register_client(claude)

        cheapest = get_cheapest_available()
        assert cheapest.provider == LLMProvider.DEEPSEEK

    def test_get_best_available(self):
        # Register both
        deepseek = DeepSeekClient(api_key="key1")
        claude = ClaudeClient(api_key="key2")
        register_client(deepseek)
        register_client(claude)

        best = get_best_available()
        assert best.provider == LLMProvider.CLAUDE

    def test_get_cheapest_when_only_claude(self):
        claude = ClaudeClient(api_key="key")
        register_client(claude)

        cheapest = get_cheapest_available()
        assert cheapest.provider == LLMProvider.CLAUDE
