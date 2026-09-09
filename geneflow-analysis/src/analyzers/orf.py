"""ORF (Open Reading Frame) detection analyzer."""

from dataclasses import dataclass

from src.analyzers.analyzer import BaseAnalyzer
from src.constants import translate
from src.models import ORF, Sequence


@dataclass
class ORFResult:
    """Result of ORF detection."""

    orfs: list[ORF]
    totalOrfs: int
    longestOrf: ORF | None
    searchedFrames: list[int]


class ORFAnalyzer(BaseAnalyzer):
    """
    Analyzer for detecting Open Reading Frames.

    Finds all ORFs in a DNA sequence across multiple reading frames.
    An ORF is defined as a region starting with ATG and ending with
    a stop codon (TAA, TAG, TGA).
    """

    STOP_CODONS = {"TAA", "TAG", "TGA"}
    START_CODON = "ATG"

    DEFAULT_MIN_LENGTH = 30

    @property
    def name(self) -> str:
        return "orf"

    def analyze(
        self,
        sequence: Sequence,
        min_length: int = DEFAULT_MIN_LENGTH,
        frames: list[int] | None = None,
        **_,
    ) -> ORFResult:
        """
        Detect ORFs in a sequence.

        Args:
            sequence: DNA sequence to analyze
            min_length: Minimum ORF length in amino acids
            frames: Reading frames to search (default: all six)

        Returns:
            ORFResult with detected ORFs
        """
        self.validate_sequence(sequence)

        if frames is None:
            frames = [1, 2, 3, -1, -2, -3]

        for f in frames:
            if f not in [1, 2, 3, -1, -2, -3]:
                raise ValueError(f"Invalid frame: {f}")

        all_orfs = []
        seq_str = sequence.sequence.upper()

        for frame in frames:
            orfs = self._find_orfs_in_frame(seq_str, frame, min_length)
            all_orfs.extend(orfs)

        all_orfs.sort(key=lambda x: x.length, reverse=True)

        longest = all_orfs[0] if all_orfs else None

        return ORFResult(
            orfs=all_orfs,
            totalOrfs=len(all_orfs),
            longestOrf=longest,
            searchedFrames=frames,
        )

    def _find_orfs_in_frame(
        self,
        sequence: str,
        frame: int,
        min_length: int,
    ) -> list[ORF]:
        """Find ORFs in a specific reading frame."""
        orfs = []

        if frame < 0:
            seq = self._reverse_complement(sequence)
            strand = "-"
            actual_frame = abs(frame)
        else:
            seq = sequence
            strand = "+"
            actual_frame = frame

        start_offset = actual_frame - 1
        seq_len = len(seq)

        open_orfs = []

        for i in range(start_offset, seq_len - 2, 3):
            codon = seq[i : i + 3]

            if codon == self.START_CODON:
                open_orfs.append(i)

            elif codon in self.STOP_CODONS:
                for start_pos in open_orfs:
                    end_pos = i + 3
                    orf_seq = seq[start_pos:end_pos]
                    protein = translate(orf_seq, frame=1)

                    aa_length = len(protein) - 1

                    if aa_length >= min_length:
                        if strand == "-":
                            orig_start = len(sequence) - end_pos
                            orig_end = len(sequence) - start_pos
                        else:
                            orig_start = start_pos
                            orig_end = end_pos

                        orfs.append(
                            ORF(
                                start=orig_start,
                                end=orig_end,
                                frame=actual_frame,
                                strand=strand,
                                length=aa_length,
                                sequence=orf_seq,
                                proteinSequence=protein,
                            )
                        )

                open_orfs = []

        return orfs

    def _reverse_complement(self, sequence: str) -> str:
        """Get reverse complement of sequence."""
        complement = {"A": "T", "T": "A", "G": "C", "C": "G", "N": "N"}
        return "".join(complement.get(b, "N") for b in reversed(sequence))

    def find_longest_orf(self, sequence: Sequence) -> ORF | None:
        """
        Find the longest ORF in the sequence.

        Args:
            sequence: DNA sequence to analyze

        Returns:
            Longest ORF found, or None if none found
        """
        result = self.analyze(sequence, min_length=1)
        return result.longestOrf

    def summarize(self, result: ORFResult) -> dict:
        """Create summary statistics from ORF result."""
        if not result.orfs:
            return {
                "totalOrfs": 0,
                "longestLength": 0,
                "avgLength": 0,
                "frameDistribution": {},
                "strandDistribution": {"+": 0, "-": 0},
            }

        frame_dist = {}
        strand_dist = {"+": 0, "-": 0}

        total_length = 0

        for orf in result.orfs:
            frame_key = f"{orf.strand}{orf.frame}"
            frame_dist[frame_key] = frame_dist.get(frame_key, 0) + 1
            strand_dist[orf.strand] += 1
            total_length += orf.length

        return {
            "totalOrfs": result.totalOrfs,
            "longestLength": result.longestOrf.length if result.longestOrf else 0,
            "avgLength": round(total_length / result.totalOrfs, 1),
            "frameDistribution": frame_dist,
            "strandDistribution": strand_dist,
        }
