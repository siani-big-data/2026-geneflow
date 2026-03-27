"""Multiple sequence aligner using progressive alignment."""

import uuid
from Bio import Align

from src.models import AlignmentResult, AlignmentType
from src.alignment.aligner import BaseAligner
from src.alignment.pairwise import PairwiseAligner


class MultipleAligner(BaseAligner):
    """
    Multiple sequence aligner using progressive alignment.

    Builds alignment by progressively adding sequences,
    aligning each new sequence to the growing profile.
    """

    @property
    def alignment_type(self) -> AlignmentType:
        return AlignmentType.MULTIPLE

    def align(
        self,
        sequences: list[str],
        alignment_id: str | None = None,
        **options
    ) -> AlignmentResult:
        """
        Perform multiple sequence alignment.

        Args:
            sequences: List of sequences to align (3+)
            alignment_id: Optional ID for the alignment

        Returns:
            AlignmentResult with aligned sequences
        """
        self.validate_sequences(sequences, min_count=2)

        # Normalize sequences
        seqs = [s.upper() for s in sequences]

        if len(seqs) == 2:
            # Just do pairwise
            pairwise = PairwiseAligner()
            return pairwise.align(seqs, alignment_id=alignment_id, **options)

        # Progressive alignment
        aligned = self._progressive_align(seqs, **options)

        # Calculate statistics
        identity = self._calculate_average_identity(aligned)
        gaps = self.count_gaps(aligned)
        score = self._calculate_score(aligned)

        return AlignmentResult(
            alignmentId=alignment_id or str(uuid.uuid4()),
            type=AlignmentType.MULTIPLE,
            sequences=sequences,
            alignedSequences=aligned,
            score=score,
            identity=identity,
            gaps=gaps,
            consensus=None,
        )

    def _progressive_align(self, sequences: list[str], **options) -> list[str]:
        """
        Build multiple alignment progressively.

        Strategy:
        1. Align first two sequences
        2. For each additional sequence, align it to the profile
        """
        if len(sequences) < 2:
            return sequences

        pairwise = PairwiseAligner()

        # Start with first two sequences
        result = pairwise.align(sequences[:2], **options)
        aligned = list(result.alignedSequences)

        # Add remaining sequences one by one
        for seq in sequences[2:]:
            aligned = self._add_sequence_to_alignment(aligned, seq, pairwise, **options)

        return aligned

    def _add_sequence_to_alignment(
        self,
        aligned: list[str],
        new_seq: str,
        pairwise: PairwiseAligner,
        **options
    ) -> list[str]:
        """Add a new sequence to existing alignment."""
        # Build consensus of current alignment
        consensus = self._build_simple_consensus(aligned)

        # Align new sequence to consensus
        result = pairwise.align([consensus, new_seq], **options)
        aligned_consensus, aligned_new = result.alignedSequences

        # Adjust existing sequences to match new gaps in consensus
        new_aligned = []
        for existing in aligned:
            adjusted = self._adjust_sequence(existing, consensus, aligned_consensus)
            new_aligned.append(adjusted)

        new_aligned.append(aligned_new)

        return new_aligned

    def _build_simple_consensus(self, aligned: list[str]) -> str:
        """Build simple consensus from aligned sequences."""
        if not aligned:
            return ""

        length = len(aligned[0])
        consensus = []

        for i in range(length):
            bases = [seq[i] for seq in aligned if i < len(seq)]
            # Remove gaps for consensus base
            non_gap_bases = [b for b in bases if b != "-"]

            if non_gap_bases:
                # Most common base
                consensus.append(max(set(non_gap_bases), key=non_gap_bases.count))
            else:
                consensus.append("-")

        return "".join(consensus)

    def _adjust_sequence(
        self, original: str, old_consensus: str, new_consensus: str
    ) -> str:
        """Adjust sequence to match new gap positions in consensus."""
        result = []
        orig_idx = 0

        for i, new_char in enumerate(new_consensus):
            if i < len(old_consensus):
                old_char = old_consensus[i]

                if new_char == "-" and old_char != "-":
                    # New gap inserted
                    result.append("-")
                elif orig_idx < len(original):
                    result.append(original[orig_idx])
                    orig_idx += 1
                else:
                    result.append("-")
            else:
                # Past old consensus length
                if new_char == "-":
                    result.append("-")
                elif orig_idx < len(original):
                    result.append(original[orig_idx])
                    orig_idx += 1
                else:
                    result.append("-")

        return "".join(result)

    def _calculate_average_identity(self, aligned: list[str]) -> float:
        """Calculate average pairwise identity."""
        if len(aligned) < 2:
            return 100.0

        total_identity = 0.0
        comparisons = 0

        for i in range(len(aligned)):
            for j in range(i + 1, len(aligned)):
                identity = self.calculate_identity([aligned[i], aligned[j]])
                total_identity += identity
                comparisons += 1

        return round(total_identity / comparisons, 2) if comparisons > 0 else 0.0

    def _calculate_score(self, aligned: list[str]) -> float:
        """Calculate alignment score based on column conservation."""
        if not aligned or not aligned[0]:
            return 0.0

        length = len(aligned[0])
        score = 0.0

        for i in range(length):
            column = [seq[i] for seq in aligned if i < len(seq)]
            non_gap = [b for b in column if b != "-"]

            if non_gap:
                # Score based on conservation
                most_common = max(set(non_gap), key=non_gap.count)
                matches = non_gap.count(most_common)
                score += matches / len(column)

        return round(score, 2)
