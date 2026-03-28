"""Pairwise sequence aligner using Needleman-Wunsch algorithm."""

import uuid

from Bio import Align

from src.alignment.aligner import BaseAligner
from src.models import AlignmentResult, AlignmentType


class PairwiseAligner(BaseAligner):
    """
    Pairwise sequence aligner using Needleman-Wunsch (global alignment).

    Uses BioPython's PairwiseAligner for optimal global alignment.
    """

    # Default scoring parameters
    DEFAULT_MATCH_SCORE = 2
    DEFAULT_MISMATCH_SCORE = -1
    DEFAULT_GAP_OPEN = -10
    DEFAULT_GAP_EXTEND = -0.5

    @property
    def alignment_type(self) -> AlignmentType:
        return AlignmentType.PAIRWISE

    def align(
        self,
        sequences: list[str],
        alignment_id: str | None = None,
        match_score: int = DEFAULT_MATCH_SCORE,
        mismatch_score: int = DEFAULT_MISMATCH_SCORE,
        gap_open: float = DEFAULT_GAP_OPEN,
        gap_extend: float = DEFAULT_GAP_EXTEND,
        **_,
    ) -> AlignmentResult:
        """
        Perform pairwise global alignment.

        Args:
            sequences: Two sequences to align
            alignment_id: Optional ID for the alignment
            match_score: Score for matching bases
            mismatch_score: Score for mismatching bases
            gap_open: Penalty for opening a gap
            gap_extend: Penalty for extending a gap

        Returns:
            AlignmentResult with aligned sequences
        """
        self.validate_sequences(sequences, min_count=2)

        if len(sequences) > 2:
            # Only use first two sequences for pairwise
            sequences = sequences[:2]

        seq1, seq2 = sequences[0].upper(), sequences[1].upper()

        # Create BioPython aligner
        aligner = Align.PairwiseAligner()
        aligner.mode = "global"
        aligner.match_score = match_score
        aligner.mismatch_score = mismatch_score
        aligner.open_gap_score = gap_open
        aligner.extend_gap_score = gap_extend

        # Perform alignment
        alignments = aligner.align(seq1, seq2)

        if not alignments:
            raise ValueError("No alignment found")

        # Get best alignment
        best = alignments[0]
        score = best.score

        # Extract aligned sequences
        aligned_seqs = self._extract_aligned_sequences(best, seq1, seq2)

        # Calculate statistics
        identity = self.calculate_identity(aligned_seqs)
        gaps = self.count_gaps(aligned_seqs)

        return AlignmentResult(
            alignmentId=alignment_id or str(uuid.uuid4()),
            type=AlignmentType.PAIRWISE,
            sequences=sequences,
            alignedSequences=aligned_seqs,
            score=float(score),
            identity=identity,
            gaps=gaps,
            consensus=None,
        )

    def _extract_aligned_sequences(self, alignment, seq1: str, seq2: str) -> list[str]:
        """Extract aligned sequences from BioPython alignment object."""
        # BioPython format varies, try to extract sequences
        try:
            # Try using alignment coordinates
            aligned1 = []
            aligned2 = []

            coords = alignment.coordinates

            # Walk through alignment coordinates
            for i in range(len(coords[0]) - 1):
                s1_start, s1_end = coords[0][i], coords[0][i + 1]
                s2_start, s2_end = coords[1][i], coords[1][i + 1]

                s1_len = s1_end - s1_start
                s2_len = s2_end - s2_start

                if s1_len == s2_len:
                    # Match/mismatch region
                    aligned1.append(seq1[s1_start:s1_end])
                    aligned2.append(seq2[s2_start:s2_end])
                elif s1_len > 0 and s2_len == 0:
                    # Gap in seq2
                    aligned1.append(seq1[s1_start:s1_end])
                    aligned2.append("-" * s1_len)
                elif s2_len > 0 and s1_len == 0:
                    # Gap in seq1
                    aligned1.append("-" * s2_len)
                    aligned2.append(seq2[s2_start:s2_end])

            return ["".join(aligned1), "".join(aligned2)]

        except Exception:
            # Fallback: return original sequences (no gaps)
            return [seq1, seq2]

    def align_local(
        self, sequences: list[str], alignment_id: str | None = None, **options
    ) -> AlignmentResult:
        """
        Perform local alignment (Smith-Waterman style).

        Args:
            sequences: Two sequences to align
            alignment_id: Optional ID
            **options: Scoring options

        Returns:
            AlignmentResult
        """
        self.validate_sequences(sequences, min_count=2)

        seq1, seq2 = sequences[0].upper(), sequences[1].upper()

        aligner = Align.PairwiseAligner()
        aligner.mode = "local"
        aligner.match_score = options.get("match_score", self.DEFAULT_MATCH_SCORE)
        aligner.mismatch_score = options.get("mismatch_score", self.DEFAULT_MISMATCH_SCORE)
        aligner.open_gap_score = options.get("gap_open", self.DEFAULT_GAP_OPEN)
        aligner.extend_gap_score = options.get("gap_extend", self.DEFAULT_GAP_EXTEND)

        alignments = aligner.align(seq1, seq2)

        if not alignments:
            raise ValueError("No alignment found")

        best = alignments[0]
        aligned_seqs = self._extract_aligned_sequences(best, seq1, seq2)

        return AlignmentResult(
            alignmentId=alignment_id or str(uuid.uuid4()),
            type=AlignmentType.PAIRWISE,
            sequences=sequences,
            alignedSequences=aligned_seqs,
            score=float(best.score),
            identity=self.calculate_identity(aligned_seqs),
            gaps=self.count_gaps(aligned_seqs),
            consensus=None,
        )
