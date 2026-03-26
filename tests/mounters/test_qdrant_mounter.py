"""Tests for QdrantMounter."""

import pytest
from unittest.mock import AsyncMock, MagicMock, patch

from src.mounters.qdrant import (
    QdrantMounter,
    QdrantConnection,
    SEQUENCES_COLLECTION,
    ANNOTATIONS_COLLECTION,
    TRACES_COLLECTION,
)


@pytest.fixture
def mock_connection():
    """Create a mock Qdrant connection."""
    conn = MagicMock(spec=QdrantConnection)
    conn.connect = AsyncMock()
    conn.close = AsyncMock()
    conn.ensuREDACTED = AsyncMock()
    conn.upsert_points = AsyncMock()
    conn.delete_points = AsyncMock()
    conn.delete_by_filter = AsyncMock()
    conn.health_check = AsyncMock(return_value=True)
    conn.drop_collection = AsyncMock()
    return conn


@pytest.fixture
def qdrant_mounter(mock_connection):
    """Create a QdrantMounter with mocked connection."""
    with patch("src.mounters.qdrant.mounter.QdrantConnection") as MockConn:
        MockConn.return_value = mock_connection
        mounter = QdrantMounter(
            qdrant_url="http://localhost:6333",
            qdrant_api_key="test-key",
        )
        mounter._connection = mock_connection
        return mounter


class TestQdrantMounter:
    """Tests for QdrantMounter class."""

    async def test_start_creates_collections(self, qdrant_mounter, mock_connection):
        """Test that starting the mounter creates all collections."""
        await qdrant_mounter.start()

        mock_connection.connect.assert_called_once()
        assert mock_connection.ensuREDACTED.call_count == 3

        # Verify all collections were created with correct params
        calls = mock_connection.ensuREDACTED.call_args_list
        collection_names = [call.kwargs["name"] for call in calls]

        assert SEQUENCES_COLLECTION.name in collection_names
        assert ANNOTATIONS_COLLECTION.name in collection_names
        assert TRACES_COLLECTION.name in collection_names

        assert qdrant_mounter._running is True

    async def test_stop(self, qdrant_mounter, mock_connection):
        """Test stopping the mounter."""
        qdrant_mounter._running = True

        await qdrant_mounter.stop()

        mock_connection.close.assert_called_once()
        assert qdrant_mounter._running is False

    async def test_handle_sequence_embedded(self, qdrant_mounter, mock_connection):
        """Test handling AISequenceEmbedded event."""
        event = {
            "type": "AISequenceEmbedded",
            "category": "ai",
            "payload": {
                "trace_id": "trace-123",
                "embedding": [0.1] * 768,
                "study_id": "study-456",
                "owner_id": "user-789",
                "sequence_length": 1500,
                "format": "ab1",
            },
        }

        await qdrant_mounter.handle_event(event)

        mock_connection.upsert_points.assert_called_once()
        call_args = mock_connection.upsert_points.call_args
        assert call_args.kwargs["collection"] == SEQUENCES_COLLECTION.name
        assert len(call_args.kwargs["points"]) == 1

        point = call_args.kwargs["points"][0]
        assert point.id == "trace-123"
        assert len(point.vector) == 768

    async def test_handle_annotation_embedded(self, qdrant_mounter, mock_connection):
        """Test handling AIAnnotationEmbedded event."""
        event = {
            "type": "AIAnnotationEmbedded",
            "category": "ai",
            "payload": {
                "annotation_id": "ann-123",
                "embedding": [0.2] * 1536,
                "trace_id": "trace-456",
                "study_id": "study-789",
                "owner_id": "user-001",
                "text_content": "This is a test annotation",
                "annotation_type": "note",
            },
        }

        await qdrant_mounter.handle_event(event)

        mock_connection.upsert_points.assert_called_once()
        call_args = mock_connection.upsert_points.call_args
        assert call_args.kwargs["collection"] == ANNOTATIONS_COLLECTION.name

        point = call_args.kwargs["points"][0]
        assert point.id == "ann-123"
        assert len(point.vector) == 1536
        assert point.payload["text_content"] == "This is a test annotation"

    async def test_handle_trace_embedded(self, qdrant_mounter, mock_connection):
        """Test handling AITraceEmbedded event."""
        event = {
            "type": "AITraceEmbedded",
            "category": "ai",
            "payload": {
                "trace_id": "trace-999",
                "embedding": [0.3] * 256,
                "study_id": "study-111",
                "owner_id": "user-222",
                "metadata": {"key": "value"},
            },
        }

        await qdrant_mounter.handle_event(event)

        mock_connection.upsert_points.assert_called_once()
        call_args = mock_connection.upsert_points.call_args
        assert call_args.kwargs["collection"] == TRACES_COLLECTION.name

        point = call_args.kwargs["points"][0]
        assert point.id == "trace-999"
        assert len(point.vector) == 256

    async def test_handle_trace_deleted_cascades(self, qdrant_mounter, mock_connection):
        """Test that TraceDeleted cascades to all collections."""
        event = {
            "type": "TraceDeleted",
            "category": "traces",
            "payload": {"id": "trace-to-delete"},
        }

        await qdrant_mounter.handle_event(event)

        # Should delete from sequences (by id)
        # Should delete from traces (by id)
        # Should delete from annotations (by filter on trace_id)
        assert mock_connection.delete_points.call_count == 2
        mock_connection.delete_by_filter.assert_called_once()

        filter_call = mock_connection.delete_by_filter.call_args
        assert filter_call.kwargs["collection"] == ANNOTATIONS_COLLECTION.name
        assert filter_call.kwargs["field"] == "trace_id"
        assert filter_call.kwargs["value"] == "trace-to-delete"

    async def test_handle_annotation_deleted(self, qdrant_mounter, mock_connection):
        """Test handling AnnotationDeleted event."""
        event = {
            "type": "AnnotationDeleted",
            "category": "traces",
            "payload": {"id": "ann-to-delete"},
        }

        await qdrant_mounter.handle_event(event)

        mock_connection.delete_points.assert_called_once_with(
            collection=ANNOTATIONS_COLLECTION.name,
            point_ids=["ann-to-delete"],
        )

    async def test_handle_embedding_deleted(self, qdrant_mounter, mock_connection):
        """Test handling AIEmbeddingDeleted event."""
        event = {
            "type": "AIEmbeddingDeleted",
            "category": "ai",
            "payload": {
                "id": "point-to-delete",
                "collection": "geneflow_sequences",
            },
        }

        await qdrant_mounter.handle_event(event)

        mock_connection.delete_points.assert_called_once_with(
            collection="geneflow_sequences",
            point_ids=["point-to-delete"],
        )

    async def test_health_check(self, qdrant_mounter, mock_connection):
        """Test health check."""
        result = await qdrant_mounter.health_check()

        assert result is True
        mock_connection.health_check.assert_called_once()

    async def test_health_check_unhealthy(self, qdrant_mounter, mock_connection):
        """Test health check when unhealthy."""
        mock_connection.health_check.return_value = False

        result = await qdrant_mounter.health_check()

        assert result is False

    async def test_rebuild_drops_and_recreates(self, qdrant_mounter, mock_connection):
        """Test that rebuild drops and recreates all collections."""
        await qdrant_mounter.rebuild()

        # Should drop all 3 collections
        assert mock_connection.drop_collection.call_count == 3

        # Should recreate all 3 collections
        assert mock_connection.ensuREDACTED.call_count == 3

        # Metrics should be reset
        assert qdrant_mounter._metrics["events_processed"] == 0

    async def test_categories(self, qdrant_mounter):
        """Test mounter categories."""
        assert qdrant_mounter.categories == ["ai", "traces"]
        assert qdrant_mounter.handles_category("ai") is True
        assert qdrant_mounter.handles_category("traces") is True
        assert qdrant_mounter.handles_category("users") is False

    async def test_invalid_event_ignored(self, qdrant_mounter, mock_connection):
        """Test that invalid events are ignored."""
        event = {
            "type": "UnknownEvent",
            "category": "ai",
            "payload": {},
        }

        await qdrant_mounter.handle_event(event)

        mock_connection.upsert_points.assert_not_called()
        mock_connection.delete_points.assert_not_called()

    async def test_missing_embedding_skipped(self, qdrant_mounter, mock_connection):
        """Test that events with missing embeddings are skipped."""
        event = {
            "type": "AISequenceEmbedded",
            "category": "ai",
            "payload": {
                "trace_id": "trace-123",
                # Missing embedding
                "study_id": "study-456",
            },
        }

        await qdrant_mounter.handle_event(event)

        mock_connection.upsert_points.assert_not_called()


