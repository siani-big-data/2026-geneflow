"""Analysis processing worker."""

from typing import Any

import structlog

from src.config import Settings
from src.events.events import (
    TrimmingCompleted,
    HeterozygoteDetectionCompleted,
    MotifSearchCompleted,
    TranslationCompleted,
    ORFDetectionCompleted,
    RestrictionAnalysisCompleted,
)
from src.events.publisher import EventBusPublisher
from src.models import AnalysisJob, AnalysisType, TrimmingAlgorithm, Sequence
from src.analyzers import (
    QualityAnalyzer,
    TrimmingAnalyzer,
    HeterozygoteAnalyzer,
    MotifAnalyzer,
    TranslationAnalyzer,
    ORFAnalyzer,
    RestrictionAnalyzer,
)
from src.workers.base import BaseWorker

logger = structlog.get_logger()


class AnalysisWorker(BaseWorker):
    """
    Worker for sequence analysis jobs.

    Consumes from geneflow:jobs:analysis stream.
    Performs quality analysis, trimming, and other analysis types.
    """

    def __init__(
        self,
        redis,
        publisher: EventBusPublisher,
        settings: Settings,
    ):
        super().__init__(redis, publisher, settings)
        self._quality_analyzer = QualityAnalyzer()
        self._trimming_analyzer = TrimmingAnalyzer()
        self._heterozygote_analyzer = HeterozygoteAnalyzer()
        self._motif_analyzer = MotifAnalyzer()
        self._translation_analyzer = TranslationAnalyzer()
        self._orf_analyzer = ORFAnalyzer()
        self._restriction_analyzer = RestrictionAnalyzer()

    @property
    def name(self) -> str:
        return "AnalysisWorker"

    @property
    def stream_name(self) -> str:
        return f"{self._settings.jobs_stream_prefix}:analysis"

    async def process_job(self, job_id: str, job_data: dict[str, Any]) -> None:
        """Process an analysis job."""
        try:
            # Parse job data
            job = AnalysisJob.from_dict(job_data)

            logger.info(
                "processing_analysis",
                trace_id=job.traceId,
                analysis_type=job.analysisType.value,
            )

            # Dispatch to appropriate handler
            if job.analysisType == AnalysisType.QUALITY:
                await self._process_quality(job)
            elif job.analysisType == AnalysisType.TRIMMING:
                await self._process_trimming(job)
            elif job.analysisType == AnalysisType.HETEROZYGOTE:
                await self._process_heterozygote(job)
            elif job.analysisType == AnalysisType.MOTIF:
                await self._process_motif(job)
            elif job.analysisType == AnalysisType.TRANSLATION:
                await self._process_translation(job)
            elif job.analysisType == AnalysisType.ORF:
                await self._process_orf(job)
            elif job.analysisType == AnalysisType.RESTRICTION:
                await self._process_restriction(job)
            else:
                raise ValueError(f"Unknown analysis type: {job.analysisType}")

        except Exception as e:
            logger.error(
                "analysis_failed",
                trace_id=job_data.get("traceId", "unknown"),
                analysis_type=job_data.get("analysisType", "unknown"),
                error=str(e),
                error_type=type(e).__name__,
            )
            raise

    async def _process_quality(self, job: AnalysisJob) -> None:
        """Process quality analysis."""
        if not job.sequence:
            raise ValueError("Sequence required for quality analysis")

        seq = Sequence(
            id=job.traceId,
            sequence=job.sequence,
            quality=job.quality,
        )

        result = self._quality_analyzer.analyze(seq)

        logger.info(
            "quality_analysis_completed",
            trace_id=job.traceId,
            mean_quality=result.meanQuality,
            gc_content=result.gcContent,
        )

    async def _process_trimming(self, job: AnalysisJob) -> None:
        """Process trimming analysis."""
        if not job.sequence:
            raise ValueError("Sequence required for trimming")
        if not job.quality:
            raise ValueError("Quality scores required for trimming")

        seq = Sequence(
            id=job.traceId,
            sequence=job.sequence,
            quality=job.quality,
        )

        options = dict(job.options)
        algorithm_name = options.pop("algorithm", "modified_mott")
        algorithm = TrimmingAlgorithm(algorithm_name)

        result = self._trimming_analyzer.analyze(
            seq,
            algorithm=algorithm,
            **options,
        )

        logger.info(
            "trimming_completed",
            trace_id=job.traceId,
            original_length=result.originalLength,
            trimmed_length=result.trimmedLength,
        )

        event = TrimmingCompleted(
            traceId=job.traceId,
            algorithm=algorithm.value,
            originalLength=result.originalLength,
            trimmedLength=result.trimmedLength,
            trimStart=result.trimStart,
            trimEnd=result.trimEnd,
            correlationId=job.traceId,
        )
        await self._publisher.publish(event)

    async def _process_heterozygote(self, job: AnalysisJob) -> None:
        """Process heterozygote detection."""
        if not job.sequence:
            raise ValueError("Sequence required for heterozygote detection")

        seq = Sequence(
            id=job.traceId,
            sequence=job.sequence,
            quality=job.quality,
        )

        result = self._heterozygote_analyzer.analyze(
            seq,
            chromatogram=job.chromatogram,
            **job.options,
        )

        logger.info(
            "heterozygote_detection_completed",
            trace_id=job.traceId,
            heterozygote_count=result.heterozygoteCount,
        )

        event = HeterozygoteDetectionCompleted(
            traceId=job.traceId,
            heterozygoteCount=result.heterozygoteCount,
            positions=[c.position for c in result.calls],
            correlationId=job.traceId,
        )
        await self._publisher.publish(event)

    async def _process_motif(self, job: AnalysisJob) -> None:
        """Process motif search."""
        if not job.sequence:
            raise ValueError("Sequence required for motif search")

        pattern = job.options.get("pattern", "")
        if not pattern:
            raise ValueError("Pattern required for motif search")

        seq = Sequence(
            id=job.traceId,
            sequence=job.sequence,
            quality=job.quality,
        )

        result = self._motif_analyzer.analyze(
            seq,
            pattern=pattern,
            search_complement=job.options.get("search_complement", False),
            use_regex=job.options.get("use_regex", False),
        )

        logger.info(
            "motif_search_completed",
            trace_id=job.traceId,
            pattern=pattern,
            match_count=result.matchCount,
        )

        event = MotifSearchCompleted(
            traceId=job.traceId,
            pattern=pattern,
            matchCount=result.matchCount,
            positions=[m.start for m in result.matches],
            correlationId=job.traceId,
        )
        await self._publisher.publish(event)

    async def _process_translation(self, job: AnalysisJob) -> None:
        """Process translation."""
        if not job.sequence:
            raise ValueError("Sequence required for translation")

        seq = Sequence(
            id=job.traceId,
            sequence=job.sequence,
            quality=job.quality,
        )

        frame = job.options.get("frame", 1)
        result = self._translation_analyzer.analyze(seq, frame=frame)

        logger.info(
            "translation_completed",
            trace_id=job.traceId,
            frame=frame,
            protein_length=result.proteinLength,
        )

        event = TranslationCompleted(
            traceId=job.traceId,
            frame=frame,
            proteinLength=result.proteinLength,
            correlationId=job.traceId,
        )
        await self._publisher.publish(event)

    async def _process_orf(self, job: AnalysisJob) -> None:
        """Process ORF detection."""
        if not job.sequence:
            raise ValueError("Sequence required for ORF detection")

        seq = Sequence(
            id=job.traceId,
            sequence=job.sequence,
            quality=job.quality,
        )

        min_length = job.options.get("min_length", 30)
        result = self._orf_analyzer.analyze(seq, min_length=min_length)

        longest_length = result.longestOrf.length if result.longestOrf else 0

        logger.info(
            "orf_detection_completed",
            trace_id=job.traceId,
            orf_count=result.totalOrfs,
            longest_orf=longest_length,
        )

        event = ORFDetectionCompleted(
            traceId=job.traceId,
            orfCount=result.totalOrfs,
            longestOrfLength=longest_length,
            correlationId=job.traceId,
        )
        await self._publisher.publish(event)

    async def _process_restriction(self, job: AnalysisJob) -> None:
        """Process restriction analysis."""
        if not job.sequence:
            raise ValueError("Sequence required for restriction analysis")

        seq = Sequence(
            id=job.traceId,
            sequence=job.sequence,
            quality=job.quality,
        )

        enzymes = job.options.get("enzymes")
        result = self._restriction_analyzer.analyze(seq, enzymes=enzymes)

        enzymes_with_sites = list(set(s.enzyme for s in result.sites))

        logger.info(
            "restriction_analysis_completed",
            trace_id=job.traceId,
            total_sites=result.totalSites,
            enzyme_count=result.enzymeCount,
        )

        event = RestrictionAnalysisCompleted(
            traceId=job.traceId,
            enzymeCount=result.enzymeCount,
            totalSites=result.totalSites,
            enzymesWithSites=enzymes_with_sites,
            correlationId=job.traceId,
        )
        await self._publisher.publish(event)
