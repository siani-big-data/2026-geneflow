"""Claude (Anthropic) LLM client.

Wrapper around the existing Anthropic client to conform to LLMClient interface.
"""

from typing import Any

import structlog
from anthropic import AsyncAnthropic

from .base import LLMClient, LLMProvider, LLMResponse, Message, ToolCall

logger = structlog.get_logger()


class ClaudeClient(LLMClient):
    """Claude API client using Anthropic SDK."""

    DEFAULT_MODEL = "claude-sonnet-4-20250514"

    def __init__(
        self,
        api_key: str,
        model: str | None = None,
    ):
        self._api_key = api_key
        self._model = model or self.DEFAULT_MODEL
        self._client = AsyncAnthropic(api_key=api_key) if api_key else None

        # Metrics
        self._requests_made = 0
        self._total_input_tokens = 0
        self._total_output_tokens = 0
        self._errors = 0

    @property
    def provider(self) -> LLMProvider:
        return LLMProvider.CLAUDE

    @property
    def is_available(self) -> bool:
        return bool(self._api_key)

    @property
    def model_name(self) -> str:
        return self._model

    async def chat(
        self,
        messages: list[Message],
        system_prompt: str | None = None,
        tools: list[dict] | None = None,
        max_tokens: int = 4096,
        temperature: float = 0.7,
    ) -> LLMResponse:
        """Send chat completion to Claude."""
        if not self._client:
            raise ValueError("Claude API key not configured")

        # Convert messages to Anthropic format
        api_messages = [{"role": msg.role, "content": msg.content} for msg in messages]

        # Build request kwargs
        kwargs: dict[str, Any] = {
            "model": self._model,
            "max_tokens": max_tokens,
            "messages": api_messages,
        }

        if system_prompt:
            kwargs["system"] = system_prompt

        if temperature != 0.7:  # Only set if non-default
            kwargs["temperature"] = temperature

        # Convert tools to Anthropic format if provided
        if tools:
            anthropic_tools = self._convert_tools_to_anthropic(tools)
            kwargs["tools"] = anthropic_tools

        try:
            response = await self._client.messages.create(**kwargs)

            self._requests_made += 1
            self._total_input_tokens += response.usage.input_tokens
            self._total_output_tokens += response.usage.output_tokens

            # Parse response content
            content = ""
            tool_calls = []

            for block in response.content:
                if block.type == "text":
                    content += block.text
                elif block.type == "tool_use":
                    tool_calls.append(
                        ToolCall(
                            id=block.id,
                            name=block.name,
                            arguments=block.input if isinstance(block.input, dict) else {},
                        )
                    )

            logger.debug(
                "claude_response",
                model=self._model,
                input_tokens=response.usage.input_tokens,
                output_tokens=response.usage.output_tokens,
                tool_calls=len(tool_calls),
            )

            return LLMResponse(
                content=content,
                tool_calls=tool_calls,
                input_tokens=response.usage.input_tokens,
                output_tokens=response.usage.output_tokens,
                model=response.model,
                finish_reason=response.stop_reason or "",
            )

        except Exception as e:
            self._errors += 1
            logger.error("claude_api_error", error=str(e))
            raise

    def _convert_tools_to_anthropic(self, tools: list[dict]) -> list[dict]:
        """Convert OpenAI-style tools to Anthropic format."""
        anthropic_tools = []
        for tool in tools:
            if tool.get("type") == "function":
                func = tool["function"]
                anthropic_tools.append(
                    {
                        "name": func["name"],
                        "description": func.get("description", ""),
                        "input_schema": func.get("parameters", {"type": "object"}),
                    }
                )
            else:
                # Already in Anthropic format
                anthropic_tools.append(tool)
        return anthropic_tools

    def get_metrics(self) -> dict:
        """Return usage metrics."""
        total_tokens = self._total_input_tokens + self._total_output_tokens
        # Claude pricing varies by model, using Sonnet estimates
        # Input: $3/1M, Output: $15/1M
        estimated_cost = self._total_input_tokens * 0.000003 + self._total_output_tokens * 0.000015

        return {
            "provider": self.provider.value,
            "model": self._model,
            "requestsMade": self._requests_made,
            "totalInputTokens": self._total_input_tokens,
            "totalOutputTokens": self._total_output_tokens,
            "totalTokens": total_tokens,
            "estimatedCostUsd": round(estimated_cost, 6),
            "errors": self._errors,
        }
