"""Trace processing worker."""

from typing import Any

import structlog

from src.config import Settings
from src.events.events import TraceProcessed, TraceProcessingFailed
from src.events.publisher import EventBusPublisher
from src.models import TraceProcessingJob
from src.parsers import ParserFactory
from src.workers.base import BaseWorker

logger = structlog.get_logger()


class TraceWorker(BaseWorker):
    """
    Worker for processing trace files.

    Consumes from geneflow:jobs:traces stream.
    Parses AB1, SCF, FASTQ, FASTA files and publishes results.
    """

    def __init__(
        self,
        redis,
        publisher: EventBusPublisher,
        settings: Settings,
        storage_provider=None,
    ):
        super().__init__(redis, publisher, settings)
        self._storage = storage_provider
        self._parser_factory = ParserFactory()

    @property
    def name(self) -> str:
        return "TraceWorker"

    @property
    def stream_name(self) -> str:
        return f"{self._settings.jobs_stream_prefix}:traces"

    async def process_job(self, job_id: str, job_data: dict[str, Any]) -> None:
        """Process a trace file parsing job."""
        try:
            # Parse job data
            job = TraceProcessingJob.from_dict(job_data)

            logger.info(
                "processing_trace",
                trace_id=job.traceId,
                study_id=job.studyId,
                format=job.format.value,
                file_name=job.fileName,
            )

            # Fetch trace file data
            trace_data = await self._fetch_trace_data(job)

            # Parse the trace
            parser = self._parser_factory.get_parser(job.format)
            parsed = parser.parse(trace_data, trace_id=job.traceId)

            logger.info(
                "trace_parsed",
                trace_id=job.traceId,
                sequence_length=len(parsed.sequence),
                has_chromatogram=parsed.chromatogram is not None,
                has_quality=parsed.qualityMetrics is not None,
            )

            # Publish success event
            event = TraceProcessed(
                traceId=job.traceId,
                studyId=job.studyId,
                format=job.format.value,
                sequenceLength=len(parsed.sequence),
                meanQuality=parsed.qualityMetrics.meanQuality if parsed.qualityMetrics else None,
                hasChromatogram=parsed.chromatogram is not None,
                correlationId=job.traceId,
            )
            await self._publisher.publish(event)

            # Store result if storage provider available
            if self._storage:
                await self._stoREDACTED(job, parsed)

        except Exception as e:
            logger.error(
                "trace_processing_failed",
                trace_id=job_data.get("traceId", "unknown"),
                error=str(e),
                error_type=type(e).__name__,
            )

            # Publish failure event
            event = TraceProcessingFailed(
                traceId=job_data.get("traceId", "unknown"),
                studyId=job_data.get("studyId", "unknown"),
                error=str(e),
                errorType=type(e).__name__,
                correlationId=job_data.get("traceId"),
            )
            await self._publisher.publish(event)

            raise

    async def _fetch_trace_data(self, job: TraceProcessingJob) -> bytes:
        """Fetch trace file data from storage."""
        if self._storage:
            return await self._storage.get(job.storagePath)

        # Fallback: try local file
        try:
            with open(job.storagePath, "rb") as f:
                return f.read()
        except FileNotFoundError:
            raise ValueError(f"Trace file not found: {job.storagePath}")

    async def _stoREDACTED(self, job: TraceProcessingJob, parsed) -> None:
        """Store parsed trace result."""
        import json

        result_path = f"{job.studyId}/{job.traceId}/parsed.json"
        result_data = json.dumps(parsed.to_dict()).encode("utf-8")

        await self._storage.put(result_path, result_data)

        logger.debug(
            "result_stored",
            trace_id=job.traceId,
            path=result_path,
        )
