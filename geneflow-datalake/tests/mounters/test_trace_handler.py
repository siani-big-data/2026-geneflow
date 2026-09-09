"""Tests for TraceHandler."""

import json
from unittest.mock import AsyncMock, MagicMock

import pytest

from src.mounters.storage.chunking import TraceManifest
from src.mounters.storage.handlers.trace_handler import TraceHandler


@pytest.fixture
def mock_connection():
    conn = MagicMock()
    conn.put_object = AsyncMock(return_value="etag-x")
    conn.get_object = AsyncMock()
    conn.delete_object = AsyncMock()
    conn.list_objects = AsyncMock(return_value=[])
    conn.object_exists = AsyncMock(return_value=True)
    return conn


@pytest.fixture
def handler(mock_connection):
    return TraceHandler(mock_connection, bucket="traces-bucket", chunk_size=10)


class TestTraceHandlerUploaded:
    async def test_handle_uploaded_stores_original_and_chunks(self, handler, mock_connection):
        payload = {
            "id": "trace-1",
            "original_file": b"AB1FILEBYTES",
            "original_extension": "ab1",
            "parsed_data": {
                "filename": "f.ab1",
                "format": "AB1",
                "sequence": "ATCGATCGAT",  # 10 bases, chunk_size=10 -> 1 chunk
                "quality_scores": [30] * 10,
            },
        }

        await handler.handle_uploaded(payload)

        # First call is the original file
        calls = mock_connection.put_object.call_args_list
        keys = [c.args[1] for c in calls]
        assert "traces/trace-1/original.ab1" in keys
        # Should also write a manifest and a chunk
        assert any(k.startswith("traces/trace-1/chunks/") for k in keys)
        assert "traces/trace-1/manifest.json" in keys

    async def test_handle_uploaded_default_extension(self, handler, mock_connection):
        payload = {
            "id": "trace-2",
            "original_file": b"",
            # no original_extension -> default ab1
            "parsed_data": {"sequence": "ATCG"},
        }
        await handler.handle_uploaded(payload)
        keys = [c.args[1] for c in mock_connection.put_object.call_args_list]
        assert "traces/trace-2/original.ab1" in keys

    async def test_handle_uploaded_with_chromatogram_flag(self, handler, mock_connection):
        payload = {
            "id": "trace-3",
            "original_file": b"X",
            "original_extension": "scf",
            "parsed_data": {
                "filename": "x.scf",
                "format": "SCF",
                "sequence": "AT",
                "chromatogram": {"A": [1, 2]},
                "quality_scores": [10, 20],
            },
        }
        await handler.handle_uploaded(payload)
        # find manifest call
        manifest_calls = [
            c
            for c in mock_connection.put_object.call_args_list
            if c.args[1].endswith("manifest.json")
        ]
        assert manifest_calls
        manifest_body = json.loads(manifest_calls[0].args[2].decode())
        assert manifest_body["has_chromatogram"] is True
        assert manifest_body["has_quality_scores"] is True


class TestTraceHandlerProcessed:
    async def test_handle_processed_missing_trace_id(self, handler, mock_connection):
        await handler.handle_processed({"parsedData": {"manifest": {}, "chunks": []}})
        mock_connection.put_object.assert_not_called()

    async def test_handle_processed_missing_parsed_data(self, handler, mock_connection):
        await handler.handle_processed({"traceId": "t1"})
        mock_connection.put_object.assert_not_called()

    async def test_handle_processed_no_chunks(self, handler, mock_connection):
        await handler.handle_processed(
            {"traceId": "t1", "parsedData": {"manifest": {"total_bases": 0}, "chunks": []}}
        )
        mock_connection.put_object.assert_not_called()

    async def test_handle_processed_writes_chunks_and_manifest(self, handler, mock_connection):
        payload = {
            "traceId": "trace-9",
            "parsedData": {
                "manifest": {"total_bases": 5, "chunk_count": 2},
                "chunks": [
                    {"index": 0, "bases": "ATC"},
                    {"index": 1, "bases": "GA"},
                ],
            },
        }
        await handler.handle_processed(payload)
        keys = [c.args[1] for c in mock_connection.put_object.call_args_list]
        assert "traces/trace-9/chunks/chunk_0000.json" in keys
        assert "traces/trace-9/chunks/chunk_0001.json" in keys
        assert "traces/trace-9/manifest.json" in keys
        # Check chunk content
        chunk_calls = [
            c for c in mock_connection.put_object.call_args_list if "chunks/chunk_0000" in c.args[1]
        ]
        chunk_body = json.loads(chunk_calls[0].args[2].decode())
        assert chunk_body["bases"] == "ATC"

    async def test_handle_processed_chunk_index_default_zero(self, handler, mock_connection):
        payload = {
            "traceId": "t",
            "parsedData": {
                "manifest": {},
                "chunks": [{"bases": "AT"}],  # no index -> 0
            },
        }
        await handler.handle_processed(payload)
        keys = [c.args[1] for c in mock_connection.put_object.call_args_list]
        assert "traces/t/chunks/chunk_0000.json" in keys


