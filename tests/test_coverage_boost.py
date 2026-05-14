"""Targeted tests to raise line and branch coverage on under-tested modules."""

from __futuREDACTED import annotations

import asyncio
import sys
from pathlib import Path
from unittest.mock import AsyncMock, MagicMock, patch

import httpx
import pytest

from src.analyzers.heterozygote import HeterozygoteAnalyzer
from src.analyzers.orf import ORFAnalyzer
from src.analyzers.translation import TranslationAnalyzer
from src.models import ChromatogramData, Sequence, TraceFormat
from src.parsers.parser import BaseParser, ParserFactory
from src.storage.base import StorageError
from src.storage.http import HTTPStorageProvider


# ---------------------------------------------------------------------------
# HeterozygoteAnalyzer: chromatogram detection branches
# ---------------------------------------------------------------------------


def _chromatogram(num_samples: int = 8, num_bases: int = 4) -> ChromatogramData:
    """Build a simple chromatogram with controllable peaks."""
    return ChromatogramData(
        traceA=[0] * num_samples,
        traceC=[0] * num_samples,
        traceG=[0] * num_samples,
        traceT=[0] * num_samples,
        baseCalls=list(range(num_bases)),
        peakLocations=list(range(num_bases)),
    )


class TestHeterozygoteChromatogram:
    def test_detect_heterozygote_from_peaks(self):
        chrom = _chromatogram()
        chrom.traceA[0] = 100
        chrom.traceG[0] = 50
        chrom.traceC[1] = 100
        chrom.traceT[1] = 40
        seq = Sequence(id="s", sequence="NN")

        result = HeterozygoteAnalyzer().analyze(
            seq, chromatogram=chrom, min_ratio=0.3, max_ratio=0.7, min_confidence=0.5
        )

        assert result.heterozygoteCount >= 1
        assert all(c.confidence >= 0.5 for c in result.calls)

    def test_skips_when_primary_peak_zero(self):
        chrom = _chromatogram()
        seq = Sequence(id="s", sequence="N")

        result = HeterozygoteAnalyzer().analyze(seq, chromatogram=chrom)

        assert result.heterozygoteCount == 0

    def test_ratio_outside_range_filtered(self):
        chrom = _chromatogram()
        chrom.traceA[0] = 100
        chrom.traceG[0] = 5
        seq = Sequence(id="s", sequence="A")

        result = HeterozygoteAnalyzer().analyze(seq, chromatogram=chrom)

        assert result.heterozygoteCount == 0

    def test_low_confidence_filtered(self):
        chrom = _chromatogram()
        chrom.traceA[0] = 100
        chrom.traceG[0] = 70
        seq = Sequence(id="s", sequence="A")

        result = HeterozygoteAnalyzer().analyze(
            seq, chromatogram=chrom, min_confidence=0.99
        )

        assert result.heterozygoteCount == 0

    def test_sequence_shorter_than_peaks_breaks(self):
        chrom = _chromatogram(num_bases=10)
        seq = Sequence(id="s", sequence="A")

        result = HeterozygoteAnalyzer().analyze(seq, chromatogram=chrom)

        assert result.totalPositions == 1

    def test_peak_location_beyond_trace_uses_zero(self):
        chrom = ChromatogramData(
            traceA=[5],
            traceC=[10],
            traceG=[0],
            traceT=[0],
            baseCalls=[0],
            peakLocations=[100],
        )
        seq = Sequence(id="s", sequence="N")

        result = HeterozygoteAnalyzer().analyze(seq, chromatogram=chrom)

        assert result.heterozygoteCount == 0

    def test_summarize_with_calls(self):
        seq = Sequence(id="s", sequence="ARYAA")
        analyzer = HeterozygoteAnalyzer()

        result = analyzer.analyze(seq)
        summary = analyzer.summarize(result)

        assert summary["count"] == 2
        assert "R" in summary["iupacCodes"]
        assert summary["avgConfidence"] > 0

    def test_summarize_empty(self):
        seq = Sequence(id="s", sequence="ACGT")
        analyzer = HeterozygoteAnalyzer()

        summary = analyzer.summarize(analyzer.analyze(seq))

        assert summary["count"] == 0
        assert summary["rate"] == 0.0
        assert summary["iupacCodes"] == {}


