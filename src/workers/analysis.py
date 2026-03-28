"""Analysis processing worker."""

from typing import Any

import structlog

from src.config import Settings
from src.events.events import TrimmingCompleted
from src.events.publisher import EventBusPublisher
from src.models import AnalysisJob, AnalysisType, TrimmingAlgorithm, Sequence
from src.analyzers import QualityAnalyzer, TrimmingAnalyzer
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

        # Create Sequence object
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

        # Create Sequence object
        seq = Sequence(
            id=job.traceId,
            sequence=job.sequence,
            quality=job.quality,
        )

        # Get algorithm from options
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

        # Publish event
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
        # Will be implemented in Phase 6
        logger.info(
            "heterozygote_detection_placeholder",
            trace_id=job.traceId,
        )

    async def _process_motif(self, job: AnalysisJob) -> None:
        """Process motif search."""
        # Will be implemented in Phase 6
        logger.info(
            "motif_search_placeholder",
            trace_id=job.traceId,
        )

    async def _process_translation(self, job: AnalysisJob) -> None:
        """Process translation."""
        # Will be implemented in Phase 6
        logger.info(
            "translation_placeholder",
            trace_id=job.traceId,
        )

    async def _process_orf(self, job: AnalysisJob) -> None:
        """Process ORF detection."""
        # Will be implemented in Phase 6
        logger.info(
            "orf_detection_placeholder",
            trace_id=job.traceId,
        )

    async def _process_restriction(self, job: AnalysisJob) -> None:
        """Process restriction analysis."""
        # Will be implemented in Phase 6
        logger.info(
            "restriction_analysis_placeholder",
            trace_id=job.traceId,
        )
