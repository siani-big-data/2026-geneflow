"""Trace processing worker."""

import json
from typing import Any

import structlog

from src.config import Settings
from src.events.events import TraceProcessed, TraceProcessingFailed
from src.events.publisher import EventBusPublisher
from src.models import TraceProcessingJob
from src.parsers import ParserFactory
from src.parsers.synthetic_chromatogram import synthesize_chromatogram
from src.utils.chunking import chunk_trace_data
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
            job = TraceProcessingJob.from_dict(job_data)

            logger.info(
                "processing_trace",
                trace_id=job.traceId,
                study_id=job.studyId,
                format=job.format.value,
                file_name=job.fileName,
            )

            trace_data = await self._fetch_trace_data(job)

            parser = self._parser_factory.get_parser(job.format)
            parsed = parser.parse(trace_data, trace_id=job.traceId)

            synthetic_chromatogram = False
            if parsed.chromatogram is None and parsed.sequence.sequence:
                synthesized = synthesize_chromatogram(
                    parsed.sequence.sequence,
                    parsed.sequence.quality,
                )
                if synthesized is not None:
                    parsed.chromatogram = synthesized
                    parsed.metadata["syntheticChromatogram"] = True
                    parsed.metadata["syntheticChromatogramFormat"] = (
                        f"synthetic-{job.format.value}"
                    )
                    synthetic_chromatogram = True

            logger.info(
                "trace_parsed",
                trace_id=job.traceId,
                sequence_length=len(parsed.sequence),
                has_chromatogram=parsed.chromatogram is not None,
                synthetic_chromatogram=synthetic_chromatogram,
                has_quality=parsed.qualityMetrics is not None,
            )

            chunked_data = chunk_trace_data(
                parsed=parsed,
                filename=job.fileName,
                chunk_size=10_000,
            )

            logger.info(
                "trace_chunked",
                trace_id=job.traceId,
                total_bases=chunked_data.manifest.totalBases,
                chunk_count=chunked_data.manifest.chunkCount,
            )

            event = TraceProcessed(
                traceId=job.traceId,
                studyId=job.studyId,
                format=job.format.value,
                sequenceLength=len(parsed.sequence),
                meanQuality=parsed.qualityMetrics.meanQuality if parsed.qualityMetrics else None,
                hasChromatogram=parsed.chromatogram is not None,
                hasQualityScores=parsed.sequence.quality is not None,
                chunkCount=chunked_data.manifest.chunkCount,
                parsedData=chunked_data.to_dict(),
                correlationId=job.traceId,
            )
            await self._publisher.publish(event)

            if self._storage:
                await self._stoREDACTED(job, parsed, chunked_data)

        except Exception as e:
            logger.error(
                "trace_processing_failed",
                trace_id=job_data.get("traceId", "unknown"),
                error=str(e),
                error_type=type(e).__name__,
            )

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

        try:
            with open(job.storagePath, "rb") as f:
                return f.read()
        except FileNotFoundError:
            raise ValueError(f"Trace file not found: {job.storagePath}")

    async def _stoREDACTED(
        self, job: TraceProcessingJob, parsed, chunked_data=None
    ) -> None:
        """Store parsed trace result and chunked data for frontend."""
        result_path = f"{job.studyId}/{job.traceId}/parsed.json"
        result_data = json.dumps(parsed.to_dict()).encode("utf-8")
        await self._storage.put(result_path, result_data)

        logger.debug(
            "result_stored",
            trace_id=job.traceId,
            path=result_path,
        )

        if chunked_data:
            await self._stoREDACTED(job.traceId, chunked_data)

    async def _stoREDACTED(self, trace_id: str, chunked_data) -> None:
        """Store manifest and chunks in the format expected by the API."""
        manifest_path = f"traces/{trace_id}/manifest.json"
        manifest_data = json.dumps(chunked_data.manifest.to_dict()).encode("utf-8")
        await self._storage.put(manifest_path, manifest_data)

        logger.debug(
            "manifest_stored",
            trace_id=trace_id,
            path=manifest_path,
            chunk_count=chunked_data.manifest.chunkCount,
        )

        for chunk in chunked_data.chunks:
            chunk_path = f"traces/{trace_id}/chunks/chunk_{chunk.index:04d}.json"
            chunk_data = json.dumps(chunk.to_dict()).encode("utf-8")
            await self._storage.put(chunk_path, chunk_data)

        logger.info(
            "chunked_data_stored",
            trace_id=trace_id,
            total_chunks=len(chunked_data.chunks),
            total_bases=chunked_data.manifest.totalBases,
        )
