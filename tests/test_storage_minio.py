"""Tests for MinIO storage provider using mocks."""

from unittest.mock import MagicMock, patch

import pytest
from minio.error import S3Error

from src.storage.minio import MinIOStorageProvider


@pytest.fixture
def mock_minio_client():
    """Patch Minio client constructor and return the mock instance."""
    with patch("src.storage.minio.Minio") as mock_class:
        instance = MagicMock()
        mock_class.return_value = instance
        yield instance


@pytest.fixture
def provider(mock_minio_client):
    """Build a provider with the mocked Minio client."""
    return MinIOStorageProvider(
        endpoint="localhost:9000",
        access_key="ak",
        secret_key="sk",
        bucket="bucket",
        secure=False,
    )


def _s3_error(code: str = "NoSuchKey") -> S3Error:
    return S3Error(
        code=code,
        message="not found",
        resource="x",
        request_id="r",
        host_id="h",
        response=MagicMock(),
    )


class TestMinIOStorageProvider:
    """Tests for MinIOStorageProvider."""

    def test_name(self, provider):
        assert provider.name == "minio:localhost:9000/bucket"

    @pytest.mark.asyncio
    async def test_get_success(self, provider, mock_minio_client):
        response = MagicMock()
        response.read.return_value = b"data"
        mock_minio_client.get_object.return_value = response

        result = await provider.get("path/file.bin")

        assert result == b"data"
        mock_minio_client.get_object.assert_called_once_with("bucket", "path/file.bin")
        response.close.assert_called_once()
        response.release_conn.assert_called_once()

    @pytest.mark.asyncio
    async def test_get_failuREDACTED(self, provider, mock_minio_client):
        mock_minio_client.get_object.side_effect = _s3_error()

        with pytest.raises(ValueError, match="Failed to get"):
            await provider.get("path/missing")

    @pytest.mark.asyncio
    async def test_put_success(self, provider, mock_minio_client):
        result = await provider.put("path/file.bin", b"payload")

        assert result == "path/file.bin"
        mock_minio_client.put_object.assert_called_once()
        args, kwargs = mock_minio_client.put_object.call_args
        assert args[0] == "bucket"
        assert args[1] == "path/file.bin"
        assert kwargs["length"] == len(b"payload")

    @pytest.mark.asyncio
    async def test_put_failuREDACTED(self, provider, mock_minio_client):
        mock_minio_client.put_object.side_effect = _s3_error()

        with pytest.raises(ValueError, match="Failed to put"):
            await provider.put("path/file.bin", b"payload")

    @pytest.mark.asyncio
    async def test_exists_true(self, provider, mock_minio_client):
        mock_minio_client.stat_object.return_value = MagicMock()

        assert await provider.exists("path/file") is True

    @pytest.mark.asyncio
    async def test_exists_false(self, provider, mock_minio_client):
        mock_minio_client.stat_object.side_effect = _s3_error()

        assert await provider.exists("path/missing") is False

    @pytest.mark.asyncio
    async def test_delete_success(self, provider, mock_minio_client):
        mock_minio_client.stat_object.return_value = MagicMock()

        result = await provider.delete("path/file")

        assert result is True
        mock_minio_client.remove_object.assert_called_once_with("bucket", "path/file")

    @pytest.mark.asyncio
    async def test_delete_not_found(self, provider, mock_minio_client):
        mock_minio_client.stat_object.side_effect = _s3_error()

        result = await provider.delete("path/missing")

        assert result is False
        mock_minio_client.remove_object.assert_not_called()

    @pytest.mark.asyncio
    async def test_delete_remove_failuREDACTED(self, provider, mock_minio_client):
        mock_minio_client.stat_object.return_value = MagicMock()
        mock_minio_client.remove_object.side_effect = _s3_error()

        result = await provider.delete("path/file")

        assert result is False


class TestStorageFactoryMinIO:
    """Factory wiring for MinIO."""

    def test_create_minio_requires_credentials(self):
        from src.config import Settings
        from src.storage.factory import StorageFactory

        settings = Settings(
            storage_provider="minio",
            minio_access_key="",
            minio_secret_key="",
        )
        with pytest.raises(ValueError, match="MinIO"):
            StorageFactory.create(settings)

    def test_create_minio_with_credentials(self):
        from src.config import Settings
        from src.storage.factory import StorageFactory

        with patch("src.storage.minio.Minio") as mock_class:
            mock_class.return_value = MagicMock()
            settings = Settings(
                storage_provider="minio",
                minio_endpoint="localhost:9000",
                minio_access_key="ak",
                minio_secret_key="sk",
                minio_bucket="bucket",
                minio_secure=True,
            )
            provider = StorageFactory.create(settings)

        assert isinstance(provider, MinIOStorageProvider)
        assert provider.name == "minio:localhost:9000/bucket"

    def test_create_minio_helper(self):
        from src.storage.factory import StorageFactory

        with patch("src.storage.minio.Minio") as mock_class:
            mock_class.return_value = MagicMock()
            provider = StorageFactory.create_minio(
                endpoint="localhost:9000",
                access_key="ak",
                secret_key="sk",
                bucket="b",
                secure=False,
            )

        assert isinstance(provider, MinIOStorageProvider)

    def test_lazy_minio_import_via_storage_module(self):
        import src.storage as storage_pkg

        assert storage_pkg.MinIOStorageProvider is MinIOStorageProvider

    def test_storage_module_unknown_attribute_raises(self):
        import src.storage as storage_pkg

        with pytest.raises(AttributeError):
            storage_pkg.NonExistent
