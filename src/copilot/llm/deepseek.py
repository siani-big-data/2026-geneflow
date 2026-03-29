"""DeepSeek LLM client.

DeepSeek API is OpenAI-compatible, making integration straightforward.
Pricing: ~$0.07 per 100K tokens (very cost-effective)

API Docs: https://platform.deepseek.com/api-docs
"""

import json
from typing import Any

import httpx
import structlog

from .base import LLMClient, LLMProvider, LLMResponse, Message, ToolCall

logger = structlog.get_logger()


class DeepSeekClient(LLMClient):
    """DeepSeek API client (OpenAI-compatible)."""

    BASE_URL = "https://api.deepseek.com/v1"
    DEFAULT_MODEL = "deepseek-chat"  # or "deepseek-coder" for code tasks

    def __init__(
        self,
        api_key: str,
        model: str | None = None,
        base_url: str | None = None,
    ):
        self._api_key = api_key
        self._model = model or self.DEFAULT_MODEL
        self._base_url = base_url or self.BASE_URL
        self._client = httpx.AsyncClient(
            base_url=self._base_url,
            headers={
                "Authorization": f"Bearer {self._api_key}",
                "Content-Type": "application/json",
            },
            timeout=60.0,
        )

        # Metrics
        self._requests_made = 0
        self._total_input_tokens = 0
        self._total_output_tokens = 0
        self._errors = 0

    @property
    def provider(self) -> LLMProvider:
        return LLMProvider.DEEPSEEK

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
        """Send chat completion to DeepSeek."""
        if not self.is_available:
            raise ValueError("DeepSeek API key not configured")

        # Build messages in OpenAI format
        api_messages = []

        if system_prompt:
            api_messages.append({"role": "system", "content": system_prompt})

        for msg in messages:
            api_messages.append({"role": msg.role, "content": msg.content})

        # Build request payload
        payload: dict[str, Any] = {
            "model": self._model,
            "messages": api_messages,
            "max_tokens": max_tokens,
            "temperature": temperature,
        }

        # Add tools if provided (function calling)
        if tools:
            payload["tools"] = tools
            payload["tool_choice"] = "auto"

        try:
            response = await self._client.post("/chat/completions", json=payload)
            response.raise_for_status()
            data = response.json()

            self._requests_made += 1

            # Parse response
            choice = data["choices"][0]
            message = choice["message"]
            usage = data.get("usage", {})

            input_tokens = usage.get("prompt_tokens", 0)
            output_tokens = usage.get("completion_tokens", 0)

            self._total_input_tokens += input_tokens
            self._total_output_tokens += output_tokens

            # Parse tool calls if present
            tool_calls = []
            if "tool_calls" in message and message["tool_calls"]:
                for tc in message["tool_calls"]:
                    tool_calls.append(
                        ToolCall(
                            id=tc["id"],
                            name=tc["function"]["name"],
                            arguments=json.loads(tc["function"]["arguments"]),
                        )
                    )

            logger.debug(
                "deepseek_response",
                model=self._model,
                input_tokens=input_tokens,
                output_tokens=output_tokens,
                tool_calls=len(tool_calls),
            )

            return LLMResponse(
                content=message.get("content", "") or "",
                tool_calls=tool_calls,
                input_tokens=input_tokens,
                output_tokens=output_tokens,
                model=data.get("model", self._model),
                finish_reason=choice.get("finish_reason", ""),
            )

        except httpx.HTTPStatusError as e:
            self._errors += 1
            logger.error("deepseek_api_error", status=e.response.status_code, error=str(e))
            raise
        except Exception as e:
            self._errors += 1
            logger.error("deepseek_error", error=str(e))
            raise

    def get_metrics(self) -> dict:
        """Return usage metrics."""
        total_tokens = self._total_input_tokens + self._total_output_tokens
        # DeepSeek pricing: ~$0.14/1M input, $0.28/1M output (approx)
        estimated_cost = (
            self._total_input_tokens * 0.00000014 + self._total_output_tokens * 0.00000028
        )

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

    async def close(self) -> None:
        """Close the HTTP client."""
        await self._client.aclose()
