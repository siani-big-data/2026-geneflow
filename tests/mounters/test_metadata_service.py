"""Tests for MetadataService."""

import json

from src.mounters.storage.services.metadata_service import MetadataService


class TestMetadataService:
    def test_create_photo_metadata_returns_bytes(self):
        svc = MetadataService()
        data = svc.create_photo_metadata(
            profile_id="p1",
            extension="jpg",
            content_type="image/jpeg",
            photo_size=1234,
            thumbnail_size=56,
        )
        assert isinstance(data, bytes)
        parsed = json.loads(data.decode())
        assert parsed["profile_id"] == "p1"
        assert parsed["extension"] == "jpg"
        assert parsed["content_type"] == "image/jpeg"
        assert parsed["size_bytes"] == 1234
        assert parsed["thumbnail_size_bytes"] == 56
        assert parsed["uploaded_at"].endswith("Z")

    def test_parse_photo_metadata_roundtrip(self):
        svc = MetadataService()
        encoded = svc.create_photo_metadata(
            profile_id="p2",
            extension="png",
            content_type="image/png",
            photo_size=10,
            thumbnail_size=0,
        )
        parsed = svc.parse_photo_metadata(encoded)
        assert parsed["profile_id"] == "p2"
        assert parsed["extension"] == "png"
        assert parsed["thumbnail_size_bytes"] == 0

    def test_parse_photo_metadata_arbitrary_json(self):
        svc = MetadataService()
        raw = json.dumps({"foo": "bar"}).encode()
        assert svc.parse_photo_metadata(raw) == {"foo": "bar"}
