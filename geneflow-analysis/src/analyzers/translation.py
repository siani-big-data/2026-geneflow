"""Translation analyzer for DNA to protein conversion."""

from dataclasses import dataclass

from src.analyzers.analyzer import BaseAnalyzer
from src.constants import translate
from src.models import Sequence


@dataclass
class TranslationResult:
    """Result of sequence translation."""

    frame: int
    proteinSequence: str
    proteinLength: int
    stopCodonCount: int
    startCodonPositions: list[int]
    aminoAcidComposition: dict[str, int]


class TranslationAnalyzer(BaseAnalyzer):
    """
    Analyzer for translating DNA sequences to protein.

    Supports:
    - All six reading frames
    - Standard genetic code
    - Amino acid composition analysis
    """

    @property
    def name(self) -> str:
        return "translation"

    def analyze(self, sequence: Sequence, frame: int = 1, **_) -> TranslationResult:
        """
        Translate DNA sequence to protein.

        Args:
            sequence: DNA sequence to translate
            frame: Reading frame (1, 2, 3 for forward; -1, -2, -3 for reverse)

        Returns:
            TranslationResult with protein sequence
        """
        self.validate_sequence(sequence)

        if frame not in [1, 2, 3, -1, -2, -3]:
            raise ValueError("Frame must be 1, 2, 3, -1, -2, or -3")

        seq_str = sequence.sequence.upper()

        if frame < 0:
            seq_str = self._reverse_complement(seq_str)
            frame = abs(frame)

        protein = translate(seq_str, frame=frame - 1)

        stop_count = protein.count("*")

        start_positions = self._find_start_codons(seq_str, frame)

        composition = self._calculate_composition(protein)

        return TranslationResult(
            frame=frame,
            proteinSequence=protein,
            proteinLength=len(protein),
            stopCodonCount=stop_count,
            startCodonPositions=start_positions,
            aminoAcidComposition=composition,
        )

    def translate_all_frames(self, sequence: Sequence) -> list[TranslationResult]:
        """
        Translate sequence in all six reading frames.

        Args:
            sequence: DNA sequence to translate

        Returns:
            List of TranslationResult for each frame
        """
        results = []

        for frame in [1, 2, 3, -1, -2, -3]:
            result = self.analyze(sequence, frame=frame)
            results.append(result)

        return results

    def _reverse_complement(self, sequence: str) -> str:
        """Get reverse complement of sequence."""
        complement = {"A": "T", "T": "A", "G": "C", "C": "G", "N": "N"}
        return "".join(complement.get(b, "N") for b in reversed(sequence))

    def _find_start_codons(self, sequence: str, frame: int) -> list[int]:
        """Find positions of start codons (ATG)."""
        positions = []
        start = frame - 1

        for i in range(start, len(sequence) - 2, 3):
            codon = sequence[i : i + 3]
            if codon == "ATG":
                positions.append(i)

        return positions

    def _calculate_composition(self, protein: str) -> dict[str, int]:
        """Calculate amino acid composition."""
        composition = {}

        for aa in protein:
            if aa != "*":
                composition[aa] = composition.get(aa, 0) + 1

        return composition

    def analyze_composition(self, protein: str) -> dict:
        """
        Analyze protein composition in detail.

        Args:
            protein: Protein sequence

        Returns:
            Dictionary with composition analysis
        """
        composition = self._calculate_composition(protein)
        total = sum(composition.values())

        if total == 0:
            return {
                "total": 0,
                "composition": {},
                "percentages": {},
                "properties": {},
            }

        percentages = {aa: round(count / total * 100, 2) for aa, count in composition.items()}

        properties = {
            "hydrophobic": 0,
            "hydrophilic": 0,
            "charged": 0,
            "aromatic": 0,
        }

        hydrophobic = set("AILMFVWP")
        hydrophilic = set("STNQ")
        charged = set("DEKRH")
        aromatic = set("FYW")

        for aa, count in composition.items():
            if aa in hydrophobic:
                properties["hydrophobic"] += count
            if aa in hydrophilic:
                properties["hydrophilic"] += count
            if aa in charged:
                properties["charged"] += count
            if aa in aromatic:
                properties["aromatic"] += count

        for prop in properties:
            properties[prop] = round(properties[prop] / total * 100, 2)

        return {
            "total": total,
            "composition": composition,
            "percentages": percentages,
            "properties": properties,
        }
