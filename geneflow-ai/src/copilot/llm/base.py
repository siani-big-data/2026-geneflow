"""Base classes for LLM providers."""

from abc import ABC, abstractmethod
from dataclasses import dataclass, field
from enum import Enum
from typing import Any


class LLMProvider(str, Enum):
    """Available LLM providers."""

    CLAUDE = "claude"
    DEEPSEEK = "deepseek"
    OPENAI = "openai"  # Future: GPT-4, etc.
    OLLAMA = "ollama"  # Local OpenAI-compatible (Qwen, Llama, Mistral, ...)


@dataclass
class Message:
    """Chat message."""

    role: str  # "system", "user", "assistant"
    content: str


@dataclass
class ToolCall:
    """Tool call from LLM."""

    id: str
    name: str
    arguments: dict[str, Any]


@dataclass
class LLMResponse:
    """Response from LLM."""

    content: str
    tool_calls: list[ToolCall] = field(default_factory=list)
    input_tokens: int = 0
    output_tokens: int = 0
    model: str = ""
    finish_reason: str = ""

    @property
    def has_tool_calls(self) -> bool:
        return len(self.tool_calls) > 0

    @property
    def total_tokens(self) -> int:
        return self.input_tokens + self.output_tokens


class LLMClient(ABC):
    """Abstract base class for LLM clients."""

    @property
    @abstractmethod
    def provider(self) -> LLMProvider:
        """Return the provider type."""
        pass

    @property
    @abstractmethod
    def is_available(self) -> bool:
        """Check if the client is configured and available."""
        pass

    @property
    @abstractmethod
    def model_name(self) -> str:
        """Return the model name being used."""
        pass

    @abstractmethod
    async def chat(
        self,
        messages: list[Message],
        system_prompt: str | None = None,
        tools: list[dict] | None = None,
        max_tokens: int = 4096,
        temperature: float = 0.7,
    ) -> LLMResponse:
        """Send chat completion request.

        Args:
            messages: List of conversation messages
            system_prompt: System prompt (optional)
            tools: List of tool definitions for function calling
            max_tokens: Maximum tokens in response
            temperature: Sampling temperature

        Returns:
            LLMResponse with content and/or tool calls
        """
        pass

    @abstractmethod
    def get_metrics(self) -> dict:
        """Return usage metrics."""
        pass
