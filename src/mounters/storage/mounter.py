"""Storage mounter for trace binary data."""

import json
import structlog
from datetime import datetime

from src.mounters.base import BaseMounter
from src.mounters.storage.connection import StorageConnection
from src.mounters.storage.chunking import TraceManifest, chunk_sequence

logger = structlog.get_logger()


class StorageMounter(BaseMounter):
    """Mounter for storing trace binary data in S3-compatible storage."""

    def __init__(
        self,
        endpoint_url: str,
        access_key: str,
        secret_key: str,
        bucket: str,
        chunk_size: int = 10000,
    ):
        super().__init__(
            name="storage",
            categories=["traces"],
        )
        self._connection = StorageConnection(
            endpoint_url=endpoint_url,
            access_key=access_key,
            secret_key=secret_key,
        )
        self._bucket = bucket
        self._chunk_size = chunk_size

    async def start(self) -> None:
        """Start the mounter."""
        await self._connection.connect()
        await self._connection.ensuREDACTED(self._bucket)
        self._running = True
        logger.info("storage_mounter_started", bucket=self._bucket)

    async def stop(self) -> None:
        """Stop the mounter."""
        await self._connection.close()
        self._running = False
        logger.info("storage_mounter_stopped")

    async def handle_event(self, event: dict) -> None:
        """Handle an incoming event."""
        event_type = event.get("type", "")
        payload = event.get("payload", {})

        if event_type == "TraceUploaded":
            await self._handle_trace_uploaded(payload)
        elif event_type == "TraceDeleted":
            await self._handle_trace_deleted(payload)

        self._metrics["events_processed"] += 1

    async def _handle_trace_uploaded(self, payload: dict) -> None:
        """Handle TraceUploaded event."""
        trace_id = payload.get("id")
        original_file = payload.get("original_file", b"")
        extension = payload.get("original_extension", "ab1")
        parsed_data = payload.get("parsed_data", {})

        # Store original file
        original_key = f"traces/{trace_id}/original.{extension}"
        await self._connection.put_object(self._bucket, original_key, original_file)

        # Store chunked parsed data
        await self._stoREDACTED(trace_id, parsed_data)

        logger.info("storage_trace_uploaded", trace_id=trace_id)

    async def _handle_trace_deleted(self, payload: dict) -> None:
        """Handle TraceDeleted event."""
        trace_id = payload.get("id")
        prefix = f"traces/{trace_id}/"

        # List and delete all objects for this trace
        objects = await self._connection.list_objects(self._bucket, prefix)
        for obj in objects:
            await self._connection.delete_object(self._bucket, obj["key"])

        logger.info("storage_trace_deleted", trace_id=trace_id)

    async def _stoREDACTED(self, trace_id: str, parsed_data: dict) -> TraceManifest:
        """Store trace data in chunks."""
        sequence = parsed_data.get("sequence", "")
        quality_scores = parsed_data.get("quality_scores", [])

        chunks_data = chunk_sequence(sequence, self._chunk_size)
        chunk_metas = []

        for i, (start, end, chunk_seq) in enumerate(chunks_data):
            chunk_filename = f"chunk_{i:04d}.json"
            chunk_key = f"traces/{trace_id}/chunks/{chunk_filename}"

            chunk_content = {
                "index": i,
                "start_position": start,
                "end_position": end,
                "bases": chunk_seq,
                "quality_scores": quality_scores[start:end] if quality_scores else [],
            }

            await self._connection.put_object(
                self._bucket,
                chunk_key,
                json.dumps(chunk_content).encode(),
            )

            chunk_metas.append({
                "index": i,
                "start_position": start,
                "end_position": end,
                "base_count": len(chunk_seq),
                "filename": chunk_filename,
            })

        manifest = TraceManifest(
            trace_id=trace_id,
            original_filename=parsed_data.get("filename", "unknown"),
            format=parsed_data.get("format", "unknown"),
            total_bases=len(sequence),
            chunk_size=self._chunk_size,
            chunk_count=len(chunks_data),
            has_chromatogram="chromatogram" in parsed_data,
            has_quality_scores=bool(quality_scores),
            created_at=datetime.utcnow().isoformat() + "Z",
            chunks=[],  # Simplified for this mounter
        )

        manifest_key = f"traces/{trace_id}/manifest.json"
        manifest_data = manifest.to_dict()
        manifest_data["chunks"] = chunk_metas  # Use raw dicts for storage
        await self._connection.put_object(
            self._bucket,
            manifest_key,
            json.dumps(manifest_data).encode(),
        )

        return manifest

    async def get_manifest(self, trace_id: str) -> TraceManifest | None:
        """Get the manifest for a trace."""
        manifest_key = f"traces/{trace_id}/manifest.json"

        if not await self._connection.object_exists(self._bucket, manifest_key):
            return None

        data = await self._connection.get_object(self._bucket, manifest_key)
        manifest_dict = json.loads(data.decode())
        return TraceManifest.from_dict(manifest_dict)

    async def get_chunk(self, trace_id: str, chunk_index: int) -> dict | None:
        """Get a specific chunk for a trace."""
        chunk_key = f"traces/{trace_id}/chunks/chunk_{chunk_index:04d}.json"
        data = await self._connection.get_object(self._bucket, chunk_key)
        return json.loads(data.decode())

    async def get_original(self, trace_id: str) -> tuple[bytes, str] | None:
        """Get the original file for a trace."""
        prefix = f"traces/{trace_id}/original."
        objects = await self._connection.list_objects(self._bucket, prefix)

        if not objects:
            return None

        key = objects[0]["key"]
        extension = key.split(".")[-1]
        data = await self._connection.get_object(self._bucket, key)
        return data, extension

    async def health_check(self) -> bool:
        """Check if storage connection is healthy."""
        return await self._connection.health_check()

    async def rebuild(self) -> None:
        """Rebuild by deleting all stored data."""
        objects = await self._connection.list_objects(self._bucket, "traces/")
        for obj in objects:
            await self._connection.delete_object(self._bucket, obj["key"])

        self._metrics["events_processed"] = 0
        logger.info("storage_mounter_rebuild")
