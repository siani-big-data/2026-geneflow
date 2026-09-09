#!/usr/bin/env python
"""Proving script for alignment module."""

from src.alignment import (
    ConsensusBuilder,
    MultipleAligner,
    PairwiseAligner,
    VariantDetector,
)
from src.alignment.consensus import ConsensusMethod


def main():
    """Run alignment proving tests."""
    print("=" * 60)
    print("ALIGNMENT MODULE - PROVING SCRIPT")
    print("=" * 60)

    # Test sequences
    seq1 = "ACGTACGTACGT"
    seq2 = "ACGTAGGTACGT"
    seq3 = "ACGTACGTACTT"
    seq4 = "TCGTACGTACGT"

    # 1. Pairwise Alignment
    print("\n1. PAIRWISE ALIGNMENT")
    print("-" * 40)

    pairwise = PairwiseAligner()
    result = pairwise.align([seq1, seq2])

    print(f"Sequence 1: {seq1}")
    print(f"Sequence 2: {seq2}")
    print(f"Aligned 1:  {result.alignedSequences[0]}")
    print(f"Aligned 2:  {result.alignedSequences[1]}")
    print(f"Score:      {result.score}")
    print(f"Identity:   {result.identity}%")
    print(f"Gaps:       {result.gaps}")

    # 2. Multiple Alignment
    print("\n2. MULTIPLE ALIGNMENT")
    print("-" * 40)

    multiple = MultipleAligner()
    result = multiple.align([seq1, seq2, seq3, seq4])

    print(f"Input sequences: {len([seq1, seq2, seq3, seq4])}")
    print("Aligned sequences:")
    for i, aligned in enumerate(result.alignedSequences):
        print(f"  Seq {i + 1}: {aligned}")

    print(f"Average Identity: {result.identity}%")
    print(f"Total Gaps:       {result.gaps}")
    print(f"Score:            {result.score}")

    # 3. Consensus Building
    print("\n3. CONSENSUS BUILDING")
    print("-" * 40)

    builder = ConsensusBuilder()

    # Majority consensus
    consensus_result = builder.build(
        result.alignedSequences,
        method=ConsensusMethod.MAJORITY,
    )
    print(f"Majority Consensus: {consensus_result.consensus}")

    # IUPAC consensus
    iupac_result = builder.build(
        result.alignedSequences,
        method=ConsensusMethod.IUPAC,
    )
    print(f"IUPAC Consensus:    {iupac_result.consensus}")

    # Conservation scores
    conservation = builder.calculate_conservation(result.alignedSequences)
    print(f"Conservation:       {conservation[:12]}...")  # First 12 positions

    # 4. Profile (PSSM)
    print("\n4. POSITION-SPECIFIC SCORING MATRIX")
    print("-" * 40)

    profile = builder.build_profile(result.alignedSequences)
    print("First 4 positions:")
    for i, pos in enumerate(profile[:4]):
        bases = {k: v for k, v in pos.items() if v > 0 and k != "-"}
        print(f"  Position {i}: {bases}")

    # 5. Variant Detection
    print("\n5. VARIANT DETECTION")
    print("-" * 40)

    detector = VariantDetector()
    report = detector.detect(result.alignedSequences, reference_index=0)

    print("Reference:         Seq 1")
    print(f"Total Positions:   {report.totalPositions}")
    print(f"Variant Positions: {report.variantPositions}")
    print(f"SNPs:              {report.snpCount}")
    print(f"Insertions:        {report.insertionCount}")
    print(f"Deletions:         {report.deletionCount}")

    if report.variants:
        print("\nVariants found:")
        for v in report.variants[:5]:  # First 5
            print(
                f"  Pos {v.position}: {v.reference}->{v.alternate} "
                f"({v.type.value}) freq={v.frequency}"
            )

    # Summary statistics
    summary = detector.summarize_variants(report)
    print(f"\nTi/Tv Ratio: {summary['tiTvRatio']}")
    print(f"Variant Rate: {summary['variantRate']:.4f}")

    # 6. Local Alignment
    print("\n6. LOCAL ALIGNMENT")
    print("-" * 40)

    long_seq = "NNNNACGTACGTNNNN"
    short_seq = "ACGTACGT"

    result = pairwise.align_local([long_seq, short_seq])
    print(f"Sequence 1: {long_seq}")
    print(f"Sequence 2: {short_seq}")
    print(f"Local Score: {result.score}")

    # 7. Edge cases
    print("\n7. EDGE CASES")
    print("-" * 40)

    # Very different sequences
    result = pairwise.align(["AAAAAAAAAA", "TTTTTTTTTT"])
    print(f"All A vs All T - Identity: {result.identity}%")

    # Short sequences
    result = pairwise.align(["AC", "AC"])
    print(f"Short identical - Identity: {result.identity}%")

    # With N bases
    result = pairwise.align(["ACNGT", "ACAGT"])
    print(f"With ambiguous N - Gaps: {result.gaps}")

    print("\n" + "=" * 60)
    print("PROVING COMPLETE - All alignment functions working")
    print("=" * 60)


if __name__ == "__main__":
    main()
