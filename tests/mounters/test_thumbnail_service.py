"""Tests for ThumbnailService."""

import sys
from io import BytesIO

from src.mounters.storage.services.thumbnail_service import ThumbnailService


def _make_image_bytes(format_name: str = "JPEG", size=(200, 200), mode="RGB") -> bytes:
    """Create real image bytes using PIL."""
    from PIL import Image

    img = Image.new(mode, size, color="red")
    buf = BytesIO()
    img.save(buf, format=format_name)
    return buf.getvalue()


class TestThumbnailService:
    async def test_generate_jpeg(self):
        svc = ThumbnailService(size=(50, 50), quality=80)
        photo = _make_image_bytes("JPEG")
        thumb = await svc.generate(photo, "jpg")
        assert thumb is not None
        assert isinstance(thumb, bytes)
        assert len(thumb) > 0

    async def test_generate_png(self):
        svc = ThumbnailService(size=(50, 50))
        photo = _make_image_bytes("PNG")
        thumb = await svc.generate(photo, "png")
        assert thumb is not None

    async def test_generate_unknown_extension_defaults_to_jpeg(self):
        svc = ThumbnailService(size=(50, 50))
        photo = _make_image_bytes("JPEG")
        thumb = await svc.generate(photo, "tiff")  # not in FORMAT_MAP -> JPEG default
        assert thumb is not None

    async def test_generate_uppercase_extension(self):
        svc = ThumbnailService(size=(50, 50))
        photo = _make_image_bytes("JPEG")
        thumb = await svc.generate(photo, "JPEG")
        assert thumb is not None

    async def test_generate_rgba_mode_converts(self):
        svc = ThumbnailService(size=(50, 50))
        # RGBA mode triggers convert("RGB")
        photo = _make_image_bytes("PNG", mode="RGBA")
        thumb = await svc.generate(photo, "png")
        assert thumb is not None

    async def test_generate_palette_mode_converts(self):
        from PIL import Image

        # P (palette) mode triggers convert("RGB")
        img = Image.new("P", (100, 100))
        buf = BytesIO()
        img.save(buf, format="PNG")
        svc = ThumbnailService(size=(50, 50))
        thumb = await svc.generate(buf.getvalue(), "png")
        assert thumb is not None

    async def test_generate_invalid_data_returns_none(self):
        svc = ThumbnailService()
        thumb = await svc.generate(b"not an image", "jpg")
        assert thumb is None

    async def test_generate_empty_data_returns_none(self):
        svc = ThumbnailService()
        assert await svc.generate(b"", "jpg") is None

    async def test_generate_pillow_not_installed(self, monkeypatch):
        """Simulate ImportError when PIL is not installed."""
        # Block PIL import to force the ImportError branch
        real_import = (
            __builtins__["__import__"]
            if isinstance(__builtins__, dict)
            else __builtins__.__import__
        )

        def blocked(name, *args, **kwargs):
            if name == "PIL" or name.startswith("PIL."):
                raise ImportError("No module named 'PIL'")
            return real_import(name, *args, **kwargs)

        monkeypatch.setattr("builtins.__import__", blocked)
        # Also remove cached PIL
        for mod in list(sys.modules):
            if mod == "PIL" or mod.startswith("PIL."):
                monkeypatch.delitem(sys.modules, mod, raising=False)

        svc = ThumbnailService()
        result = await svc.generate(b"x", "jpg")
        assert result is None

    def test_init_defaults(self):
        svc = ThumbnailService()
        assert svc._size == (150, 150)
        assert svc._quality == 85

    def test_init_custom(self):
        svc = ThumbnailService(size=(64, 64), quality=70)
        assert svc._size == (64, 64)
        assert svc._quality == 70
