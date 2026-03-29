"""API for GeneFlow AI service."""

from datetime import datetime, timezone
from typing import Any, Optional

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel

from src.models import ServiceStatus

# =============================================================================
# Request/Response Models
# =============================================================================


class HealthResponse(BaseModel):
    """Health check response."""

    status: str
    timestamp: str
    version: str = "1.0.0"
    redis: str = "unknown"
    claude: str = "unknown"
    blast: str = "unknown"


class ServiceHealthInfo(BaseModel):
    """Detailed service health info."""

    status: str
    analysesCompleted: int
    analysesFailed: int
    lastAnalysisAt: Optional[str] = None
    averageProcessingTimeMs: float


class CopilotAskRequest(BaseModel):
    """Request for Copilot ask endpoint."""

    question: str
    conversationId: Optional[str] = None
    traceId: Optional[str] = None


class CopilotAskResponse(BaseModel):
    """Response from Copilot ask endpoint."""

    answer: str
    conversationId: str
    messageCount: int = 0
    error: bool = False


class BlastSearchRequest(BaseModel):
    """Request for BLAST search endpoint."""

    sequence: str
    program: str = "blastn"
    database: str = "nt"


class BlastSearchResponse(BaseModel):
    """Response from BLAST search endpoint."""

    rid: str
    status: str
    hitCount: int = 0
    topHitAccession: Optional[str] = None
    topHitOrganism: Optional[str] = None


class AgentAskRequest(BaseModel):
    """Request for Agent ask endpoint."""

    question: str
    contextId: Optional[str] = None
    traceId: Optional[str] = None


class AgentAskResponse(BaseModel):
    """Response from Agent ask endpoint."""

    answer: str
    contextId: str
    traceId: Optional[str] = None
    messageCount: int = 0
    error: bool = False


# =============================================================================
# Global State (set by main.py)
# =============================================================================

_service_status: ServiceStatus = ServiceStatus.STOPPED
_redis_healthy: bool = False
_claude_configured: bool = False
_blast_configured: bool = False
_metrics: dict = {}
_chat_handler: Any = None
_report_generator: Any = None
_blast_client: Any = None
_agent: Any = None


# =============================================================================
# App Factory
# =============================================================================


