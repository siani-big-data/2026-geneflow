"""Molecular Biology Agent for GeneFlow AI.

This module provides the main agent that powers the GeneFlow copilot,
combining Claude LLM capabilities with ML models and external APIs.
"""

from dataclasses import dataclass, field
from datetime import datetime, timezone
from typing import Any, Callable, Optional
from uuid import uuid4

import numpy as np
import structlog
from anthropic import AsyncAnthropic

from src.config import Settings
from src.models import AnalysisResult

from .ai_service import GeneFlowAIService, initialize_ai_service
from .tools import get_tools_for_api

logger = structlog.get_logger()


@dataclass
class AgentContext:
    """Context for an agent conversation."""

    contextId: str = field(default_factory=lambda: str(uuid4()))
    traceId: Optional[str] = None
    studyId: Optional[str] = None
    analysisData: Optional[dict] = None
    messages: list[dict] = field(default_factory=list)
    createdAt: datetime = field(default_factory=lambda: datetime.now(timezone.utc))

    def add_message(self, role: str, content: Any) -> None:
        """Add a message to context."""
        self.messages.append({"role": role, "content": content})

    def to_dict(self) -> dict:
        """Serialize to dict."""
        return {
            "contextId": self.contextId,
            "traceId": self.traceId,
            "studyId": self.studyId,
            "messageCount": len(self.messages),
            "createdAt": self.createdAt.isoformat(),
        }


ToolHandler = Callable[[dict], Any]