class TestTraceHandlerDeleted:
    async def test_handle_deleted_with_id(self, handler, mock_connection):
        mock_connection.list_objects.return_value = [
            {"key": "traces/d1/original.ab1"},
            {"key": "traces/d1/manifest.json"},
        ]
        await handler.handle_deleted({"id": "d1"})
        assert mock_connection.delete_object.call_count == 2

    async def test_handle_deleted_with_trace_id_key(self, handler, mock_connection):
        mock_connection.list_objects.return_value = [{"key": "traces/d2/x"}]
        await handler.handle_deleted({"traceId": "d2"})
        mock_connection.delete_object.assert_called_once_with("traces-bucket", "traces/d2/x")

    async def test_handle_deleted_no_objects(self, handler, mock_connection):
        mock_connection.list_objects.return_value = []
        await handler.handle_deleted({"id": "d3"})
        mock_connection.delete_object.assert_not_called()


class TestTraceHandlerGet:
    async def test_get_manifest_not_exists(self, handler, mock_connection):
        mock_connection.object_exists.return_value = False
        result = await handler.get_manifest("nope")
        assert result is None

    async def test_get_manifest_returns_manifest(self, handler, mock_connection):
        manifest_data = {
            "trace_id": "t1",
            "original_filename": "f.ab1",
            "format": "AB1",
            "total_bases": 4,
            "chunk_size": 10,
            "chunk_count": 1,
            "has_chromatogram": False,
            "has_quality_scores": False,
            "created_at": "2026-03-26T10:00:00Z",
            "chunks": [],
        }
        mock_connection.object_exists.return_value = True
        mock_connection.get_object.return_value = json.dumps(manifest_data).encode()
        result = await handler.get_manifest("t1")
        assert isinstance(result, TraceManifest)
        assert result.trace_id == "t1"

    async def test_get_chunk(self, handler, mock_connection):
        body = {"index": 2, "bases": "AT"}
        mock_connection.get_object.return_value = json.dumps(body).encode()
        result = await handler.get_chunk("t1", 2)
        assert result == body
        mock_connection.get_object.assert_called_once_with(
            "traces-bucket", "traces/t1/chunks/chunk_0002.json"
        )

    async def test_get_original_empty(self, handler, mock_connection):
        mock_connection.list_objects.return_value = []
        assert await handler.get_original("t-none") is None

    async def test_get_original_returns_data(self, handler, mock_connection):
        mock_connection.list_objects.return_value = [{"key": "traces/t/original.ab1"}]
        mock_connection.get_object.return_value = b"BINARY"
        data, ext = await handler.get_original("t")
        assert data == b"BINARY"
        assert ext == "ab1"


class TestTraceHandlerAnalysis:
    async def test_handle_analysis_result_missing_data(self, handler, mock_connection):
        await handler.handle_analysis_result({"traceId": None, "analysisType": "x"})
        mock_connection.put_object.assert_not_called()

    async def test_handle_analysis_result_missing_type(self, handler, mock_connection):
        await handler.handle_analysis_result({"traceId": "t", "analysisType": None})
        mock_connection.put_object.assert_not_called()

    async def test_handle_analysis_result_stores_with_metadata(self, handler, mock_connection):
        await handler.handle_analysis_result(
            {"traceId": "t1", "analysisType": "trimming", "resultData": {"score": 5}}
        )
        call = mock_connection.put_object.call_args
        assert call.args[1] == "traces/t1/analysis/trimming.json"
        body = json.loads(call.args[2].decode())
        assert body["trace_id"] == "t1"
        assert body["analysis_type"] == "trimming"
        assert body["score"] == 5
        assert "stored_at" in body

    async def test_handle_analysis_result_no_result_data(self, handler, mock_connection):
        await handler.handle_analysis_result({"traceId": "t1", "analysisType": "motif"})
        call = mock_connection.put_object.call_args
        assert call.args[1] == "traces/t1/analysis/motif.json"

    async def test_get_analysis_result_not_exists(self, handler, mock_connection):
        mock_connection.object_exists.return_value = False
        assert await handler.get_analysis_result("t", "trim") is None

    async def test_get_analysis_result_returns_data(self, handler, mock_connection):
        mock_connection.object_exists.return_value = True
        mock_connection.get_object.return_value = json.dumps({"trace_id": "t"}).encode()
        result = await handler.get_analysis_result("t", "trim")
        assert result == {"trace_id": "t"}

    async def test_list_analysis_results(self, handler, mock_connection):
        mock_connection.list_objects.return_value = [
            {"key": "traces/t/analysis/trim.json"},
            {"key": "traces/t/analysis/motif.json"},
            {"key": "traces/t/analysis/other.txt"},  # filtered out
        ]
        results = await handler.list_analysis_results("t")
        assert "trim" in results
        assert "motif" in results
        assert len(results) == 2

    async def test_list_analysis_results_empty(self, handler, mock_connection):
        mock_connection.list_objects.return_value = []
        assert await handler.list_analysis_results("t") == []
