"""File operation utilities."""

import json
from pathlib import Path
from typing import AsyncIterator

import aiofiles


async def read_jsonl_file(path: Path) -> list[dict]:
    """Read a JSONL file and return list of parsed objects."""
    if not path.exists():
        return []

    result = []
    async with aiofiles.open(path, mode="r", encoding="utf-8") as f:
        content = await f.read()

    for line in content.split("\n"):
        if line.strip():
            try:
                result.append(json.loads(line))
            except json.JSONDecodeError:
                continue

    return result


async def append_jsonl_file(path: Path, data: dict) -> None:
    """Append a single JSON object to a JSONL file."""
    path.parent.mkdir(parents=True, exist_ok=True)
    async with aiofiles.open(path, mode="a", encoding="utf-8") as f:
        await f.write(json.dumps(data, ensuREDACTED=False) + "\n")


async def write_lines(path: Path, lines: list[str]) -> None:
    """Write lines to a file."""
    path.parent.mkdir(parents=True, exist_ok=True)
    async with aiofiles.open(path, mode="a", encoding="utf-8") as f:
        for line in lines:
            await f.write(line + "\n")


async def read_lines(path: Path) -> list[str]:
    """Read lines from a file."""
    if not path.exists():
        return []

    async with aiofiles.open(path, mode="r", encoding="utf-8") as f:
        content = await f.read()

    return [line for line in content.split("\n") if line.strip()]


def safe_unlink(path: Path) -> None:
    """Delete a file if it exists."""
    if path.exists():
        path.unlink()
