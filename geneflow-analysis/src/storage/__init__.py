"""Storage module for GeneFlow Analysis Worker."""

from src.storage.base import BaseStorageProvider
from src.storage.factory import StorageFactory
from src.storage.http import HTTPStorageProvider
from src.storage.local import LocalStorageProvider
from src.storage.supabase import SupabaseStorageProvider

__all__ = [
    "BaseStorageProvider",
    "LocalStorageProvider",
    "HTTPStorageProvider",
    "SupabaseStorageProvider",
    "StorageFactory",
]


def __getattr__(name: str):
    """Lazy import for optional dependencies."""
    if name == "MinIOStorageProvider":
        from src.storage.minio import MinIOStorageProvider

        return MinIOStorageProvider
    raise AttributeError(f"module {__name__!r} has no attribute {name!r}")
