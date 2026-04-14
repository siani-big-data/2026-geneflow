from io import BytesIO

import structlog

from src.constants import THUMBNAIL_QUALITY, THUMBNAIL_SIZE

logger = structlog.get_logger()

FORMAT_MAP = {
    "jpg": "JPEG",
    "jpeg": "JPEG",
    "png": "PNG",
    "gif": "GIF",
    "webp": "WEBP",
}


class ThumbnailService:
    """Generates thumbnails from images."""

    def __init__(
        self,
        size: tuple[int, int] = THUMBNAIL_SIZE,
        quality: int = THUMBNAIL_QUALITY,
    ):
        self._size = size
        self._quality = quality

    async def generate(self, photo_data: bytes, extension: str) -> bytes | None:
        """Generate a thumbnail from photo data.

        Returns thumbnail bytes or None if generation fails.
        """
        try:
            from PIL import Image

            image = Image.open(BytesIO(photo_data))

            if image.mode in ("RGBA", "P"):
                image = image.convert("RGB")

            image.thumbnail(self._size, Image.Resampling.LANCZOS)

            output = BytesIO()
            save_format = FORMAT_MAP.get(extension.lower(), "JPEG")
            image.save(output, format=save_format, quality=self._quality)
            output.seek(0)

            return output.read()

        except ImportError:
            logger.warning("pillow_not_installed_skipping_thumbnail")
            return None
        except Exception as e:
            logger.error("thumbnail_generation_failed", error=str(e))
            return None
