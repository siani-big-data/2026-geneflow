from dataclasses import dataclass, field
from datetime import datetime
from enum import Enum
from typing import Optional

class TraceFormat(str, Enum):
    """Supported trace file formats."""

    AB1 = "ab1"
    SCF = "scf"
    FASTQ = "fastq"
    FASTA = "fasta"


class Base(str, Enum):
    """Nucleotide bases."""

    A = "A"
    C = "C"
    G = "G"
    T = "T"
    N = "N"


class AlignmentType(str, Enum):
    """Alignment types."""

    PAIRWISE = "pairwise"
    MULTIPLE = "multiple"


class TrimmingAlgorithm(str, Enum):
    """Trimming algorithms."""

    MODIFIED_MOTT = "modified_mott"
    SLIDING_WINDOW = "sliding_window"
    QUALITY_THRESHOLD = "quality_threshold"


class AnalysisType(str, Enum):
    """Analysis types."""

    QUALITY = "quality"
    TRIMMING = "trimming"
    HETEROZYGOTE = "heterozygote"
    MOTIF = "motif"
    TRANSLATION = "translation"
    ORF = "orf"
    RESTRICTION = "restriction"


class JobStatus(str, Enum):
    """Job processing status."""

    PENDING = "pending"
    PROCESSING = "processing"
    COMPLETED = "completed"
    FAILED = "failed"


class WorkerStatus(str, Enum):
    """Worker status."""

    STARTING = "starting"
    RUNNING = "running"
    STOPPING = "stopping"
    STOPPED = "stopped"


# =============================================================================
# Domain Models
# =============================================================================


