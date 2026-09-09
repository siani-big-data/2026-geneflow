"""Storage provider factory."""

from src.config import Settings
from src.storage.base import BaseStorageProvider
from src.storage.http import HTTPStorageProvider
from src.storage.local import LocalStorageProvider
from src.storage.supabase import SupabaseStorageProvider


class StorageFactory:
    """Factory for creating storage providers."""

    @staticmethod
    def create(settings: Settings) -> BaseStorageProvider:
        """
        Create storage provider based on settings.

        Args:
            settings: Application settings

        Returns:
            Configured storage provider
        """
        provider_type = settings.storage_provider.lower()

        if provider_type == "local":
            return LocalStorageProvider(
                base_path=settings.local_storage_path,
            )

        elif provider_type == "http":
            return HTTPStorageProvider()

        elif provider_type == "minio":
            if not settings.minio_access_key or not settings.minio_secret_key:
                raise ValueError(
                    "MinIO storage requires WORKER_MINIO_ACCESS_KEY and WORKER_MINIO_SECRET_KEY"
                )

            from src.storage.minio import MinIOStorageProvider

            return MinIOStorageProvider(
                endpoint=settings.minio_endpoint,
                access_key=settings.minio_access_key,
                secret_key=settings.minio_secret_key,
                bucket=settings.minio_bucket,
                secure=settings.minio_secure,
            )

        elif provider_type == "supabase":
            if not settings.supabase_url or not settings.supabase_key:
                raise ValueError(
                    "Supabase storage requires WORKER_SUPABASE_URL and WORKER_SUPABASE_KEY"
                )

            return SupabaseStorageProvider(
                url=settings.supabase_url,
                key=settings.supabase_key,
                bucket=settings.supabase_bucket,
            )

        else:
            raise ValueError(f"Unknown storage provider: {provider_type}")

    @staticmethod
    def create_local(base_path: str = "./data") -> LocalStorageProvider:
        """Create local storage provider."""
        return LocalStorageProvider(base_path=base_path)

    @staticmethod
    def create_http(
        timeout: float = 30.0,
        headers: dict[str, str] | None = None,
    ) -> HTTPStorageProvider:
        """Create HTTP storage provider."""
        return HTTPStorageProvider(timeout=timeout, headers=headers)

    @staticmethod
    def create_minio(
        endpoint: str,
        access_key: str,
        secret_key: str,
        bucket: str = "geneflow",
        secure: bool = False,
    ) -> "BaseStorageProvider":
        """Create MinIO storage provider."""
        from src.storage.minio import MinIOStorageProvider

        return MinIOStorageProvider(
            endpoint=endpoint,
            access_key=access_key,
            secret_key=secret_key,
            bucket=bucket,
            secure=secure,
        )

    @staticmethod
    def create_supabase(
        url: str,
        key: str,
        bucket: str = "geneflow-traces",
    ) -> SupabaseStorageProvider:
        """Create Supabase storage provider."""
        return SupabaseStorageProvider(url=url, key=key, bucket=bucket)
