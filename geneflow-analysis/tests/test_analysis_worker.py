"""Extended tests for AnalysisWorker covering all analysis branches."""

from unittest.mock import AsyncMock

import pytest

from src.config import Settings
from src.workers import AnalysisWorker


@pytest.fixture
def mock_redis():
    return AsyncMock()


@pytest.fixture
def mock_publisher():
    pub = AsyncMock()
    pub.publish = AsyncMock()
    return pub


@pytest.fixture
def settings():
    return Settings(eventbus_enabled=False)


@pytest.fixture
def worker(mock_redis, mock_publisher, settings):
    return AnalysisWorker(mock_redis, mock_publisher, settings)


class TestHeterozygote:
    @pytest.mark.asyncio
    async def test_heterozygote_publishes_events(self, worker, mock_publisher):
        await worker.process_job(
            "msg-1",
            {
                "traceId": "t-1",
                "analysisType": "heterozygote",
                "sequence": "ACGTRYACGT",
                "quality": [30] * 10,
                "options": {},
            },
        )
        assert mock_publisher.publish.call_count == 2

    @pytest.mark.asyncio
    async def test_heterozygote_requires_sequence(self, worker):
        with pytest.raises(ValueError, match="Sequence required"):
            await worker.process_job(
                "msg-1",
                {
                    "traceId": "t-1",
                    "analysisType": "heterozygote",
                    "sequence": None,
                    "options": {},
                },
            )


class TestMotif:
    @pytest.mark.asyncio
    async def test_motif_search_publishes_events(self, worker, mock_publisher):
        await worker.process_job(
            "msg-1",
            {
                "traceId": "t-1",
                "analysisType": "motif",
                "sequence": "ACGTACGTACGT",
                "options": {"pattern": "ACGT"},
            },
        )
        assert mock_publisher.publish.call_count == 2

    @pytest.mark.asyncio
    async def test_motif_requires_pattern(self, worker):
        with pytest.raises(ValueError, match="Pattern required"):
            await worker.process_job(
                "msg-1",
                {
                    "traceId": "t-1",
                    "analysisType": "motif",
                    "sequence": "ACGT",
                    "options": {},
                },
            )

    @pytest.mark.asyncio
    async def test_motif_requires_sequence(self, worker):
        with pytest.raises(ValueError, match="Sequence required"):
            await worker.process_job(
                "msg-1",
                {
                    "traceId": "t-1",
                    "analysisType": "motif",
                    "sequence": None,
                    "options": {"pattern": "ACGT"},
                },
            )


class TestTranslation:
    @pytest.mark.asyncio
    async def test_translation_publishes_events(self, worker, mock_publisher):
        await worker.process_job(
            "msg-1",
            {
                "traceId": "t-1",
                "analysisType": "translation",
                "sequence": "ATGAAATAA",
                "options": {"frame": 1},
            },
        )
        assert mock_publisher.publish.call_count == 2

    @pytest.mark.asyncio
    async def test_translation_requires_sequence(self, worker):
        with pytest.raises(ValueError, match="Sequence required"):
            await worker.process_job(
                "msg-1",
                {
                    "traceId": "t-1",
                    "analysisType": "translation",
                    "sequence": None,
                    "options": {},
                },
            )


class TestORF:
    @pytest.mark.asyncio
    async def test_orf_publishes_events(self, worker, mock_publisher):
        await worker.process_job(
            "msg-1",
            {
                "traceId": "t-1",
                "analysisType": "orf",
                "sequence": "ATGAAACCCGGGTAA" + "T" * 60,
                "options": {"min_length": 1},
            },
        )
        assert mock_publisher.publish.call_count == 2

    @pytest.mark.asyncio
    async def test_orf_requires_sequence(self, worker):
        with pytest.raises(ValueError, match="Sequence required"):
            await worker.process_job(
                "msg-1",
                {
                    "traceId": "t-1",
                    "analysisType": "orf",
                    "sequence": None,
                    "options": {},
                },
            )


class TestRestriction:
    @pytest.mark.asyncio
    async def test_restriction_publishes_events(self, worker, mock_publisher):
        await worker.process_job(
            "msg-1",
            {
                "traceId": "t-1",
                "analysisType": "restriction",
                "sequence": "GAATTCGAATTCAAAAGAATTC",
                "options": {"enzymes": ["EcoRI"]},
            },
        )
        assert mock_publisher.publish.call_count == 2

    @pytest.mark.asyncio
    async def test_restriction_requires_sequence(self, worker):
        with pytest.raises(ValueError, match="Sequence required"):
            await worker.process_job(
                "msg-1",
                {
                    "traceId": "t-1",
                    "analysisType": "restriction",
                    "sequence": None,
                    "options": {},
                },
            )


class TestDispatchErrors:
    @pytest.mark.asyncio
    async def test_logs_and_raises_on_internal_failure(self, worker):
        with pytest.raises(ValueError):
            await worker.process_job(
                "msg-1",
                {
                    "traceId": "t-1",
                    "analysisType": "translation",
                    "sequence": None,
                    "options": {},
                },
            )
