"""Mounter engine for managing multiple mounters."""

import json
import structlog
from datetime import datetime
from pathlib import Path
from typing import Any

from src.mounters.base import BaseMounter, MounterMode

logger = structlog.get_logger()


class MounterEngine:
    """Engine for managing and coordinating multiple mounters."""

    def __init__(self, datalake_path: str | None = None):
        self._mounters: list[BaseMounter] = []
        self._datalake_path = Path(datalake_path) if datalake_path else None

    @property
    def mounters(self) -> list[BaseMounter]:
        """Get all registered mounters."""
        return self._mounters

    def register(self, mounter: BaseMounter) -> None:
        """Register a mounter with the engine."""
        self._mounters.append(mounter)
        logger.info("mounter_registered", name=mounter.name, categories=mounter.categories)

    def get_mounter(self, name: str) -> BaseMounter | None:
        """Get a mounter by name."""
        for mounter in self._mounters:
            if mounter.name == name:
                return mounter
        return None

    async def start(self) -> None:
        """Start all registered mounters."""
        for mounter in self._mounters:
            await mounter.start()
            logger.info("mounter_started", name=mounter.name)

    async def stop(self) -> None:
        """Stop all registered mounters."""
        for mounter in self._mounters:
            await mounter.stop()
            logger.info("mounter_stopped", name=mounter.name)

    async def run(
        self,
        mode: MounterMode = MounterMode.REPLAY,
        from_date: datetime | None = None,
        to_date: datetime | None = None,
        categories: list[str] | None = None,
    ) -> dict[str, Any]:
        """Run the engine in the specified mode."""
        result = {
            "mode": mode.value,
            "events_processed": 0,
            "events_failed": 0,
        }

        await self.start()

        if mode == MounterMode.REBUILD:
            for mounter in self._mounters:
                await mounter.rebuild()

        if mode in (MounterMode.REPLAY, MounterMode.REBUILD):
            events_result = await self._replay_events(from_date, to_date, categories)
            result["events_processed"] = events_result["processed"]
            result["events_failed"] = events_result["failed"]

        return result

    async def _replay_events(
        self,
        from_date: datetime | None,
        to_date: datetime | None,
        categories: list[str] | None,
    ) -> dict[str, int]:
        """Replay events from the datalake."""
        processed = 0
        failed = 0

        if not self._datalake_path:
            return {"processed": processed, "failed": failed}

        events_path = self._datalake_path / "events"
        if not events_path.exists():
            return {"processed": processed, "failed": failed}

        for category_dir in events_path.iterdir():
            if not category_dir.is_dir():
                continue

            category = category_dir.name

            if categories and category not in categories:
                continue

            for event_file in sorted(category_dir.glob("*.jsonl")):
                file_date = self._parse_date_from_filename(event_file.name)

                if file_date:
                    if from_date and file_date < from_date.date():
                        continue
                    if to_date and file_date > to_date.date():
                        continue

                with open(event_file, "r") as f:
                    for line in f:
                        try:
                            event = json.loads(line.strip())
                            await self._dispatch_event(event)
                            processed += 1
                        except Exception as e:
                            logger.error("event_replay_failed", error=str(e))
                            failed += 1

        return {"processed": processed, "failed": failed}

    async def _dispatch_event(self, event: dict) -> None:
        """Dispatch an event to all relevant mounters."""
        category = event.get("category", "")

        for mounter in self._mounters:
            if mounter.handles_category(category):
                await mounter.handle_event(event)

    async def health_check(self) -> dict[str, bool]:
        """Check health of all mounters."""
        health = {}
        for mounter in self._mounters:
            health[mounter.name] = await mounter.health_check()
        return health

    def get_metrics(self) -> dict[str, dict]:
        """Get metrics from all mounters."""
        return {mounter.name: mounter.get_metrics() for mounter in self._mounters}

    def _parse_date_from_filename(self, filename: str) -> datetime | None:
        """Parse date from filename like 2026-03-25.jsonl."""
        try:
            date_str = filename.replace(".jsonl", "")
            return datetime.strptime(date_str, "%Y-%m-%d").date()
        except ValueError:
            return None
