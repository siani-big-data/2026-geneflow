"""Tests for PhotoValidator."""

from src.mounters.storage.validators.photo_validator import PhotoValidator


class TestPhotoValidator:
    def test_validate_valid_photo(self):
        v = PhotoValidator(max_size=1000, allowed_extensions=frozenset({"jpg", "png"}))
        assert v.validate(b"x" * 500, "jpg", "p1") is True

    def test_validate_uppercase_extension(self):
        v = PhotoValidator(max_size=1000, allowed_extensions=frozenset({"jpg"}))
        assert v.validate(b"x", "JPG", "p1") is True

    def test_validate_with_leading_dot(self):
        v = PhotoValidator(max_size=1000, allowed_extensions=frozenset({"jpg"}))
        assert v.validate(b"x", ".jpg", "p1") is True

    def test_validate_too_large(self):
        v = PhotoValidator(max_size=10, allowed_extensions=frozenset({"jpg"}))
        assert v.validate(b"x" * 100, "jpg", "p1") is False

    def test_validate_unsupported_extension(self):
        v = PhotoValidator(max_size=1000, allowed_extensions=frozenset({"jpg"}))
        assert v.validate(b"x", "bmp", "p1") is False

    def test_validate_uses_defaults(self):
        v = PhotoValidator()
        assert v.validate(b"x", "jpg", "p1") is True
        assert v.validate(b"x", "exe", "p1") is False

    def test_normalize_extension_strips_dot_and_lowercases(self):
        v = PhotoValidator()
        assert v.normalize_extension(".JPG") == "jpg"
        assert v.normalize_extension("PNG") == "png"
        assert v.normalize_extension("jpeg") == "jpeg"

    def test_validate_empty_data(self):
        v = PhotoValidator()
        # empty data, valid extension - passes
        assert v.validate(b"", "jpg", "p1") is True

    def test_validate_at_boundary_size(self):
        v = PhotoValidator(max_size=10, allowed_extensions=frozenset({"jpg"}))
        # boundary: exactly equal -> still valid (not > max_size)
        assert v.validate(b"x" * 10, "jpg", "p1") is True
        assert v.validate(b"x" * 11, "jpg", "p1") is False
