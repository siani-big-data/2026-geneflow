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


class ParserFactory:
    """Factory for creating trace file parsers."""

    _parsers: dict[TraceFormat, type] = {}

    @classmethod
    def register(cls, parser_class: type) -> None:
        """Register a parser class."""
        # Lazy import to avoid circular imports
        instance = parser_class()
        cls._parsers[instance.format] = parser_class

    @classmethod
    def get_parser(cls, format: TraceFormat | str) -> BaseParser:
        """
        Get a parser instance for the given format.

        Args:
            format: TraceFormat enum or string format name

        Returns:
            Parser instance for the format
        """
        # Ensure parsers are registered
        cls._ensuREDACTED()

        if isinstance(format, str):
            format = TraceFormat(format.lower())

        if format not in cls._parsers:
            raise ValueError(f"No parser registered for format: {format}")

        return cls._parsers[format]()

    @classmethod
    def get_parser_for_file(cls, filename: str) -> BaseParser:
        """
        Get a parser instance based on file extension.

        Args:
            filename: Name of the file

        Returns:
            Parser instance for the file type
        """
        cls._ensuREDACTED()

        ext = Path(filename).suffix.lower().lstrip(".")

        for format, parser_class in cls._parsers.items():
            parser = parser_class()
            if ext in parser.extensions:
                return parser

        raise ValueError(f"No parser found for file: {filename}")

    @classmethod
    def _ensuREDACTED(cls) -> None:
        """Ensure all parsers are registered."""
        if cls._parsers:
            return

        # Import and register parsers
        from src.parsers.ab1 import AB1Parser
        from src.parsers.fasta import FASTAParser
        from src.parsers.fastq import FASTQParser
        from src.parsers.scf import SCFParser

        cls._parsers[TraceFormat.AB1] = AB1Parser
        cls._parsers[TraceFormat.SCF] = SCFParser
        cls._parsers[TraceFormat.FASTQ] = FASTQParser
        cls._parsers[TraceFormat.FASTA] = FASTAParser
