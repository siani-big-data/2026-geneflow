"""Tests for StorageMounter."""

import json
from unittest.mock import AsyncMock, MagicMock, patch

import pytest

from src.mounters.storage import StorageMounter
from src.mounters.storage.chunking import TraceManifest


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
    conn.get_presigned_url = AsyncMock(return_value="https://example.com/presigned")
    return conn


@pytest.fixture
def mock_trace_handler():
    """Create a mock trace handler."""
    handler = MagicMock()
    handler.handle_uploaded = AsyncMock()
    handler.handle_deleted = AsyncMock()
    handler.get_manifest = AsyncMock()
    handler.get_chunk = AsyncMock()
    handler.get_original = AsyncMock()
    return handler


@pytest.fixture
def mock_photo_handler():
    """Create a mock photo handler."""
    handler = MagicMock()
    handler.handle_uploaded = AsyncMock()
    handler.handle_deleted = AsyncMock()
    handler.get_photo = AsyncMock()
    handler.get_thumbnail = AsyncMock()
    handler.get_photo_url = AsyncMock()
    handler.get_thumbnail_url = AsyncMock()
    return handler


@pytest.fixture
def storage_mounter(mock_connection, mock_trace_handler, mock_photo_handler):
    """Create a StorageMounter with mocked dependencies."""
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
        mounter._trace_handler = mock_trace_handler
        mounter._photo_handler = mock_photo_handler
        mounter._running = True
        return mounter


