"""Alignment processing worker."""

from typing import Any

import structlog

from src.alignment import ConsensusBuilder, MultipleAligner, PairwiseAligner
from src.alignment.consensus import ConsensusMethod
from src.config import Settings
from src.events.events import AlignmentCompleted, AlignmentFailed
from src.events.publisher import EventBusPublisher
from src.models import AlignmentJob, AlignmentType
from src.workers.base import BaseWorker

logger = structlog.get_logger()


class AlignmentWorker(BaseWorker):
    """
    Worker for sequence alignment jobs.

    Consumes from geneflow:jobs:alignments stream.
    Performs pairwise or multiple sequence alignment.
    """

    def __init__(
        self,
        redis,
        publisher: EventBusPublisher,
        settings: Settings,
    ):
        super().__init__(redis, publisher, settings)
        self._pairwise_aligner = PairwiseAligner()
        self._multiple_aligner = MultipleAligner()
        self._consensus_builder = ConsensusBuilder()

    @property
    def name(self) -> str:
        return "AlignmentWorker"

    @property
    def stream_name(self) -> str:
        return f"{self._settings.jobs_stream_prefix}:alignments"

    async def process_job(self, job_id: str, job_data: dict[str, Any]) -> None:
        """Process an alignment job."""
        try:
            # Parse job data
            job = AlignmentJob.from_dict(job_data)

            logger.info(
                "processing_alignment",
                alignment_id=job.alignmentId,
                type=job.type.value,
                sequence_count=len(job.sequences),
            )

            # Validate sequences
            if not job.sequences or len(job.sequences) < 2:
                raise ValueError("At least 2 sequences required for alignment")

            # Perform alignment based on type
            if job.type == AlignmentType.PAIRWISE:
                result = self._pairwise_aligner.align(
                    job.sequences,
                    alignment_id=job.alignmentId,
                    **job.options,
                )
            else:
                result = self._multiple_aligner.align(
                    job.sequences,
                    alignment_id=job.alignmentId,
                    **job.options,
                )

            # Build consensus if requested
            consensus = None
            if job.options.get("build_consensus", False):
                method = ConsensusMethod(job.options.get("consensus_method", "majority"))
                consensus_result = self._consensus_builder.build(
                    result.alignedSequences,
                    method=method,
                )
                consensus = consensus_result.consensus

            logger.info(
                "alignment_completed",
                alignment_id=job.alignmentId,
                score=result.score,
                identity=result.identity,
                gaps=result.gaps,
                has_consensus=consensus is not None,
            )

            # Publish success event
            event = AlignmentCompleted(
                alignmentId=job.alignmentId,
                type=job.type.value,
                sequenceCount=len(job.sequences),
                score=result.score,
                identity=result.identity,
                hasConsensus=consensus is not None,
                correlationId=job.alignmentId,
            )
            await self._publisher.publish(event)

        except Exception as e:
            logger.error(
                "alignment_failed",
                alignment_id=job_data.get("alignmentId", "unknown"),
                error=str(e),
                error_type=type(e).__name__,
            )

            # Publish failure event
            event = AlignmentFailed(
                alignmentId=job_data.get("alignmentId", "unknown"),
                error=str(e),
                errorType=type(e).__name__,
                correlationId=job_data.get("alignmentId"),
            )
            await self._publisher.publish(event)

            raise
