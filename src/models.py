"""Domain models for GeneFlow AI service."""

from dataclasses import dataclass, field
from datetime import datetime, timezone
from enum import Enum
from typing import Any, Optional
from uuid import uuid4


class AnalysisType(str, Enum):
    """Types of AI analysis."""

    QUALITY = "quality"
    BLAST = "blast"
    VARIANTS = "variants"
    ANNOTATIONS = "annotations"
    COPILOT = "copilot"


class AnalysisStatus(str, Enum):
    """Analysis job status."""

    PENDING = "pending"
    RUNNING = "running"
    COMPLETED = "completed"
    FAILED = "failed"


class ServiceStatus(str, Enum):
    """Service status."""

    STARTING = "starting"
    RUNNING = "running"
    STOPPING = "stopping"
    STOPPED = "stopped"


# =============================================================================
# BLAST Models
# =============================================================================


@dataclass
class BlastHit:
    """A BLAST search hit."""

    accession: str
    description: str
    score: float
    eValue: float
    identity: float
    queryStart: int
    queryEnd: int
    subjectStart: int
    subjectEnd: int
    organism: Optional[str] = None

    def to_dict(self) -> dict[str, Any]:
        """Serialize to dictionary."""
        return {
            "accession": self.accession,
            "description": self.description,
            "score": self.score,
            "eValue": self.eValue,
            "identity": self.identity,
            "queryStart": self.queryStart,
            "queryEnd": self.queryEnd,
            "subjectStart": self.subjectStart,
            "subjectEnd": self.subjectEnd,
            "organism": self.organism,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "BlastHit":
        """Create from dictionary."""
        return cls(
            accession=data["accession"],
            description=data["description"],
            score=data["score"],
            eValue=data["eValue"],
            identity=data["identity"],
            queryStart=data["queryStart"],
            queryEnd=data["queryEnd"],
            subjectStart=data["subjectStart"],
            subjectEnd=data["subjectEnd"],
            organism=data.get("organism"),
        )


# =============================================================================
# Quality Models
# =============================================================================


@dataclass
class QualityPrediction:
    """Quality prediction result."""

    predictedAccuracy: float
    errorProbability: float
    lowQualityRegions: list[tuple[int, int]]
    suggestedTrimStart: int
    suggestedTrimEnd: int
    confidence: float

    def to_dict(self) -> dict[str, Any]:
        """Serialize to dictionary."""
        return {
            "predictedAccuracy": self.predictedAccuracy,
            "errorProbability": self.errorProbability,
            "lowQualityRegions": self.lowQualityRegions,
            "suggestedTrimStart": self.suggestedTrimStart,
            "suggestedTrimEnd": self.suggestedTrimEnd,
            "confidence": self.confidence,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "QualityPrediction":
        """Create from dictionary."""
        return cls(
            predictedAccuracy=data["predictedAccuracy"],
            errorProbability=data["errorProbability"],
            lowQualityRegions=[tuple(r) for r in data["lowQualityRegions"]],
            suggestedTrimStart=data["suggestedTrimStart"],
            suggestedTrimEnd=data["suggestedTrimEnd"],
            confidence=data["confidence"],
        )


# =============================================================================
# Variant Models
# =============================================================================


@dataclass
class VariantCall:
    """A detected variant."""

    position: int
    referenceBase: str
    alternateBase: str
    variantType: str  # "snp", "insertion", "deletion"
    clinicalSignificance: Optional[str] = None
    confidence: float = 0.0
    annotation: Optional[str] = None

    def to_dict(self) -> dict[str, Any]:
        """Serialize to dictionary."""
        return {
            "position": self.position,
            "referenceBase": self.referenceBase,
            "alternateBase": self.alternateBase,
            "variantType": self.variantType,
            "clinicalSignificance": self.clinicalSignificance,
            "confidence": self.confidence,
            "annotation": self.annotation,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "VariantCall":
        """Create from dictionary."""
        return cls(
            position=data["position"],
            referenceBase=data["referenceBase"],
            alternateBase=data["alternateBase"],
            variantType=data["variantType"],
            clinicalSignificance=data.get("clinicalSignificance"),
            confidence=data.get("confidence", 0.0),
            annotation=data.get("annotation"),
        )


# =============================================================================
# Annotation Models
# =============================================================================


@dataclass
class SequenceAnnotation:
    """A sequence annotation."""

    start: int
    end: int
    featureType: str
    strand: str
    name: Optional[str] = None
    description: Optional[str] = None
    confidence: float = 0.0
    source: str = "ai"

    def to_dict(self) -> dict[str, Any]:
        """Serialize to dictionary."""
        return {
            "start": self.start,
            "end": self.end,
            "featureType": self.featureType,
            "strand": self.strand,
            "name": self.name,
            "description": self.description,
            "confidence": self.confidence,
            "source": self.source,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "SequenceAnnotation":
        """Create from dictionary."""
        return cls(
            start=data["start"],
            end=data["end"],
            featureType=data["featureType"],
            strand=data["strand"],
            name=data.get("name"),
            description=data.get("description"),
            confidence=data.get("confidence", 0.0),
            source=data.get("source", "ai"),
        )


# =============================================================================
# Analysis Result
# =============================================================================


@dataclass
class AnalysisResult:
    """Complete AI analysis result."""

    analysisId: str = field(default_factory=lambda: str(uuid4()))
    traceId: str = ""
    studyId: str = ""
    status: AnalysisStatus = AnalysisStatus.PENDING
    createdAt: datetime = field(default_factory=lambda: datetime.now(timezone.utc))
    completedAt: Optional[datetime] = None
    processingTimeMs: int = 0

    # Results
    quality: Optional[QualityPrediction] = None
    blastHits: list[BlastHit] = field(default_factory=list)
    variants: list[VariantCall] = field(default_factory=list)
    annotations: list[SequenceAnnotation] = field(default_factory=list)

    # Copilot
    summary: Optional[str] = None
    recommendations: list[str] = field(default_factory=list)

    # Metadata
    overallConfidence: float = 0.0
    warnings: list[str] = field(default_factory=list)
    errors: list[str] = field(default_factory=list)

    def to_dict(self) -> dict[str, Any]:
        """Serialize to dictionary."""
        data = {
            "analysisId": self.analysisId,
            "traceId": self.traceId,
            "studyId": self.studyId,
            "status": self.status.value,
            "createdAt": self.createdAt.isoformat(),
            "processingTimeMs": self.processingTimeMs,
            "overallConfidence": self.overallConfidence,
            "warnings": self.warnings,
            "errors": self.errors,
        }

        if self.completedAt:
            data["completedAt"] = self.completedAt.isoformat()
        if self.quality:
            data["quality"] = self.quality.to_dict()
        if self.blastHits:
            data["blastHits"] = [h.to_dict() for h in self.blastHits]
        if self.variants:
            data["variants"] = [v.to_dict() for v in self.variants]
        if self.annotations:
            data["annotations"] = [a.to_dict() for a in self.annotations]
        if self.summary:
            data["summary"] = self.summary
        if self.recommendations:
            data["recommendations"] = self.recommendations

        return data

    @classmethod
    def from_dict(cls, data: dict) -> "AnalysisResult":
        """Create from dictionary."""
        result = cls(
            analysisId=data.get("analysisId", str(uuid4())),
            traceId=data.get("traceId", ""),
            studyId=data.get("studyId", ""),
            status=AnalysisStatus(data.get("status", "pending")),
            processingTimeMs=data.get("processingTimeMs", 0),
            overallConfidence=data.get("overallConfidence", 0.0),
            warnings=data.get("warnings", []),
            errors=data.get("errors", []),
            summary=data.get("summary"),
            recommendations=data.get("recommendations", []),
        )

        if data.get("quality"):
            result.quality = QualityPrediction.from_dict(data["quality"])
        if data.get("blastHits"):
            result.blastHits = [BlastHit.from_dict(h) for h in data["blastHits"]]
        if data.get("variants"):
            result.variants = [VariantCall.from_dict(v) for v in data["variants"]]
        if data.get("annotations"):
            result.annotations = [SequenceAnnotation.from_dict(a) for a in data["annotations"]]

        return result


# =============================================================================
# Service Metrics
# =============================================================================


@dataclass
class ServiceMetrics:
    """Metrics for the AI service."""

    analysesStarted: int = 0
    analysesCompleted: int = 0
    analysesFailed: int = 0
    lastAnalysisAt: Optional[datetime] = None
    averageProcessingTimeMs: float = 0.0

    def to_dict(self) -> dict[str, Any]:
        """Serialize to dictionary."""
        return {
            "analysesStarted": self.analysesStarted,
            "analysesCompleted": self.analysesCompleted,
            "analysesFailed": self.analysesFailed,
            "lastAnalysisAt": self.lastAnalysisAt.isoformat() if self.lastAnalysisAt else None,
            "averageProcessingTimeMs": self.averageProcessingTimeMs,
        }
