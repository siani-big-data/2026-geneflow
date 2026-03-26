"""Tests for StorageMounter."""

import json
import pytest
from unittest.mock import AsyncMock, MagicMock, patch

from src.mounters.storage import StorageMounter, TraceManifest, ChunkMetadata


@pytest.fixture
def mock_connection():
    """Create a mock storage connection."""
    conn = MagicMock()
    conn.connect = AsyncMock()
    conn.close = AsyncMock()
    conn.ensuREDACTED = AsyncMock()
    conn.put_object = AsyncMock(return_value="etag-123")
    conn.get_object = AsyncMock()
    conn.delete_object = AsyncMock()
    conn.list_objects = AsyncMock(return_value=[])
    conn.object_exists = AsyncMock(return_value=True)
    conn.health_check = AsyncMock(return_value=True)
    return conn


@pytest.fixture
def storage_mounter(mock_connection):
    """Create a StorageMounter with mocked connection."""
    with patch("src.mounters.storage.mounter.StorageConnection") as MockConn:
        MockConn.return_value = mock_connection
        mounter = StorageMounter(
            endpoint_url="http://minio:9000",
            access_key="test-key",
            secret_key="test-secret",
            bucket="test-bucket",
            chunk_size=100,
        )
        mounter._connection = mock_connection
        return mounter


class TestStorageMounter:
    """Tests for StorageMounter class."""

    async def test_start(self, storage_mounter, mock_connection):
        """Test starting the mounter."""
        await storage_mounter.start()

        mock_connection.ensuREDACTED.assert_called_once_with("test-bucket")
        assert storage_mounter._running is True

    async def test_stop(self, storage_mounter):
        """Test stopping the mounter."""
        storage_mounter._running = True

        await storage_mounter.stop()

        assert storage_mounter._running is False

    async def test_health_check(self, storage_mounter, mock_connection):
        """Test health check."""
        result = await storage_mounter.health_check()

        assert result is True
        mock_connection.health_check.assert_called_once()

    async def test_handle_trace_uploaded(self, storage_mounter, mock_connection):
        """Test handling TraceUploaded event."""
        event = {
            "type": "TraceUploaded",
            "payload": {
                "id": "trace-123",
                "original_file": b"binary data here",
                "original_extension": "ab1",
                "parsed_data": {
                    "filename": "sample.ab1",
                    "format": "AB1",
                    "sequence": "ATCGATCG" * 20,  # 160 bases
                    "quality_scores": [30] * 160,
                },
            },
        }

        await storage_mounter.handle_event(event)

        # Should store original file
        calls = mock_connection.put_object.call_args_list
        assert len(calls) >= 1

        # Check original file was stored
        original_call = [c for c in calls if "original.ab1" in str(c)]
        assert len(original_call) == 1

    async def test_handle_trace_deleted(self, storage_mounter, mock_connection):
        """Test handling TraceDeleted event."""
        mock_connection.list_objects.return_value = [
            {"key": "traces/trace-123/original.ab1"},
            {"key": "traces/trace-123/manifest.json"},
            {"key": "traces/trace-123/chunks/0000.json"},
        ]

        event = {
            "type": "TraceDeleted",
            "payload": {"id": "trace-123"},
        }

        await storage_mounter.handle_event(event)

        # Should list and delete all objects
        mock_connection.list_objects.assert_called_once()
        assert mock_connection.delete_object.call_count == 3

    async def test_stoREDACTED(self, storage_mounter, mock_connection):
        """Test storing chunked trace data."""
        parsed_data = {
            "filename": "test.fasta",
            "format": "FASTA",
            "sequence": "A" * 250,  # Will create 3 chunks with chunk_size=100
        }

        manifest = await storage_mounter._stoREDACTED("trace-456", parsed_data)

        assert manifest.total_bases == 250
        assert manifest.chunk_count == 3

        # Should have stored 3 chunks + 1 manifest
        assert mock_connection.put_object.call_count == 4

    async def test_get_manifest(self, storage_mounter, mock_connection):
        """Test getting a manifest."""
        manifest_data = {
            "trace_id": "trace-789",
            "original_filename": "sample.ab1",
            "format": "AB1",
            "total_bases": 100,
            "chunk_size": 100,
            "chunk_count": 1,
            "has_chromatogram": False,
            "has_quality_scores": True,
            "created_at": "2026-03-26T10:00:00Z",
            "chunks": [
                {"index": 0, "start_position": 0, "end_position": 100, "base_count": 100, "filename": "chunk_0000.json"}
            ],
        }
        mock_connection.get_object.return_value = json.dumps(manifest_data).encode()

        manifest = await storage_mounter.get_manifest("trace-789")

        assert manifest is not None
        assert manifest.trace_id == "trace-789"
        assert manifest.total_bases == 100

    async def test_get_manifest_not_found(self, storage_mounter, mock_connection):
        """Test getting a non-existent manifest."""
        mock_connection.object_exists.return_value = False

        manifest = await storage_mounter.get_manifest("nonexistent")

        assert manifest is None

    async def test_get_chunk(self, storage_mounter, mock_connection):
        """Test getting a specific chunk."""
        chunk_data = {
            "index": 0,
            "start_position": 0,
            "end_position": 100,
            "bases": "A" * 100,
            "quality_scores": [30] * 100,
        }
        mock_connection.get_object.return_value = json.dumps(chunk_data).encode()

        chunk = await storage_mounter.get_chunk("trace-123", 0)

        assert chunk is not None
        assert chunk["index"] == 0
        assert len(chunk["bases"]) == 100

    async def test_get_original(self, storage_mounter, mock_connection):
        """Test getting the original file."""
        mock_connection.list_objects.return_value = [
            {"key": "traces/trace-123/original.ab1"}
        ]
        mock_connection.get_object.return_value = b"binary file data"

        result = await storage_mounter.get_original("trace-123")

        assert result is not None
        data, extension = result
        assert data == b"binary file data"
        assert extension == "ab1"

    async def test_get_original_not_found(self, storage_mounter, mock_connection):
        """Test getting non-existent original file."""
        mock_connection.list_objects.return_value = []

        result = await storage_mounter.get_original("nonexistent")

        assert result is None

    async def test_rebuild(self, storage_mounter, mock_connection):
        """Test rebuilding storage."""
        mock_connection.list_objects.return_value = [
            {"key": "traces/trace-1/original.ab1"},
            {"key": "traces/trace-2/manifest.json"},
        ]

        await storage_mounter.rebuild()

        # Should delete all objects
        assert mock_connection.delete_object.call_count == 2

    def test_categories(self, storage_mounter):
        """Test mounter categories."""
        assert storage_mounter.categories == ["traces"]
        assert storage_mounter.handles_category("traces") is True
        assert storage_mounter.handles_category("users") is False
