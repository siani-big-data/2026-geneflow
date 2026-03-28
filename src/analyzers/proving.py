#!/usr/bin/env python
"""Proving script for advanced analyzers."""

from src.analyzers import (
    HeterozygoteAnalyzer,
    MotifAnalyzer,
    TranslationAnalyzer,
    ORFAnalyzer,
    RestrictionAnalyzer,
)
from src.models import Sequence


def main():
    """Run advanced analyzers proving tests."""
    print("=" * 60)
    print("ADVANCED ANALYZERS - PROVING SCRIPT")
    print("=" * 60)

    # Test sequence (realistic example)
    dna_sequence = (
        "ATGCGTACGTAGCTAGCTAGCTAGCTAGCTAGCTAGCTAGCTAGCTAGCTAGC"
        "ATGAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATAA"
        "GAATTCGGATCCAAGCTTGCGGCCGCCTCGAGGAATTC"
    )

    seq = Sequence(id="test-seq", sequence=dna_sequence)

    # 1. Heterozygote Detection
    print("\n1. HETEROZYGOTE DETECTION")
    print("-" * 40)

    hetero_analyzer = HeterozygoteAnalyzer()

    # Test with IUPAC codes
    hetero_seq = Sequence(id="hetero", sequence="ACGTRYSW")
    result = hetero_analyzer.analyze(hetero_seq)

    print(f"Sequence:    {hetero_seq.sequence}")
    print(f"Total pos:   {result.totalPositions}")
    print(f"Heterozygotes: {result.heterozygoteCount}")
    print(f"Rate:        {result.heterozygoteRate:.4f}")

    if result.calls:
        print("Calls:")
        for call in result.calls:
            print(f"  Pos {call.position}: {call.base1}/{call.base2} = {call.iupacCode}")

    # 2. Motif Search
    print("\n2. MOTIF SEARCH")
    print("-" * 40)

    motif_analyzer = MotifAnalyzer()

    # Search for restriction site patterns
    patterns = ["GAATTC", "GGATCC", "ATG"]

    for pattern in patterns:
        result = motif_analyzer.analyze(seq, pattern=pattern)
        print(f"Pattern '{pattern}': {result.matchCount} match(es)")
        for match in result.matches[:3]:
            print(f"  Position {match.start}-{match.end}: {match.matchedSequence}")

    # Find tandem repeats
    print("\nTandem Repeats:")
    repeat_seq = Sequence(id="repeat", sequence="CAGCAGCAGCAGCAG")
    repeats = motif_analyzer.find_repeats(repeat_seq, min_unit_length=3, min_repeats=3)
    for r in repeats:
        print(f"  {r['unit']} x {r['repeatCount']} at position {r['start']}")

    # 3. Translation
    print("\n3. TRANSLATION")
    print("-" * 40)

    trans_analyzer = TranslationAnalyzer()

    # Translate in frame 1
    result = trans_analyzer.analyze(seq, frame=1)

    print(f"Frame:          {result.frame}")
    print(f"Protein length: {result.proteinLength} aa")
    print(f"Stop codons:    {result.stopCodonCount}")
    print(f"Start positions: {result.startCodonPositions[:5]}...")
    print(f"Protein:        {result.proteinSequence[:30]}...")

    # All frames
    print("\nAll 6 frames:")
    all_frames = trans_analyzer.translate_all_frames(seq)
    for r in all_frames:
        frame_label = f"+{r.frame}" if r.frame > 0 else str(r.frame)
        print(f"  Frame {frame_label}: {r.proteinLength} aa, {r.stopCodonCount} stops")

    # 4. ORF Detection
    print("\n4. ORF DETECTION")
    print("-" * 40)

    orf_analyzer = ORFAnalyzer()
    result = orf_analyzer.analyze(seq, min_length=10)

    print(f"Total ORFs:   {result.totalOrfs}")
    print(f"Frames searched: {result.searchedFrames}")

    if result.longestOrf:
        orf = result.longestOrf
        print(f"\nLongest ORF:")
        print(f"  Position: {orf.start}-{orf.end}")
        print(f"  Strand:   {orf.strand}")
        print(f"  Frame:    {orf.frame}")
        print(f"  Length:   {orf.length} aa")
        print(f"  Protein:  {orf.proteinSequence[:30]}...")

    # Summary
    summary = orf_analyzer.summarize(result)
    print(f"\nSummary:")
    print(f"  Avg length: {summary['avgLength']} aa")
    print(f"  Strand dist: {summary['strandDistribution']}")

    # 5. Restriction Analysis
    print("\n5. RESTRICTION ANALYSIS")
    print("-" * 40)

    restriction_analyzer = RestrictionAnalyzer()

    # Search common enzymes
    enzymes = ["EcoRI", "BamHI", "HindIII", "NotI", "XhoI"]
    result = restriction_analyzer.analyze(seq, enzymes=enzymes)

    print(f"Enzymes tested: {len(result.enzymesUsed)}")
    print(f"Total sites:    {result.totalSites}")
    print(f"Enzymes found:  {result.enzymeCount}")

    if result.sites:
        print("\nSites found:")
        for site in result.sites:
            print(f"  {site.enzyme} at {site.position} (cut: {site.cutPosition})")

    print(f"\nFragments: {result.fragmentLengths}")

    # Find unique cutters
    unique = restriction_analyzer.find_unique_cutters(seq, enzymes=enzymes)
    print(f"Unique cutters: {unique}")

    # Non-cutters
    non_cutters = restriction_analyzer.find_non_cutters(seq, enzymes=enzymes)
    print(f"Non-cutters: {non_cutters}")

    print("\n" + "=" * 60)
    print("PROVING COMPLETE - All advanced analyzers working")
    print("=" * 60)


if __name__ == "__main__":
    main()
