from typing import Final

BUFFER_DEFAULT_MAX_SIZE: Final[int] = 1000
BUFFER_FLUSH_INTERVAL_SECONDS: Final[float] = 5.0

RETRY_MAX_ATTEMPTS: Final[int] = 5
RETRY_BASE_DELAY_SECONDS: Final[float] = 1.0
RETRY_MAX_DELAY_SECONDS: Final[float] = 300.0

DEDUP_TTL_HOURS: Final[int] = 24
DEDUP_MAX_SIZE: Final[int] = 100_000
DEDUP_CLEANUP_INTERVAL_SECONDS: Final[float] = 300.0

REDIS_BLOCK_MS: Final[int] = 5000
REDIS_BATCH_SIZE: Final[int] = 100
REDIS_RECONNECT_DELAY_SECONDS: Final[float] = 5.0

PHOTO_MAX_SIZE_BYTES: Final[int] = 10 * 1024 * 1024
PHOTO_ALLOWED_EXTENSIONS: Final[frozenset] = frozenset({"jpg", "jpeg", "png", "gif", "webp"})
THUMBNAIL_SIZE: Final[tuple[int, int]] = (150, 150)
THUMBNAIL_QUALITY: Final[int] = 85

API_DEFAULT_PAGE_LIMIT: Final[int] = 1000
API_MAX_PAGE_LIMIT: Final[int] = 10_000

DATE_FORMAT: Final[str] = "%Y-%m-%d"
