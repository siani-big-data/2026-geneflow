"""Phylogenetic analysis worker."""

from typing import Any

import structlog

from src.config import Settings
from src.events.events import PhylogenyCompleted, PhylogenyFailed
from src.events.publisher import EventBusPublisher
from src.models import PhylogenyJob
from src.phylogeny import DistanceMethod, PhylogenyAnalyzer, TreeMethod
from src.workers.base import BaseWorker

logger = structlog.get_logger()


class PhylogenyWorker(BaseWorker):
    """
    Worker for phylogenetic analysis jobs.

    Consumes from geneflow:jobs:phylogeny stream.
    Performs distance calculation, tree building, and bootstrap analysis.
    """

    def __init__(
        self,
        redis,
        publisher: EventBusPublisher,
        settings: Settings,
    ):
        super().__init__(redis, publisher, settings)
        self._analyzer = PhylogenyAnalyzer()

    @property
    def name(self) -> str:
        return "PhylogenyWorker"

    @property
    def stream_name(self) -> str:
        return f"{self._settings.jobs_stream_prefix}:phylogeny"

    async def process_job(self, job_id: str, job_data: dict[str, Any]) -> None:
        """Process a phylogenetic analysis job."""
        job = PhylogenyJob.from_dict(job_data)

        try:
            logger.info(
                "processing_phylogeny",
                analysis_id=job.analysisId,
                alignment_id=job.alignmentId,
                sequence_count=len(job.alignedSequences),
                distance_method=job.distanceMethod,
                tree_method=job.treeMethod,
                bootstrap_replicates=job.bootstrapReplicates,
            )

            distance_method = DistanceMethod(job.distanceMethod)
            tree_method = TreeMethod(job.treeMethod)

            result = self._analyzer.analyze(
                aligned_sequences=job.alignedSequences,
                labels=job.labels,
                distance_method=distance_method,
                tree_method=tree_method,
                bootstrap_replicates=job.bootstrapReplicates,
                analysis_id=job.analysisId,
            )

            logger.info(
                "phylogeny_completed",
                analysis_id=job.analysisId,
                sequence_count=result.sequence_count,
                alignment_length=result.alignment_length,
                newick_length=len(result.tree.newick),
                has_bootstrap=result.bootstrap is not None,
            )

            event = PhylogenyCompleted(
                analysisId=job.analysisId,
                alignmentId=job.alignmentId,
                sequenceCount=result.sequence_count,
                treeMethod=result.tree.method,
                distanceMethod=result.distance_matrix.method,
                hasBootstrap=result.bootstrap is not None,
                bootstrapReplicates=job.bootstrapReplicates,
                correlationId=job.analysisId,
            )
            await self._publisher.publish(event)

        except Exception as e:
            logger.error(
                "phylogeny_failed",
                analysis_id=job.analysisId,
                error=str(e),
                error_type=type(e).__name__,
            )

            event = PhylogenyFailed(
                analysisId=job.analysisId,
                error=str(e),
                errorType=type(e).__name__,
                correlationId=job.analysisId,
            )
            await self._publisher.publish(event)
            raise
