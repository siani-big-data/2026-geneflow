import base64

import structlog

from src.mounters.storage.connection import StorageConnection
from src.mounters.storage.services import MetadataService, ThumbnailService
from src.mounters.storage.validators import PhotoValidator

logger = structlog.get_logger()


class ProfilePhotoHandler:
    """Handles profile photo storage operations."""

    def __init__(
        self,
        connection: StorageConnection,
        bucket: str,
        validator: PhotoValidator | None = None,
        thumbnail_service: ThumbnailService | None = None,
        metadata_service: MetadataService | None = None,
    ):
        self._connection = connection
        self._bucket = bucket
        self._validator = validator or PhotoValidator()
        self._thumbnail_service = thumbnail_service or ThumbnailService()
        self._metadata_service = metadata_service or MetadataService()

    async def handle_uploaded(self, payload: dict) -> None:
        """Handle ProfilePhotoUploadedEvent."""
        profile_id = self._extract_profile_id(payload)
        photo_data_b64 = payload.get("photo_data") or payload.get("PhotoData")
        extension = payload.get("extension") or payload.get("Extension") or "jpg"
        content_type = payload.get("content_type") or payload.get("ContentType") or "image/jpeg"

        if not profile_id or not photo_data_b64:
            logger.warning("profile_photo_upload_missing_data", profile_id=profile_id)
            return

        try:
            photo_data = base64.b64decode(photo_data_b64)
        except Exception as e:
            logger.error("profile_photo_decode_failed", profile_id=profile_id, error=str(e))
            return

        ext = self._validator.normalize_extension(extension)
        if not self._validator.validate(photo_data, ext, profile_id):
            return

        photo_key = f"profiles/{profile_id}/photo.{ext}"
        await self._connection.put_object(self._bucket, photo_key, photo_data)

        thumbnail_data = await self._thumbnail_service.generate(photo_data, ext)
        thumbnail_size = 0
        if thumbnail_data:
            thumbnail_key = f"profiles/{profile_id}/thumbnail.{ext}"
            await self._connection.put_object(self._bucket, thumbnail_key, thumbnail_data)
            thumbnail_size = len(thumbnail_data)

        metadata = self._metadata_service.create_photo_metadata(
            profile_id=profile_id,
            extension=ext,
            content_type=content_type,
            photo_size=len(photo_data),
            thumbnail_size=thumbnail_size,
        )
        metadata_key = f"profiles/{profile_id}/photo_metadata.json"
        await self._connection.put_object(self._bucket, metadata_key, metadata)

        logger.info("storage_profile_photo_uploaded", profile_id=profile_id, size=len(photo_data))

    async def handle_deleted(self, payload: dict) -> None:
        """Handle ProfilePhotoDeletedEvent."""
        profile_id = payload.get("profile_id") or payload.get("ProfileId")

        if not profile_id:
            return

        prefix = f"profiles/{profile_id}/"
        objects = await self._connection.list_objects(self._bucket, prefix)
        for obj in objects:
            await self._connection.delete_object(self._bucket, obj["key"])

        logger.info("storage_profile_photo_deleted", profile_id=profile_id)

    async def get_photo(self, profile_id: str) -> tuple[bytes, str] | None:
        """Get the profile photo for a profile."""
        metadata = await self._get_metadata(profile_id)
        if not metadata:
            return None

        extension = metadata.get("extension", "jpg")
        photo_key = f"profiles/{profile_id}/photo.{extension}"
        photo_data = await self._connection.get_object(self._bucket, photo_key)

        return photo_data, extension

    async def get_thumbnail(self, profile_id: str) -> tuple[bytes, str] | None:
        """Get the thumbnail for a profile photo."""
        metadata = await self._get_metadata(profile_id)
        if not metadata:
            return None

        extension = metadata.get("extension", "jpg")
        thumbnail_key = f"profiles/{profile_id}/thumbnail.{extension}"

        if not await self._connection.object_exists(self._bucket, thumbnail_key):
            return None

        thumbnail_data = await self._connection.get_object(self._bucket, thumbnail_key)
        return thumbnail_data, extension

    async def get_photo_url(self, profile_id: str) -> str | None:
        """Get the URL for a profile photo."""
        metadata = await self._get_metadata(profile_id)
        if not metadata:
            return None

        extension = metadata.get("extension", "jpg")
        return f"/{self._bucket}/profiles/{profile_id}/photo.{extension}"

    async def get_thumbnail_url(self, profile_id: str) -> str | None:
        """Get the URL for a profile thumbnail."""
        metadata = await self._get_metadata(profile_id)
        if not metadata:
            return None

        extension = metadata.get("extension", "jpg")
        return f"/{self._bucket}/profiles/{profile_id}/thumbnail.{extension}"

    async def _get_metadata(self, profile_id: str) -> dict | None:
        """Get photo metadata for a profile."""
        metadata_key = f"profiles/{profile_id}/photo_metadata.json"

        if not await self._connection.object_exists(self._bucket, metadata_key):
            return None

        data = await self._connection.get_object(self._bucket, metadata_key)
        return self._metadata_service.parse_photo_metadata(data)

    def _extract_profile_id(self, payload: dict) -> str | None:
        """Extract profile ID, handling nested C# value objects."""
        profile_id_raw = payload.get("profile_id") or payload.get("ProfileId")
        if isinstance(profile_id_raw, dict):
            return profile_id_raw.get("value") or profile_id_raw.get("Value")
        return profile_id_raw