class MolecularBiologyAgent:
    """
    Agent especializado en biología molecular.

    Usa Claude con herramientas para responder preguntas sobre
    secuencias, análisis BLAST, variantes y anotaciones.

    Integra modelos de ML para:
    - Clasificación taxonómica jerárquica
    - Detección de heterocigotos
    - Predicción de puntos de recorte
    - Clasificación de calidad
    """

    SYSTEM_PROMPT = """Eres un experto en biología molecular y bioinformática, \
especializado en análisis de secuenciación Sanger.

Tu rol es ayudar a investigadores a interpretar resultados de secuenciación:
- Análisis de calidad de secuencias
- Identificación de organismos mediante BLAST o clasificación por ML
- Detección e interpretación de variantes
- Anotación de características genómicas

Tienes acceso a herramientas para:
1. Consultar datos de trazas y análisis previos
2. Ejecutar búsquedas BLAST en NCBI
3. Clasificar taxonómicamente secuencias (modelo de red neuronal)
4. Detectar posiciones heterocigotas en cromatogramas
5. Predecir puntos óptimos de recorte por calidad
6. Clasificar calidad por posición

Modelos de ML disponibles:
- TaxonomyClassifier: Clasifica secuencias desde reino hasta género
- HeterozygoteClassifier: Detecta posiciones con doble pico (SNPs)
- TrimmingPredictor: Recomienda puntos de recorte
- QualityClassifier: Clasifica calidad en bins Q10-Q50+

Directrices:
- Responde siempre en español
- Sé preciso y técnico pero accesible
- Cuando uses una herramienta, explica brevemente por qué
- Si los datos son insuficientes para una conclusión, indícalo
- Sugiere análisis adicionales cuando sea apropiado
- Usa nomenclatura estándar (HGVS para variantes, etc.)
- Indica el nivel de confianza de las predicciones de ML

Contexto actual del usuario:
{context}"""

    def __init__(
        self,
        settings: Settings,
        trace_provider: Optional[Callable[[str], Optional[AnalysisResult]]] = None,
        blast_handler: Optional[ToolHandler] = None,
        ai_service: Optional[GeneFlowAIService] = None,
    ):
        self._settings = settings
        self._client: Optional[AsyncAnthropic] = None
        self._contexts: dict[str, AgentContext] = {}
        self._trace_provider = trace_provider
        self._blast_handler = blast_handler
        self._ai_service = ai_service

        # Tool handlers - core tools
        self._tool_handlers: dict[str, ToolHandler] = {
            # Core tools
            "get_trace_analysis": self._handle_get_trace,
            "search_blast": self._handle_search_blast,
            "get_quality_assessment": self._handle_get_quality,
            "explain_variant": self._handle_explain_variant,
            "compaREDACTED": self._handle_compaREDACTED,
            # ML-powered tools
            "classify_taxonomy": self._handle_classify_taxonomy,
            "detect_heterozygotes": self._handle_detect_heterozygotes,
            "predict_trim_points": self._handle_predict_trim_points,
            "classify_quality_ml": self._handle_classify_quality_ml,
            "analyze_trace_ml": self._handle_analyze_trace_ml,
        }

        if settings.claude_api_key:
            self._client = AsyncAnthropic(api_key=settings.claude_api_key)

        # Metrics
        self._requests = 0
        self._tool_calls = 0
        self._errors = 0
        self._ml_calls = 0

    @property
    def is_available(self) -> bool:
        """Check if agent is available."""
        return self._client is not None

    @property
    def metrics(self) -> dict:
        """Get agent metrics."""
        return {
            "requests": self._requests,
            "toolCalls": self._tool_calls,
            "mlCalls": self._ml_calls,
            "errors": self._errors,
            "activeContexts": len(self._contexts),
            "available": self.is_available,
            "mlModelsLoaded": self._ai_service.loaded_models if self._ai_service else [],
        }

    async def initialize(self) -> None:
        """Initialize the agent and its dependencies."""
        if self._ai_service is None:
            self._ai_service = await initialize_ai_service()
        elif not self._ai_service.is_initialized:
            await self._ai_service.initialize()

        logger.info(
            "agent_initialized",
            claude_available=self.is_available,
            ml_models=self._ai_service.available_models if self._ai_service else [],
        )

    def set_trace_provider(self, provider: Callable[[str], Optional[AnalysisResult]]) -> None:
        """Set the trace data provider function."""
        self._trace_provider = provider

    def set_blast_handler(self, handler: ToolHandler) -> None:
        """Set the BLAST search handler."""
        self._blast_handler = handler

    def set_ai_service(self, service: GeneFlowAIService) -> None:
        """Set the AI service for ML models."""
        self._ai_service = service

    def get_context(self, context_id: str) -> Optional[AgentContext]:
        """Get a context by ID."""
        return self._contexts.get(context_id)

    def list_contexts(self) -> list[dict]:
        """List all active contexts."""
        return [ctx.to_dict() for ctx in self._contexts.values()]

    def delete_context(self, context_id: str) -> bool:
        """Delete a context."""
        if context_id in self._contexts:
            del self._contexts[context_id]
            return True
        return False

    async def ask(
        self,
        question: str,
        context_id: Optional[str] = None,
        trace_id: Optional[str] = None,
    ) -> dict[str, Any]:
        """
        Ask a question to the agent.

        Args:
            question: User's question
            context_id: Optional context ID to continue conversation
            trace_id: Optional trace ID for context

        Returns:
            Dict with answer and metadata
        """
        if not self._client:
            return {
                "answer": "Agente no disponible. Configure AI_CLAUDE_API_KEY.",
                "error": True,
            }

        # Ensure AI service is initialized
        if self._ai_service is None or not self._ai_service.is_initialized:
            await self.initialize()

        # Get or create context
        if context_id and context_id in self._contexts:
            context = self._contexts[context_id]
        else:
            context = AgentContext(
                contextId=context_id or str(uuid4()),
                traceId=trace_id,
            )
            self._contexts[context.contextId] = context

        # Update trace if provided
        if trace_id and trace_id != context.traceId:
            context.traceId = trace_id
            context.analysisData = None  # Clear cached data

        # Load trace data if needed
        if context.traceId and not context.analysisData:
            await self._load_trace_data(context)

        # Build context string for system prompt
        context_str = self._build_context_string(context)
        system_prompt = self.SYSTEM_PROMPT.format(context=context_str)

        # Add user message
        context.add_message("user", question)

        try:
            # Call Claude with tools
            response = await self._run_agent_loop(
                system_prompt=system_prompt,
                messages=context.messages,
                context=context,
            )

            # Add assistant response
            context.add_message("assistant", response)

            self._requests += 1

            logger.info(
                "agent_question_answered",
                context_id=context.contextId,
                trace_id=context.traceId,
                question_length=len(question),
                response_length=len(response),
            )

            return {
                "answer": response,
                "contextId": context.contextId,
                "traceId": context.traceId,
                "messageCount": len(context.messages),
            }

        except Exception as e:
            self._errors += 1
            logger.error("agent_error", error=str(e))
            return {
                "answer": f"Error al procesar: {str(e)}",
                "error": True,
                "contextId": context.contextId,
            }

    async def _run_agent_loop(
        self,
        system_prompt: str,
        messages: list[dict],
        context: AgentContext,
        max_iterations: int = 5,
    ) -> str:
        """
        Run the agent loop with tool use.

        Continues until Claude returns a final text response
        or max iterations reached.
        """
        current_messages = list(messages)

        for _ in range(max_iterations):
            response = await self._client.messages.create(
                model=self._settings.claude_model,
                max_tokens=self._settings.claude_max_tokens,
                system=system_prompt,
                tools=get_tools_for_api(),
                messages=current_messages,
            )

            # Check if we have a final text response
            if response.stop_reason == "end_turn":
                # Extract text from response
                text_parts = []
                for block in response.content:
                    if block.type == "text":
                        text_parts.append(block.text)
                return "\n".join(text_parts)

            # Handle tool use
            if response.stop_reason == "tool_use":
                # Process all tool calls
                tool_results = []
                for block in response.content:
                    if block.type == "tool_use":
                        self._tool_calls += 1
                        result = await self._execute_tool(block.name, block.input, context)
                        tool_results.append(
                            {
                                "type": "tool_result",
                                "tool_use_id": block.id,
                                "content": str(result),
                            }
                        )

                # Add assistant message with tool use
                current_messages.append(
                    {
                        "role": "assistant",
                        "content": response.content,
                    }
                )

                # Add tool results
                current_messages.append(
                    {
                        "role": "user",
                        "content": tool_results,
                    }
                )

        # Max iterations reached
        return "Se alcanzó el límite de iteraciones. Por favor, reformula tu pregunta."

    async def _execute_tool(self, tool_name: str, tool_input: dict, context: AgentContext) -> Any:
        """Execute a tool and return result."""
        logger.debug("executing_tool", tool=tool_name, input=tool_input)

        handler = self._tool_handlers.get(tool_name)
        if not handler:
            return f"Herramienta '{tool_name}' no disponible."

        try:
            # Pass context for tools that need it
            if tool_name in (
                "get_trace_analysis",
                "get_quality_assessment",
                "detect_heterozygotes",
                "predict_trim_points",
                "classify_quality_ml",
                "analyze_trace_ml",
            ):
                return await handler(tool_input, context)
            else:
                return await handler(tool_input)
        except Exception as e:
            logger.error("tool_error", tool=tool_name, error=str(e))
            return f"Error ejecutando {tool_name}: {str(e)}"

    async def _load_trace_data(self, context: AgentContext) -> None:
        """Load trace data into context."""
        if not self._trace_provider or not context.traceId:
            return

        try:
            analysis = self._trace_provider(context.traceId)
            if analysis:
                context.analysisData = analysis.to_dict()
                context.studyId = analysis.studyId
                logger.debug("trace_data_loaded", trace_id=context.traceId)
        except Exception as e:
            logger.warning("trace_data_load_failed", error=str(e))

    def _build_context_string(self, context: AgentContext) -> str:
        """Build context string for system prompt."""
        parts = []

        if context.traceId:
            parts.append(f"Traza activa: {context.traceId}")

        if context.studyId:
            parts.append(f"Estudio: {context.studyId}")

        if context.analysisData:
            data = context.analysisData
            parts.append(f"Estado del análisis: {data.get('status', 'desconocido')}")

            if data.get("blastHits"):
                top_hit = data["blastHits"][0]
                parts.append(
                    f"Organismo identificado: {top_hit.get('organism', 'N/A')} "
                    f"({top_hit.get('identity', 0):.1f}% identidad)"
                )

            if data.get("variants"):
                parts.append(f"Variantes detectadas: {len(data['variants'])}")

        # Add ML service info
        if self._ai_service and self._ai_service.is_initialized:
            parts.append(f"Modelos ML disponibles: {', '.join(self._ai_service.available_models)}")

        if not parts:
            return "Sin contexto de traza activo."

        return "\n".join(parts)

    # =========================================================================
    # Core Tool Handlers
    # =========================================================================

    async def _handle_get_trace(self, params: dict, context: AgentContext) -> dict[str, Any]:
        """Handle get_trace_analysis tool."""
        trace_id = params.get("traceId") or context.traceId

        if not trace_id:
            return {"error": "No se especificó traceId"}

        if context.analysisData and context.traceId == trace_id:
            return context.analysisData

        if not self._trace_provider:
            return {"error": "Proveedor de trazas no configurado"}

        analysis = self._trace_provider(trace_id)
        if not analysis:
            return {"error": f"Traza {trace_id} no encontrada"}

        return analysis.to_dict()

    async def _handle_search_blast(self, params: dict) -> dict[str, Any]:
        """Handle search_blast tool."""
        if not self._blast_handler:
            return {"error": "Servicio BLAST no configurado"}

        sequence = params.get("sequence", "")
        if len(sequence) < 20:
            return {"error": "Secuencia muy corta (mínimo 20 bp)"}

        return await self._blast_handler(params)

    async def _handle_get_quality(self, params: dict, context: AgentContext) -> dict[str, Any]:
        """Handle get_quality_assessment tool."""
        trace_id = params.get("traceId") or context.traceId

        if not trace_id:
            return {"error": "No se especificó traceId"}

        # Get from context if available
        if context.analysisData and context.traceId == trace_id:
            quality = context.analysisData.get("quality")
            if quality:
                return quality
            return {"error": "No hay datos de calidad disponibles"}

        return {"error": f"Datos de calidad no disponibles para {trace_id}"}

    async def _handle_explain_variant(self, params: dict) -> dict[str, Any]:
        """Handle explain_variant tool."""
        position = params.get("position", 0)
        reference = params.get("reference", "")
        alternate = params.get("alternate", "")
        gene = params.get("gene", "")

        # Build variant description
        variant_type = "SNP" if len(reference) == len(alternate) == 1 else "INDEL"
        if len(alternate) > len(reference):
            variant_type = "inserción"
        elif len(alternate) < len(reference):
            variant_type = "deleción"

        return {
            "position": position,
            "change": f"{reference}>{alternate}",
            "type": variant_type,
            "gene": gene or "no especificado",
            "interpretation": (
                f"Variante {variant_type} en posición {position}. "
                f"Cambio de {reference} a {alternate}. "
                "Para determinar significancia clínica, se recomienda "
                "consultar bases de datos como ClinVar o gnomAD."
            ),
            "note": "Análisis básico. Consultar bases de datos especializadas.",
        }

    async def _handle_compaREDACTED(self, params: dict) -> dict[str, Any]:
        """Handle compaREDACTED tool."""
        seq1 = params.get("sequence1", "").upper()
        seq2 = params.get("sequence2", "").upper()

        if not seq1 or not seq2:
            return {"error": "Se requieren ambas secuencias"}

        # Simple comparison
        differences = []
        min_len = min(len(seq1), len(seq2))

        for i in range(min_len):
            if seq1[i] != seq2[i]:
                differences.append(
                    {
                        "position": i + 1,
                        "seq1": seq1[i],
                        "seq2": seq2[i],
                    }
                )

        return {
            "length1": len(seq1),
            "length2": len(seq2),
            "comparedLength": min_len,
            "differences": differences[:20],  # Limit to 20
            "totalDifferences": len(differences),
            "identity": round((min_len - len(differences)) / min_len * 100, 2)
            if min_len > 0
            else 0,
        }

    # =========================================================================
    # ML-Powered Tool Handlers
    # =========================================================================

    async def _handle_classify_taxonomy(self, params: dict) -> dict[str, Any]:
        """Handle classify_taxonomy tool using TaxonomyClassifier."""
        if not self._ai_service:
            return {"error": "Servicio de ML no disponible"}

        sequence = params.get("sequence", "")
        if len(sequence) < 100:
            return {"error": "Secuencia muy corta (mínimo 100 bp)"}

        self._ml_calls += 1
        result = await self._ai_service.classify_taxonomy(sequence)

        if "error" not in result:
            # Format for better readability
            if "hierarchy" in result:
                result["taxonomia"] = " > ".join(result["hierarchy"])

        return result

    async def _handle_detect_heterozygotes(
        self, params: dict, context: AgentContext
    ) -> dict[str, Any]:
        """Handle detect_heterozygotes tool using HeterozygoteClassifier."""
        if not self._ai_service:
            return {"error": "Servicio de ML no disponible"}

        trace_id = params.get("traceId") or context.traceId
        threshold = params.get("threshold", 0.5)

        if not trace_id:
            return {"error": "No se especificó traceId"}

        # Get signals from trace data
        if context.analysisData and context.traceId == trace_id:
            signals = context.analysisData.get("signals")
            if signals is None:
                return {"error": "No hay señales de cromatograma disponibles"}

            # Convert to numpy array if needed
            if isinstance(signals, dict):
                signals = np.array([
                    signals.get("A", []),
                    signals.get("C", []),
                    signals.get("G", []),
                    signals.get("T", []),
                ])
            elif isinstance(signals, list):
                signals = np.array(signals)

            self._ml_calls += 1
            return await self._ai_service.detect_heterozygotes(signals, threshold)

        return {"error": f"Datos de traza no disponibles para {trace_id}"}

    async def _handle_predict_trim_points(
        self, params: dict, context: AgentContext
    ) -> dict[str, Any]:
        """Handle predict_trim_points tool using TrimmingPredictor."""
        if not self._ai_service:
            return {"error": "Servicio de ML no disponible"}

        trace_id = params.get("traceId") or context.traceId

        if not trace_id:
            return {"error": "No se especificó traceId"}

        # Get quality scores from trace data
        if context.analysisData and context.traceId == trace_id:
            quality_scores = context.analysisData.get("qualityScores")
            if quality_scores is None:
                quality_scores = context.analysisData.get("quality", {}).get("scores")

            if quality_scores is None:
                return {"error": "No hay scores de calidad disponibles"}

            self._ml_calls += 1
            return await self._ai_service.predict_trim_points(quality_scores)

        return {"error": f"Datos de traza no disponibles para {trace_id}"}

    async def _handle_classify_quality_ml(
        self, params: dict, context: AgentContext
    ) -> dict[str, Any]:
        """Handle classify_quality_ml tool using QualityClassifierCNN."""
        if not self._ai_service:
            return {"error": "Servicio de ML no disponible"}

        trace_id = params.get("traceId") or context.traceId

        if not trace_id:
            return {"error": "No se especificó traceId"}

        # Get signals from trace data
        if context.analysisData and context.traceId == trace_id:
            signals = context.analysisData.get("signals")
            if signals is None:
                return {"error": "No hay señales de cromatograma disponibles"}

            # Convert to numpy array
            if isinstance(signals, dict):
                signals = np.array([
                    signals.get("A", []),
                    signals.get("C", []),
                    signals.get("G", []),
                    signals.get("T", []),
                ])
            elif isinstance(signals, list):
                signals = np.array(signals)

            self._ml_calls += 1
            return await self._ai_service.classify_quality(signals)

        return {"error": f"Datos de traza no disponibles para {trace_id}"}

    async def _handle_analyze_trace_ml(
        self, params: dict, context: AgentContext
    ) -> dict[str, Any]:
        """Handle analyze_trace_ml tool - comprehensive ML analysis."""
        if not self._ai_service:
            return {"error": "Servicio de ML no disponible"}

        trace_id = params.get("traceId") or context.traceId

        if not trace_id:
            return {"error": "No se especificó traceId"}

        if not context.analysisData or context.traceId != trace_id:
            return {"error": f"Datos de traza no disponibles para {trace_id}"}

        data = context.analysisData

        # Extract required data
        sequence = data.get("sequence", "")
        quality_scores = data.get("qualityScores") or data.get("quality", {}).get("scores", [])
        signals = data.get("signals")

        if not sequence:
            return {"error": "No hay secuencia disponible"}

        # Convert signals if available
        signals_array = None
        if signals:
            if isinstance(signals, dict):
                signals_array = np.array([
                    signals.get("A", []),
                    signals.get("C", []),
                    signals.get("G", []),
                    signals.get("T", []),
                ])
            elif isinstance(signals, list):
                signals_array = np.array(signals)

        self._ml_calls += 1
        return await self._ai_service.analyze_trace(
            sequence=sequence,
            quality_scores=quality_scores,
            signals=signals_array,
        )
