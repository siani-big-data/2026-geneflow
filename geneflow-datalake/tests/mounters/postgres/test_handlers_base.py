"""Tests for BaseHandler."""

from unittest.mock import AsyncMock, MagicMock

import pytest

from src.mounters.postgres.handlers.base import BaseHandler


class DummyHandler(BaseHandler):
    def __init__(self, connection):
        super().__init__(connection)
        self._event_mappings = {"Foo": "handle_foo"}
        self.called_with = None

    async def handle_foo(self, payload):
        self.called_with = payload


@pytest.fixture
def conn():
    c = MagicMock()
    c.execute = AsyncMock()
    return c


class TestBaseHandler:
    async def test_handle_known_event(self, conn):
        h = DummyHandler(conn)
        await h.handle("Foo", {"x": 1})
        assert h.called_with == {"x": 1}

    async def test_handle_unknown_event(self, conn):
        h = DummyHandler(conn)
        await h.handle("Unknown", {})
        assert h.called_with is None

    async def test_handle_event_mapping_method_missing(self, conn):
        h = DummyHandler(conn)
        h._event_mappings["Bar"] = "handle_bar_nonexistent"
        # Should not raise even though method does not exist
        await h.handle("Bar", {})
        assert h.called_with is None
