"""Chat handler for conversational interactions with Copilot."""

from dataclasses import dataclass, field
from datetime import datetime, timezone
from typing import Any, Optional
from uuid import uuid4

import structlog

from src.config import Settings
from src.models import AnalysisResult

from .client import ClaudeClient
from .prompts import PromptTemplates

logger = structlog.get_logger()


@dataclass
class Message:
    """A chat message."""

    role: str  # "user" or "assistant"
    content: str
    timestamp: datetime = field(default_factory=lambda: datetime.now(timezone.utc))


@dataclass
class Conversation:
    """A conversation context."""

    conversationId: str = field(default_factory=lambda: str(uuid4()))
    traceId: Optional[str] = None
    analysisId: Optional[str] = None
    messages: list[Message] = field(default_factory=list)
    context: Optional[dict] = None
    createdAt: datetime = field(default_factory=lambda: datetime.now(timezone.utc))

    def add_message(self, role: str, content: str) -> None:
        """Add a message to the conversation."""
        self.messages.append(Message(role=role, content=content))

    def get_messages_for_api(self) -> list[dict]:
        """Get messages formatted for Claude API."""
        return [
            {"role": msg.role, "content": msg.content}
            for msg in self.messages
        ]

    def to_dict(self) -> dict[str, Any]:
        """Serialize to dictionary."""
        return {
            "conversationId": self.conversationId,
            "traceId": self.traceId,
            "analysisId": self.analysisId,
            "messageCount": len(self.messages),
            "createdAt": self.createdAt.isoformat(),
        }