class TestQdrantConnection:
    """Tests for QdrantConnection class."""

    async def test_connect(self):
        """Test connecting to Qdrant."""
        with patch("src.mounters.qdrant.connection.AsyncQdrantClient") as MockClient:
            mock_client = AsyncMock()
            MockClient.return_value = mock_client

            conn = QdrantConnection(url="http://localhost:6333", api_key="test")
            await conn.connect()

            MockClient.assert_called_once_with(
                url="http://localhost:6333",
                api_key="test",
            )

    async def test_close(self):
        """Test closing Qdrant connection."""
        with patch("src.mounters.qdrant.connection.AsyncQdrantClient") as MockClient:
            mock_client = AsyncMock()
            MockClient.return_value = mock_client

            conn = QdrantConnection(url="http://localhost:6333")
            await conn.connect()
            await conn.close()

            mock_client.close.assert_called_once()

    async def test_ensuREDACTED(self):
        """Test creating a new collection."""
        with patch("src.mounters.qdrant.connection.AsyncQdrantClient") as MockClient:
            mock_client = AsyncMock()
            mock_collections = MagicMock()
            mock_collections.collections = []
            mock_client.get_collections.return_value = mock_collections
            MockClient.return_value = mock_client

            conn = QdrantConnection(url="http://localhost:6333")
            await conn.connect()
            await conn.ensuREDACTED("test_collection", 768)

            mock_client.create_collection.assert_called_once()

    async def test_ensuREDACTED(self):
        """Test that existing collection is not recreated."""
        with patch("src.mounters.qdrant.connection.AsyncQdrantClient") as MockClient:
            mock_client = AsyncMock()
            mock_collection = MagicMock()
            mock_collection.name = "existing_collection"
            mock_collections = MagicMock()
            mock_collections.collections = [mock_collection]
            mock_client.get_collections.return_value = mock_collections
            MockClient.return_value = mock_client

            conn = QdrantConnection(url="http://localhost:6333")
            await conn.connect()
            await conn.ensuREDACTED("existing_collection", 768)

            mock_client.create_collection.assert_not_called()
