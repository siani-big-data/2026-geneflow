__all__ = [
    "StorageProvider",
    "LocalStorageProvider",
    "MinIOStorageProvider",
    "SupabaseStorageProvider",
    "get_storage_provider",
]

from src.storage.local import LocalStorageProvider
from src.storage.minio import MinIOStorageProvider
from src.storage.storage import StorageProvider
from src.storage.supabase import SupabaseStorageProvider


def get_storage_provider(provider: str, **kwargs) -> StorageProvider:
    """Factory to get the correct storage provider."""
    if provider == "local":
        return LocalStorageProvider(
            base_path=kwargs.get("local_storage_path", "./data/datalake")
        )
    elif provider == "minio":
        return MinIOStorageProvider(
            endpoint=kwargs.get("minio_endpoint", ""),
            access_key=kwargs.get("minio_access_key", ""),
            secret_key=kwargs.get("minio_secret_key", ""),
            bucket=kwargs.get("minio_bucket", "geneflow-datalake"),
            secure=kwargs.get("minio_secure", True),
        )
    elif provider == "supabase":
        return SupabaseStorageProvider(
            url=kwargs.get("supabase_url", ""),
            key=kwargs.get("supabase_key", ""),
            bucket=kwargs.get("supabase_bucket", "geneflow-datalake"),
        )
    else:
        raise ValueError(f"Unknown storage provider: {provider}")