# ---------------------------------------------------------------------------
# TranslationAnalyzer: composition, reverse frames, all frames
# ---------------------------------------------------------------------------


class TestTranslation:
    def test_invalid_frame_raises(self):
        with pytest.raises(ValueError, match="Frame must be"):
            TranslationAnalyzer().analyze(Sequence(id="s", sequence="ATG"), frame=99)

    def test_reverse_frame(self):
        result = TranslationAnalyzer().analyze(
            Sequence(id="s", sequence="ATGAAATAA"), frame=-1
        )
        assert result.proteinLength >= 0

    def test_translate_all_frames(self):
        results = TranslationAnalyzer().translate_all_frames(
            Sequence(id="s", sequence="ATGAAATAAATG")
        )
        assert len(results) == 6
        assert {r.frame for r in results} == {1, 2, 3}

    def test_analyze_composition_empty(self):
        info = TranslationAnalyzer().analyze_composition("*")
        assert info == {"total": 0, "composition": {}, "percentages": {}, "properties": {}}

    def test_analyze_composition_full(self):
        info = TranslationAnalyzer().analyze_composition("MAAFKDEW")
        assert info["total"] == 8
        assert info["percentages"]["A"] > 0
        assert info["properties"]["hydrophobic"] > 0
        assert info["properties"]["charged"] > 0
        assert info["properties"]["aromatic"] > 0

    def test_reverse_complement_unknown_base(self):
        result = TranslationAnalyzer().analyze(
            Sequence(id="s", sequence="ATGXYZTAA"), frame=-1
        )
        assert result is not None


# ---------------------------------------------------------------------------
# ORFAnalyzer: invalid frames, summarize, find_longest_orf
# ---------------------------------------------------------------------------


class TestORFAnalyzerExtended:
    def test_invalid_frame_raises(self):
        with pytest.raises(ValueError, match="Invalid frame"):
            ORFAnalyzer().analyze(Sequence(id="s", sequence="ATG"), frames=[7])

    def test_find_longest_orf_present(self):
        seq = Sequence(id="s", sequence="ATG" + "AAA" * 30 + "TAA")
        longest = ORFAnalyzer().find_longest_orf(seq)
        assert longest is not None
        assert longest.length > 0

    def test_find_longest_orf_none(self):
        assert ORFAnalyzer().find_longest_orf(Sequence(id="s", sequence="AAAAAA")) is None

    def test_summarize_with_orfs(self):
        seq = Sequence(id="s", sequence="ATG" + "AAA" * 40 + "TAA")
        analyzer = ORFAnalyzer()
        result = analyzer.analyze(seq, min_length=1)
        summary = analyzer.summarize(result)
        assert summary["totalOrfs"] >= 1
        assert summary["longestLength"] > 0
        assert summary["avgLength"] > 0

    def test_summarize_empty(self):
        summary = ORFAnalyzer().summarize(
            ORFAnalyzer().analyze(Sequence(id="s", sequence="AAA"), min_length=10)
        )
        assert summary["totalOrfs"] == 0
        assert summary["longestLength"] == 0

    def test_negative_frame_path(self):
        seq = Sequence(id="s", sequence="TTA" + "TTT" * 30 + "CAT"[::-1])
        result = ORFAnalyzer().analyze(seq, min_length=1, frames=[-1])
        assert result.searchedFrames == [-1]


# ---------------------------------------------------------------------------
# ParserFactory: all branches
# ---------------------------------------------------------------------------


