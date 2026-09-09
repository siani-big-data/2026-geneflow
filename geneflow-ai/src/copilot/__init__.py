"""Copilot module - Claude API integration with ML models for GeneFlow AI.

This module provides the AI-powered copilot for DNA sequence analysis,
combining Claude LLM capabilities with neural network models for:

- Taxonomy classification (kingdom to genus)
- Heterozygote detection
- Quality classification and prediction
- Sequence trimming recommendations
- BLAST integration
- Variant interpretation

Components:
    - MolecularBiologyAgent: Main agent combining Claude + ML tools
    - GeneFlowAIService: Unified ML model management
    - ChatHandler: Simple chat interface
    - ReportGenerator: Structured report generation

Usage:
    from src.copilot import MolecularBiologyAgent, initialize_ai_service

    # Initialize
    agent = MolecularBiologyAgent(settings)
    await agent.initialize()

    # Ask questions
    response = await agent.ask("¿Qué organismo es esta secuencia?")
"""

from .agent import AgentContext, MolecularBiologyAgent
from .ai_service import (
    AIServiceConfig,
    GeneFlowAIService,
    ModelInfo,
    get_ai_service,
    initialize_ai_service,
)
from .chat import ChatHandler
from .client import ClaudeClient
from .prompts import PromptTemplates
from .reports import ReportGenerator
from .tools import (
    AGENT_TOOLS,
    ANALYSIS_TOOLS,
    EXTERNAL_TOOLS,
    ML_TOOLS,
    get_all_tools,
    get_ml_tool_names,
    get_tool_by_name,
    get_tool_names,
    get_tools_for_api,
)

__all__ = [
    # Agent
    "AgentContext",
    "MolecularBiologyAgent",
    # AI Service
    "AIServiceConfig",
    "GeneFlowAIService",
    "ModelInfo",
    "get_ai_service",
    "initialize_ai_service",
    # Chat & Client
    "ChatHandler",
    "ClaudeClient",
    # Prompts & Reports
    "PromptTemplates",
    "ReportGenerator",
    # Tools
    "AGENT_TOOLS",
    "ANALYSIS_TOOLS",
    "EXTERNAL_TOOLS",
    "ML_TOOLS",
    "get_all_tools",
    "get_ml_tool_names",
    "get_tool_by_name",
    "get_tool_names",
    "get_tools_for_api",
]
