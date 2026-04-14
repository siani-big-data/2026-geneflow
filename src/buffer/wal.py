"""Write-Ahead Log for event durability."""

from pathlib import Path

import aiofiles
import structlog

from src.utils.files import read_lines, safe_unlink

logger = structlog.get_logger()


class WriteAheadLog:
    """Write-Ahead Log for pre-flush durability."""

    def __init__(self, path: str = "./data/wal"):
        self.path = Path(path)

    async def initialize(self) -> None:
        """Create WAL directory."""
        self.path.mkdir(parents=True, exist_ok=True)

    async def write(self, category: str, date_str: str, event_line: str) -> None:
        """Write event to WAL."""
        wal_file = self._get_file(category, date_str)
        async with aiofiles.open(wal_file, mode="a", encoding="utf-8") as f:
            await f.write(event_line + "\n")

    async def clear(self, category: str, date_str: str) -> None:
        """Delete WAL after successful flush."""
        safe_unlink(self._get_file(category, date_str))

    async def recover(self) -> dict[tuple[str, str], list[str]]:
        """Recover events from WAL on startup.

        Returns dict of (category, date_str) -> list of event lines.
        """
        if not self.path.exists():
            return {}

        recovered = {}
        for wal_file in self.path.glob("*.wal"):
            try:
                name = wal_file.stem
                parts = name.rsplit("_", 1)
                if len(parts) != 2:
                    continue

                category, date_str = parts
                lines = await read_lines(wal_file)

                if lines:
                    recovered[(category, date_str)] = lines
                    logger.debug("wal_file_recovered", file=str(wal_file), count=len(lines))

            except Exception as e:
                logger.error("wal_recovery_failed", file=str(wal_file), error=str(e))

        if recovered:
            total = sum(len(lines) for lines in recovered.values())
            logger.info("wal_recovered", total_events=total, files=len(recovered))

        return recovered

    def _get_file(self, category: str, date_str: str) -> Path:
        """Get WAL file path for category and date."""
        return self.path / f"{category}_{date_str}.wal"
