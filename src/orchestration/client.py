"""Client for communicating with geneflow-analysis service."""

import asyncio
import json
import logging
import uuid
from dataclasses import dataclass
from typing import Any

try:
    import redis.asyncio as aioredis
    REDIS_AVAILABLE = True
except ImportError:
    REDIS_AVAILABLE = False

from .schemas import (
    ORF,
    AlignmentResult,
    AlignmentType,
    MotifMatch,
    MotifResult,
    ORFResult,
    QualityMetrics,
    RestrictionResult,
    RestrictionSite,
    TranslationResult,
    TrimmingAlgorithm,
    TrimmingResult,
)

logger = logging.getLogger(__name__)


@dataclass
class ClientConfig:
    """Configuration for GeneFlow client."""
    redis_url: str = "redis://localhost:6379"
    jobs_prefix: str = "geneflow:jobs"
    events_prefix: str = "geneflow:events"
    consumer_group: str = "geneflow-ai-consumers"
    consumer_name: str = "geneflow-ai-1"
    timeout_seconds: float = 30.0
    block_ms: int = 1000


class GeneFlowClient:
    """Client for invoking geneflow-analysis capabilities.

    Supports two modes:
    1. Async Redis mode (production): Submit jobs to Redis Streams
    2. Direct mode (development): Call analyzers directly (requires geneflow-analysis in path)
    """

    def __init__(self, config: ClientConfig | None = None, direct_mode: bool = False):
        self.config = config or ClientConfig()
        self.direct_mode = direct_mode
        self._redis: Any = None
        self._direct_analyzers: dict = {}
        self._initialized = False

    async def initialize(self) -> None:
        """Initialize the client."""
        if self._initialized:
            return

        if self.direct_mode:
            await self._init_direct_mode()
        else:
            await self._init_redis_mode()

        self._initialized = True

    async def _init_redis_mode(self) -> None:
        """Initialize Redis connection."""
        if not REDIS_AVAILABLE:
            raise ImportError("redis package not installed. Install with: pip install redis")

        self._redis = await aioredis.from_url(
            self.config.redis_url,
            encoding="utf-8",
            decode_responses=True,
        )

        # Create consumer groups if they don't exist
        for stream in ["traces", "alignments", "analysis"]:
            stream_name = f"{self.config.events_prefix}:{stream}"
            try:
                await self._redis.xgroup_create(
                    stream_name,
                    self.config.consumer_group,
                    id="0",
                    mkstream=True,
                )
            except Exception:
                pass  # Group already exists

        logger.info(f"Connected to Redis at {self.config.redis_url}")

    async def _init_direct_mode(self) -> None:
        """Initialize direct mode with local analyzers."""
        try:
            # Try importing from geneflow-analysis
            import sys
            from pathlib import Path

            # Add geneflow-analysis to path if needed
            analysis_path = Path(__file__).parent.parent.parent.parent / "geneflow-analysis"
            if analysis_path.exists() and str(analysis_path) not in sys.path:
                sys.path.insert(0, str(analysis_path))

            from src.alignment import MultipleAligner, PairwiseAligner
            from src.analyzers import (
                HeterozygoteAnalyzer,
                MotifAnalyzer,
                ORFAnalyzer,
                QualityAnalyzer,
                RestrictionAnalyzer,
                TranslationAnalyzer,
                TrimmingAnalyzer,
            )

            self._direct_analyzers = {
                "quality_enhanced": QualityAnalyzer(),
                "trimming": TrimmingAnalyzer(),
                "motif": MotifAnalyzer(),
                "orf": ORFAnalyzer(),
                "translation": TranslationAnalyzer(),
                "restriction": RestrictionAnalyzer(),
                "heterozygote_training": HeterozygoteAnalyzer(),
                "pairwise": PairwiseAligner(),
                "multiple": MultipleAligner(),
            }

            logger.info("Direct mode initialized with local analyzers")

        except ImportError as e:
            raise ImportError(
                f"Could not import geneflow-analysis modules: {e}. "
                "Make sure geneflow-analysis is in the path or use Redis mode."
            )

    async def close(self) -> None:
        """Close the client."""
        if self._redis:
            await self._redis.close()
            self._redis = None
        self._initialized = False

    async def _submit_job(
        self,
        stream: str,
        job_data: dict,
        correlation_id: str | None = None,
    ) -> str:
        """Submit a job to Redis Stream."""
        job_id = correlation_id or str(uuid.uuid4())
        stream_name = f"{self.config.jobs_prefix}:{stream}"

        await self._redis.xadd(
            stream_name,
            {"data": json.dumps(job_data)},
        )

        return job_id

    async def _wait_for_result(
        self,
        stream: str,
        correlation_id: str,
        timeout: float | None = None,
    ) -> dict | None:
        """Wait for a result event."""
        timeout = timeout or self.config.timeout_seconds
        stream_name = f"{self.config.events_prefix}:{stream}"
        deadline = asyncio.get_event_loop().time() + timeout

        while asyncio.get_event_loop().time() < deadline:
            try:
                messages = await self._redis.xread(
                    {stream_name: "0"},
                    count=100,
                    block=self.config.block_ms,
                )

                for _, msg_list in messages:
                    for msg_id, msg_data in msg_list:
                        data = json.loads(msg_data.get("data", "{}"))
                        if data.get("correlationId") == correlation_id:
                            return data

            except Exception as e:
                logger.warning(f"Error reading from stream: {e}")
                await asyncio.sleep(0.1)

        return None

    # ==================== Analysis Methods ====================

    async def analyze_quality(
        self,
        sequence: str,
        quality_scores: list[int] | None = None,
    ) -> QualityMetrics:
        """Analyze sequence quality_enhanced.

        Args:
            sequence: DNA sequence
            quality_scores: Optional Phred quality_enhanced scores

        Returns:
            QualityMetrics with Q20, Q30, GC content, etc.
        """
        await self.initialize()

        if self.direct_mode:
            return await self._analyze_quality_direct(sequence, quality_scores)
        else:
            return await self._analyze_quality_redis(sequence, quality_scores)

    async def _analyze_quality_direct(
        self,
        sequence: str,
        quality_scores: list[int] | None,
    ) -> QualityMetrics:
        """Direct quality_enhanced analysis."""
        from src.models import Sequence

        analyzer = self._direct_analyzers["quality_enhanced"]
        seq = Sequence(id="temp", sequence=sequence, quality=quality_scores or [])
        result = analyzer.analyze(seq)

        return QualityMetrics(
            mean_quality=result.meanQuality,
            q20_percentage=result.q20Percentage,
            q30_percentage=result.q30Percentage,
            gc_content=result.gcContent,
            length=result.length,
            ambiguous_count=getattr(result, "ambiguousCount", 0),
            snr=getattr(result, "snr", None),
        )

    async def _analyze_quality_redis(
        self,
        sequence: str,
        quality_scores: list[int] | None,
    ) -> QualityMetrics:
        """Quality analysis via Redis."""
        correlation_id = str(uuid.uuid4())

        await self._submit_job(
            "analysis",
            {
                "traceId": correlation_id,
                "analysisType": "quality_enhanced",
                "sequence": sequence,
                "quality_enhanced": quality_scores or [],
            },
            correlation_id,
        )

        result = await self._wait_for_result("analysis", correlation_id)
        if not result:
            raise TimeoutError("Quality analysis timed out")

        data = result.get("data", {})
        return QualityMetrics(
            mean_quality=data.get("meanQuality", 0),
            q20_percentage=data.get("q20Percentage", 0),
            q30_percentage=data.get("q30Percentage", 0),
            gc_content=data.get("gcContent", 0),
            length=data.get("length", len(sequence)),
            ambiguous_count=data.get("ambiguousCount", 0),
            snr=data.get("snr"),
        )

    async def trim_sequence(
        self,
        sequence: str,
        quality_scores: list[int],
        algorithm: TrimmingAlgorithm = TrimmingAlgorithm.MODIFIED_MOTT,
        cutoff: float = 0.05,
        window_size: int = 10,
        threshold: int = 20,
    ) -> TrimmingResult:
        """Trim low-quality_enhanced regions from a sequence.

        Args:
            sequence: DNA sequence
            quality_scores: Phred quality_enhanced scores
            algorithm: Trimming algorithm to use
            cutoff: Cutoff for modified_mott algorithm
            window_size: Window size for sliding_window algorithm
            threshold: Quality threshold for quality_threshold algorithm

        Returns:
            TrimmingResult with trimmed sequence and positions
        """
        await self.initialize()

        options = {
            "algorithm": algorithm.value,
            "cutoff": cutoff,
            "window_size": window_size,
            "threshold": threshold,
        }

        if self.direct_mode:
            return await self._trim_direct(sequence, quality_scores, options)
        else:
            return await self._trim_redis(sequence, quality_scores, options)

    async def _trim_direct(
        self,
        sequence: str,
        quality_scores: list[int],
        options: dict,
    ) -> TrimmingResult:
        """Direct trimming."""
        from src.models import Sequence

        analyzer = self._direct_analyzers["trimming"]
        seq = Sequence(id="temp", sequence=sequence, quality=quality_scores)
        result = analyzer.analyze(seq, **options)

        return TrimmingResult(
            original_length=result.originalLength,
            trimmed_length=result.trimmedLength,
            trim_start=result.trimStart,
            trim_end=result.trimEnd,
            trimmed_sequence=result.trimmedSequence,
            algorithm=options["algorithm"],
        )

    async def _trim_redis(
        self,
        sequence: str,
        quality_scores: list[int],
        options: dict,
    ) -> TrimmingResult:
        """Trimming via Redis."""
        correlation_id = str(uuid.uuid4())

        await self._submit_job(
            "analysis",
            {
                "traceId": correlation_id,
                "analysisType": "trimming",
                "sequence": sequence,
                "quality_enhanced": quality_scores,
                "options": options,
            },
            correlation_id,
        )

        result = await self._wait_for_result("analysis", correlation_id)
        if not result:
            raise TimeoutError("Trimming analysis timed out")

        data = result.get("data", {})
        return TrimmingResult(
            original_length=data.get("originalLength", len(sequence)),
            trimmed_length=data.get("trimmedLength", 0),
            trim_start=data.get("trimStart", 0),
            trim_end=data.get("trimEnd", 0),
            trimmed_sequence=data.get("trimmedSequence", ""),
            algorithm=options["algorithm"],
        )

    async def search_motifs(
        self,
        sequence: str,
        pattern: str,
        search_complement: bool = False,
        use_regex: bool = False,
    ) -> MotifResult:
        """Search for motif patterns in a sequence.

        Args:
            sequence: DNA sequence
            pattern: Motif pattern (supports IUPAC ambiguity codes)
            search_complement: Also search reverse complement
            use_regex: Treat pattern as regex

        Returns:
            MotifResult with matches
        """
        await self.initialize()

        options = {
            "pattern": pattern,
            "search_complement": search_complement,
            "use_regex": use_regex,
        }

        if self.direct_mode:
            return await self._search_motifs_direct(sequence, options)
        else:
            return await self._search_motifs_redis(sequence, options)

    async def _search_motifs_direct(
        self,
        sequence: str,
        options: dict,
    ) -> MotifResult:
        """Direct motif search."""
        from src.models import Sequence

        analyzer = self._direct_analyzers["motif"]
        seq = Sequence(id="temp", sequence=sequence)
        result = analyzer.analyze(seq, **options)

        matches = [
            MotifMatch(
                pattern=m.pattern,
                start=m.start,
                end=m.end,
                matched_sequence=m.matchedSequence,
                strand=m.strand,
            )
            for m in result.matches
        ]

        return MotifResult(
            match_count=result.matchCount,
            matches=matches,
        )

    async def _search_motifs_redis(
        self,
        sequence: str,
        options: dict,
    ) -> MotifResult:
        """Motif search via Redis."""
        correlation_id = str(uuid.uuid4())

        await self._submit_job(
            "analysis",
            {
                "traceId": correlation_id,
                "analysisType": "motif",
                "sequence": sequence,
                "options": options,
            },
            correlation_id,
        )

        result = await self._wait_for_result("analysis", correlation_id)
        if not result:
            raise TimeoutError("Motif search timed out")

        data = result.get("data", {})
        matches = [
            MotifMatch(
                pattern=m.get("pattern", options["pattern"]),
                start=m.get("start", 0),
                end=m.get("end", 0),
                matched_sequence=m.get("matchedSequence", ""),
                strand=m.get("strand", "forward"),
            )
            for m in data.get("matches", [])
        ]

        return MotifResult(
            match_count=data.get("matchCount", len(matches)),
            matches=matches,
        )

    async def detect_orfs(
        self,
        sequence: str,
        min_length: int = 30,
    ) -> ORFResult:
        """Detect open reading frames in a sequence.

        Args:
            sequence: DNA sequence
            min_length: Minimum ORF length in amino acids

        Returns:
            ORFResult with detected ORFs
        """
        await self.initialize()

        if self.direct_mode:
            return await self._detect_orfs_direct(sequence, min_length)
        else:
            return await self._detect_orfs_redis(sequence, min_length)

    async def _detect_orfs_direct(
        self,
        sequence: str,
        min_length: int,
    ) -> ORFResult:
        """Direct ORF detection."""
        from src.models import Sequence

        analyzer = self._direct_analyzers["orf"]
        seq = Sequence(id="temp", sequence=sequence)
        result = analyzer.analyze(seq, min_length=min_length)

        orfs = [
            ORF(
                start=o.start,
                end=o.end,
                length=o.length,
                frame=o.frame,
                sequence=o.sequence,
            )
            for o in result.orfs
        ]

        return ORFResult(
            total_orfs=result.totalOrfs,
            longest_orf_length=result.longestOrfLength,
            orfs=orfs,
        )

    async def _detect_orfs_redis(
        self,
        sequence: str,
        min_length: int,
    ) -> ORFResult:
        """ORF detection via Redis."""
        correlation_id = str(uuid.uuid4())

        await self._submit_job(
            "analysis",
            {
                "traceId": correlation_id,
                "analysisType": "orf",
                "sequence": sequence,
                "options": {"min_length": min_length},
            },
            correlation_id,
        )

        result = await self._wait_for_result("analysis", correlation_id)
        if not result:
            raise TimeoutError("ORF detection timed out")

        data = result.get("data", {})
        orfs = [
            ORF(
                start=o.get("start", 0),
                end=o.get("end", 0),
                length=o.get("length", 0),
                frame=o.get("frame", 1),
                sequence=o.get("sequence", ""),
            )
            for o in data.get("orfs", [])
        ]

        return ORFResult(
            total_orfs=data.get("totalOrfs", len(orfs)),
            longest_orf_length=data.get("longestOrfLength", 0),
            orfs=orfs,
        )

    async def translate(
        self,
        sequence: str,
        frame: int = 1,
    ) -> TranslationResult:
        """Translate DNA to protein sequence.

        Args:
            sequence: DNA sequence
            frame: Reading frame (1-6, negative for reverse complement)

        Returns:
            TranslationResult with protein sequence
        """
        await self.initialize()

        if self.direct_mode:
            return await self._translate_direct(sequence, frame)
        else:
            return await self._translate_redis(sequence, frame)

    async def _translate_direct(
        self,
        sequence: str,
        frame: int,
    ) -> TranslationResult:
        """Direct translation."""
        from src.models import Sequence

        analyzer = self._direct_analyzers["translation"]
        seq = Sequence(id="temp", sequence=sequence)
        result = analyzer.analyze(seq, frame=frame)

        return TranslationResult(
            protein_sequence=result.proteinSequence,
            protein_length=result.proteinLength,
            frame=frame,
        )

    async def _translate_redis(
        self,
        sequence: str,
        frame: int,
    ) -> TranslationResult:
        """Translation via Redis."""
        correlation_id = str(uuid.uuid4())

        await self._submit_job(
            "analysis",
            {
                "traceId": correlation_id,
                "analysisType": "translation",
                "sequence": sequence,
                "options": {"frame": frame},
            },
            correlation_id,
        )

        result = await self._wait_for_result("analysis", correlation_id)
        if not result:
            raise TimeoutError("Translation timed out")

        data = result.get("data", {})
        return TranslationResult(
            protein_sequence=data.get("proteinSequence", ""),
            protein_length=data.get("proteinLength", 0),
            frame=frame,
        )

    async def find_restriction_sites(
        self,
        sequence: str,
        enzymes: list[str] | None = None,
    ) -> RestrictionResult:
        """Find restriction enzyme cut sites.

        Args:
            sequence: DNA sequence
            enzymes: List of enzyme names (None = common enzymes)

        Returns:
            RestrictionResult with cut sites
        """
        await self.initialize()

        if self.direct_mode:
            return await self._restriction_direct(sequence, enzymes)
        else:
            return await self._restriction_redis(sequence, enzymes)

    async def _restriction_direct(
        self,
        sequence: str,
        enzymes: list[str] | None,
    ) -> RestrictionResult:
        """Direct restriction analysis."""
        from src.models import Sequence

        analyzer = self._direct_analyzers["restriction"]
        seq = Sequence(id="temp", sequence=sequence)
        result = analyzer.analyze(seq, enzymes=enzymes)

        sites = [
            RestrictionSite(
                enzyme=s.enzyme,
                position=s.position,
                cut_sequence=s.cutSequence,
            )
            for s in result.sites
        ]

        return RestrictionResult(
            enzyme_count=result.enzymeCount,
            total_sites=result.totalSites,
            sites=sites,
        )

    async def _restriction_redis(
        self,
        sequence: str,
        enzymes: list[str] | None,
    ) -> RestrictionResult:
        """Restriction analysis via Redis."""
        correlation_id = str(uuid.uuid4())

        await self._submit_job(
            "analysis",
            {
                "traceId": correlation_id,
                "analysisType": "restriction",
                "sequence": sequence,
                "options": {"enzymes": enzymes} if enzymes else {},
            },
            correlation_id,
        )

        result = await self._wait_for_result("analysis", correlation_id)
        if not result:
            raise TimeoutError("Restriction analysis timed out")

        data = result.get("data", {})
        sites = [
            RestrictionSite(
                enzyme=s.get("enzyme", ""),
                position=s.get("position", 0),
                cut_sequence=s.get("cutSequence", ""),
            )
            for s in data.get("sites", [])
        ]

        return RestrictionResult(
            enzyme_count=data.get("enzymeCount", 0),
            total_sites=data.get("totalSites", len(sites)),
            sites=sites,
        )

    async def align_sequences(
        self,
        sequences: list[str],
        alignment_type: AlignmentType = AlignmentType.PAIRWISE,
        build_consensus: bool = True,
    ) -> AlignmentResult:
        """Align multiple sequences.

        Args:
            sequences: List of DNA sequences to align
            alignment_type: Type of alignment (pairwise or multiple)
            build_consensus: Build consensus sequence

        Returns:
            AlignmentResult with aligned sequences
        """
        await self.initialize()

        if self.direct_mode:
            return await self._align_direct(sequences, alignment_type, build_consensus)
        else:
            return await self._align_redis(sequences, alignment_type, build_consensus)

    async def _align_direct(
        self,
        sequences: list[str],
        alignment_type: AlignmentType,
        build_consensus: bool,
    ) -> AlignmentResult:
        """Direct alignment."""
        aligner_key = "pairwise" if alignment_type == AlignmentType.PAIRWISE else "multiple"
        aligner = self._direct_analyzers[aligner_key]
        result = aligner.align(sequences, build_consensus=build_consensus)

        return AlignmentResult(
            alignment_id=str(uuid.uuid4()),
            alignment_type=alignment_type.value,
            aligned_sequences=result.alignedSequences,
            score=result.score,
            identity=result.identity,
            gaps=result.gaps,
            consensus=result.consensus if build_consensus else None,
        )

    async def _align_redis(
        self,
        sequences: list[str],
        alignment_type: AlignmentType,
        build_consensus: bool,
    ) -> AlignmentResult:
        """Alignment via Redis."""
        correlation_id = str(uuid.uuid4())

        await self._submit_job(
            "alignments",
            {
                "alignmentId": correlation_id,
                "type": alignment_type.value,
                "sequences": sequences,
                "options": {"build_consensus": build_consensus},
            },
            correlation_id,
        )

        result = await self._wait_for_result("alignments", correlation_id)
        if not result:
            raise TimeoutError("Alignment timed out")

        data = result.get("data", {})
        return AlignmentResult(
            alignment_id=correlation_id,
            alignment_type=alignment_type.value,
            aligned_sequences=data.get("alignedSequences", []),
            score=data.get("score", 0),
            identity=data.get("identity", 0),
            gaps=data.get("gaps", 0),
            consensus=data.get("consensus"),
        )
