"""AB1 (Applied Biosystems) trace file parser."""

import struct
from typing import Any

from src.models import (
    ChromatogramData,
    ParsedTrace,
    QualityMetrics,
    Sequence,
    TraceFormat,
)
from src.parsers.parser import BaseParser
from src.constants import gc_content


class AB1Parser(BaseParser):
    """Parser for AB1/ABI trace files."""

    # AB1 file magic number
    MAGIC = b"ABIF"

    # Directory entry size in bytes
    DIR_ENTRY_SIZE = 28

    # Important tag names
    TAGS = {
        "PBAS": 1,   # Called bases (edited)
        "PCON": 1,   # Quality values
        "DATA": 9,   # Trace data A (9), C (10), G (11), T (12)
        "PLOC": 2,   # Peak locations
        "SMPL": 1,   # Sample name
        "TUBE": 1,   # Tube/well
    }

    @property
    def format(self) -> TraceFormat:
        return TraceFormat.AB1

    @property
    def extensions(self) -> list[str]:
        return ["ab1", "abi"]

    def parse(self, data: bytes, trace_id: str) -> ParsedTrace:
        """Parse AB1 file data."""
        if len(data) < 128:
            raise ValueError("File too small to be valid AB1")

        # Check magic number
        if data[:4] != self.MAGIC:
            raise ValueError(f"Invalid AB1 file: expected {self.MAGIC}, got {data[:4]}")

        # Parse header
        header = self._parse_header(data)

        # Read directory entries
        entries = self._read_directory(data, header)

        # Extract sequence
        sequence_str = self._get_string(data, entries, "PBAS", 1)
        if not sequence_str:
            # Try alternative tag
            sequence_str = self._get_string(data, entries, "PBAS", 2)

        if not sequence_str:
            raise ValueError("No sequence data found in AB1 file")

        # Extract quality scores
        quality = self._get_bytes_as_ints(data, entries, "PCON", 1)
        if not quality:
            quality = self._get_bytes_as_ints(data, entries, "PCON", 2)

        # Create sequence object
        sequence = Sequence(
            id=trace_id,
            sequence=sequence_str,
            quality=quality,
            name=self._get_string(data, entries, "SMPL", 1),
        )

        # Extract chromatogram data
        chromatogram = self._extract_chromatogram(data, entries)

        # Calculate quality metrics
        quality_metrics = self._calculate_metrics(sequence_str, quality)

        # Extract metadata
        metadata = self._extract_metadata(data, entries)

        return ParsedTrace(
            traceId=trace_id,
            format=TraceFormat.AB1,
            sequence=sequence,
            chromatogram=chromatogram,
            qualityMetrics=quality_metrics,
            metadata=metadata,
        )

    def _parse_header(self, data: bytes) -> dict[str, Any]:
        """Parse AB1 file header."""
        # Header format (big-endian):
        # 0-3: Magic "ABIF"
        # 4-5: Version
        # 6-25: Directory entry for root
        # 26-127: Reserved

        version = struct.unpack(">H", data[4:6])[0]

        # Root directory entry
        dir_entry = self._parse_dir_entry(data[6:34])

        return {
            "version": version,
            "numEntries": dir_entry["numElements"],
            "dataOffset": dir_entry["dataOffset"],
        }

    def _parse_dir_entry(self, entry_data: bytes) -> dict[str, Any]:
        """Parse a single directory entry (28 bytes)."""
        if len(entry_data) < 28:
            raise ValueError("Directory entry too small")

        tag_name = entry_data[0:4].decode("ascii", errors="replace")
        tag_number = struct.unpack(">I", entry_data[4:8])[0]
        element_type = struct.unpack(">H", entry_data[8:10])[0]
        element_size = struct.unpack(">H", entry_data[10:12])[0]
        num_elements = struct.unpack(">I", entry_data[12:16])[0]
        data_size = struct.unpack(">I", entry_data[16:20])[0]
        data_offset = struct.unpack(">I", entry_data[20:24])[0]

        # If data fits in 4 bytes, it's stored in dataOffset field
        if data_size <= 4:
            data_offset = 20  # Relative offset within this entry

        return {
            "tagName": tag_name,
            "tagNumber": tag_number,
            "elementType": element_type,
            "elementSize": element_size,
            "numElements": num_elements,
            "dataSize": data_size,
            "dataOffset": data_offset,
        }

    def _read_directory(self, data: bytes, header: dict) -> dict[tuple[str, int], dict]:
        """Read all directory entries."""
        entries = {}
        offset = header["dataOffset"]
        num_entries = header["numEntries"]

        for i in range(num_entries):
            entry_start = offset + (i * self.DIR_ENTRY_SIZE)
            entry_data = data[entry_start : entry_start + self.DIR_ENTRY_SIZE]

            if len(entry_data) < self.DIR_ENTRY_SIZE:
                break

            entry = self._parse_dir_entry(entry_data)

            # Adjust offset for inline data
            if entry["dataSize"] <= 4:
                entry["dataOffset"] = entry_start + 20

            key = (entry["tagName"], entry["tagNumber"])
            entries[key] = entry

        return entries

    def _get_data(
        self, data: bytes, entries: dict, tag_name: str, tag_number: int
    ) -> bytes | None:
        """Get raw data for a tag."""
        key = (tag_name, tag_number)
        if key not in entries:
            return None

        entry = entries[key]
        offset = entry["dataOffset"]
        size = entry["dataSize"]

        return data[offset : offset + size]

    def _get_string(
        self, data: bytes, entries: dict, tag_name: str, tag_number: int
    ) -> str | None:
        """Get string data for a tag."""
        raw = self._get_data(data, entries, tag_name, tag_number)
        if raw is None:
            return None

        # Remove null terminator if present
        if raw and raw[-1] == 0:
            raw = raw[:-1]

        return raw.decode("ascii", errors="replace")

    def _get_bytes_as_ints(
        self, data: bytes, entries: dict, tag_name: str, tag_number: int
    ) -> list[int] | None:
        """Get byte data as list of integers."""
        raw = self._get_data(data, entries, tag_name, tag_number)
        if raw is None:
            return None
        return list(raw)

    def _get_shorts(
        self, data: bytes, entries: dict, tag_name: str, tag_number: int
    ) -> list[int] | None:
        """Get short (2-byte) data as list of integers."""
        raw = self._get_data(data, entries, tag_name, tag_number)
        if raw is None:
            return None

        shorts = []
        for i in range(0, len(raw), 2):
            if i + 2 <= len(raw):
                value = struct.unpack(">H", raw[i : i + 2])[0]
                shorts.append(value)

        return shorts

    def _extract_chromatogram(
        self, data: bytes, entries: dict
    ) -> ChromatogramData | None:
        """Extract chromatogram trace data."""
        # DATA tags: 9=G, 10=A, 11=T, 12=C (channel order varies)
        # Standard order is 9=A, 10=C, 11=G, 12=T but this can vary
        trace_data = {}

        for tag_num in [9, 10, 11, 12]:
            trace = self._get_shorts(data, entries, "DATA", tag_num)
            if trace:
                trace_data[tag_num] = trace

        if len(trace_data) < 4:
            return None

        # Get peak locations
        peak_locs = self._get_shorts(data, entries, "PLOC", 2)
        if not peak_locs:
            peak_locs = self._get_shorts(data, entries, "PLOC", 1)

        # Get base calls (positions)
        base_calls = list(range(len(peak_locs))) if peak_locs else []

        # Standard mapping (may need adjustment based on FWO_ tag)
        return ChromatogramData(
            traceA=trace_data.get(10, []),
            traceC=trace_data.get(12, []),
            traceG=trace_data.get(9, []),
            traceT=trace_data.get(11, []),
            baseCalls=base_calls,
            peakLocations=peak_locs or [],
        )

    def _calculate_metrics(
        self, sequence: str, quality: list[int] | None
    ) -> QualityMetrics | None:
        """Calculate quality metrics from sequence and quality scores."""
        if not quality:
            return QualityMetrics(
                meanQuality=0.0,
                q20Percentage=0.0,
                q30Percentage=0.0,
                gcContent=gc_content(sequence),
                length=len(sequence),
                ambiguousCount=sum(1 for b in sequence if b not in "ACGT"),
            )

        mean_q = sum(quality) / len(quality) if quality else 0.0
        q20_count = sum(1 for q in quality if q >= 20)
        q30_count = sum(1 for q in quality if q >= 30)

        return QualityMetrics(
            meanQuality=round(mean_q, 2),
            q20Percentage=round((q20_count / len(quality)) * 100, 2) if quality else 0.0,
            q30Percentage=round((q30_count / len(quality)) * 100, 2) if quality else 0.0,
            gcContent=round(gc_content(sequence), 2),
            length=len(sequence),
            ambiguousCount=sum(1 for b in sequence if b not in "ACGT"),
        )

    def _extract_metadata(self, data: bytes, entries: dict) -> dict[str, Any]:
        """Extract additional metadata from AB1 file."""
        metadata = {}

        # Sample name
        sample = self._get_string(data, entries, "SMPL", 1)
        if sample:
            metadata["sampleName"] = sample

        # Tube/well
        tube = self._get_string(data, entries, "TUBE", 1)
        if tube:
            metadata["tube"] = tube

        # Run name
        run = self._get_string(data, entries, "RunN", 1)
        if run:
            metadata["runName"] = run

        return metadata
