"""BLAST data models."""

from dataclasses import dataclass, field
from datetime import datetime, timezone
from typing import Any, Optional

from src.models import BlastHit


@dataclass
class BlastJob:
    """A BLAST search job."""

    rid: str  # Request ID
    status: str = "WAITING"  # WAITING, READY, FAILED
    submittedAt: datetime = field(default_factory=lambda: datetime.now(timezone.utc))
    program: str = "blastn"
    database: str = "nt"

    def to_dict(self) -> dict[str, Any]:
        """Serialize to dictionary."""
        return {
            "rid": self.rid,
            "status": self.status,
            "submittedAt": self.submittedAt.isoformat(),
            "program": self.program,
            "database": self.database,
        }


@dataclass
class BlastSearchResult:
    """Result of a BLAST search."""

    rid: str
    hits: list[BlastHit] = field(default_factory=list)
    queryLength: int = 0
    database: str = "nt"
    program: str = "blastn"
    completedAt: datetime = field(default_factory=lambda: datetime.now(timezone.utc))
    searchTimeSeconds: float = 0.0

    @property
    def hitCount(self) -> int:
        """Get number of hits."""
        return len(self.hits)

    @property
    def topHit(self) -> Optional[BlastHit]:
        """Get top hit if any."""
        return self.hits[0] if self.hits else None

    def to_dict(self) -> dict[str, Any]:
        """Serialize to dictionary."""
        return {
            "rid": self.rid,
            "hits": [h.to_dict() for h in self.hits],
            "hitCount": self.hitCount,
            "queryLength": self.queryLength,
            "database": self.database,
            "program": self.program,
            "completedAt": self.completedAt.isoformat(),
            "searchTimeSeconds": self.searchTimeSeconds,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "BlastSearchResult":
        """Create from dictionary."""
        result = cls(
            rid=data.get("rid", ""),
            queryLength=data.get("queryLength", 0),
            database=data.get("database", "nt"),
            program=data.get("program", "blastn"),
            searchTimeSeconds=data.get("searchTimeSeconds", 0.0),
        )

        if data.get("hits"):
            result.hits = [BlastHit.from_dict(h) for h in data["hits"]]

        return result
