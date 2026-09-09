"""Restriction enzyme analysis."""

from dataclasses import dataclass

from src.analyzers.analyzer import BaseAnalyzer
from src.constants import RESTRICTION_ENZYMES
from src.models import RestrictionSite, Sequence


@dataclass
class RestrictionResult:
    """Result of restriction enzyme analysis."""

    sites: list[RestrictionSite]
    totalSites: int
    enzymeCount: int
    enzymesUsed: list[str]
    fragmentLengths: list[int]


class RestrictionAnalyzer(BaseAnalyzer):
    """
    Analyzer for restriction enzyme cut site detection.

    Identifies restriction enzyme recognition sites and calculates
    resulting fragment sizes.
    """

    @property
    def name(self) -> str:
        return "restriction"

    def analyze(
        self, sequence: Sequence, enzymes: list[str] | None = None, **_
    ) -> RestrictionResult:
        """
        Find restriction enzyme cut sites in sequence.

        Args:
            sequence: DNA sequence to analyze
            enzymes: List of enzyme names to search for (default: all)

        Returns:
            RestrictionResult with found sites
        """
        self.validate_sequence(sequence)

        seq_str = sequence.sequence.upper()

        enzyme_lookup = {name.upper(): name for name in RESTRICTION_ENZYMES.keys()}

        if enzymes:
            enzyme_list = []
            for e in enzymes:
                canonical = enzyme_lookup.get(e.upper())
                if not canonical:
                    raise ValueError(f"Unknown enzyme: {e}")
                enzyme_list.append(canonical)
        else:
            enzyme_list = list(RESTRICTION_ENZYMES.keys())

        all_sites = []

        for enzyme_name in enzyme_list:
            enzyme_data = RESTRICTION_ENZYMES[enzyme_name]
            sites = self._find_enzyme_sites(seq_str, enzyme_name, enzyme_data)
            all_sites.extend(sites)

        all_sites.sort(key=lambda s: s.position)

        fragments = self._calculate_fragments(seq_str, all_sites)

        enzymes_with_sites = set(s.enzyme for s in all_sites)

        return RestrictionResult(
            sites=all_sites,
            totalSites=len(all_sites),
            enzymeCount=len(enzymes_with_sites),
            enzymesUsed=enzyme_list,
            fragmentLengths=fragments,
        )

    def _find_enzyme_sites(
        self,
        sequence: str,
        enzyme_name: str,
        enzyme_data: tuple,
    ) -> list[RestrictionSite]:
        """Find all cut sites for a specific enzyme."""
        sites = []

        recognition, cut_offset, overhang = enzyme_data

        import re

        pattern = self._recognition_to_regex(recognition)

        try:
            compiled = re.compile(pattern)

            for match in compiled.finditer(sequence):
                pos = match.start()
                cut_pos = pos + cut_offset

                sites.append(
                    RestrictionSite(
                        enzyme=enzyme_name,
                        position=pos,
                        cutPosition=cut_pos,
                        recognitionSequence=recognition,
                        overhang=overhang,
                    )
                )
        except re.error:
            pass

        return sites

    def _recognition_to_regex(self, recognition: str) -> str:
        """Convert IUPAC recognition sequence to regex."""
        iupac_map = {
            "A": "A",
            "C": "C",
            "G": "G",
            "T": "T",
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

        regex_parts = []
        for char in recognition.upper():
            if char in iupac_map:
                regex_parts.append(iupac_map[char])
            else:
                regex_parts.append(char)

        return "".join(regex_parts)

    def _calculate_fragments(
        self,
        sequence: str,
        sites: list[RestrictionSite],
    ) -> list[int]:
        """Calculate fragment lengths from cut positions."""
        if not sites:
            return [len(sequence)]

        cut_positions = sorted(set(s.cutPosition for s in sites))

        fragments = []
        prev_pos = 0

        for pos in cut_positions:
            if pos > prev_pos:
                fragments.append(pos - prev_pos)
            prev_pos = pos

        if prev_pos < len(sequence):
            fragments.append(len(sequence) - prev_pos)

        return sorted(fragments, reverse=True)

    def find_unique_cutters(
        self,
        sequence: Sequence,
        enzymes: list[str] | None = None,
    ) -> list[str]:
        """
        Find enzymes that cut exactly once.

        Args:
            sequence: DNA sequence to analyze
            enzymes: Enzymes to check (default: all)

        Returns:
            List of enzyme names that cut once
        """
        result = self.analyze(sequence, enzymes=enzymes)

        enzyme_counts = {}
        for site in result.sites:
            enzyme_counts[site.enzyme] = enzyme_counts.get(site.enzyme, 0) + 1

        return [e for e, count in enzyme_counts.items() if count == 1]

    def find_non_cutters(
        self,
        sequence: Sequence,
        enzymes: list[str] | None = None,
    ) -> list[str]:
        """
        Find enzymes that don't cut the sequence.

        Args:
            sequence: DNA sequence to analyze
            enzymes: Enzymes to check (default: all)

        Returns:
            List of enzyme names that don't cut
        """
        result = self.analyze(sequence, enzymes=enzymes)

        enzymes_with_sites = set(s.enzyme for s in result.sites)

        return [e for e in result.enzymesUsed if e not in enzymes_with_sites]

    def summarize(self, result: RestrictionResult) -> dict:
        """Create summary statistics from restriction result."""
        if not result.sites:
            return {
                "totalSites": 0,
                "enzymeCount": 0,
                "sitesPerEnzyme": {},
                "avgFragmentLength": (
                    len(result.fragmentLengths[0]) if result.fragmentLengths else 0
                ),
                "fragmentCount": len(result.fragmentLengths),
            }

        sites_per_enzyme = {}
        for site in result.sites:
            sites_per_enzyme[site.enzyme] = sites_per_enzyme.get(site.enzyme, 0) + 1

        avg_fragment = sum(result.fragmentLengths) / len(result.fragmentLengths)

        return {
            "totalSites": result.totalSites,
            "enzymeCount": result.enzymeCount,
            "sitesPerEnzyme": sites_per_enzyme,
            "avgFragmentLength": round(avg_fragment, 1),
            "fragmentCount": len(result.fragmentLengths),
            "largestFragment": max(result.fragmentLengths),
            "smallestFragment": min(result.fragmentLengths),
        }
