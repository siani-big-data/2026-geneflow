"""Async Qdrant client connection wrapper."""

import structlog
from qdrant_client import AsyncQdrantClient
from qdrant_client.models import (
    Distance,
    FieldCondition,
    Filter,
    MatchValue,
    PointStruct,
    VectorParams,
)

logger = structlog.get_logger()


class QdrantConnection:
    """Async connection wrapper for Qdrant vector database."""

    def __init__(self, url: str, api_key: str | None = None):
        self._url = url
        self._api_key = api_key
        self._client: AsyncQdrantClient | None = None

    async def connect(self) -> None:
        """Establish connection to Qdrant."""
        self._client = AsyncQdrantClient(
            url=self._url,
            api_key=self._api_key if self._api_key else None,
        )
        logger.info("qdrant_connected", url=self._url)

    async def close(self) -> None:
        """Close the Qdrant connection."""
        if self._client:
            await self._client.close()
            self._client = None
            logger.info("qdrant_disconnected")

    @property
    def client(self) -> AsyncQdrantClient:
        """Get the underlying Qdrant client."""
        if not self._client:
            raise RuntimeError("Qdrant connection not established")
        return self._client

    async def ensuREDACTED(
        self,
        name: str,
        vector_size: int,
        distance: Distance = Distance.COSINE,
    ) -> None:
        """Ensure a collection exists, creating it if necessary."""
        collections = await self.client.get_collections()
        existing_names = [c.name for c in collections.collections]

        if name not in existing_names:
            await self.client.create_collection(
                collection_name=name,
                vectors_config=VectorParams(size=vector_size, distance=distance),
            )
            logger.info("qdrant_collection_created", collection=name, vector_size=vector_size)
        else:
            logger.debug("qdrant_collection_exists", collection=name)

    async def upsert_points(
        self,
        collection: str,
        points: list[PointStruct],
    ) -> None:
        """Upsert points into a collection."""
        if not points:
            return

        await self.client.upsert(
            collection_name=collection,
            points=points,
        )
        logger.debug("qdrant_points_upserted", collection=collection, count=len(points))

    async def delete_points(
        self,
        collection: str,
        point_ids: list[str],
    ) -> None:
        """Delete points by their IDs."""
        if not point_ids:
            return

        await self.client.delete(
            collection_name=collection,
            points_selector=point_ids,
        )
        logger.debug("qdrant_points_deleted", collection=collection, count=len(point_ids))

    async def delete_by_filter(
        self,
        collection: str,
        field: str,
        value: str,
    ) -> None:
        """Delete points matching a field filter."""
        await self.client.delete(
            collection_name=collection,
            points_selector=Filter(
                must=[
                    FieldCondition(
                        key=field,
                        match=MatchValue(value=value),
                    )
                ]
            ),
        )
        logger.debug(
            "qdrant_points_deleted_by_filter",
            collection=collection,
            field=field,
            value=value,
        )

    async def health_check(self) -> bool:
        """Check if Qdrant is healthy."""
        try:
            await self.client.get_collections()
            return True
        except Exception as e:
            logger.warning("qdrant_health_check_failed", error=str(e))
            return False

    async def drop_collection(self, name: str) -> None:
        """Drop a collection if it exists."""
        collections = await self.client.get_collections()
        existing_names = [c.name for c in collections.collections]

        if name in existing_names:
            await self.client.delete_collection(collection_name=name)
            logger.info("qdrant_collection_dropped", collection=name)
