"""Qdrant mounter for vector embedding storage."""

import structlog
from qdrant_client.models import PointStruct

from src.mounters.base import BaseMounter
from src.mounters.qdrant.connection import QdrantConnection
from src.mounters.qdrant.collections import (
    COLLECTIONS,
    SEQUENCES_COLLECTION,
    ANNOTATIONS_COLLECTION,
    TRACES_COLLECTION,
)

logger = structlog.get_logger()


class QdrantMounter(BaseMounter):
    """Mounter for projecting vector embeddings to Qdrant."""

    def __init__(self, qdrant_url: str, qdrant_api_key: str | None = None):
        super().__init__(
            name="qdrant",
            categories=["ai", "traces"],
        )
        self._connection = QdrantConnection(url=qdrant_url, api_key=qdrant_api_key)

    async def start(self) -> None:
        """Start the mounter and ensure collections exist."""
        await self._connection.connect()

        for collection in COLLECTIONS:
            await self._connection.ensuREDACTED(
                name=collection.name,
                vector_size=collection.vector_size,
                distance=collection.distance,
            )

        self._running = True
        logger.info("qdrant_mounter_started")

    async def stop(self) -> None:
        """Stop the mounter and close connection."""
        await self._connection.close()
        self._running = False
        logger.info("qdrant_mounter_stopped")

    async def handle_event(self, event: dict) -> None:
        """Handle an incoming event."""
        event_type = event.get("type", "")
        payload = event.get("payload", {})

        handlers = {
            "AISequenceEmbedded": self._handle_sequence_embedded,
            "AIAnnotationEmbedded": self._handle_annotation_embedded,
            "AITraceEmbedded": self._handle_trace_embedded,
            "AIEmbeddingDeleted": self._handle_embedding_deleted,
            "TraceDeleted": self._handle_trace_deleted,
            "AnnotationDeleted": self._handle_annotation_deleted,
        }

        handler = handlers.get(event_type)
        if handler:
            await handler(payload)
            self._metrics["events_processed"] += 1
        else:
            logger.debug("qdrant_event_ignored", event_type=event_type)

    async def _handle_sequence_embedded(self, payload: dict) -> None:
        """Handle AISequenceEmbedded event."""
        trace_id = payload.get("trace_id")
        embedding = payload.get("embedding", [])

        if not trace_id or not embedding:
            logger.warning("qdrant_invalid_sequence_event", payload=payload)
            return

        point = PointStruct(
            id=trace_id,
            vector=embedding,
            payload={
                "trace_id": trace_id,
                "study_id": payload.get("study_id"),
                "owner_id": payload.get("owner_id"),
                "sequence_length": payload.get("sequence_length"),
                "format": payload.get("format"),
            },
        )

        await self._connection.upsert_points(
            collection=SEQUENCES_COLLECTION.name,
            points=[point],
        )
        logger.info("qdrant_sequence_embedded", trace_id=trace_id)

    async def _handle_annotation_embedded(self, payload: dict) -> None:
        """Handle AIAnnotationEmbedded event."""
        annotation_id = payload.get("annotation_id")
        embedding = payload.get("embedding", [])

        if not annotation_id or not embedding:
            logger.warning("qdrant_invalid_annotation_event", payload=payload)
            return

        point = PointStruct(
            id=annotation_id,
            vector=embedding,
            payload={
                "annotation_id": annotation_id,
                "trace_id": payload.get("trace_id"),
                "study_id": payload.get("study_id"),
                "owner_id": payload.get("owner_id"),
                "text_content": payload.get("text_content"),
                "annotation_type": payload.get("annotation_type"),
            },
        )

        await self._connection.upsert_points(
            collection=ANNOTATIONS_COLLECTION.name,
            points=[point],
        )
        logger.info("qdrant_annotation_embedded", annotation_id=annotation_id)

    async def _handle_trace_embedded(self, payload: dict) -> None:
        """Handle AITraceEmbedded event."""
        trace_id = payload.get("trace_id")
        embedding = payload.get("embedding", [])

        if not trace_id or not embedding:
            logger.warning("qdrant_invalid_trace_event", payload=payload)
            return

        point = PointStruct(
            id=trace_id,
            vector=embedding,
            payload={
                "trace_id": trace_id,
                "study_id": payload.get("study_id"),
                "owner_id": payload.get("owner_id"),
                "metadata": payload.get("metadata", {}),
            },
        )

        await self._connection.upsert_points(
            collection=TRACES_COLLECTION.name,
            points=[point],
        )
        logger.info("qdrant_trace_embedded", trace_id=trace_id)

    async def _handle_embedding_deleted(self, payload: dict) -> None:
        """Handle AIEmbeddingDeleted event."""
        point_id = payload.get("id")
        collection_name = payload.get("collection")

        if not point_id or not collection_name:
            logger.warning("qdrant_invalid_delete_event", payload=payload)
            return

        await self._connection.delete_points(
            collection=collection_name,
            point_ids=[point_id],
        )
        logger.info("qdrant_embedding_deleted", point_id=point_id, collection=collection_name)

    async def _handle_trace_deleted(self, payload: dict) -> None:
        """Handle TraceDeleted event - cascade delete from all collections."""
        trace_id = payload.get("id")

        if not trace_id:
            logger.warning("qdrant_invalid_trace_delete_event", payload=payload)
            return

        # Delete from sequences collection (keyed by trace_id)
        await self._connection.delete_points(
            collection=SEQUENCES_COLLECTION.name,
            point_ids=[trace_id],
        )

        # Delete from traces collection (keyed by trace_id)
        await self._connection.delete_points(
            collection=TRACES_COLLECTION.name,
            point_ids=[trace_id],
        )

        # Delete related annotations by trace_id filter
        await self._connection.delete_by_filter(
            collection=ANNOTATIONS_COLLECTION.name,
            field="trace_id",
            value=trace_id,
        )

        logger.info("qdrant_trace_deleted_cascade", trace_id=trace_id)

    async def _handle_annotation_deleted(self, payload: dict) -> None:
        """Handle AnnotationDeleted event."""
        annotation_id = payload.get("id")

        if not annotation_id:
            logger.warning("qdrant_invalid_annotation_delete_event", payload=payload)
            return

        await self._connection.delete_points(
            collection=ANNOTATIONS_COLLECTION.name,
            point_ids=[annotation_id],
        )
        logger.info("qdrant_annotation_deleted", annotation_id=annotation_id)

    async def health_check(self) -> bool:
        """Check if Qdrant connection is healthy."""
        return await self._connection.health_check()

    async def rebuild(self) -> None:
        """Rebuild by dropping and recreating all collections."""
        logger.info("qdrant_rebuild_started")

        for collection in COLLECTIONS:
            await self._connection.drop_collection(collection.name)
            await self._connection.ensuREDACTED(
                name=collection.name,
                vector_size=collection.vector_size,
                distance=collection.distance,
            )

        self._metrics["events_processed"] = 0
        logger.info("qdrant_rebuild_completed")
