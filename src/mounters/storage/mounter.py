"""Storage mounter for trace and profile binary data."""

import base64
import json
from datetime import datetime
from io import BytesIO

import structlog

from src.mounters.base import BaseMounter
from src.mounters.storage.chunking import TraceManifest, chunk_sequence
from src.mounters.storage.connection import StorageConnection

logger = structlog.get_logger()

# Photo settings
MAX_PHOTO_SIZE = 10 * 1024 * 1024  # 10 MB
ALLOWED_PHOTO_EXTENSIONS = {"jpg", "jpeg", "png", "gif", "webp"}
THUMBNAIL_SIZE = (150, 150)


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
        super().__init__(
            name="storage",
            categories=["traces", "profiles"],
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
        event_type = event.get("type", "") or event.get("event_type", "")
        payload = event.get("payload", {}) or event.get("data", {})

        # Trace events
        if event_type == "TraceUploaded":
            await self._handle_trace_uploaded(payload)
        elif event_type == "TraceDeleted":
            await self._handle_trace_deleted(payload)
        # Profile photo events
        elif event_type == "ProfilePhotoUploadedEvent":
            await self._handle_profile_photo_uploaded(payload)
        elif event_type == "ProfilePhotoDeletedEvent":
            await self._handle_profile_photo_deleted(payload)

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

    # =========================================================================
    # PROFILE PHOTO HANDLERS
    # =========================================================================

    async def _handle_profile_photo_uploaded(self, payload: dict) -> None:
        """Handle ProfilePhotoUploadedEvent."""
        profile_id = payload.get("profile_id") or payload.get("ProfileId")
        photo_data_b64 = payload.get("photo_data") or payload.get("PhotoData")
        extension = payload.get("extension") or payload.get("Extension") or "jpg"
        content_type = payload.get("content_type") or payload.get("ContentType") or "image/jpeg"

        if not profile_id or not photo_data_b64:
            logger.warning("profile_photo_upload_missing_data", profile_id=profile_id)
            return

        # Decode base64 photo data
        try:
            photo_data = base64.b64decode(photo_data_b64)
        except Exception as e:
            logger.error("profile_photo_decode_failed", profile_id=profile_id, error=str(e))
            return

        # Validate size
        if len(photo_data) > MAX_PHOTO_SIZE:
            logger.warning(
                "profile_photo_too_large",
                profile_id=profile_id,
                size=len(photo_data),
                max_size=MAX_PHOTO_SIZE,
            )
            return

        # Validate extension
        ext_lower = extension.lower().lstrip(".")
        if ext_lower not in ALLOWED_PHOTO_EXTENSIONS:
            logger.warning(
                "profile_photo_invalid_extension",
                profile_id=profile_id,
                extension=extension,
            )
            return

        # Store original photo
        photo_key = f"profiles/{profile_id}/photo.{ext_lower}"
        await self._connection.put_object(self._bucket, photo_key, photo_data)

        # Generate and store thumbnail
        thumbnail_data = await self._generate_thumbnail(photo_data, ext_lower)
        if thumbnail_data:
            thumbnail_key = f"profiles/{profile_id}/thumbnail.{ext_lower}"
            await self._connection.put_object(self._bucket, thumbnail_key, thumbnail_data)

        # Store metadata
        metadata = {
            "profile_id": profile_id,
            "extension": ext_lower,
            "content_type": content_type,
            "size_bytes": len(photo_data),
            "thumbnail_size_bytes": len(thumbnail_data) if thumbnail_data else 0,
            "uploaded_at": datetime.utcnow().isoformat() + "Z",
        }
        metadata_key = f"profiles/{profile_id}/photo_metadata.json"
        await self._connection.put_object(self._bucket, metadata_key, json.dumps(metadata).encode())

        logger.info(
            "storage_profile_photo_uploaded",
            profile_id=profile_id,
            size=len(photo_data),
        )

    async def _handle_profile_photo_deleted(self, payload: dict) -> None:
        """Handle ProfilePhotoDeletedEvent."""
        profile_id = payload.get("profile_id") or payload.get("ProfileId")

        if not profile_id:
            return

        prefix = f"profiles/{profile_id}/"

        # List and delete all photo-related objects
        objects = await self._connection.list_objects(self._bucket, prefix)
        for obj in objects:
            await self._connection.delete_object(self._bucket, obj["key"])

        logger.info("storage_profile_photo_deleted", profile_id=profile_id)

    async def _generate_thumbnail(self, photo_data: bytes, extension: str) -> bytes | None:
        """Generate a thumbnail from photo data."""
        try:
            from PIL import Image

            image = Image.open(BytesIO(photo_data))

            # Convert to RGB if necessary (for PNG with transparency)
            if image.mode in ("RGBA", "P"):
                image = image.convert("RGB")

            # Create thumbnail
            image.thumbnail(THUMBNAIL_SIZE, Image.Resampling.LANCZOS)

            # Save to bytes
            output = BytesIO()
            format_map = {"jpg": "JPEG", "jpeg": "JPEG", "png": "PNG", "gif": "GIF", "webp": "WEBP"}
            save_format = format_map.get(extension.lower(), "JPEG")
            image.save(output, format=save_format, quality=85)
            output.seek(0)

            return output.read()
        except ImportError:
            logger.warning("pillow_not_installed_skipping_thumbnail")
            return None
        except Exception as e:
            logger.error("thumbnail_generation_failed", error=str(e))
            return None

    # =========================================================================
    # PROFILE PHOTO GETTERS
    # =========================================================================

    async def get_profile_photo(self, profile_id: str) -> tuple[bytes, str] | None:
        """Get the profile photo for a profile."""
        metadata_key = f"profiles/{profile_id}/photo_metadata.json"

        if not await self._connection.object_exists(self._bucket, metadata_key):
            return None

        metadata_data = await self._connection.get_object(self._bucket, metadata_key)
        metadata = json.loads(metadata_data.decode())
        extension = metadata.get("extension", "jpg")

        photo_key = f"profiles/{profile_id}/photo.{extension}"
        photo_data = await self._connection.get_object(self._bucket, photo_key)

        return photo_data, extension

    async def get_profile_thumbnail(self, profile_id: str) -> tuple[bytes, str] | None:
        """Get the thumbnail for a profile photo."""
        metadata_key = f"profiles/{profile_id}/photo_metadata.json"

        if not await self._connection.object_exists(self._bucket, metadata_key):
            return None

        metadata_data = await self._connection.get_object(self._bucket, metadata_key)
        metadata = json.loads(metadata_data.decode())
        extension = metadata.get("extension", "jpg")

        thumbnail_key = f"profiles/{profile_id}/thumbnail.{extension}"
        if not await self._connection.object_exists(self._bucket, thumbnail_key):
            return None

        thumbnail_data = await self._connection.get_object(self._bucket, thumbnail_key)
        return thumbnail_data, extension

    async def get_profile_photo_url(self, profile_id: str) -> str | None:
        """Get the URL for a profile photo."""
        metadata_key = f"profiles/{profile_id}/photo_metadata.json"

        if not await self._connection.object_exists(self._bucket, metadata_key):
            return None

        metadata_data = await self._connection.get_object(self._bucket, metadata_key)
        metadata = json.loads(metadata_data.decode())
        extension = metadata.get("extension", "jpg")

        photo_key = f"profiles/{profile_id}/photo.{extension}"
        return f"/{self._bucket}/{photo_key}"

    async def get_profile_thumbnail_url(self, profile_id: str) -> str | None:
        """Get the URL for a profile thumbnail."""
        metadata_key = f"profiles/{profile_id}/photo_metadata.json"

        if not await self._connection.object_exists(self._bucket, metadata_key):
            return None

        metadata_data = await self._connection.get_object(self._bucket, metadata_key)
        metadata = json.loads(metadata_data.decode())
        extension = metadata.get("extension", "jpg")

        thumbnail_key = f"profiles/{profile_id}/thumbnail.{extension}"
        return f"/{self._bucket}/{thumbnail_key}"

    # =========================================================================
    # TRACE CHUNKING
    # =========================================================================

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

            chunk_metas.append(
                {
                    "index": i,
                    "start_position": start,
                    "end_position": end,
                    "base_count": len(chunk_seq),
                    "filename": chunk_filename,
                }
            )

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
