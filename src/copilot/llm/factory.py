"""Factory for creating LLM clients."""

from typing import Optional

import structlog

from .base import LLMClient, LLMProvider
from .claude import ClaudeClient
from .deepseek import DeepSeekClient

logger = structlog.get_logger()

# Global client instances
_clients: dict[LLMProvider, LLMClient] = {}
_default_provider: LLMProvider = LLMProvider.CLAUDE


def create_llm_client(
    provider: LLMProvider,
    api_key: str,
    model: str | None = None,
    **kwargs,
) -> LLMClient:
    """Create an LLM client for the specified provider.

    Args:
        provider: The LLM provider to use
        api_key: API key for the provider
        model: Optional model name override
        **kwargs: Additional provider-specific arguments

    Returns:
        Configured LLMClient instance
    """
    if provider == LLMProvider.CLAUDE:
        return ClaudeClient(api_key=api_key, model=model)
    elif provider == LLMProvider.DEEPSEEK:
        return DeepSeekClient(api_key=api_key, model=model, **kwargs)
    else:
        raise ValueError(f"Unsupported LLM provider: {provider}")


def register_client(client: LLMClient) -> None:
    """Register a client globally."""
    _clients[client.provider] = client
    logger.info("llm_client_registered", provider=client.provider.value, model=client.model_name)


def get_llm_client(provider: Optional[LLMProvider] = None) -> Optional[LLMClient]:
    """Get a registered LLM client.

    Args:
        provider: Specific provider to get, or None for default

    Returns:
        LLMClient instance or None if not registered
    """
    target = provider or _default_provider
    return _clients.get(target)


def set_default_provider(provider: LLMProvider) -> None:
    """Set the default LLM provider."""
    global _default_provider
    _default_provider = provider
    logger.info("default_llm_provider_set", provider=provider.value)


def get_default_provider() -> LLMProvider:
    """Get the current default provider."""
    return _default_provider


def list_available_providers() -> list[dict]:
    """List all registered providers and their status."""
    return [
        {
            "provider": client.provider.value,
            "model": client.model_name,
            "available": client.is_available,
            "metrics": client.get_metrics(),
        }
        for client in _clients.values()
    ]


def get_cheapest_available() -> Optional[LLMClient]:
    """Get the cheapest available LLM client.

    Priority: DeepSeek > OpenAI > Claude
    """
    priority = [LLMProvider.DEEPSEEK, LLMProvider.OPENAI, LLMProvider.CLAUDE]

    for provider in priority:
        client = _clients.get(provider)
        if client and client.is_available:
            return client

    return None


def get_best_available() -> Optional[LLMClient]:
    """Get the best quality_enhanced available LLM client.

    Priority: Claude > OpenAI > DeepSeek
    """
    priority = [LLMProvider.CLAUDE, LLMProvider.OPENAI, LLMProvider.DEEPSEEK]

    for provider in priority:
        client = _clients.get(provider)
        if client and client.is_available:
            return client

    return None
