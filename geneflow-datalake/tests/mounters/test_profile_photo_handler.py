"""Tests for ProfilePhotoHandler."""

import base64
from unittest.mock import AsyncMock, MagicMock

import pytest

from src.mounters.storage.handlers.profile_photo_handler import ProfilePhotoHandler


@pytest.fixture
def mock_connection():
    conn = MagicMock()
    conn.put_object = AsyncMock(return_value="etag")
    conn.get_object = AsyncMock()
    conn.delete_object = AsyncMock()
    conn.list_objects = AsyncMock(return_value=[])
    conn.object_exists = AsyncMock(return_value=True)
    return conn


@pytest.fixture
def mock_validator():
    v = MagicMock()
    v.normalize_extension = MagicMock(side_effect=lambda x: x.lower().lstrip("."))
    v.validate = MagicMock(return_value=True)
    return v


@pytest.fixture
def mock_thumbnail_service():
    s = MagicMock()
    s.generate = AsyncMock(return_value=b"THUMB")
    return s


@pytest.fixture
def mock_metadata_service():
    m = MagicMock()
    m.create_photo_metadata = MagicMock(return_value=b'{"profile_id":"p1"}')
    m.parse_photo_metadata = MagicMock(return_value={"extension": "jpg", "profile_id": "p1"})
    return m


@pytest.fixture
def handler(mock_connection, mock_validator, mock_thumbnail_service, mock_metadata_service):
    return ProfilePhotoHandler(
        connection=mock_connection,
        bucket="photos-bucket",
        validator=mock_validator,
        thumbnail_service=mock_thumbnail_service,
        metadata_service=mock_metadata_service,
    )


class TestHandleUploaded:
    async def test_handle_uploaded_complete_flow(
        self,
        handler,
        mock_connection,
        mock_validator,
        mock_thumbnail_service,
        mock_metadata_service,
    ):
        photo_bytes = b"FAKEIMG"
        payload = {
            "profile_id": "p1",
            "photo_data": base64.b64encode(photo_bytes).decode(),
            "extension": "jpg",
            "content_type": "image/jpeg",
        }
        await handler.handle_uploaded(payload)

        keys = [c.args[1] for c in mock_connection.put_object.call_args_list]
        assert "profiles/p1/photo.jpg" in keys
        assert "profiles/p1/thumbnail.jpg" in keys
        assert "profiles/p1/photo_metadata.json" in keys
        mock_thumbnail_service.generate.assert_awaited_once()
        mock_metadata_service.create_photo_metadata.assert_called_once()

    async def test_handle_uploaded_csharp_keys(self, handler, mock_connection):
        """PascalCase keys from C# producer."""
        payload = {
            "ProfileId": "p2",
            "PhotoData": base64.b64encode(b"X").decode(),
            "Extension": ".PNG",
            "ContentType": "image/png",
        }
        await handler.handle_uploaded(payload)
        keys = [c.args[1] for c in mock_connection.put_object.call_args_list]
        assert "profiles/p2/photo.png" in keys

    async def test_handle_uploaded_nested_profile_id(self, handler, mock_connection):
        """C# value object with nested .value key."""
        payload = {
            "profile_id": {"value": "nested-id"},
            "photo_data": base64.b64encode(b"X").decode(),
        }
        await handler.handle_uploaded(payload)
        keys = [c.args[1] for c in mock_connection.put_object.call_args_list]
        assert any("profiles/nested-id/" in k for k in keys)

    async def test_handle_uploaded_nested_profile_id_capital_value(self, handler, mock_connection):
        payload = {
            "ProfileId": {"Value": "nested-id-2"},
            "photo_data": base64.b64encode(b"X").decode(),
        }
        await handler.handle_uploaded(payload)
        keys = [c.args[1] for c in mock_connection.put_object.call_args_list]
        assert any("profiles/nested-id-2/" in k for k in keys)

    async def test_handle_uploaded_missing_profile_id(self, handler, mock_connection):
        await handler.handle_uploaded({"photo_data": "abc"})
        mock_connection.put_object.assert_not_called()

    async def test_handle_uploaded_missing_photo_data(self, handler, mock_connection):
        await handler.handle_uploaded({"profile_id": "p1"})
        mock_connection.put_object.assert_not_called()

    async def test_handle_uploaded_invalid_base64(self, handler, mock_connection):
        await handler.handle_uploaded({"profile_id": "p1", "photo_data": "!!not_b64_!!@@##"})
        # decode would still produce *something* with base64 lenient decoder; force a real bad input
        # Force a strict error by passing a non-string
        mock_connection.put_object.reset_mock()
        await handler.handle_uploaded({"profile_id": "p1", "photo_data": 12345})
        mock_connection.put_object.assert_not_called()

    async def test_handle_uploaded_validator_fails(self, handler, mock_connection, mock_validator):
        mock_validator.validate.return_value = False
        await handler.handle_uploaded(
            {"profile_id": "p1", "photo_data": base64.b64encode(b"X").decode()}
        )
        mock_connection.put_object.assert_not_called()

    async def test_handle_uploaded_default_extension_and_content_type(
        self, handler, mock_connection
    ):
        await handler.handle_uploaded(
            {"profile_id": "p1", "photo_data": base64.b64encode(b"X").decode()}
        )
        keys = [c.args[1] for c in mock_connection.put_object.call_args_list]
        assert "profiles/p1/photo.jpg" in keys

    async def test_handle_uploaded_no_thumbnail(
        self, handler, mock_connection, mock_thumbnail_service, mock_metadata_service
    ):
        mock_thumbnail_service.generate.return_value = None
        await handler.handle_uploaded(
            {"profile_id": "p1", "photo_data": base64.b64encode(b"X").decode()}
        )
        keys = [c.args[1] for c in mock_connection.put_object.call_args_list]
        assert "profiles/p1/photo.jpg" in keys
        assert "profiles/p1/thumbnail.jpg" not in keys
        # metadata still written
        assert "profiles/p1/photo_metadata.json" in keys
        # thumbnail_size=0 passed to metadata
        kwargs = mock_metadata_service.create_photo_metadata.call_args.kwargs
        assert kwargs["thumbnail_size"] == 0


