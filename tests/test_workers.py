"""Tests for workers module."""

import pytest
from unittest.mock import AsyncMock, MagicMock, patch

from src.workers import BaseWorker, TraceWorker, AlignmentWorker, AnalysisWorker
from src.workers.base import BaseWorker
from src.models import WorkerStatus, TraceFormat, AlignmentType, AnalysisType
from src.config import Settings


class ConcreteWorker(BaseWorker):
    """Concrete implementation for testing BaseWorker."""

    @property
    def name(self) -> str:
        return "TestWorker"

    @property
    def stream_name(self) -> str:
        return "test:stream"

    async def process_job(self, job_id: str, job_data: dict) -> None:
        pass


class TestBaseWorker:
    """Tests for BaseWorker."""

    @pytest.fixture
    def mock_redis(self):
        redis = AsyncMock()
        redis.ping = AsyncMock(return_value=True)
        redis.xgroup_create = AsyncMock()
        redis.xreadgroup = AsyncMock(return_value=[])
        redis.xack = AsyncMock()
        return redis

    @pytest.fixture
    def mock_publisher(self):
        return AsyncMock()

    @pytest.fixture
    def settings(self):
        return Settings()

    def test_initial_status_is_stopped(self, mock_redis, mock_publisher, settings):
        """Test that worker starts in stopped state."""
        worker = ConcreteWorker(mock_redis, mock_publisher, settings)
        assert worker.status == WorkerStatus.STOPPED

    def test_metrics_initialized(self, mock_redis, mock_publisher, settings):
        """Test that metrics are initialized."""
        worker = ConcreteWorker(mock_redis, mock_publisher, settings)
        assert worker.metrics.jobsProcessed == 0
        assert worker.metrics.jobsFailed == 0

    def test_parse_message_data_decodes_bytes(self, mock_redis, mock_publisher, settings):
        """Test message data parsing from bytes."""
        worker = ConcreteWorker(mock_redis, mock_publisher, settings)

        data = {
            b"key1": b"value1",
            b"key2": b'{"nested": "json"}',
        }

        parsed = worker._parse_message_data(data)

        assert parsed["key1"] == "value1"
        assert parsed["key2"] == {"nested": "json"}

    def test_parse_message_data_flattens_data_key(self, mock_redis, mock_publisher, settings):
        """Test that nested 'data' key is flattened."""
        worker = ConcreteWorker(mock_redis, mock_publisher, settings)

        data = {
            "data": '{"traceId": "123", "studyId": "456"}',
        }

        parsed = worker._parse_message_data(data)

        assert parsed["traceId"] == "123"
        assert parsed["studyId"] == "456"


class TestTraceWorker:
    """Tests for TraceWorker."""

    @pytest.fixture
    def mock_redis(self):
        redis = AsyncMock()
        return redis

    @pytest.fixture
    def mock_publisher(self):
        return AsyncMock()

    @pytest.fixture
    def settings(self):
        return Settings()

    def test_worker_name(self, mock_redis, mock_publisher, settings):
        """Test worker name."""
        worker = TraceWorker(mock_redis, mock_publisher, settings)
        assert worker.name == "TraceWorker"

    def test_stream_name(self, mock_redis, mock_publisher, settings):
        """Test stream name."""
        worker = TraceWorker(mock_redis, mock_publisher, settings)
        assert "traces" in worker.stream_name

    @pytest.mark.asyncio
    async def test_process_job_publishes_event(self, mock_redis, mock_publisher, settings):
        """Test that processing publishes events."""
        worker = TraceWorker(mock_redis, mock_publisher, settings)

        # Mock storage to return FASTA data
        fasta_data = b">test\nACGTACGT\n"

        with patch.object(worker, "_fetch_trace_data", return_value=fasta_data):
            job_data = {
                "traceId": "trace-123",
                "studyId": "study-456",
                "fileName": "test.fasta",
                "storagePath": "/path/to/test.fasta",
                "format": "fasta",
            }

            await worker.process_job("msg-1", job_data)

        # Check event was published
        mock_publisher.publish.assert_called_once()
        event = mock_publisher.publish.call_args[0][0]
        assert event.traceId == "trace-123"


