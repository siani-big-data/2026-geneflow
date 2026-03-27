"""Event definitions for GeneFlow Analysis Worker."""

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
    source: str = "geneflow-analysis"
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
# Trace Events
# =============================================================================


@dataclass
class TraceProcessed(BaseEvent):
    """Event emitted when a trace is successfully processed."""

    traceId: str = ""
    studyId: str = ""
    format: str = ""
    sequenceLength: int = 0
    meanQuality: Optional[float] = None
    hasChromatogram: bool = False

    @property
    def category(self) -> str:
        return "traces"

    def _get_data(self) -> dict[str, Any]:
        return {
            "traceId": self.traceId,
            "studyId": self.studyId,
            "format": self.format,
            "sequenceLength": self.sequenceLength,
            "meanQuality": self.meanQuality,
            "hasChromatogram": self.hasChromatogram,
        }


@dataclass
class TraceProcessingFailed(BaseEvent):
    """Event emitted when trace processing fails."""

    traceId: str = ""
    studyId: str = ""
    error: str = ""
    errorType: str = ""

    @property
    def category(self) -> str:
        return "traces"

    def _get_data(self) -> dict[str, Any]:
        return {
            "traceId": self.traceId,
            "studyId": self.studyId,
            "error": self.error,
            "errorType": self.errorType,
        }


# =============================================================================
# Alignment Events
# =============================================================================


@dataclass
class AlignmentCompleted(BaseEvent):
    """Event emitted when alignment is completed."""

    alignmentId: str = ""
    type: str = ""
    sequenceCount: int = 0
    score: float = 0.0
    identity: float = 0.0
    hasConsensus: bool = False

    @property
    def category(self) -> str:
        return "alignments"

    def _get_data(self) -> dict[str, Any]:
        return {
            "alignmentId": self.alignmentId,
            "type": self.type,
            "sequenceCount": self.sequenceCount,
            "score": self.score,
            "identity": self.identity,
            "hasConsensus": self.hasConsensus,
        }


@dataclass
class AlignmentFailed(BaseEvent):
    """Event emitted when alignment fails."""

    alignmentId: str = ""
    error: str = ""
    errorType: str = ""

    @property
    def category(self) -> str:
        return "alignments"

    def _get_data(self) -> dict[str, Any]:
        return {
            "alignmentId": self.alignmentId,
            "error": self.error,
            "errorType": self.errorType,
        }


# =============================================================================
# Analysis Events
# =============================================================================


@dataclass
class TrimmingCompleted(BaseEvent):
    """Event emitted when trimming is completed."""

    traceId: str = ""
    algorithm: str = ""
    originalLength: int = 0
    trimmedLength: int = 0
    trimStart: int = 0
    trimEnd: int = 0

    @property
    def category(self) -> str:
        return "analysis"

    def _get_data(self) -> dict[str, Any]:
        return {
            "traceId": self.traceId,
            "algorithm": self.algorithm,
            "originalLength": self.originalLength,
            "trimmedLength": self.trimmedLength,
            "trimStart": self.trimStart,
            "trimEnd": self.trimEnd,
        }


@dataclass
class HeterozygoteDetectionCompleted(BaseEvent):
    """Event emitted when heterozygote detection is completed."""

    traceId: str = ""
    heterozygoteCount: int = 0
    positions: list[int] = field(default_factory=list)

    @property
    def category(self) -> str:
        return "analysis"

    def _get_data(self) -> dict[str, Any]:
        return {
            "traceId": self.traceId,
            "heterozygoteCount": self.heterozygoteCount,
            "positions": self.positions,
        }


@dataclass
class MotifSearchCompleted(BaseEvent):
    """Event emitted when motif search is completed."""

    traceId: str = ""
    pattern: str = ""
    matchCount: int = 0
    positions: list[int] = field(default_factory=list)

    @property
    def category(self) -> str:
        return "analysis"

    def _get_data(self) -> dict[str, Any]:
        return {
            "traceId": self.traceId,
            "pattern": self.pattern,
            "matchCount": self.matchCount,
            "positions": self.positions,
        }


@dataclass
class TranslationCompleted(BaseEvent):
    """Event emitted when translation is completed."""

    traceId: str = ""
    frame: int = 0
    proteinLength: int = 0

    @property
    def category(self) -> str:
        return "analysis"

    def _get_data(self) -> dict[str, Any]:
        return {
            "traceId": self.traceId,
            "frame": self.frame,
            "proteinLength": self.proteinLength,
        }


@dataclass
class ORFDetectionCompleted(BaseEvent):
    """Event emitted when ORF detection is completed."""

    traceId: str = ""
    orfCount: int = 0
    longestOrfLength: int = 0

    @property
    def category(self) -> str:
        return "analysis"

    def _get_data(self) -> dict[str, Any]:
        return {
            "traceId": self.traceId,
            "orfCount": self.orfCount,
            "longestOrfLength": self.longestOrfLength,
        }


@dataclass
class RestrictionAnalysisCompleted(BaseEvent):
    """Event emitted when restriction analysis is completed."""

    traceId: str = ""
    enzymeCount: int = 0
    totalSites: int = 0
    enzymesWithSites: list[str] = field(default_factory=list)

    @property
    def category(self) -> str:
        return "analysis"

    def _get_data(self) -> dict[str, Any]:
        return {
            "traceId": self.traceId,
            "enzymeCount": self.enzymeCount,
            "totalSites": self.totalSites,
            "enzymesWithSites": self.enzymesWithSites,
        }


# =============================================================================
# System Events
# =============================================================================


@dataclass
class WorkerStarted(BaseEvent):
    """Event emitted when the worker starts."""

    workerName: str = ""
    workerId: str = ""
    enabledWorkers: list[str] = field(default_factory=list)

    @property
    def category(self) -> str:
        return "system"

    def _get_data(self) -> dict[str, Any]:
        return {
            "workerName": self.workerName,
            "workerId": self.workerId,
            "enabledWorkers": self.enabledWorkers,
        }


@dataclass
class WorkerStopped(BaseEvent):
    """Event emitted when the worker stops."""

    workerName: str = ""
    workerId: str = ""
    reason: str = ""
    jobsProcessed: int = 0

    @property
    def category(self) -> str:
        return "system"

    def _get_data(self) -> dict[str, Any]:
        return {
            "workerName": self.workerName,
            "workerId": self.workerId,
            "reason": self.reason,
            "jobsProcessed": self.jobsProcessed,
        }
