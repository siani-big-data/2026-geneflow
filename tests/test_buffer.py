import asyncio
from datetime import datetime
from pathlib import Path

import pytest

from src.buffer import EventBuffer


@pytest.mark.asyncio
async def test_add_event(buffer: EventBuffer):
    await buffer.add("users", datetime(2026, 3, 25), '{"test": 1}', "stream:users", "1-0")

    assert buffer.size == 1


@pytest.mark.asyncio
async def test_flush_on_max_size(buffer: EventBuffer):
    # buffer.max_size = 3 (from fixture)
    await buffer.add("users", datetime(2026, 3, 25), '{"e": 1}', "s", "1-0")
    await buffer.add("users", datetime(2026, 3, 25), '{"e": 2}', "s", "2-0")

    assert buffer.size == 2
    assert len(buffer._flushed) == 0

    # Third event triggers flush
    await buffer.add("users", datetime(2026, 3, 25), '{"e": 3}', "s", "3-0")

    assert buffer.size == 0
    assert len(buffer._flushed) == 1
    assert len(buffer._flushed[0]["lines"]) == 3


@pytest.mark.asyncio
async def test_flush_on_interval(temp_dir: Path):
    flushed = []

    async def callback(cat, date, lines, acks):
        flushed.append(lines)

    buffer = EventBuffer(
        flush_callback=callback,
        max_size=100,  # High, won't trigger
        flush_interval=0.1,  # 100ms
        wal_path=str(temp_dir / "wal"),
    )

    await buffer.start()
    await buffer.add("users", datetime(2026, 3, 25), '{"test": 1}', "s", "1-0")

    assert len(flushed) == 0

    # Wait for interval flush
    await asyncio.sleep(0.2)

    assert len(flushed) == 1
    await buffer.stop()


@pytest.mark.asyncio
async def test_wal_written(buffer: EventBuffer, temp_dir: Path):
    await buffer.add("users", datetime(2026, 3, 25), '{"test": 1}', "s", "1-0")

    wal_file = Path(buffer.wal_path) / "users_2026-03-25.wal"
    assert wal_file.exists()

    content = wal_file.read_text()
    assert '{"test": 1}' in content


@pytest.mark.asyncio
async def test_wal_cleared_after_flush(buffer: EventBuffer, temp_dir: Path):
    # Add enough to trigger flush
    await buffer.add("users", datetime(2026, 3, 25), '{"e": 1}', "s", "1-0")
    await buffer.add("users", datetime(2026, 3, 25), '{"e": 2}', "s", "2-0")
    await buffer.add("users", datetime(2026, 3, 25), '{"e": 3}', "s", "3-0")

    wal_file = Path(buffer.wal_path) / "users_2026-03-25.wal"
    assert not wal_file.exists()  # Should be deleted after flush


@pytest.mark.asyncio
async def test_wal_recovery(temp_dir: Path):
    wal_path = temp_dir / "wal"
    wal_path.mkdir(parents=True)

    # Create a WAL file manually
    wal_file = wal_path / "users_2026-03-25.wal"
    wal_file.write_text('{"recovered": 1}\n{"recovered": 2}\n')

    flushed = []

    async def callback(cat, date, lines, acks):
        flushed.append(lines)

    buffer = EventBuffer(
        flush_callback=callback,
        max_size=100,
        flush_interval=10.0,
        wal_path=str(wal_path),
    )

    await buffer.start()

    # Recovered events should be in buffer
    assert buffer.size == 2

    await buffer.stop()


@pytest.mark.asyncio
async def test_pending_acks_included(buffer: EventBuffer):
    await buffer.add("users", datetime(2026, 3, 25), '{"e": 1}', "stream:users", "1-0")
    await buffer.add("users", datetime(2026, 3, 25), '{"e": 2}', "stream:users", "2-0")
    await buffer.add("users", datetime(2026, 3, 25), '{"e": 3}', "stream:users", "3-0")

    assert len(buffer._flushed) == 1
    acks = buffer._flushed[0]["acks"]

    assert len(acks) == 3
    assert ("stream:users", "1-0") in acks
    assert ("stream:users", "2-0") in acks
    assert ("stream:users", "3-0") in acks


@pytest.mark.asyncio
async def test_groups_by_category_and_date(temp_dir: Path):
    flushed = []

    async def callback(cat, date, lines, acks):
        flushed.append({"category": cat, "lines": lines})

    # Use higher max_size to prevent auto-flush
    buffer = EventBuffer(
        flush_callback=callback,
        max_size=100,
        flush_interval=10.0,
        wal_path=str(temp_dir / "wal"),
    )
    await buffer.start()

    await buffer.add("users", datetime(2026, 3, 25), '{"e": 1}', "s", "1-0")
    await buffer.add("traces", datetime(2026, 3, 25), '{"e": 2}', "s", "2-0")
    await buffer.add("users", datetime(2026, 3, 26), '{"e": 3}', "s", "3-0")

    # 3 different groups, so 3 buffer entries
    assert len(buffer._buffer) == 3

    await buffer.stop()