def create_app() -> FastAPI:
    """Create FastAPI application."""
    app = FastAPI(
        title="GeneFlow AI",
        description="AI-powered sequence analysis for GeneFlow platform",
        version="1.0.0",
        docs_url="/docs" if True else None,  # Enable in dev
        redoc_url=None,
    )

    # =========================================================================
    # Health Endpoints
    # =========================================================================

    @app.get("/health", response_model=HealthResponse)
    async def health_check() -> HealthResponse:
        """
        Health check endpoint for Docker/K8s.

        Returns service status and component health.
        """
        if _service_status == ServiceStatus.RUNNING and _redis_healthy:
            overall_status = "healthy"
        elif _service_status == ServiceStatus.RUNNING:
            overall_status = "degraded"
        else:
            overall_status = "unhealthy"

        return HealthResponse(
            status=overall_status,
            timestamp=datetime.now(timezone.utc).isoformat(),
            redis="connected" if _redis_healthy else "disconnected",
            claude="configured" if _claude_configured else "not_configured",
            blast="configured" if _blast_configured else "not_configured",
        )

    @app.get("/health/details", response_model=ServiceHealthInfo)
    async def health_details() -> ServiceHealthInfo:
        """Get detailed health info."""
        return ServiceHealthInfo(
            status=_service_status.value,
            analysesCompleted=_metrics.get("analysesCompleted", 0),
            analysesFailed=_metrics.get("analysesFailed", 0),
            lastAnalysisAt=_metrics.get("lastAnalysisAt"),
            averageProcessingTimeMs=_metrics.get("averageProcessingTimeMs", 0.0),
        )

    @app.get("/ready")
    async def readiness_check() -> dict:
        """
        Readiness check for K8s.

        Returns 200 if service is ready to handle requests.
        """
        if _service_status != ServiceStatus.RUNNING:
            raise HTTPException(status_code=503, detail="Service not running")

        if not _redis_healthy:
            raise HTTPException(status_code=503, detail="Redis not connected")

        return {"ready": True}

    @app.get("/live")
    async def liveness_check() -> dict:
        """
        Liveness check for K8s.

        Returns 200 if the process is alive.
        """
        return {"alive": True}

    # =========================================================================
    # Copilot Endpoints
    # =========================================================================

    @app.post("/copilot/ask", response_model=CopilotAskResponse)
    async def copilot_ask(request: CopilotAskRequest) -> CopilotAskResponse:
        """
        Ask a question to the Copilot.

        Optionally provide a conversation ID to continue a conversation.
        """
        if not _chat_handler:
            raise HTTPException(status_code=503, detail="Copilot service not initialized")

        if not _chat_handler.is_available:
            raise HTTPException(status_code=503, detail="Claude API not configured")

        result = await _chat_handler.ask(
            question=request.question,
            conversation_id=request.conversationId,
        )

        return CopilotAskResponse(
            answer=result.get("answer", ""),
            conversationId=result.get("conversationId", ""),
            messageCount=result.get("messageCount", 0),
            error=result.get("error", False),
        )

    @app.get("/copilot/conversations")
    async def copilot_list_conversations() -> dict:
        """List all active conversations."""
        if not _chat_handler:
            raise HTTPException(status_code=503, detail="Copilot service not initialized")

        return {"conversations": _chat_handler.list_conversations()}

    @app.delete("/copilot/conversations/{conversation_id}")
    async def copilot_delete_conversation(conversation_id: str) -> dict:
        """Delete a conversation."""
        if not _chat_handler:
            raise HTTPException(status_code=503, detail="Copilot service not initialized")

        deleted = _chat_handler.delete_conversation(conversation_id)
        if not deleted:
            raise HTTPException(status_code=404, detail="Conversation not found")

        return {"deleted": True, "conversationId": conversation_id}

    @app.get("/copilot/status")
    async def copilot_status() -> dict:
        """Get Copilot service status."""
        return {
            "available": _chat_handler.is_available if _chat_handler else False,
            "configured": _claude_configured,
            "activeConversations": (
                len(_chat_handler.list_conversations()) if _chat_handler else 0
            ),
        }

    # =========================================================================
    # Agent Endpoints (Molecular Biology Agent with Tools)
    # =========================================================================

    @app.post("/agent/ask", response_model=AgentAskResponse)
    async def agent_ask(request: AgentAskRequest) -> AgentAskResponse:
        """
        Ask a question to the Molecular Biology Agent.

        The agent can use tools to:
        - Get trace analysis data
        - Execute BLAST searches
        - Explain variants
        - Compare sequences

        Provide a traceId to give context about a specific trace.
        """
        if not _agent:
            raise HTTPException(status_code=503, detail="Agent not initialized")

        if not _agent.is_available:
            raise HTTPException(status_code=503, detail="Claude API not configured")

        result = await _agent.ask(
            question=request.question,
            context_id=request.contextId,
            trace_id=request.traceId,
        )

        return AgentAskResponse(
            answer=result.get("answer", ""),
            contextId=result.get("contextId", ""),
            traceId=result.get("traceId"),
            messageCount=result.get("messageCount", 0),
            error=result.get("error", False),
        )

    @app.get("/agent/contexts")
    async def agent_list_contexts() -> dict:
        """List all active agent contexts."""
        if not _agent:
            raise HTTPException(status_code=503, detail="Agent not initialized")

        return {"contexts": _agent.list_contexts()}

    @app.delete("/agent/contexts/{context_id}")
    async def agent_delete_context(context_id: str) -> dict:
        """Delete an agent context."""
        if not _agent:
            raise HTTPException(status_code=503, detail="Agent not initialized")

        deleted = _agent.delete_context(context_id)
        if not deleted:
            raise HTTPException(status_code=404, detail="Context not found")

        return {"deleted": True, "contextId": context_id}

    @app.get("/agent/status")
    async def agent_status() -> dict:
        """Get Agent service status."""
        if not _agent:
            return {
                "available": False,
                "configured": False,
                "activeContexts": 0,
            }

        metrics = _agent.metrics
        return {
            "available": metrics.get("available", False),
            "configured": _claude_configured,
            "activeContexts": metrics.get("activeContexts", 0),
            "requests": metrics.get("requests", 0),
            "toolCalls": metrics.get("toolCalls", 0),
        }

    @app.get("/agent/tools")
    async def agent_list_tools() -> dict:
        """List available agent tools."""
        from src.copilot.tools import AGENT_TOOLS

        tools = [{"name": t["name"], "description": t["description"]} for t in AGENT_TOOLS]
        return {"tools": tools}

    # =========================================================================
    # BLAST Endpoints
    # =========================================================================

    @app.post("/blast/search", response_model=BlastSearchResponse)
    async def blast_search(request: BlastSearchRequest) -> BlastSearchResponse:
        """
        Submit a BLAST search and wait for results.

        This is a synchronous endpoint that waits for the search to complete.
        For long sequences, consider using the async endpoint.
        """
        if not _blast_client:
            raise HTTPException(status_code=503, detail="BLAST service not initialized")

        if not _blast_client.is_configured:
            raise HTTPException(status_code=503, detail="BLAST not configured. Set AI_BLAST_EMAIL.")

        try:
            result = await _blast_client.search(
                sequence=request.sequence,
                program=request.program,
                database=request.database,
            )

            top_hit = result.topHit

            return BlastSearchResponse(
                rid=result.rid,
                status="completed",
                hitCount=result.hitCount,
                topHitAccession=top_hit.accession if top_hit else None,
                topHitOrganism=top_hit.organism if top_hit else None,
            )

        except Exception as e:
            raise HTTPException(status_code=500, detail=str(e))

    @app.post("/blast/submit")
    async def blast_submit(request: BlastSearchRequest) -> dict:
        """
        Submit a BLAST search job without waiting for results.

        Returns a job ID (RID) that can be used to check status and get results.
        """
        if not _blast_client:
            raise HTTPException(status_code=503, detail="BLAST service not initialized")

        if not _blast_client.is_configured:
            raise HTTPException(status_code=503, detail="BLAST not configured. Set AI_BLAST_EMAIL.")

        try:
            job = await _blast_client.submit_search(
                sequence=request.sequence,
                program=request.program,
                database=request.database,
            )

            return job.to_dict()

        except Exception as e:
            raise HTTPException(status_code=500, detail=str(e))

    @app.get("/blast/status/{rid}")
    async def blast_status(rid: str) -> dict:
        """Get status of a BLAST job."""
        if not _blast_client:
            raise HTTPException(status_code=503, detail="BLAST service not initialized")

        try:
            status = await _blast_client.check_status(rid)
            return {"rid": rid, "status": status}

        except Exception as e:
            raise HTTPException(status_code=500, detail=str(e))

    @app.get("/blast/results/{rid}")
    async def blast_results(rid: str) -> dict:
        """Get results of a completed BLAST job."""
        if not _blast_client:
            raise HTTPException(status_code=503, detail="BLAST service not initialized")

        try:
            result = await _blast_client.get_results(rid)
            return result.to_dict()

        except Exception as e:
            raise HTTPException(status_code=500, detail=str(e))

    @app.get("/blast/metrics")
    async def blast_metrics() -> dict:
        """Get BLAST service metrics."""
        if not _blast_client:
            return {"configured": False}

        return _blast_client.metrics

    return app


# =============================================================================
# State Setters (called by main.py)
# =============================================================================


def set_service_status(status: ServiceStatus) -> None:
    """Set service status."""
    global _service_status
    _service_status = status


def set_redis_health(healthy: bool) -> None:
    """Set Redis connection health status."""
    global _redis_healthy
    _redis_healthy = healthy


def set_claude_configured(configured: bool) -> None:
    """Set Claude API configuration status."""
    global _claude_configured
    _claude_configured = configured


def set_metrics(metrics: dict) -> None:
    """Set service metrics."""
    global _metrics
    _metrics = metrics


def set_chat_handler(handler: Any) -> None:
    """Set chat handler instance."""
    global _chat_handler
    _chat_handler = handler


def set_report_generator(generator: Any) -> None:
    """Set report generator instance."""
    global _report_generator
    _report_generator = generator


def set_blast_client(client: Any) -> None:
    """Set BLAST client instance."""
    global _blast_client, _blast_configured
    _blast_client = client
    _blast_configured = client.is_configured if client else False


def set_agent(agent: Any) -> None:
    """Set molecular biology agent instance."""
    global _agent
    _agent = agent


# Create app instance
app = create_app()