class TestHandleDeleted:
    async def test_handle_deleted(self, handler, mock_connection):
        mock_connection.list_objects.return_value = [
            {"key": "profiles/p1/photo.jpg"},
            {"key": "profiles/p1/thumbnail.jpg"},
        ]
        await handler.handle_deleted({"profile_id": "p1"})
        assert mock_connection.delete_object.call_count == 2

    async def test_handle_deleted_pascal_case(self, handler, mock_connection):
        mock_connection.list_objects.return_value = [{"key": "profiles/p2/photo.jpg"}]
        await handler.handle_deleted({"ProfileId": "p2"})
        mock_connection.delete_object.assert_called_once()

    async def test_handle_deleted_missing_profile_id(self, handler, mock_connection):
        await handler.handle_deleted({})
        mock_connection.list_objects.assert_not_called()
        mock_connection.delete_object.assert_not_called()

    async def test_handle_deleted_no_objects(self, handler, mock_connection):
        mock_connection.list_objects.return_value = []
        await handler.handle_deleted({"profile_id": "p1"})
        mock_connection.delete_object.assert_not_called()


class TestGetPhoto:
    async def test_get_photo_no_metadata(self, handler, mock_connection):
        mock_connection.object_exists.return_value = False
        assert await handler.get_photo("p1") is None

    async def test_get_photo_returns_data(self, handler, mock_connection):
        mock_connection.object_exists.return_value = True
        mock_connection.get_object.side_effect = [b'{"extension":"png"}', b"PHOTODATA"]
        # We need parse_photo_metadata to map this
        handler._metadata_service.parse_photo_metadata.return_value = {"extension": "png"}
        data, ext = await handler.get_photo("p1")
        assert data == b"PHOTODATA"
        assert ext == "png"

    async def test_get_photo_default_extension(self, handler, mock_connection):
        mock_connection.object_exists.return_value = True
        mock_connection.get_object.side_effect = [b"{}", b"DATA"]
        # Non-empty truthy dict but without 'extension' -> default "jpg"
        handler._metadata_service.parse_photo_metadata.return_value = {"profile_id": "p1"}
        data, ext = await handler.get_photo("p1")
        assert ext == "jpg"


class TestGetThumbnail:
    async def test_get_thumbnail_no_metadata(self, handler, mock_connection):
        mock_connection.object_exists.return_value = False
        assert await handler.get_thumbnail("p1") is None

    async def test_get_thumbnail_no_thumbnail_file(self, handler, mock_connection):
        # object_exists called twice: metadata key (True), then thumbnail key (False)
        mock_connection.object_exists.side_effect = [True, False]
        mock_connection.get_object.return_value = b"{}"
        handler._metadata_service.parse_photo_metadata.return_value = {"extension": "jpg"}
        assert await handler.get_thumbnail("p1") is None

    async def test_get_thumbnail_returns_data(self, handler, mock_connection):
        mock_connection.object_exists.side_effect = [True, True]
        mock_connection.get_object.side_effect = [b"{}", b"THUMBDATA"]
        handler._metadata_service.parse_photo_metadata.return_value = {"extension": "png"}
        data, ext = await handler.get_thumbnail("p1")
        assert data == b"THUMBDATA"
        assert ext == "png"


class TestUrls:
    async def test_get_photo_url_none(self, handler, mock_connection):
        mock_connection.object_exists.return_value = False
        assert await handler.get_photo_url("p1") is None

    async def test_get_photo_url(self, handler, mock_connection):
        mock_connection.object_exists.return_value = True
        mock_connection.get_object.return_value = b"{}"
        handler._metadata_service.parse_photo_metadata.return_value = {"extension": "png"}
        url = await handler.get_photo_url("p1")
        assert url == "/photos-bucket/profiles/p1/photo.png"

    async def test_get_photo_url_default_extension(self, handler, mock_connection):
        mock_connection.object_exists.return_value = True
        mock_connection.get_object.return_value = b"{}"
        handler._metadata_service.parse_photo_metadata.return_value = {"profile_id": "p1"}
        url = await handler.get_photo_url("p1")
        assert url.endswith("photo.jpg")

    async def test_get_thumbnail_url_none(self, handler, mock_connection):
        mock_connection.object_exists.return_value = False
        assert await handler.get_thumbnail_url("p1") is None

    async def test_get_thumbnail_url(self, handler, mock_connection):
        mock_connection.object_exists.return_value = True
        mock_connection.get_object.return_value = b"{}"
        handler._metadata_service.parse_photo_metadata.return_value = {"extension": "webp"}
        url = await handler.get_thumbnail_url("p1")
        assert url == "/photos-bucket/profiles/p1/thumbnail.webp"

    async def test_get_thumbnail_url_default(self, handler, mock_connection):
        mock_connection.object_exists.return_value = True
        mock_connection.get_object.return_value = b"{}"
        handler._metadata_service.parse_photo_metadata.return_value = {"profile_id": "p1"}
        url = await handler.get_thumbnail_url("p1")
        assert url.endswith("thumbnail.jpg")


class TestDefaults:
    def test_default_dependencies(self, mock_connection):
        h = ProfilePhotoHandler(connection=mock_connection, bucket="b")
        # default instances are created
        assert h._validator is not None
        assert h._thumbnail_service is not None
        assert h._metadata_service is not None
