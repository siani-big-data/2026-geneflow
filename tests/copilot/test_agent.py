"""Tests for Molecular Biology Agent."""

from unittest.mock import AsyncMock, patch

import pytest

from src.config import Settings
from src.copilot.agent import AgentContext, MolecularBiologyAgent
from src.copilot.tools import AGENT_TOOLS, get_tool_names, get_tools_for_api
from src.models import AnalysisResult, AnalysisStatus


@pytest.fixture
def settings_without_api_key():
    """Settings without API key."""
    return Settings(claude_api_key="")


@pytest.fixture
def settings_with_api_key():
    """Settings with API key."""
    return Settings(claude_api_key="test-api-key")


@pytest.fixture
def sample_analysis():
    """Create a sample analysis result."""
    return AnalysisResult(
        traceId="trace-123",
        studyId="study-456",
        analysisId="analysis-789",
        status=AnalysisStatus.COMPLETED,
        overallConfidence=0.95,
    )


class TestAgentContext:
    """Tests for AgentContext."""

    def test_context_creation(self):
        """Context is created with defaults."""
        ctx = AgentContext()

        assert ctx.contextId
        assert ctx.messages == []
        assert ctx.traceId is None

    def test_context_with_trace(self):
        """Context can be created with trace ID."""
        ctx = AgentContext(traceId="TR-123")

        assert ctx.traceId == "TR-123"

    def test_add_message(self):
        """add_message adds messages to context."""
        ctx = AgentContext()
        ctx.add_message("user", "Hello")
        ctx.add_message("assistant", "Hi!")

        assert len(ctx.messages) == 2
        assert ctx.messages[0]["role"] == "user"
        assert ctx.messages[1]["content"] == "Hi!"

    def test_to_dict(self):
        """to_dict serializes context."""
        ctx = AgentContext(
            contextId="ctx-123",
            traceId="TR-456",
            studyId="study-789",
        )
        ctx.add_message("user", "Test")

        data = ctx.to_dict()

        assert data["contextId"] == "ctx-123"
        assert data["traceId"] == "TR-456"
        assert data["messageCount"] == 1


class TestAgentTools:
    """Tests for agent tools."""

    def test_tools_defined(self):
        """Agent tools are defined."""
        assert len(AGENT_TOOLS) >= 5

    def test_get_tool_names(self):
        """get_tool_names returns tool names."""
        names = get_tool_names()

        assert "get_trace_analysis" in names
        assert "search_blast" in names
        assert "explain_variant" in names

    def test_get_tools_for_api(self):
        """get_tools_for_api returns properly formatted tools."""
        tools = get_tools_for_api()

        for tool in tools:
            assert "name" in tool
            assert "description" in tool
            assert "input_schema" in tool


class TestMolecularBiologyAgent:
    """Tests for MolecularBiologyAgent."""

    def test_not_available_without_api_key(self, settings_without_api_key):
        """Agent is not available without API key."""
        agent = MolecularBiologyAgent(settings_without_api_key)

        assert agent.is_available is False

    def test_available_with_api_key(self, settings_with_api_key):
        """Agent is available with API key."""
        with patch("src.copilot.agent.AsyncAnthropic"):
            agent = MolecularBiologyAgent(settings_with_api_key)

            assert agent.is_available is True

    def test_metrics_initial(self, settings_without_api_key):
        """Initial metrics are zero."""
        agent = MolecularBiologyAgent(settings_without_api_key)
        metrics = agent.metrics

        assert metrics["requests"] == 0
        assert metrics["toolCalls"] == 0
        assert metrics["errors"] == 0
        assert metrics["activeContexts"] == 0

    def test_list_contexts_empty(self, settings_without_api_key):
        """list_contexts returns empty list initially."""
        agent = MolecularBiologyAgent(settings_without_api_key)

        assert agent.list_contexts() == []

    def test_get_context_not_found(self, settings_without_api_key):
        """get_context returns None for unknown context."""
        agent = MolecularBiologyAgent(settings_without_api_key)

        assert agent.get_context("unknown") is None

    def test_delete_context(self, settings_without_api_key):
        """delete_context removes context."""
        agent = MolecularBiologyAgent(settings_without_api_key)
        agent._contexts["ctx-123"] = AgentContext(contextId="ctx-123")

        result = agent.delete_context("ctx-123")

        assert result is True
        assert "ctx-123" not in agent._contexts

    def test_delete_context_not_found(self, settings_without_api_key):
        """delete_context returns False for unknown context."""
        agent = MolecularBiologyAgent(settings_without_api_key)

        result = agent.delete_context("unknown")

        assert result is False

    @pytest.mark.asyncio
    async def test_ask_not_available(self, settings_without_api_key):
        """ask returns error when not available."""
        agent = MolecularBiologyAgent(settings_without_api_key)

        result = await agent.ask("Hello?")

        assert result["error"] is True
        assert "no disponible" in result["answer"].lower()

    def test_set_trace_provider(self, settings_without_api_key, sample_analysis):
        """set_trace_provider sets the provider function."""
        agent = MolecularBiologyAgent(settings_without_api_key)

        def provider(trace_id: str):
            return sample_analysis if trace_id == "trace-123" else None

        agent.set_trace_provider(provider)

        assert agent._trace_provider is not None

    def test_set_blast_handler(self, settings_without_api_key):
        """set_blast_handler sets the handler function."""
        agent = MolecularBiologyAgent(settings_without_api_key)

        async def handler(params: dict):
            return {"hits": []}

        agent.set_blast_handler(handler)

        assert agent._blast_handler is not None


