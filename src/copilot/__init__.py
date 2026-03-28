"""Copilot module - Claude API integration for GeneFlow AI."""

from .agent import AgentContext, MolecularBiologyAgent
from .chat import ChatHandler
from .client import ClaudeClient
from .prompts import PromptTemplates
from .reports import ReportGenerator
from .tools import AGENT_TOOLS, get_tools_for_api

__all__ = [
    "AgentContext",
    "AGENT_TOOLS",
    "ChatHandler",
    "ClaudeClient",
    "MolecularBiologyAgent",
    "PromptTemplates",
    "ReportGenerator",
    "get_tools_for_api",
]
