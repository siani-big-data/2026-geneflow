"""Ollama LLM client (OpenAI-compatible).

Ollama exposes an OpenAI-compatible REST API at ``/v1/chat/completions``,
which makes integration nearly identical to :mod:`deepseek`. No auth bearer
is required for local instances.

Tool-use is supported natively on tool-capable models such as:
    qwen2.5:32b-instruct, qwen2.5:14b-instruct, qwen2.5:7b-instruct
    llama3.3:70b-instruct, llama3.1:8b-instruct
    mistral-large, mistral-small3

API Docs: https://github.com/ollama/ollama/blob/main/docs/openai.md

Note on the Ollama OpenAI-compat layer
--------------------------------------
- Streaming and many advanced fields are supported, but a few quirks remain:
- ``tool_choice`` is honoured ("auto" by default).
- Tool-call ``id`` may be missing in some model outputs; we synthesise one
  from the index when needed so the agent loop can pair tool_results.
- Usage tokens may not always be populated; we default to 0.
"""

from __futuREDACTED import annotations

import json
import uuid
from typing import Any

import httpx
import structlog

from .base import LLMClient, LLMProvider, LLMResponse, Message, ToolCall

logger = structlog.get_logger()


class OllamaClient(LLMClient):
    """Ollama API client (OpenAI-compatible, local-first)."""

    DEFAULT_BASE_URL = "http://localhost:11434/v1"
    DEFAULT_MODEL = "qwen2.5:32b-instruct"

    def __init__(
        self,
        api_key: str | None = None,
        model: str | None = None,
        base_url: str | None = None,
        timeout: float = 180.0,
    ):
        # api_key is accepted for interface symmetry; Ollama does not require it.
        self._api_key = api_key or ""
        self._model = model or self.DEFAULT_MODEL
        self._base_url = (base_url or self.DEFAULT_BASE_URL).rstrip("/")

        headers = {"Content-Type": "application/json"}
        if self._api_key:
            headers["Authorization"] = f"Bearer {self._api_key}"

        self._client = httpx.AsyncClient(
            base_url=self._base_url,
            headers=headers,
            timeout=timeout,
        )

        # Metrics
        self._requests_made = 0
        self._total_input_tokens = 0
        self._total_output_tokens = 0
        self._errors = 0

    # ------------------------------------------------------------------ Props

    @property
    def provider(self) -> LLMProvider:
        return LLMProvider.OLLAMA

    @property
    def is_available(self) -> bool:
        # Local Ollama is considered available if the base URL is set.
        return bool(self._base_url)

    @property
    def model_name(self) -> str:
        return self._model

    # ------------------------------------------------------------------ Chat

    async def chat(
        self,
        messages: list[Message],
        system_prompt: str | None = None,
        tools: list[dict] | None = None,
        max_tokens: int = 4096,
        temperature: float = 0.7,
    ) -> LLMResponse:
        """Send chat completion to Ollama (OpenAI-compatible /v1)."""
        api_messages: list[dict[str, Any]] = []
        if system_prompt:
            api_messages.append({"role": "system", "content": system_prompt})
        for msg in messages:
            api_messages.append({"role": msg.role, "content": msg.content})

        payload: dict[str, Any] = {
            "model": self._model,
            "messages": api_messages,
            "max_tokens": max_tokens,
            "temperature": temperature,
        }

        if tools:
            payload["tools"] = tools
            payload["tool_choice"] = "auto"

        try:
            response = await self._client.post("/chat/completions", json=payload)
            response.raise_for_status()
            data = response.json()
            self._requests_made += 1

            choice = (data.get("choices") or [{}])[0]
            message = choice.get("message", {}) or {}
            usage = data.get("usage", {}) or {}

            input_tokens = int(usage.get("prompt_tokens", 0) or 0)
            output_tokens = int(usage.get("completion_tokens", 0) or 0)
            self._total_input_tokens += input_tokens
            self._total_output_tokens += output_tokens

            tool_calls: list[ToolCall] = []
            for idx, tc in enumerate(message.get("tool_calls") or []):
                fn = tc.get("function") or {}
                raw_args = fn.get("arguments", "{}")
                # Ollama may return arguments as dict or JSON string
                if isinstance(raw_args, str):
                    try:
                        args = json.loads(raw_args) if raw_args else {}
                    except json.JSONDecodeError:
                        logger.warning(
                            "ollama_tool_args_not_json",
                            tool=fn.get("name"),
                            raw=raw_args[:200],
                        )
                        args = {}
                else:
                    args = raw_args or {}

                tool_calls.append(
                    ToolCall(
                        id=tc.get("id") or f"call_{idx}_{uuid.uuid4().hex[:8]}",
                        name=fn.get("name", ""),
                        arguments=args,
                    )
                )

            logger.debug(
                "ollama_response",
                model=self._model,
                input_tokens=input_tokens,
                output_tokens=output_tokens,
                tool_calls=len(tool_calls),
            )

            return LLMResponse(
                content=message.get("content") or "",
                tool_calls=tool_calls,
                input_tokens=input_tokens,
                output_tokens=output_tokens,
                model=data.get("model", self._model),
                finish_reason=choice.get("finish_reason", ""),
            )

        except httpx.HTTPStatusError as e:
            self._errors += 1
            logger.error(
                "ollama_api_error",
                status=e.response.status_code,
                body=e.response.text[:400],
            )
            raise
        except httpx.RequestError as e:
            self._errors += 1
            logger.error("ollama_connection_error", error=str(e))
            raise
        except Exception as e:
            self._errors += 1
            logger.error("ollama_error", error=str(e))
            raise

    # ----------------------------------------------------------------- Metrics

    def get_metrics(self) -> dict:
        """Return usage metrics. Local inference cost is 0."""
        return {
            "provider": self.provider.value,
            "model": self._model,
            "baseUrl": self._base_url,
            "requestsMade": self._requests_made,
            "totalInputTokens": self._total_input_tokens,
            "totalOutputTokens": self._total_output_tokens,
            "totalTokens": self._total_input_tokens + self._total_output_tokens,
            "estimatedCostUsd": 0.0,
            "errors": self._errors,
        }

    async def close(self) -> None:
        """Close the HTTP client."""
        await self._client.aclose()
