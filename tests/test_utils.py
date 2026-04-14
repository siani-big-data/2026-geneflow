"""Tests for utility modules."""

import tempfile
from datetime import datetime
from pathlib import Path

import pytest

from src.utils.dates import format_date, format_datetime, parse_date, parse_datetime
from src.utils.files import (
    append_jsonl_file,
    read_jsonl_file,
    read_lines,
    safe_unlink,
    write_lines,
)


class TestParseDateFunction:
    """Tests for parse_date function."""

    def test_parse_valid_date_string(self):
        result = parse_date("2026-03-25")

        assert result == datetime(2026, 3, 25)

    def test_parse_datetime_passthrough(self):
        dt = datetime(2026, 3, 25, 10, 30)

        result = parse_date(dt)

        assert result == dt

    def test_parse_none_returns_none(self):
        result = parse_date(None)

        assert result is None

    def test_parse_invalid_format_returns_none(self):
        result = parse_date("25-03-2026")

        assert result is None

    def test_parse_empty_string_returns_none(self):
        result = parse_date("")

        assert result is None


class TestParseDatetimeFunction:
    """Tests for parse_datetime function."""

    def test_parse_iso_with_z_suffix(self):
        result = parse_datetime("2026-03-25T10:30:00Z")

        assert result is not None
        assert result.year == 2026
        assert result.month == 3
        assert result.day == 25
        assert result.hour == 10
        assert result.minute == 30

    def test_parse_iso_with_timezone_offset(self):
        result = parse_datetime("2026-03-25T10:30:00+00:00")

        assert result is not None
        assert result.hour == 10

    def test_parse_iso_with_milliseconds_z(self):
        result = parse_datetime("2026-03-25T10:30:00.123Z")

        assert result is not None
        assert result.microsecond == 123000

    def test_parse_datetime_passthrough(self):
        dt = datetime(2026, 3, 25, 10, 30)

        result = parse_datetime(dt)

        assert result == dt

    def test_parse_none_returns_none(self):
        result = parse_datetime(None)

        assert result is None

    def test_parse_invalid_returns_none(self):
        result = parse_datetime("not a date")

        assert result is None


class TestFormatDateFunction:
    """Tests for format_date function."""

    def test_format_datetime_to_date_string(self):
        dt = datetime(2026, 3, 25, 10, 30)

        result = format_date(dt)

        assert result == "2026-03-25"

    def test_format_none_returns_none(self):
        result = format_date(None)

        assert result is None


class TestFormatDatetimeFunction:
    """Tests for format_datetime function."""

    def test_format_to_iso_with_z(self):
        dt = datetime(2026, 3, 25, 10, 30, 0, 123000)

        result = format_datetime(dt)

        assert result == "2026-03-25T10:30:00.123000Z"

    def test_format_none_returns_none(self):
        result = format_datetime(None)

        assert result is None


class TestReadJsonlFile:
    """Tests for read_jsonl_file function."""

    @pytest.mark.asyncio
    async def test_read_existing_jsonl(self, temp_dir: Path):
        file_path = temp_dir / "test.jsonl"
        file_path.write_text('{"a": 1}\n{"b": 2}\n')

        result = await read_jsonl_file(file_path)

        assert len(result) == 2
        assert result[0] == {"a": 1}
        assert result[1] == {"b": 2}

    @pytest.mark.asyncio
    async def test_read_nonexistent_returns_empty(self, temp_dir: Path):
        file_path = temp_dir / "nonexistent.jsonl"

        result = await read_jsonl_file(file_path)

        assert result == []

    @pytest.mark.asyncio
    async def test_skip_invalid_json_lines(self, temp_dir: Path):
        file_path = temp_dir / "mixed.jsonl"
        file_path.write_text('{"valid": 1}\nnot json\n{"valid": 2}\n')

        result = await read_jsonl_file(file_path)

        assert len(result) == 2
        assert result[0] == {"valid": 1}
        assert result[1] == {"valid": 2}

    @pytest.mark.asyncio
    async def test_skip_empty_lines(self, temp_dir: Path):
        file_path = temp_dir / "sparse.jsonl"
        file_path.write_text('{"a": 1}\n\n\n{"b": 2}\n')

        result = await read_jsonl_file(file_path)

        assert len(result) == 2


class TestAppendJsonlFile:
    """Tests for append_jsonl_file function."""

    @pytest.mark.asyncio
    async def test_append_creates_file(self, temp_dir: Path):
        file_path = temp_dir / "new.jsonl"

        await append_jsonl_file(file_path, {"test": 1})

        assert file_path.exists()
        content = file_path.read_text()
        assert '{"test": 1}' in content

    @pytest.mark.asyncio
    async def test_append_to_existing(self, temp_dir: Path):
        file_path = temp_dir / "existing.jsonl"
        file_path.write_text('{"first": 1}\n')

        await append_jsonl_file(file_path, {"second": 2})

        content = file_path.read_text()
        assert '{"first": 1}' in content
        assert '{"second": 2}' in content

    @pytest.mark.asyncio
    async def test_creates_parent_directories(self, temp_dir: Path):
        file_path = temp_dir / "nested" / "dir" / "file.jsonl"

        await append_jsonl_file(file_path, {"nested": True})

        assert file_path.exists()


class TestWriteLines:
    """Tests for write_lines function."""

    @pytest.mark.asyncio
    async def test_write_multiple_lines(self, temp_dir: Path):
        file_path = temp_dir / "lines.txt"

        await write_lines(file_path, ["line1", "line2", "line3"])

        content = file_path.read_text()
        assert "line1\n" in content
        assert "line2\n" in content
        assert "line3\n" in content

    @pytest.mark.asyncio
    async def test_append_mode(self, temp_dir: Path):
        file_path = temp_dir / "append.txt"
        file_path.write_text("existing\n")

        await write_lines(file_path, ["new"])

        content = file_path.read_text()
        assert "existing" in content
        assert "new" in content


class TestReadLines:
    """Tests for read_lines function."""

    @pytest.mark.asyncio
    async def test_read_lines(self, temp_dir: Path):
        file_path = temp_dir / "read.txt"
        file_path.write_text("line1\nline2\nline3\n")

        result = await read_lines(file_path)

        assert result == ["line1", "line2", "line3"]

    @pytest.mark.asyncio
    async def test_skip_empty_lines(self, temp_dir: Path):
        file_path = temp_dir / "sparse.txt"
        file_path.write_text("line1\n\nline2\n  \n")

        result = await read_lines(file_path)

        assert len(result) == 2

    @pytest.mark.asyncio
    async def test_nonexistent_returns_empty(self, temp_dir: Path):
        file_path = temp_dir / "nonexistent.txt"

        result = await read_lines(file_path)

        assert result == []


class TestSafeUnlink:
    """Tests for safe_unlink function."""

    def test_delete_existing_file(self, temp_dir: Path):
        file_path = temp_dir / "to_delete.txt"
        file_path.write_text("content")
        assert file_path.exists()

        safe_unlink(file_path)

        assert not file_path.exists()

    def test_delete_nonexistent_no_error(self, temp_dir: Path):
        file_path = temp_dir / "nonexistent.txt"

        safe_unlink(file_path)

        assert not file_path.exists()


@pytest.fixture
def temp_dir() -> Path:
    with tempfile.TemporaryDirectory() as tmpdir:
        yield Path(tmpdir)
