"""Storage mounter for trace and profile binary data."""

import structlog

from src.mounters.base import BaseMounter
from src.mounters.storage.connection import StorageConnection
from src.mounters.storage.handlers import ProfilePhotoHandler, TraceHandler

logger = structlog.get_logger()


class StorageMounter(BaseMounter):
    """Mounter for storing trace and profile binary data in S3-compatible storage."""

    def __init__(
        self,
        endpoint_url: str,
        access_key: str,
        secret_key: str,
        bucket: str,
        chunk_size: int = 10000,
    ):
        super().__init__(name="storage", categories=["traces", "profiles"])
        self._connection = StorageConnection(
            endpoint_url=endpoint_url,
            access_key=access_key,
            secret_key=secret_key,
        )
        self._bucket = bucket
        self._chunk_size = chunk_size
        self._trace_handler: TraceHandler | None = None
        self._photo_handler: ProfilePhotoHandler | None = None

    async def start(self) -> None:
        """Start the mounter."""
        await self._connection.connect()
        await self._connection.ensuREDACTED(self._bucket)

        self._trace_handler = TraceHandler(self._connection, self._bucket, self._chunk_size)
        self._photo_handler = ProfilePhotoHandler(self._connection, self._bucket)

        self._running = True
        logger.info("storage_mounter_started", bucket=self._bucket)

    async def stop(self) -> None:
        """Stop the mounter."""
        await self._connection.close()
        self._running = False
        logger.info("storage_mounter_stopped")

    async def handle_event(self, event: dict) -> None:
        """Handle an incoming event."""
        event_type = event.get("type", "") or event.get("event_type", "")
        payload = event.get("payload", {}) or event.get("data", {})

        if event_type == "TraceUploaded":
            await self._trace_handler.handle_uploaded(payload)
        elif event_type == "TraceProcessed":
            await self._trace_handler.handle_processed(payload)
        elif event_type == "TraceDeleted":
            await self._trace_handler.handle_deleted(payload)
        elif event_type == "AnalysisResultStored":
            await self._trace_handler.handle_analysis_result(payload)
        elif event_type == "ProfilePhotoUploadedEvent":
            await self._photo_handler.handle_uploaded(payload)
        elif event_type == "ProfilePhotoDeletedEvent":
            await self._photo_handler.handle_deleted(payload)

        self._metrics["events_processed"] += 1

    async def get_manifest(self, trace_id: str):
        """Get the manifest for a trace."""
        return await self._trace_handler.get_manifest(trace_id)

    async def get_chunk(self, trace_id: str, chunk_index: int):
        """Get a specific chunk for a trace."""
        return await self._trace_handler.get_chunk(trace_id, chunk_index)

    async def get_original(self, trace_id: str):
        """Get the original file for a trace."""
        return await self._trace_handler.get_original(trace_id)

    async def get_analysis_result(self, trace_id: str, analysis_type: str):
        """Get analysis result for a trace."""
        return await self._trace_handler.get_analysis_result(trace_id, analysis_type)

    async def list_analysis_results(self, trace_id: str):
        """List available analysis results for a trace."""
        return await self._trace_handler.list_analysis_results(trace_id)

    async def get_profile_photo(self, profile_id: str):
        """Get the profile photo for a profile."""
        return await self._photo_handler.get_photo(profile_id)

    async def get_profile_thumbnail(self, profile_id: str):
        """Get the thumbnail for a profile photo."""
        return await self._photo_handler.get_thumbnail(profile_id)

    async def get_profile_photo_url(self, profile_id: str):
        """Get the URL for a profile photo."""
        return await self._photo_handler.get_photo_url(profile_id)

    async def get_profile_thumbnail_url(self, profile_id: str):
        """Get the URL for a profile thumbnail."""
        return await self._photo_handler.get_thumbnail_url(profile_id)

    async def health_check(self) -> bool:
        """Check if storage connection is healthy."""
        return await self._connection.health_check()

    async def rebuild(self, categories: list[str] | None = None) -> None:
        """Rebuild by deleting all stored data."""
        target_categories = categories or ["traces", "profiles"]

        for category in target_categories:
            objects = await self._connection.list_objects(self._bucket, f"{category}/")
            for obj in objects:
                await self._connection.delete_object(self._bucket, obj["key"])
            logger.info("storage_category_cleared", category=category)

        self._metrics["events_processed"] = 0
        logger.info("storage_mounter_rebuild", categories=target_categories)
