"""Motif search analyzer."""

import re
from dataclasses import dataclass

from src.analyzers.analyzer import BaseAnalyzer
from src.constants import reverse_complement
from src.models import MotifMatch, Sequence


@dataclass
class MotifResult:
    """Result of motif search."""

    pattern: str
    matches: list[MotifMatch]
    matchCount: int
    searchedBothStrands: bool


class MotifAnalyzer(BaseAnalyzer):
    """
    Analyzer for searching sequence motifs.

    Supports:
    - Exact pattern matching
    - IUPAC ambiguity codes
    - Regular expressions
    - Both strands search
    """

    IUPAC_REGEX = {
        "A": "A",
        "C": "C",
        "G": "G",
        "T": "T",
        "U": "U",
        "R": "[AG]",
        "Y": "[CT]",
        "S": "[GC]",
        "W": "[AT]",
        "K": "[GT]",
        "M": "[AC]",
        "B": "[CGT]",
        "D": "[AGT]",
        "H": "[ACT]",
        "V": "[ACG]",
        "N": "[ACGT]",
    }

    @property
    def name(self) -> str:
        return "motif"

    def analyze(
        self,
        sequence: Sequence,
        pattern: str = "",
        search_complement: bool = False,
        use_regex: bool = False,
        **_,
    ) -> MotifResult:
        """
        Search for a motif pattern in the sequence.

        Args:
            sequence: Sequence to search
            pattern: Pattern to search for (IUPAC codes or regex)
            search_complement: Also search reverse complement
            use_regex: Treat pattern as regex (skip IUPAC conversion)

        Returns:
            MotifResult with all matches
        """
        self.validate_sequence(sequence)

        if not pattern:
            raise ValueError("Pattern required for motif search")

        if use_regex:
            regex_pattern = pattern
        else:
            regex_pattern = self._iupac_to_regex(pattern)

        matches = []
        seq_str = sequence.sequence.upper()

        forward_matches = self._find_matches(seq_str, regex_pattern, pattern, "+")
        matches.extend(forward_matches)

        if search_complement:
            rev_comp = reverse_complement(seq_str)
            reverse_matches = self._find_matches(rev_comp, regex_pattern, pattern, "-")

            seq_len = len(seq_str)
            for match in reverse_matches:
                match.start = seq_len - match.end
                match.end = seq_len - (match.start + len(match.matchedSequence))

            matches.extend(reverse_matches)

        return MotifResult(
            pattern=pattern,
            matches=matches,
            matchCount=len(matches),
            searchedBothStrands=search_complement,
        )

    def _iupac_to_regex(self, pattern: str) -> str:
        """Convert IUPAC pattern to regex."""
        regex_parts = []

        for char in pattern.upper():
            if char in self.IUPAC_REGEX:
                regex_parts.append(self.IUPAC_REGEX[char])
            else:
                regex_parts.append(re.escape(char))

        return "".join(regex_parts)

    def _find_matches(
        self,
        sequence: str,
        regex_pattern: str,
        original_pattern: str,
        strand: str,
    ) -> list[MotifMatch]:
        """Find all pattern matches in sequence."""
        matches = []

        try:
            compiled = re.compile(regex_pattern, re.IGNORECASE)

            for match in compiled.finditer(sequence):
                matches.append(
                    MotifMatch(
                        pattern=original_pattern,
                        start=match.start(),
                        end=match.end(),
                        matchedSequence=match.group(),
                        strand=strand,
                    )
                )
        except re.error as e:
            raise ValueError(f"Invalid pattern: {e}")

        return matches

    def search_multiple(
        self,
        sequence: Sequence,
        patterns: list[str],
        search_complement: bool = False,
    ) -> dict[str, MotifResult]:
        """
        Search for multiple patterns.

        Args:
            sequence: Sequence to search
            patterns: List of patterns to search for
            search_complement: Also search reverse complement

        Returns:
            Dictionary mapping pattern to MotifResult
        """
        results = {}

        for pattern in patterns:
            results[pattern] = self.analyze(
                sequence,
                pattern=pattern,
                search_complement=search_complement,
            )

        return results

    def find_repeats(
        self,
        sequence: Sequence,
        min_unit_length: int = 2,
        max_unit_length: int = 6,
        min_repeats: int = 3,
    ) -> list[dict]:
        """
        Find tandem repeats in sequence.

        Args:
            sequence: Sequence to search
            min_unit_length: Minimum repeat unit length
            max_unit_length: Maximum repeat unit length
            min_repeats: Minimum number of consecutive repeats

        Returns:
            List of repeat regions found
        """
        self.validate_sequence(sequence)

        seq_str = sequence.sequence.upper()
        repeats = []

        for unit_len in range(min_unit_length, max_unit_length + 1):
            pattern = f"(([ACGT]{{{unit_len}}}))\\2{{{min_repeats - 1},}}"

            try:
                compiled = re.compile(pattern)

                for match in compiled.finditer(seq_str):
                    unit = match.group(2)
                    full_match = match.group(0)
                    repeat_count = len(full_match) // len(unit)

                    repeats.append(
                        {
                            "start": match.start(),
                            "end": match.end(),
                            "unit": unit,
                            "unitLength": len(unit),
                            "repeatCount": repeat_count,
                            "totalLength": len(full_match),
                            "sequence": full_match,
                        }
                    )
            except re.error:
                continue

        repeats.sort(key=lambda x: x["start"])

        return repeats
