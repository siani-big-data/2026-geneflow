import structlog

from src.constants import PHOTO_ALLOWED_EXTENSIONS, PHOTO_MAX_SIZE_BYTES

logger = structlog.get_logger()


class PhotoValidator:
    """Validates photo uploads."""

    def __init__(
        self,
        max_size: int = PHOTO_MAX_SIZE_BYTES,
        allowed_extensions: frozenset = PHOTO_ALLOWED_EXTENSIONS,
    ):
        self._max_size = max_size
        self._allowed_extensions = allowed_extensions

    def validate(self, photo_data: bytes, extension: str, profile_id: str) -> bool:
        """Validate photo data and extension.

        Returns True if valid, False otherwise. Logs warnings for invalid photos.
        """
        if len(photo_data) > self._max_size:
            logger.warning(
                "profile_photo_too_large",
                profile_id=profile_id,
                size=len(photo_data),
                max_size=self._max_size,
            )
            return False

        ext_lower = extension.lower().lstrip(".")
        if ext_lower not in self._allowed_extensions:
            logger.warning(
                "profile_photo_invalid_extension",
                profile_id=profile_id,
                extension=extension,
            )
            return False

        return True

    def normalize_extension(self, extension: str) -> str:
        """Normalize extension to lowercase without leading dot."""
        return extension.lower().lstrip(".")
