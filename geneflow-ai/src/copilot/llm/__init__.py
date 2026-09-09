"""LLM providers abstraction layer.

Supports multiple LLM backends:
- Claude (Anthropic) - High quality_enhanced, higher cost
- DeepSeek - Good quality_enhanced, very low cost ($0.07/100K tokens)

Usage:
    from src.copilot.llm import get_llm_client, LLMProvider

    client = get_llm_client(LLMProvider.DEEPSEEK)
    response = await client.chat(messages=[...])
"""

from .base import LLMClient, LLMProvider, LLMResponse, Message
from .factory import create_llm_client, get_llm_client

__all__ = [
    "LLMClient",
    "LLMProvider",
    "LLMResponse",
    "Message",
    "get_llm_client",
    "create_llm_client",
]