class TestStorageMounter:
    """Tests for StorageMounter class."""

    async def test_start(self, mock_connection):
        """Test starting the mounter."""
        with patch("src.mounters.storage.mounter.StorageConnection") as MockConn:
            MockConn.return_value = mock_connection
            mounter = StorageMounter(
                endpoint_url="http://minio:9000",
                access_key="test-key",
                secret_key="test-secret",
                bucket="test-bucket",
            )
            mounter._connection = mock_connection

            await mounter.start()

            mock_connection.ensuREDACTED.assert_called_once_with("test-bucket")
            assert mounter._running is True
            assert mounter._trace_handler is not None
            assert mounter._photo_handler is not None

    async def test_stop(self, storage_mounter, mock_connection):
        """Test stopping the mounter."""
        await storage_mounter.stop()

        mock_connection.close.assert_called_once()
        assert storage_mounter._running is False

    async def test_health_check(self, storage_mounter, mock_connection):
        """Test health check."""
        result = await storage_mounter.health_check()

        assert result is True
        mock_connection.health_check.assert_called_once()

    async def test_handle_trace_uploaded(self, storage_mounter, mock_trace_handler):
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
                    "sequence": "ATCGATCG" * 20,
                    "quality_scores": [30] * 160,
                },
            },
        }

        await storage_mounter.handle_event(event)

        mock_trace_handler.handle_uploaded.assert_called_once_with(event["payload"])

    async def test_handle_trace_deleted(self, storage_mounter, mock_trace_handler):
        """Test handling TraceDeleted event."""
        event = {
            "type": "TraceDeleted",
            "payload": {"id": "trace-123"},
        }

        await storage_mounter.handle_event(event)

        mock_trace_handler.handle_deleted.assert_called_once_with(event["payload"])

    async def test_handle_profile_photo_uploaded(self, storage_mounter, mock_photo_handler):
        """Test handling ProfilePhotoUploadedEvent."""
        event = {
            "type": "ProfilePhotoUploadedEvent",
            "payload": {
                "profileId": "profile-123",
                "photoData": b"photo data",
            },
        }

        await storage_mounter.handle_event(event)

        mock_photo_handler.handle_uploaded.assert_called_once_with(event["payload"])

    async def test_handle_profile_photo_deleted(self, storage_mounter, mock_photo_handler):
        """Test handling ProfilePhotoDeletedEvent."""
        event = {
            "type": "ProfilePhotoDeletedEvent",
            "payload": {"profileId": "profile-123"},
        }

        await storage_mounter.handle_event(event)

        mock_photo_handler.handle_deleted.assert_called_once_with(event["payload"])

    async def test_get_manifest(self, storage_mounter, mock_trace_handler):
        """Test getting a manifest."""
        manifest = TraceManifest(
            trace_id="trace-789",
            original_filename="sample.ab1",
            format="AB1",
            total_bases=100,
            chunk_size=100,
            chunk_count=1,
            has_chromatogram=False,
            has_quality_scores=True,
            created_at="2026-03-26T10:00:00Z",
            chunks=[],
        )
        mock_trace_handler.get_manifest.return_value = manifest

        result = await storage_mounter.get_manifest("trace-789")

        assert result is not None
        assert result.trace_id == "trace-789"
        assert result.total_bases == 100
        mock_trace_handler.get_manifest.assert_called_once_with("trace-789")

    async def test_get_manifest_not_found(self, storage_mounter, mock_trace_handler):
        """Test getting a non-existent manifest."""
        mock_trace_handler.get_manifest.return_value = None

        manifest = await storage_mounter.get_manifest("nonexistent")

        assert manifest is None
        mock_trace_handler.get_manifest.assert_called_once_with("nonexistent")

    async def test_get_chunk(self, storage_mounter, mock_trace_handler):
        """Test getting a specific chunk."""
        chunk_data = {
            "index": 0,
            "start_position": 0,
            "end_position": 100,
            "bases": "A" * 100,
            "quality_scores": [30] * 100,
        }
        mock_trace_handler.get_chunk.return_value = chunk_data

        chunk = await storage_mounter.get_chunk("trace-123", 0)

        assert chunk is not None
        assert chunk["index"] == 0
        assert len(chunk["bases"]) == 100
        mock_trace_handler.get_chunk.assert_called_once_with("trace-123", 0)

    async def test_get_original(self, storage_mounter, mock_trace_handler):
        """Test getting the original file."""
        mock_trace_handler.get_original.return_value = (b"binary file data", "ab1")

        result = await storage_mounter.get_original("trace-123")

        assert result is not None
        data, extension = result
        assert data == b"binary file data"
        assert extension == "ab1"
        mock_trace_handler.get_original.assert_called_once_with("trace-123")

    async def test_get_original_not_found(self, storage_mounter, mock_trace_handler):
        """Test getting non-existent original file."""
        mock_trace_handler.get_original.return_value = None

        result = await storage_mounter.get_original("nonexistent")

        assert result is None
        mock_trace_handler.get_original.assert_called_once_with("nonexistent")

    async def test_get_profile_photo(self, storage_mounter, mock_photo_handler):
        """Test getting a profile photo."""
        mock_photo_handler.get_photo.return_value = (b"photo data", "jpg")

        result = await storage_mounter.get_profile_photo("profile-123")

        assert result is not None
        data, extension = result
        assert data == b"photo data"
        assert extension == "jpg"
        mock_photo_handler.get_photo.assert_called_once_with("profile-123")

    async def test_get_profile_thumbnail(self, storage_mounter, mock_photo_handler):
        """Test getting a profile thumbnail."""
        mock_photo_handler.get_thumbnail.return_value = (b"thumbnail data", "jpg")

        result = await storage_mounter.get_profile_thumbnail("profile-123")

        assert result is not None
        data, extension = result
        assert data == b"thumbnail data"
        assert extension == "jpg"
        mock_photo_handler.get_thumbnail.assert_called_once_with("profile-123")

    async def test_rebuild(self, storage_mounter, mock_connection):
        """Test rebuilding storage."""

        def list_objects_side_effect(bucket, prefix):
            if prefix == "traces/":
                return [
                    {"key": "traces/trace-1/original.ab1"},
                    {"key": "traces/trace-2/manifest.json"},
                ]
            return []

        mock_connection.list_objects.side_effect = list_objects_side_effect

        await storage_mounter.rebuild()

        assert mock_connection.delete_object.call_count == 2

    def test_categories(self, storage_mounter):
        """Test mounter categories."""
        assert storage_mounter.categories == ["traces", "profiles"]
        assert storage_mounter.handles_category("traces") is True
        assert storage_mounter.handles_category("profiles") is True
        assert storage_mounter.handles_category("users") is False

    async def test_metrics_increment_on_event(self, storage_mounter, mock_trace_handler):
        """Test that metrics are incremented when processing events."""
        initial_count = storage_mounter._metrics["events_processed"]

        event = {
            "type": "TraceUploaded",
            "payload": {"id": "trace-123"},
        }

        await storage_mounter.handle_event(event)

        assert storage_mounter._metrics["events_processed"] == initial_count + 1