@dataclass
class Sequence:
    """DNA/RNA sequence with optional quality scores."""

    id: str
    sequence: str
    quality: Optional[list[int]] = None
    name: Optional[str] = None
    description: Optional[str] = None

    def __len__(self) -> int:
        return len(self.sequence)

    def to_dict(self) -> dict:
        """Serialize to dictionary."""
        return {
            "id": self.id,
            "sequence": self.sequence,
            "quality": self.quality,
            "name": self.name,
            "description": self.description,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "Sequence":
        """Create from dictionary."""
        return cls(
            id=data["id"],
            sequence=data["sequence"],
            quality=data.get("quality"),
            name=data.get("name"),
            description=data.get("description"),
        )


@dataclass
class ChromatogramData:
    """Chromatogram data from AB1/SCF files."""

    traceA: list[int]
    traceC: list[int]
    traceG: list[int]
    traceT: list[int]
    baseCalls: list[int]
    peakLocations: list[int]

    def to_dict(self) -> dict:
        """Serialize to dictionary."""
        return {
            "traceA": self.traceA,
            "traceC": self.traceC,
            "traceG": self.traceG,
            "traceT": self.traceT,
            "baseCalls": self.baseCalls,
            "peakLocations": self.peakLocations,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "ChromatogramData":
        """Create from dictionary."""
        return cls(
            traceA=data["traceA"],
            traceC=data["traceC"],
            traceG=data["traceG"],
            traceT=data["traceT"],
            baseCalls=data["baseCalls"],
            peakLocations=data["peakLocations"],
        )


@dataclass
class QualityMetrics:
    """Quality metrics for a sequence."""

    meanQuality: float
    q20Percentage: float
    q30Percentage: float
    gcContent: float
    length: int
    ambiguousCount: int = 0
    snr: Optional[float] = None  # Signal-to-Noise Ratio (requires chromatogram)

    def to_dict(self) -> dict:
        """Serialize to dictionary."""
        result = {
            "meanQuality": self.meanQuality,
            "q20Percentage": self.q20Percentage,
            "q30Percentage": self.q30Percentage,
            "gcContent": self.gcContent,
            "length": self.length,
            "ambiguousCount": self.ambiguousCount,
        }
        if self.snr is not None:
            result["snr"] = self.snr
        return result

    @classmethod
    def from_dict(cls, data: dict) -> "QualityMetrics":
        """Create from dictionary."""
        return cls(
            meanQuality=data["meanQuality"],
            q20Percentage=data["q20Percentage"],
            q30Percentage=data["q30Percentage"],
            gcContent=data["gcContent"],
            length=data["length"],
            ambiguousCount=data.get("ambiguousCount", 0),
            snr=data.get("snr"),
        )


@dataclass
class TrimmingResult:
    """Result of sequence trimming."""

    originalLength: int
    trimmedLength: int
    trimStart: int
    trimEnd: int
    trimmedSequence: str
    trimmedQuality: Optional[list[int]] = None
    algorithm: str = "modified_mott"

    def to_dict(self) -> dict:
        """Serialize to dictionary."""
        return {
            "originalLength": self.originalLength,
            "trimmedLength": self.trimmedLength,
            "trimStart": self.trimStart,
            "trimEnd": self.trimEnd,
            "trimmedSequence": self.trimmedSequence,
            "trimmedQuality": self.trimmedQuality,
            "algorithm": self.algorithm,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "TrimmingResult":
        """Create from dictionary."""
        return cls(
            originalLength=data["originalLength"],
            trimmedLength=data["trimmedLength"],
            trimStart=data["trimStart"],
            trimEnd=data["trimEnd"],
            trimmedSequence=data["trimmedSequence"],
            trimmedQuality=data.get("trimmedQuality"),
            algorithm=data.get("algorithm", "modified_mott"),
        )


@dataclass
class HeterozygoteCall:
    """A heterozygote position in the sequence."""

    position: int
    base1: str
    base2: str
    iupacCode: str
    ratio: float
    confidence: float

    def to_dict(self) -> dict:
        """Serialize to dictionary."""
        return {
            "position": self.position,
            "base1": self.base1,
            "base2": self.base2,
            "iupacCode": self.iupacCode,
            "ratio": self.ratio,
            "confidence": self.confidence,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "HeterozygoteCall":
        """Create from dictionary."""
        return cls(
            position=data["position"],
            base1=data["base1"],
            base2=data["base2"],
            iupacCode=data["iupacCode"],
            ratio=data["ratio"],
            confidence=data["confidence"],
        )


@dataclass
class MotifMatch:
    """A motif match in the sequence."""

    pattern: str
    start: int
    end: int
    matchedSequence: str
    strand: str = "+"

    def to_dict(self) -> dict:
        """Serialize to dictionary."""
        return {
            "pattern": self.pattern,
            "start": self.start,
            "end": self.end,
            "matchedSequence": self.matchedSequence,
            "strand": self.strand,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "MotifMatch":
        """Create from dictionary."""
        return cls(
            pattern=data["pattern"],
            start=data["start"],
            end=data["end"],
            matchedSequence=data["matchedSequence"],
            strand=data.get("strand", "+"),
        )


@dataclass
class ORF:
    """Open Reading Frame."""

    start: int
    end: int
    frame: int
    strand: str
    length: int
    sequence: str
    proteinSequence: str

    def to_dict(self) -> dict:
        """Serialize to dictionary."""
        return {
            "start": self.start,
            "end": self.end,
            "frame": self.frame,
            "strand": self.strand,
            "length": self.length,
            "sequence": self.sequence,
            "proteinSequence": self.proteinSequence,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "ORF":
        """Create from dictionary."""
        return cls(
            start=data["start"],
            end=data["end"],
            frame=data["frame"],
            strand=data["strand"],
            length=data["length"],
            sequence=data["sequence"],
            proteinSequence=data["proteinSequence"],
        )


@dataclass
class RestrictionSite:
    """Restriction enzyme cut site."""

    enzyme: str
    position: int
    cutPosition: int
    recognitionSequence: str
    overhang: str

    def to_dict(self) -> dict:
        """Serialize to dictionary."""
        return {
            "enzyme": self.enzyme,
            "position": self.position,
            "cutPosition": self.cutPosition,
            "recognitionSequence": self.recognitionSequence,
            "overhang": self.overhang,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "RestrictionSite":
        """Create from dictionary."""
        return cls(
            enzyme=data["enzyme"],
            position=data["position"],
            cutPosition=data["cutPosition"],
            recognitionSequence=data["recognitionSequence"],
            overhang=data["overhang"],
        )


@dataclass
class AlignmentResult:
    """Result of sequence alignment."""

    alignmentId: str
    type: AlignmentType
    sequences: list[str]
    alignedSequences: list[str]
    score: float
    identity: float
    gaps: int
    consensus: Optional[str] = None

    def to_dict(self) -> dict:
        """Serialize to dictionary."""
        return {
            "alignmentId": self.alignmentId,
            "type": self.type.value,
            "sequences": self.sequences,
            "alignedSequences": self.alignedSequences,
            "score": self.score,
            "identity": self.identity,
            "gaps": self.gaps,
            "consensus": self.consensus,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "AlignmentResult":
        """Create from dictionary."""
        return cls(
            alignmentId=data["alignmentId"],
            type=AlignmentType(data["type"]),
            sequences=data["sequences"],
            alignedSequences=data["alignedSequences"],
            score=data["score"],
            identity=data["identity"],
            gaps=data["gaps"],
            consensus=data.get("consensus"),
        )


@dataclass
class Variant:
    """A sequence variant (SNP or indel)."""

    position: int
    referenceBase: str
    alternateBase: str
    variantType: str  # "snp", "insertion", "deletion"
    quality: Optional[float] = None

    def to_dict(self) -> dict:
        """Serialize to dictionary."""
        return {
            "position": self.position,
            "referenceBase": self.referenceBase,
            "alternateBase": self.alternateBase,
            "variantType": self.variantType,
            "quality": self.quality,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "Variant":
        """Create from dictionary."""
        return cls(
            position=data["position"],
            referenceBase=data["referenceBase"],
            alternateBase=data["alternateBase"],
            variantType=data["variantType"],
            quality=data.get("quality"),
        )


@dataclass
class ParsedTrace:
    """Result of parsing a trace file."""

    traceId: str
    format: TraceFormat
    sequence: Sequence
    chromatogram: Optional[ChromatogramData] = None
    qualityMetrics: Optional[QualityMetrics] = None
    metadata: dict = field(default_factory=dict)

    def to_dict(self) -> dict:
        """Serialize to dictionary."""
        return {
            "traceId": self.traceId,
            "format": self.format.value,
            "sequence": self.sequence.to_dict(),
            "chromatogram": self.chromatogram.to_dict() if self.chromatogram else None,
            "qualityMetrics": self.qualityMetrics.to_dict() if self.qualityMetrics else None,
            "metadata": self.metadata,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "ParsedTrace":
        """Create from dictionary."""
        return cls(
            traceId=data["traceId"],
            format=TraceFormat(data["format"]),
            sequence=Sequence.from_dict(data["sequence"]),
            chromatogram=ChromatogramData.from_dict(data["chromatogram"])
            if data.get("chromatogram")
            else None,
            qualityMetrics=QualityMetrics.from_dict(data["qualityMetrics"])
            if data.get("qualityMetrics")
            else None,
            metadata=data.get("metadata", {}),
        )


# =============================================================================
# Job Models
# =============================================================================


@dataclass
class TraceProcessingJob:
    """Job for processing a trace file."""

    traceId: str
    studyId: str
    fileName: str
    storagePath: str
    format: TraceFormat
    options: dict = field(default_factory=dict)

    def to_dict(self) -> dict:
        """Serialize to dictionary."""
        return {
            "traceId": self.traceId,
            "studyId": self.studyId,
            "fileName": self.fileName,
            "storagePath": self.storagePath,
            "format": self.format.value,
            "options": self.options,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "TraceProcessingJob":
        """Create from dictionary."""
        return cls(
            traceId=data["traceId"],
            studyId=data["studyId"],
            fileName=data["fileName"],
            storagePath=data["storagePath"],
            format=TraceFormat(data.get("format", data.get("formatName", "ab1")).lower()),
            options=data.get("options", {}),
        )


@dataclass
class AlignmentJob:
    """Job for sequence alignment."""

    alignmentId: str
    type: AlignmentType
    traceIds: list[str]
    sequences: list[str] = field(default_factory=list)
    options: dict = field(default_factory=dict)

    def to_dict(self) -> dict:
        """Serialize to dictionary."""
        return {
            "alignmentId": self.alignmentId,
            "type": self.type.value,
            "traceIds": self.traceIds,
            "sequences": self.sequences,
            "options": self.options,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "AlignmentJob":
        """Create from dictionary."""
        type_value = data.get("type", "pairwise")
        if isinstance(type_value, int):
            type_value = "pairwise" if type_value == 1 else "multiple"
        return cls(
            alignmentId=data["alignmentId"],
            type=AlignmentType(type_value),
            traceIds=data["traceIds"],
            sequences=data.get("sequences", []),
            options=data.get("options", {}),
        )


@dataclass
class AnalysisJob:
    """Job for sequence analysis."""

    traceId: str
    analysisType: AnalysisType
    sequence: Optional[str] = None
    quality: Optional[list[int]] = None
    chromatogram: Optional[ChromatogramData] = None
    options: dict = field(default_factory=dict)

    def to_dict(self) -> dict:
        """Serialize to dictionary."""
        return {
            "traceId": self.traceId,
            "analysisType": self.analysisType.value,
            "sequence": self.sequence,
            "quality": self.quality,
            "chromatogram": self.chromatogram.to_dict() if self.chromatogram else None,
            "options": self.options,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "AnalysisJob":
        """Create from dictionary."""
        return cls(
            traceId=data["traceId"],
            analysisType=AnalysisType(data["analysisType"]),
            sequence=data.get("sequence"),
            quality=data.get("quality"),
            chromatogram=ChromatogramData.from_dict(data["chromatogram"])
            if data.get("chromatogram")
            else None,
            options=data.get("options", {}),
        )


# =============================================================================
# Worker Metrics
# =============================================================================


@dataclass
class WorkerMetrics:
    """Metrics for a worker."""

    jobsProcessed: int = 0
    jobsFailed: int = 0
    lastJobAt: Optional[datetime] = None
    averageProcessingTimeMs: float = 0.0

    def to_dict(self) -> dict:
        """Serialize to dictionary."""
        return {
            "jobsProcessed": self.jobsProcessed,
            "jobsFailed": self.jobsFailed,
            "lastJobAt": self.lastJobAt.isoformat() if self.lastJobAt else None,
            "averageProcessingTimeMs": self.averageProcessingTimeMs,
        }