class TestAlignmentWorker:
    """Tests for AlignmentWorker."""

    @pytest.fixture
    def mock_redis(self):
        return AsyncMock()

    @pytest.fixture
    def mock_publisher(self):
        return AsyncMock()

    @pytest.fixture
    def settings(self):
        return Settings()

    def test_worker_name(self, mock_redis, mock_publisher, settings):
        """Test worker name."""
        worker = AlignmentWorker(mock_redis, mock_publisher, settings)
        assert worker.name == "AlignmentWorker"

    def test_stream_name(self, mock_redis, mock_publisher, settings):
        """Test stream name."""
        worker = AlignmentWorker(mock_redis, mock_publisher, settings)
        assert "alignments" in worker.stream_name

    @pytest.mark.asyncio
    async def test_process_pairwise_alignment(self, mock_redis, mock_publisher, settings):
        """Test pairwise alignment processing."""
        worker = AlignmentWorker(mock_redis, mock_publisher, settings)

        job_data = {
            "alignmentId": "align-123",
            "type": "pairwise",
            "traceIds": ["t1", "t2"],
            "sequences": ["ACGT", "ACGT"],
            "options": {},
        }

        await worker.process_job("msg-1", job_data)

        mock_publisher.publish.assert_called_once()
        event = mock_publisher.publish.call_args[0][0]
        assert event.alignmentId == "align-123"

    @pytest.mark.asyncio
    async def test_process_multiple_alignment(self, mock_redis, mock_publisher, settings):
        """Test multiple alignment processing."""
        worker = AlignmentWorker(mock_redis, mock_publisher, settings)

        job_data = {
            "alignmentId": "align-456",
            "type": "multiple",
            "traceIds": ["t1", "t2", "t3"],
            "sequences": ["ACGT", "ACGT", "ACTT"],
            "options": {},
        }

        await worker.process_job("msg-1", job_data)

        mock_publisher.publish.assert_called_once()

    @pytest.mark.asyncio
    async def test_fails_with_insufficient_sequences(self, mock_redis, mock_publisher, settings):
        """Test failure with less than 2 sequences."""
        worker = AlignmentWorker(mock_redis, mock_publisher, settings)

        job_data = {
            "alignmentId": "align-789",
            "type": "pairwise",
            "traceIds": ["t1"],
            "sequences": ["ACGT"],
            "options": {},
        }

        with pytest.raises(ValueError, match="At least 2 sequences"):
            await worker.process_job("msg-1", job_data)


class TestAnalysisWorker:
    """Tests for AnalysisWorker."""

    @pytest.fixture
    def mock_redis(self):
        return AsyncMock()

    @pytest.fixture
    def mock_publisher(self):
        return AsyncMock()

    @pytest.fixture
    def settings(self):
        return Settings()

    def test_worker_name(self, mock_redis, mock_publisher, settings):
        """Test worker name."""
        worker = AnalysisWorker(mock_redis, mock_publisher, settings)
        assert worker.name == "AnalysisWorker"

    def test_stream_name(self, mock_redis, mock_publisher, settings):
        """Test stream name."""
        worker = AnalysisWorker(mock_redis, mock_publisher, settings)
        assert "analysis" in worker.stream_name

    @pytest.mark.asyncio
    async def test_process_quality_analysis(self, mock_redis, mock_publisher, settings):
        """Test quality analysis processing."""
        worker = AnalysisWorker(mock_redis, mock_publisher, settings)

        job_data = {
            "traceId": "trace-123",
            "analysisType": "quality",
            "sequence": "ACGTACGT",
            "quality": [30, 30, 30, 30, 30, 30, 30, 30],
            "options": {},
        }

        await worker.process_job("msg-1", job_data)
        # Quality analysis doesn't publish events yet

    @pytest.mark.asyncio
    async def test_process_trimming(self, mock_redis, mock_publisher, settings):
        """Test trimming processing."""
        worker = AnalysisWorker(mock_redis, mock_publisher, settings)

        job_data = {
            "traceId": "trace-123",
            "analysisType": "trimming",
            "sequence": "ACGTACGT",
            "quality": [30, 30, 30, 30, 30, 30, 30, 30],
            "options": {"algorithm": "modified_mott"},
        }

        await worker.process_job("msg-1", job_data)

        mock_publisher.publish.assert_called_once()

    @pytest.mark.asyncio
    async def test_fails_without_sequence(self, mock_redis, mock_publisher, settings):
        """Test failure without sequence."""
        worker = AnalysisWorker(mock_redis, mock_publisher, settings)

        job_data = {
            "traceId": "trace-123",
            "analysisType": "quality",
            "sequence": None,
            "options": {},
        }

        with pytest.raises(ValueError, match="Sequence required"):
            await worker.process_job("msg-1", job_data)

    @pytest.mark.asyncio
    async def test_fails_trimming_without_quality(self, mock_redis, mock_publisher, settings):
        """Test trimming failure without quality scores."""
        worker = AnalysisWorker(mock_redis, mock_publisher, settings)

        job_data = {
            "traceId": "trace-123",
            "analysisType": "trimming",
            "sequence": "ACGTACGT",
            "quality": None,
            "options": {},
        }

        with pytest.raises(ValueError, match="Quality scores required"):
            await worker.process_job("msg-1", job_data)