class TestParserFactory:
    def test_get_parser_by_enum(self):
        parser = ParserFactory.get_parser(TraceFormat.FASTA)
        assert parser.format == TraceFormat.FASTA

    def test_get_parser_by_string(self):
        parser = ParserFactory.get_parser("fasta")
        assert parser.format == TraceFormat.FASTA

    def test_get_parser_unknown_raises(self):
        with pytest.raises(ValueError):
            ParserFactory.get_parser("nonsense")

    def test_get_parser_for_file_fasta(self):
        parser = ParserFactory.get_parser_for_file("x.fasta")
        assert parser.format == TraceFormat.FASTA

    def test_get_parser_for_file_unknown_raises(self):
        with pytest.raises(ValueError, match="No parser"):
            ParserFactory.get_parser_for_file("x.unknown")

    def test_register_custom_parser(self):
        class _Dummy(BaseParser):
            @property
            def format(self) -> TraceFormat:
                return TraceFormat.AB1

            @property
            def extensions(self) -> list[str]:
                return ["xx"]

            def parse(self, data, trace_id):
                return None

        original = ParserFactory._parsers.copy()
        try:
            ParserFactory.register(_Dummy)
            assert ParserFactory._parsers[TraceFormat.AB1] is _Dummy
        finally:
            ParserFactory._parsers = original

    def test_parse_file_missing_raises(self, tmp_path):
        parser = ParserFactory.get_parser(TraceFormat.FASTA)
        with pytest.raises(FileNotFoundError):
            parser.parse_file(tmp_path / "nope.fasta", "t")

    def test_parse_file_reads_disk(self, tmp_path):
        parser = ParserFactory.get_parser(TraceFormat.FASTA)
        f = tmp_path / "x.fasta"
        f.write_text(">a\nACGT\n")
        parsed = parser.parse_file(f, "tid")
        assert parsed.sequence.sequence == "ACGT"

    def test_can_parse_extension_match(self):
        parser = ParserFactory.get_parser(TraceFormat.FASTA)
        assert parser.can_parse("foo.fasta") is True
        assert parser.can_parse("foo.unknown") is False


# ---------------------------------------------------------------------------
# HTTPStorageProvider: streaming + metadata + read-only operations
# ---------------------------------------------------------------------------


def _async_client(method: str, response_mock):
    client = AsyncMock()
    getattr(client, method).return_value = response_mock
    client.__aenter__.return_value = client
    client.__aexit__.return_value = None
    return client


def _stream_client(response_mock):
    client = AsyncMock()
    stream_ctx = AsyncMock()
    stream_ctx.__aenter__.return_value = response_mock
    stream_ctx.__aexit__.return_value = None
    client.stream = MagicMock(return_value=stream_ctx)
    client.__aenter__.return_value = client
    client.__aexit__.return_value = None
    return client