class TestAgentToolHandlers:
    """Tests for agent tool handlers."""

    @pytest.fixture
    def agent(self, settings_without_api_key):
        """Create agent for testing."""
        return MolecularBiologyAgent(settings_without_api_key)

    @pytest.mark.asyncio
    async def test_handle_explain_variant(self, agent):
        """_handle_explain_variant returns variant info."""
        params = {
            "position": 100,
            "reference": "A",
            "alternate": "G",
            "gene": "BRCA1",
        }

        result = await agent._handle_explain_variant(params)

        assert result["position"] == 100
        assert result["change"] == "A>G"
        assert result["type"] == "SNP"
        assert result["gene"] == "BRCA1"
        assert "interpretation" in result

    @pytest.mark.asyncio
    async def test_handle_explain_variant_insertion(self, agent):
        """_handle_explain_variant identifies insertions."""
        params = {
            "position": 50,
            "reference": "A",
            "alternate": "ATG",
        }

        result = await agent._handle_explain_variant(params)

        assert result["type"] == "inserción"

    @pytest.mark.asyncio
    async def test_handle_explain_variant_deletion(self, agent):
        """_handle_explain_variant identifies deletions."""
        params = {
            "position": 50,
            "reference": "ATG",
            "alternate": "A",
        }

        result = await agent._handle_explain_variant(params)

        assert result["type"] == "deleción"

    @pytest.mark.asyncio
    async def test_handle_compaREDACTED(self, agent):
        """_handle_compaREDACTED compares sequences."""
        params = {
            "sequence1": "ATCGATCG",
            "sequence2": "ATCGTTCG",
        }

        result = await agent._handle_compaREDACTED(params)

        assert result["length1"] == 8
        assert result["length2"] == 8
        assert result["totalDifferences"] == 1
        assert result["identity"] == 87.5

    @pytest.mark.asyncio
    async def test_handle_compaREDACTED(self, agent):
        """_handle_compaREDACTED handles empty input."""
        params = {
            "sequence1": "",
            "sequence2": "ATCG",
        }

        result = await agent._handle_compaREDACTED(params)

        assert "error" in result

    @pytest.mark.asyncio
    async def test_handle_search_blast_not_configured(self, agent):
        """_handle_search_blast returns error when not configured."""
        params = {"sequence": "ATCGATCGATCGATCGATCG"}

        result = await agent._handle_search_blast(params)

        assert "error" in result

    @pytest.mark.asyncio
    async def test_handle_search_blast_short_sequence(self, agent):
        """_handle_search_blast rejects short sequences."""
        agent._blast_handler = AsyncMock()
        params = {"sequence": "ATCG"}  # Too short

        result = await agent._handle_search_blast(params)

        assert "error" in result
        assert "corta" in result["error"].lower()

    @pytest.mark.asyncio
    async def test_handle_get_trace_no_provider(self, agent):
        """_handle_get_trace returns error without provider."""
        ctx = AgentContext(traceId="TR-123")
        params = {"traceId": "TR-123"}

        result = await agent._handle_get_trace(params, ctx)

        assert "error" in result

    @pytest.mark.asyncio
    async def test_handle_get_quality_from_context(self, agent):
        """_handle_get_quality returns quality from context."""
        ctx = AgentContext(traceId="TR-123")
        ctx.analysisData = {
            "quality": {
                "predictedAccuracy": 0.95,
                "errorProbability": 0.05,
            }
        }
        params = {}

        result = await agent._handle_get_quality(params, ctx)

        assert result["predictedAccuracy"] == 0.95
