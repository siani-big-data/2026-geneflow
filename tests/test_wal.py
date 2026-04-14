"""Tests for Write-Ahead Log module."""

import tempfile
from pathlib import Path

import pytest

from src.buffer.wal import WriteAheadLog


@pytest.fixture
def temp_dir() -> Path:
    with tempfile.TemporaryDirectory() as tmpdir:
        yield Path(tmpdir)


class TestWriteAheadLog:
    """Tests for WriteAheadLog class."""

    @pytest.mark.asyncio
    async def test_initialize_creates_directory(self, temp_dir: Path):
        wal_path = temp_dir / "wal"
        wal = WriteAheadLog(str(wal_path))

        await wal.initialize()

        assert wal_path.exists()
        assert wal_path.is_dir()

    @pytest.mark.asyncio
    async def test_write_creates_file(self, temp_dir: Path):
        wal = WriteAheadLog(str(temp_dir / "wal"))
        await wal.initialize()

        await wal.write("users", "2026-03-25", '{"test": 1}')

        wal_file = wal.path / "users_2026-03-25.wal"
        assert wal_file.exists()

    @pytest.mark.asyncio
    async def test_write_appends_to_file(self, temp_dir: Path):
        wal = WriteAheadLog(str(temp_dir / "wal"))
        await wal.initialize()

        await wal.write("users", "2026-03-25", '{"event": 1}')
        await wal.write("users", "2026-03-25", '{"event": 2}')

        wal_file = wal.path / "users_2026-03-25.wal"
        content = wal_file.read_text()
        assert '{"event": 1}' in content
        assert '{"event": 2}' in content

    @pytest.mark.asyncio
    async def test_clear_removes_file(self, temp_dir: Path):
        wal = WriteAheadLog(str(temp_dir / "wal"))
        await wal.initialize()

        await wal.write("users", "2026-03-25", '{"test": 1}')
        wal_file = wal.path / "users_2026-03-25.wal"
        assert wal_file.exists()

        await wal.clear("users", "2026-03-25")

        assert not wal_file.exists()

    @pytest.mark.asyncio
    async def test_clear_nonexistent_no_error(self, temp_dir: Path):
        wal = WriteAheadLog(str(temp_dir / "wal"))
        await wal.initialize()

        await wal.clear("nonexistent", "2026-03-25")

    @pytest.mark.asyncio
    async def test_recover_empty_directory(self, temp_dir: Path):
        wal = WriteAheadLog(str(temp_dir / "wal"))
        await wal.initialize()

        recovered = await wal.recover()

        assert recovered == {}

    @pytest.mark.asyncio
    async def test_recover_single_file(self, temp_dir: Path):
        wal = WriteAheadLog(str(temp_dir / "wal"))
        await wal.initialize()

        await wal.write("users", "2026-03-25", '{"event": 1}')
        await wal.write("users", "2026-03-25", '{"event": 2}')

        recovered = await wal.recover()

        assert len(recovered) == 1
        assert ("users", "2026-03-25") in recovered
        assert len(recovered[("users", "2026-03-25")]) == 2

    @pytest.mark.asyncio
    async def test_recover_multiple_files(self, temp_dir: Path):
        wal = WriteAheadLog(str(temp_dir / "wal"))
        await wal.initialize()

        await wal.write("users", "2026-03-25", '{"event": 1}')
        await wal.write("traces", "2026-03-25", '{"trace": 1}')
        await wal.write("users", "2026-03-26", '{"event": 2}')

        recovered = await wal.recover()

        assert len(recovered) == 3
        assert ("users", "2026-03-25") in recovered
        assert ("traces", "2026-03-25") in recovered
        assert ("users", "2026-03-26") in recovered

    @pytest.mark.asyncio
    async def test_recover_preserves_event_content(self, temp_dir: Path):
        wal = WriteAheadLog(str(temp_dir / "wal"))
        await wal.initialize()

        await wal.write("users", "2026-03-25", '{"eventId": "abc-123", "type": "Test"}')

        recovered = await wal.recover()

        lines = recovered[("users", "2026-03-25")]
        assert len(lines) == 1
        assert '{"eventId": "abc-123", "type": "Test"}' in lines[0]

    @pytest.mark.asyncio
    async def test_recover_ignores_malformed_filenames(self, temp_dir: Path):
        wal_path = temp_dir / "wal"
        wal_path.mkdir(parents=True)

        (wal_path / "malformed.wal").write_text('{"data": 1}\n')
        (wal_path / "users_2026-03-25.wal").write_text('{"valid": 1}\n')

        wal = WriteAheadLog(str(wal_path))

        recovered = await wal.recover()

        assert len(recovered) == 1
        assert ("users", "2026-03-25") in recovered

    @pytest.mark.asyncio
    async def test_recover_nonexistent_directory(self, temp_dir: Path):
        wal = WriteAheadLog(str(temp_dir / "nonexistent" / "wal"))

        recovered = await wal.recover()

        assert recovered == {}

    @pytest.mark.asyncio
    async def test_get_file_returns_correct_path(self, temp_dir: Path):
        wal = WriteAheadLog(str(temp_dir / "wal"))

        file_path = wal._get_file("users", "2026-03-25")

        assert file_path == wal.path / "users_2026-03-25.wal"

    @pytest.mark.asyncio
    async def test_different_categories_same_date(self, temp_dir: Path):
        wal = WriteAheadLog(str(temp_dir / "wal"))
        await wal.initialize()

        await wal.write("users", "2026-03-25", '{"user": 1}')
        await wal.write("traces", "2026-03-25", '{"trace": 1}')

        users_file = wal.path / "users_2026-03-25.wal"
        traces_file = wal.path / "traces_2026-03-25.wal"

        assert users_file.exists()
        assert traces_file.exists()
        assert '{"user": 1}' in users_file.read_text()
        assert '{"trace": 1}' in traces_file.read_text()

    @pytest.mark.asyncio
    async def test_same_category_different_dates(self, temp_dir: Path):
        wal = WriteAheadLog(str(temp_dir / "wal"))
        await wal.initialize()

        await wal.write("users", "2026-03-24", '{"day": 24}')
        await wal.write("users", "2026-03-25", '{"day": 25}')

        file_24 = wal.path / "users_2026-03-24.wal"
        file_25 = wal.path / "users_2026-03-25.wal"

        assert file_24.exists()
        assert file_25.exists()
        assert '{"day": 24}' in file_24.read_text()
        assert '{"day": 25}' in file_25.read_text()
