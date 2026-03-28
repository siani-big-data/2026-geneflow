"""Event definitions for GeneFlow AI service."""

import json
import uuid
from dataclasses import dataclass, field
from datetime import datetime, timezone
from typing import Any, Optional


@dataclass
class BaseEvent:
    """Base class for all events."""

    eventId: str = field(default_factory=lambda: str(uuid.uuid4()))
    timestamp: datetime = field(default_factory=lambda: datetime.now(timezone.utc))
    source: str = "geneflow-ai"
    version: str = "1.0"
    correlationId: Optional[str] = None

    @property
    def type(self) -> str:
        """Event type name (class name)."""
        return self.__class__.__name__

    @property
    def category(self) -> str:
        """Event category for routing."""
        raise NotImplementedError("Subclasses must define category")

    def to_dict(self) -> dict[str, Any]:
        """Serialize to dictionary for Redis."""
        return {
            "eventId": self.eventId,
            "type": self.type,
            "category": self.category,
            "timestamp": int(self.timestamp.timestamp() * 1000),
            "source": self.source,
            "version": self.version,
            "correlationId": self.correlationId,
            "data": json.dumps(self._get_data()),
        }

    def _get_data(self) -> dict[str, Any]:
        """Get event-specific data. Override in subclasses."""
        return {}


# =============================================================================
# AI Analysis Events
# =============================================================================


@dataclass
class AIAnalysisStarted(BaseEvent):
    """Event emitted when AI analysis starts."""

    traceId: str = ""
    studyId: str = ""
    analysisTypes: list[str] = field(default_factory=list)

    @property
    def category(self) -> str:
        return "ai"

    def _get_data(self) -> dict[str, Any]:
        return {
            "traceId": self.traceId,
            "studyId": self.studyId,
            "analysisTypes": self.analysisTypes,
        }


@dataclass
class AIAnalysisCompleted(BaseEvent):
    """Event emitted when AI analysis completes successfully."""

    traceId: str = ""
    studyId: str = ""
    analysisId: str = ""
    overallConfidence: float = 0.0
    processingTimeMs: int = 0
    summary: Optional[str] = None

    @property
    def category(self) -> str:
        return "ai"

    def _get_data(self) -> dict[str, Any]:
        return {
            "traceId": self.traceId,
            "studyId": self.studyId,
            "analysisId": self.analysisId,
            "overallConfidence": self.overallConfidence,
            "processingTimeMs": self.processingTimeMs,
            "summary": self.summary,
        }


@dataclass
class AIAnalysisFailed(BaseEvent):
    """Event emitted when AI analysis fails."""

    traceId: str = ""
    studyId: str = ""
    error: str = ""
    errorType: str = ""

    @property
    def category(self) -> str:
        return "ai"

    def _get_data(self) -> dict[str, Any]:
        return {
            "traceId": self.traceId,
            "studyId": self.studyId,
            "error": self.error,
            "errorType": self.errorType,
        }


@dataclass
class AIBlastCompleted(BaseEvent):
    """Event emitted when BLAST search completes."""

    traceId: str = ""
    studyId: str = ""
    hitCount: int = 0
    topHitAccession: Optional[str] = None
    topHitOrganism: Optional[str] = None

    @property
    def category(self) -> str:
        return "ai"

    def _get_data(self) -> dict[str, Any]:
        return {
            "traceId": self.traceId,
            "studyId": self.studyId,
            "hitCount": self.hitCount,
            "topHitAccession": self.topHitAccession,
            "topHitOrganism": self.topHitOrganism,
        }
