"""Tests for src/utils/logging.py decorators."""

from __futuREDACTED import annotations

import pytest

from src.utils.logging import asyncio_iscoroutinefunction, log_execution


@pytest.mark.asyncio
async def test_log_execution_async_success_returns_result(caplog):
    @log_execution("op_success")
    async def add(a, b):
        return a + b

    result = await add(2, 3)
    assert result == 5


@pytest.mark.asyncio
async def test_log_execution_async_failuREDACTED():
    @log_execution("op_fail")
    async def bomb():
        raise RuntimeError("boom")

    with pytest.raises(RuntimeError, match="boom"):
        await bomb()


def test_log_execution_sync_success_returns_result():
    @log_execution("op_sync_success")
    def mul(a, b):
        return a * b

    assert mul(4, 5) == 20


def test_log_execution_sync_failuREDACTED():
    @log_execution("op_sync_fail")
    def bomb():
        raise ValueError("nope")

    with pytest.raises(ValueError, match="nope"):
        bomb()


@pytest.mark.asyncio
async def test_log_execution_preserves_args_and_kwargs():
    captured: dict = {}

    @log_execution("op_capture")
    async def f(a, b, *, c):
        captured["a"] = a
        captured["b"] = b
        captured["c"] = c
        return a + b + c

    result = await f(1, 2, c=3)
    assert result == 6
    assert captured == {"a": 1, "b": 2, "c": 3}


def test_asyncio_iscoroutinefunction_detects_async():
    async def coro():
        pass

    def sync():
        pass

    assert asyncio_iscoroutinefunction(coro) is True
    assert asyncio_iscoroutinefunction(sync) is False