class ChatHandler:
    """Handles conversational interactions with Copilot."""

    def __init__(self, client: ClaudeClient, settings: Settings):
        self._client = client
        self._settings = settings
        self._conversations: dict[str, Conversation] = {}
        self._prompts = PromptTemplates()

    @property
    def is_available(self) -> bool:
        """Check if chat is available."""
        return self._client.is_configured

    def get_conversation(self, conversation_id: str) -> Optional[Conversation]:
        """Get a conversation by ID."""
        return self._conversations.get(conversation_id)

    def list_conversations(self) -> list[dict]:
        """List all active conversations."""
        return [conv.to_dict() for conv in self._conversations.values()]

    def delete_conversation(self, conversation_id: str) -> bool:
        """Delete a conversation."""
        if conversation_id in self._conversations:
            del self._conversations[conversation_id]
            return True
        return False

    async def ask(
        self,
        question: str,
        conversation_id: Optional[str] = None,
        analysis_result: Optional[AnalysisResult] = None,
    ) -> dict[str, Any]:
        """
        Ask a question, optionally in context of an analysis.

        Args:
            question: The user's question
            conversation_id: Optional conversation ID to continue
            analysis_result: Optional analysis result for context

        Returns:
            Dict with answer and metadata
        """
        if not self._client.is_configured:
            return {
                "answer": "Copilot no configurado. Configure AI_CLAUDE_API_KEY.",
                "error": True,
            }

        # Get or create conversation
        if conversation_id and conversation_id in self._conversations:
            conversation = self._conversations[conversation_id]
        else:
            conversation = Conversation(
                conversationId=conversation_id or str(uuid4()),
                traceId=analysis_result.traceId if analysis_result else None,
                analysisId=analysis_result.analysisId if analysis_result else None,
            )
            self._conversations[conversation.conversationId] = conversation

        # Update context if analysis provided
        if analysis_result:
            conversation.context = analysis_result.to_dict()
            conversation.traceId = analysis_result.traceId
            conversation.analysisId = analysis_result.analysisId

        # Build context string
        context = self._build_context(conversation)

        # Build prompt
        prompt = PromptTemplates.render(
            PromptTemplates.ANSWER_QUESTION,
            context=context,
            question=question,
        )

        try:
            # Add user message
            conversation.add_message("user", question)

            # Get response
            response = await self._client.create_message(
                system_prompt=PromptTemplates.SYSTEM_QA,
                user_message=prompt,
            )

            # Add assistant response
            conversation.add_message("assistant", response)

            logger.info(
                "chat_question_answered",
                conversation_id=conversation.conversationId,
                question_length=len(question),
                response_length=len(response),
            )

            return {
                "answer": response,
                "conversationId": conversation.conversationId,
                "messageCount": len(conversation.messages),
            }

        except Exception as e:
            logger.error("chat_error", error=str(e))
            return {
                "answer": f"Error al procesar la pregunta: {str(e)}",
                "error": True,
                "conversationId": conversation.conversationId,
            }

    async def interpret_analysis(
        self,
        analysis_result: AnalysisResult,
    ) -> dict[str, Any]:
        """
        Generate a natural language interpretation of an analysis.

        Args:
            analysis_result: The analysis result to interpret

        Returns:
            Dict with summary, recommendations, and metadata
        """
        if not self._client.is_configured:
            return {
                "summary": "Copilot no disponible.",
                "recommendations": [],
                "error": True,
            }

        # Build sections
        quality_section = PromptTemplates.format_quality_section(
            analysis_result.quality.to_dict() if analysis_result.quality else None
        )
        blast_section = PromptTemplates.format_blast_section(
            [h.to_dict() for h in analysis_result.blastHits] if analysis_result.blastHits else None
        )
        variants_section = PromptTemplates.format_variants_section(
            [v.to_dict() for v in analysis_result.variants] if analysis_result.variants else None
        )
        annotations_section = PromptTemplates.format_annotations_section(
            [a.to_dict() for a in analysis_result.annotations]
            if analysis_result.annotations
            else None
        )

        # Build prompt
        prompt = PromptTemplates.render(
            PromptTemplates.INTERPRET_ANALYSIS,
            trace_id=analysis_result.traceId,
            study_id=analysis_result.studyId,
            quality_section=quality_section,
            blast_section=blast_section,
            variants_section=variants_section,
            annotations_section=annotations_section,
        )

        # Build system prompt with context
        system_prompt = PromptTemplates.render(
            PromptTemplates.SYSTEM_ANALYST,
            organism="No especificado",
            sequence_length="N/A",
            avg_quality="N/A",
        )

        try:
            response = await self._client.create_message(
                system_prompt=system_prompt,
                user_message=prompt,
            )

            # Parse recommendations from response
            recommendations = self._extract_recommendations(response)

            logger.info(
                "analysis_interpreted",
                trace_id=analysis_result.traceId,
                response_length=len(response),
                recommendations_count=len(recommendations),
            )

            return {
                "summary": response,
                "recommendations": recommendations,
            }

        except Exception as e:
            logger.error("interpret_error", error=str(e))
            return {
                "summary": f"Error al interpretar: {str(e)}",
                "recommendations": [],
                "error": True,
            }

    def _build_context(self, conversation: Conversation) -> str:
        """Build context string from conversation."""
        if not conversation.context:
            return "No hay contexto de análisis disponible."

        ctx = conversation.context
        lines = [
            f"**Análisis ID:** {ctx.get('analysisId', 'N/A')}",
            f"**Trace ID:** {ctx.get('traceId', 'N/A')}",
            f"**Estado:** {ctx.get('status', 'N/A')}",
            f"**Confianza:** {ctx.get('overallConfidence', 'N/A')}",
        ]

        if ctx.get("summary"):
            lines.append(f"\n**Resumen previo:**\n{ctx['summary']}")

        return "\n".join(lines)

    def _extract_recommendations(self, text: str) -> list[str]:
        """Extract recommendations from response text."""
        recommendations = []

        # Look for numbered recommendations or bullet points
        import re

        # Pattern for numbered items
        patterns = [
            r'\d+\.\s*\*\*[^*]+\*\*:?\s*(.+?)(?=\d+\.|$)',
            r'[-•]\s*(.+?)(?=[-•]|$)',
            r'Recomendaci[oó]n(?:es)?[:\s]+(.+?)(?=\n|$)',
        ]

        for pattern in patterns:
            matches = re.findall(pattern, text, re.MULTILINE | re.DOTALL)
            if matches:
                for match in matches:
                    clean = match.strip()
                    if clean and len(clean) > 10:
                        recommendations.append(clean[:200])  # Limit length

        # Deduplicate and limit
        seen = set()
        unique = []
        for rec in recommendations:
            if rec not in seen:
                seen.add(rec)
                unique.append(rec)

        return unique[:5]  # Max 5 recommendations