class TestHTTPStreamingAndMetadata:
    @pytest.mark.asyncio
    async def test_get_stream_yields_chunks(self):
        provider = HTTPStorageProvider()
        response = MagicMock()
        response.raise_for_status = MagicMock()

        async def aiter_bytes(chunk_size):
            yield b"hello "
            yield b"world"

        response.aiter_bytes = aiter_bytes

        with patch("httpx.AsyncClient", return_value=_stream_client(response)):
            chunks = [c async for c in provider.get_stream("https://x/y")]
        assert b"".join(chunks) == b"hello world"

    @pytest.mark.asyncio
    async def test_get_stream_404_raises_file_not_found(self):
        provider = HTTPStorageProvider()
        err_response = MagicMock(status_code=404)
        response = MagicMock()
        response.raise_for_status.side_effect = httpx.HTTPStatusError(
            "404", request=MagicMock(), response=err_response
        )
        response.aiter_bytes = AsyncMock()

        with patch("httpx.AsyncClient", return_value=_stream_client(response)):
            with pytest.raises(FileNotFoundError):
                async for _ in provider.get_stream("https://x/y"):
                    pass

    @pytest.mark.asyncio
    async def test_get_stream_5xx_raises_storage_error(self):
        provider = HTTPStorageProvider()
        err_response = MagicMock(status_code=503)
        response = MagicMock()
        response.raise_for_status.side_effect = httpx.HTTPStatusError(
            "503", request=MagicMock(), response=err_response
        )

        with patch("httpx.AsyncClient", return_value=_stream_client(response)):
            with pytest.raises(StorageError):
                async for _ in provider.get_stream("https://x/y"):
                    pass

    @pytest.mark.asyncio
    async def test_get_stream_request_error_raises_storage_error(self):
        provider = HTTPStorageProvider()
        response = MagicMock()
        response.raise_for_status.side_effect = httpx.RequestError("boom")

        with patch("httpx.AsyncClient", return_value=_stream_client(response)):
            with pytest.raises(StorageError, match="Request failed"):
                async for _ in provider.get_stream("https://x/y"):
                    pass

    @pytest.mark.asyncio
    async def test_get_metadata_returns_headers(self):
        provider = HTTPStorageProvider()
        response = MagicMock()
        response.raise_for_status = MagicMock()
        response.headers = {
            "content-type": "text/plain",
            "content-length": "42",
            "last-modified": "yesterday",
            "etag": "abc",
        }

        with patch("httpx.AsyncClient", return_value=_async_client("head", response)):
            md = await provider.get_metadata("https://x/y")

        assert md["content_type"] == "text/plain"
        assert md["content_length"] == "42"
        assert md["etag"] == "abc"

    @pytest.mark.asyncio
    async def test_get_metadata_failuREDACTED(self):
        provider = HTTPStorageProvider()
        response = MagicMock()
        response.raise_for_status.side_effect = RuntimeError("boom")

        with patch("httpx.AsyncClient", return_value=_async_client("head", response)):
            with pytest.raises(StorageError, match="Failed to get metadata"):
                await provider.get_metadata("https://x/y")

    @pytest.mark.asyncio
    async def test_put_is_read_only(self):
        with pytest.raises(StorageError, match="read-only"):
            await HTTPStorageProvider().put("https://x/y", b"data")

    @pytest.mark.asyncio
    async def test_delete_is_read_only(self):
        with pytest.raises(StorageError, match="read-only"):
            await HTTPStorageProvider().delete("https://x/y")

    @pytest.mark.asyncio
    async def test_health_check_always_true(self):
        assert await HTTPStorageProvider().health_check() is True

    def test_validate_url_missing_host(self):
        with pytest.raises(StorageError, match="missing host"):
            HTTPStorageProvider()._validate_url("https://")


# ---------------------------------------------------------------------------
# ApplicationLifecycle: run loop, _run_api, _wait_for_shutdown, signal handlers
# ---------------------------------------------------------------------------


def _lifecycle_components():
    components = MagicMock()
    components.settings = MagicMock(
        storage_provider="local",
        redis_url="redis://x",
        redis_consumer_name="c",
        api_host="0.0.0.0",
        api_port=8000,
        log_level="INFO",
    )
    components.redis = AsyncMock()
    components.redis.ping = AsyncMock()
    components.redis.close = AsyncMock()
    components.publisher = AsyncMock()
    components.publisher.publish = AsyncMock()
    components.api = MagicMock()
    components.api.set_redis_health = MagicMock()
    components.api.app = MagicMock()
    components.workers = {}
    return components


