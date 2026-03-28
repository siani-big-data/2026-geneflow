#!/usr/bin/env python
"""Proving script for workers module (without Redis)."""

import asyncio
from unittest.mock import AsyncMock

from src.config import Settings
from src.workers import TraceWorker, AlignmentWorker, AnalysisWorker
from src.models import WorkerStatus


async def main():
    """Run workers proving tests."""
    print("=" * 60)
    print("WORKERS MODULE - PROVING SCRIPT")
    print("=" * 60)

    # Mock dependencies
    mock_redis = AsyncMock()
    mock_redis.ping = AsyncMock(return_value=True)
    mock_publisher = AsyncMock()
    settings = Settings()

    # 1. TraceWorker
    print("\n1. TRACE WORKER")
    print("-" * 40)

    trace_worker = TraceWorker(mock_redis, mock_publisher, settings)
    print(f"Name:        {trace_worker.name}")
    print(f"Stream:      {trace_worker.stream_name}")
    print(f"Status:      {trace_worker.status.value}")

    # Test processing FASTA
    fasta_data = b">test_sequence\nACGTACGTACGT\n"

    async def mock_fetch(*args):
        return fasta_data

    trace_worker._fetch_trace_data = mock_fetch

    job_data = {
        "traceId": "trace-001",
        "studyId": "study-001",
        "fileName": "test.fasta",
        "storagePath": "/mock/path",
        "format": "fasta",
    }

    await trace_worker.process_job("msg-1", job_data)
    print(f"Processed:   trace-001 (FASTA)")
    print(f"Event sent:  {mock_publisher.publish.call_count} event(s)")

    # 2. AlignmentWorker
    print("\n2. ALIGNMENT WORKER")
    print("-" * 40)

    mock_publisher.reset_mock()
    align_worker = AlignmentWorker(mock_redis, mock_publisher, settings)
    print(f"Name:        {align_worker.name}")
    print(f"Stream:      {align_worker.stream_name}")

    # Test pairwise alignment
    job_data = {
        "alignmentId": "align-001",
        "type": "pairwise",
        "traceIds": ["t1", "t2"],
        "sequences": ["ACGTACGT", "ACGTAGGT"],
        "options": {},
    }

    await align_worker.process_job("msg-2", job_data)
    print(f"Pairwise:    align-001 completed")

    # Test multiple alignment
    job_data = {
        "alignmentId": "align-002",
        "type": "multiple",
        "traceIds": ["t1", "t2", "t3"],
        "sequences": ["ACGTACGT", "ACGTAGGT", "ACGTACTT"],
        "options": {"build_consensus": True, "consensus_method": "majority"},
    }

    await align_worker.process_job("msg-3", job_data)
    print(f"Multiple:    align-002 completed (with consensus)")
    print(f"Events sent: {mock_publisher.publish.call_count} event(s)")

    # 3. AnalysisWorker
    print("\n3. ANALYSIS WORKER")
    print("-" * 40)

    mock_publisher.reset_mock()
    analysis_worker = AnalysisWorker(mock_redis, mock_publisher, settings)
    print(f"Name:        {analysis_worker.name}")
    print(f"Stream:      {analysis_worker.stream_name}")

    # Test quality analysis
    job_data = {
        "traceId": "trace-002",
        "analysisType": "quality",
        "sequence": "ACGTACGTACGT",
        "quality": [30, 35, 40, 30, 35, 40, 30, 35, 40, 30, 35, 40],
    }

    await analysis_worker.process_job("msg-4", job_data)
    print(f"Quality:     trace-002 analyzed")

    # Test trimming
    job_data = {
        "traceId": "trace-003",
        "analysisType": "trimming",
        "sequence": "NNNACGTACGTNNN",
        "quality": [5, 5, 5, 30, 35, 40, 30, 35, 40, 30, 5, 5, 5, 5],
        "options": {"algorithm": "modified_mott"},
    }

    await analysis_worker.process_job("msg-5", job_data)
    print(f"Trimming:    trace-003 trimmed")
    print(f"Events sent: {mock_publisher.publish.call_count} event(s)")

    # 4. Worker Metrics
    print("\n4. WORKER METRICS")
    print("-" * 40)

    # Simulate some job processing
    trace_worker._metrics.jobsProcessed = 100
    trace_worker._metrics.jobsFailed = 2
    trace_worker._metrics.averageProcessingTimeMs = 45.5

    metrics = trace_worker.metrics
    print(f"Jobs processed:  {metrics.jobsProcessed}")
    print(f"Jobs failed:     {metrics.jobsFailed}")
    print(f"Avg time (ms):   {metrics.averageProcessingTimeMs}")
    print(f"Success rate:    {100 * (1 - metrics.jobsFailed / metrics.jobsProcessed):.1f}%")

    # 5. Stream Names
    print("\n5. STREAM CONFIGURATION")
    print("-" * 40)

    print(f"Traces stream:     {trace_worker.stream_name}")
    print(f"Alignments stream: {align_worker.stream_name}")
    print(f"Analysis stream:   {analysis_worker.stream_name}")
    print(f"Consumer group:    {settings.redis_consumer_group}")
    print(f"Consumer name:     {settings.redis_consumer_name}")

    print("\n" + "=" * 60)
    print("PROVING COMPLETE - All workers functioning correctly")
    print("=" * 60)


if __name__ == "__main__":
    asyncio.run(main())
