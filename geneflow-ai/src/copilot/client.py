"""Claude API client for GeneFlow AI Copilot."""

from typing import Optional

import structlog
from anthropic import AsyncAnthropic

from src.config import Settings

logger = structlog.get_logger()


class ClaudeClient:
    """Async client for Claude API."""

    def __init__(self, settings: Settings):
        self._settings = settings
        self._client: Optional[AsyncAnthropic] = None
        self._requests_made = 0
        self._tokens_used = 0
        self._errors = 0

        if settings.claude_api_key:
            self._client = AsyncAnthropic(api_key=settings.claude_api_key)

    @property
    def is_configured(self) -> bool:
        """Check if Claude API is configured."""
        return self._client is not None

    @property
    def metrics(self) -> dict:
        """Get client metrics."""
        return {
            "requestsMade": self._requests_made,
            "tokensUsed": self._tokens_used,
            "errors": self._errors,
            "configured": self.is_configured,
        }

    async def create_message(
        self,
        system_prompt: str,
        user_message: str,
        max_tokens: Optional[int] = None,
    ) -> str:
        """
        Create a message using Claude API.

        Args:
            system_prompt: System prompt for context
            user_message: User message to process
            max_tokens: Maximum tokens in response

        Returns:
            Claude's response text

        Raises:
            RuntimeError: If Claude API is not configured
            Exception: If API call fails
        """
        if not self._client:
            raise RuntimeError("Claude API not configured. Set AI_CLAUDE_API_KEY.")

        try:
            response = await self._client.messages.create(
                model=self._settings.claude_model,
                max_tokens=max_tokens or self._settings.claude_max_tokens,
                system=system_prompt,
                messages=[{"role": "user", "content": user_message}],
            )

            self._requests_made += 1
            self._tokens_used += response.usage.input_tokens + response.usage.output_tokens

            logger.debug(
                "claude_message_created",
                model=self._settings.claude_model,
                input_tokens=response.usage.input_tokens,
                output_tokens=response.usage.output_tokens,
            )

            return response.content[0].text

        except Exception as e:
            self._errors += 1
            logger.error("claude_api_error", error=str(e))
            raise

    async def create_conversation(
        self,
        system_prompt: str,
        messages: list[dict],
        max_tokens: Optional[int] = None,
    ) -> str:
        """
        Create a conversation with multiple messages.

        Args:
            system_prompt: System prompt for context
            messages: List of message dicts with 'role' and 'content'
            max_tokens: Maximum tokens in response

        Returns:
            Claude's response text
        """
        if not self._client:
            raise RuntimeError("Claude API not configured. Set AI_CLAUDE_API_KEY.")

        try:
            response = await self._client.messages.create(
                model=self._settings.claude_model,
                max_tokens=max_tokens or self._settings.claude_max_tokens,
                system=system_prompt,
                messages=messages,
            )

            self._requests_made += 1
            self._tokens_used += response.usage.input_tokens + response.usage.output_tokens

            return response.content[0].text

        except Exception as e:
            self._errors += 1
            logger.error("claude_conversation_error", error=str(e))
            raise

    async def health_check(self) -> bool:
        """Check if Claude API is accessible."""
        return self.is_configured
