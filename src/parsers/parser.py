"""Base parser interface for trace files."""

from abc import ABC, abstractmethod
from pathlib import Path

from src.models import ParsedTrace, TraceFormat


class BaseParser(ABC):
    """Abstract base class for trace file parsers."""

    @property
    @abstractmethod
    def format(self) -> TraceFormat:
        """The trace format this parser handles."""
        pass

    @property
    @abstractmethod
    def extensions(self) -> list[str]:
        """File extensions this parser can handle."""
        pass

    @abstractmethod
    def parse(self, data: bytes, trace_id: str) -> ParsedTrace:
        """
        Parse trace file data.

        Args:
            data: Raw file bytes
            trace_id: Unique identifier for the trace

        Returns:
            ParsedTrace with sequence and optional chromatogram data
        """
        pass

    def parse_file(self, file_path: Path | str, trace_id: str) -> ParsedTrace:
        """
        Parse a trace file from disk.

        Args:
            file_path: Path to the trace file
            trace_id: Unique identifier for the trace

        Returns:
            ParsedTrace with sequence and optional chromatogram data
        """
        path = Path(file_path)
        if not path.exists():
            raise FileNotFoundError(f"File not found: {path}")

        data = path.read_bytes()
        return self.parse(data, trace_id)

    def can_parse(self, filename: str) -> bool:
        """Check if this parser can handle the given filename."""
        ext = Path(filename).suffix.lower().lstrip(".")
        return ext in self.extensions
