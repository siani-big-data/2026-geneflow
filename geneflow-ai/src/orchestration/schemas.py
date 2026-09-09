"""Schemas for tool inputs and outputs."""

from dataclasses import dataclass, field
from enum import Enum
from typing import Literal


class AnalysisType(str, Enum):
    """Available analysis types."""
    QUALITY = "quality_enhanced"
    TRIMMING = "trimming"
    HETEROZYGOTE = "heterozygote_training"
    MOTIF = "motif"
    TRANSLATION = "translation"
    ORF = "orf"
    RESTRICTION = "restriction"


class AlignmentType(str, Enum):
    """Alignment types."""
    PAIRWISE = "pairwise"
    MULTIPLE = "multiple"


class TrimmingAlgorithm(str, Enum):
    """Trimming algorithms."""
    MODIFIED_MOTT = "modified_mott"
    SLIDING_WINDOW = "sliding_window"
    QUALITY_THRESHOLD = "quality_threshold"


class TraceFormat(str, Enum):
    """Supported trace file formats."""
    AB1 = "ab1"
    SCF = "scf"
    FASTQ = "fastq"
    FASTA = "fasta"


@dataclass
class QualityMetrics:
    """Quality analysis results."""
    mean_quality: float
    q20_percentage: float
    q30_percentage: float
    gc_content: float
    length: int
    ambiguous_count: int = 0
    snr: float | None = None


@dataclass
class TrimmingResult:
    """Trimming analysis results."""
    original_length: int
    trimmed_length: int
    trim_start: int
    trim_end: int
    trimmed_sequence: str
    algorithm: str


@dataclass
class MotifMatch:
    """Single motif match."""
    pattern: str
    start: int
    end: int
    matched_sequence: str
    strand: Literal["forward", "reverse"] = "forward"


@dataclass
class MotifResult:
    """Motif search results."""
    match_count: int
    matches: list[MotifMatch] = field(default_factory=list)


@dataclass
class ORF:
    """Single open reading frame."""
    start: int
    end: int
    length: int
    frame: int
    sequence: str


@dataclass
class ORFResult:
    """ORF detection results."""
    total_orfs: int
    longest_orf_length: int
    orfs: list[ORF] = field(default_factory=list)


@dataclass
class TranslationResult:
    """Translation results."""
    protein_sequence: str
    protein_length: int
    frame: int


@dataclass
class RestrictionSite:
    """Single restriction site."""
    enzyme: str
    position: int
    cut_sequence: str


@dataclass
class RestrictionResult:
    """Restriction analysis results."""
    enzyme_count: int
    total_sites: int
    sites: list[RestrictionSite] = field(default_factory=list)


@dataclass
class HeterozygotePosition:
    """Detected heterozygote_training position."""
    position: int
    iupac_code: str
    peak_ratio: float | None = None


@dataclass
class HeterozygoteResult:
    """Heterozygote detection results."""
    heterozygote_count: int
    positions: list[HeterozygotePosition] = field(default_factory=list)


@dataclass
class AlignmentResult:
    """Alignment results."""
    alignment_id: str
    alignment_type: str
    aligned_sequences: list[str]
    score: float
    identity: float
    gaps: int
    consensus: str | None = None


@dataclass
class ParsedTrace:
    """Parsed trace file."""
    trace_id: str
    sequence: str
    quality_scores: list[int]
    sequence_length: int
    mean_quality: float
    has_chromatogram: bool
    format: str
    metadata: dict = field(default_factory=dict)


@dataclass
class ChromatogramData:
    """Chromatogram signal data."""
    trace_a: list[int]
    trace_c: list[int]
    trace_g: list[int]
    trace_t: list[int]
    base_calls: str
    peak_locations: list[int]
