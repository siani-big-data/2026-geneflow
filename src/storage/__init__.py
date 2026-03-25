__all__ = ["StorageProvider", "LocalStorageProvider", "get_storage_provider"]

from src.storage.local import LocalStorageProvider
from src.storage.storage import StorageProvider


def get_storage_provider(provider: str, **kwargs) -> StorageProvider:
    """Factory to get the correct storage provider."""
    if provider == "local":
        return LocalStorageProvider(
            base_path=kwargs.get("local_storage_path", "./data/datalake")
        )
    else:
        raise ValueError(f"Unknown storage provider: {provider}")