class TestLifecycleExtended:
    @pytest.mark.asyncio
    async def test_run_orchestrates_startup_wait_shutdown(self):
        from src.lifecycle import ApplicationLifecycle

        lc = ApplicationLifecycle(_lifecycle_components())

        with (
            patch.object(lc, "_setup_signal_handlers"),
            patch.object(lc, "startup", new=AsyncMock()) as start,
            patch.object(lc, "_wait_for_shutdown", new=AsyncMock()) as wait,
            patch.object(lc, "shutdown", new=AsyncMock()) as stop,
        ):
            await lc.run()

        start.assert_awaited_once()
        wait.assert_awaited_once()
        stop.assert_awaited_once()

    @pytest.mark.asyncio
    async def test_run_handles_cancelled_error(self):
        from src.lifecycle import ApplicationLifecycle

        lc = ApplicationLifecycle(_lifecycle_components())

        with (
            patch.object(lc, "_setup_signal_handlers"),
            patch.object(lc, "startup", new=AsyncMock()),
            patch.object(
                lc, "_wait_for_shutdown", new=AsyncMock(side_effect=asyncio.CancelledError())
            ),
            patch.object(lc, "shutdown", new=AsyncMock()) as stop,
        ):
            await lc.run()

        stop.assert_awaited_once()

    @pytest.mark.asyncio
    async def test_run_api_uses_uvicorn(self):
        from src.lifecycle import ApplicationLifecycle

        lc = ApplicationLifecycle(_lifecycle_components())
        server = MagicMock()
        server.serve = AsyncMock()

        with (
            patch("src.lifecycle.uvicorn.Config", return_value=MagicMock()),
            patch("src.lifecycle.uvicorn.Server", return_value=server),
        ):
            await lc._run_api()

        server.serve.assert_awaited_once()

    @pytest.mark.asyncio
    async def test_wait_for_shutdown_windows_path(self):
        from src.lifecycle import ApplicationLifecycle

        lc = ApplicationLifecycle(_lifecycle_components())

        async def finished():
            return None

        lc._worker_tasks = [asyncio.create_task(finished())]
        lc._api_task = asyncio.create_task(finished())

        with patch("src.lifecycle.sys.platform", "win32"):
            await lc._wait_for_shutdown()

    @pytest.mark.asyncio
    async def test_wait_for_shutdown_unix_cancels_pending(self):
        from src.lifecycle import ApplicationLifecycle

        lc = ApplicationLifecycle(_lifecycle_components())

        async def quick():
            return None

        lc._worker_tasks = [asyncio.create_task(quick())]
        lc._api_task = asyncio.create_task(asyncio.sleep(10))

        with patch("src.lifecycle.sys.platform", "linux"):
            await lc._wait_for_shutdown()

        try:
            await lc._api_task
        except asyncio.CancelledError:
            pass
        assert lc._api_task.cancelled() or lc._api_task.done()

    @pytest.mark.asyncio
    async def test_setup_signal_handlers_windows(self):
        from src.lifecycle import ApplicationLifecycle

        lc = ApplicationLifecycle(_lifecycle_components())

        with (
            patch("src.lifecycle.sys.platform", "win32"),
            patch("src.lifecycle.signal.signal") as sig,
        ):
            lc._setup_signal_handlers()

        sig.assert_called_once()

    @pytest.mark.asyncio
    async def test_setup_signal_handlers_unix(self):
        from src.lifecycle import ApplicationLifecycle

        if sys.platform == "win32":
            pytest.skip("unix-only path")

        lc = ApplicationLifecycle(_lifecycle_components())
        with patch("src.lifecycle.sys.platform", "linux"):
            lc._setup_signal_handlers()

        assert lc._shutdown_event is not None

    @pytest.mark.asyncio
    async def test_shutdown_publish_error_is_swallowed(self):
        from src.lifecycle import ApplicationLifecycle

        components = _lifecycle_components()
        worker = MagicMock()
        worker.stop = AsyncMock()
        worker.metrics = MagicMock(jobsProcessed=3)
        components.workers = {"w1": worker}
        components.publisher.publish.side_effect = RuntimeError("redis dead")

        lc = ApplicationLifecycle(components)

        await lc.shutdown()
        components.redis.close.assert_awaited_once()
