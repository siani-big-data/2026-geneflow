"""Tool registry with all bioinformatics tools."""


from ..client import GeneFlowClient
from ..schemas import (
    AlignmentType,
    TrimmingAlgorithm,
)
from .base import Tool, ToolParameter, ToolRegistry, ToolResult

# Global registry and client
_registry: ToolRegistry | None = None
_client: GeneFlowClient | None = None


def get_client() -> GeneFlowClient:
    """Get or create the GeneFlow client."""
    global _client
    if _client is None:
        _client = GeneFlowClient(direct_mode=False)
    return _client


def set_client(client: GeneFlowClient) -> None:
    """Set a custom client."""
    global _client
    _client = client


def get_tool_registry() -> ToolRegistry:
    """Get or create the tool registry."""
    global _registry
    if _registry is None:
        _registry = ToolRegistry()
        register_default_tools(_registry)
    return _registry


def register_default_tools(registry: ToolRegistry) -> None:
    """Register all default bioinformatics tools."""

    # ==================== Quality Analysis ====================
    async def analyze_quality_handler(
        sequence: str,
        quality_scores: list[int] | None = None,
    ) -> ToolResult:
        """Handle quality_enhanced analysis."""
        client = get_client()
        try:
            result = await client.analyze_quality(sequence, quality_scores)
            return ToolResult(
                success=True,
                data={
                    "mean_quality": result.mean_quality,
                    "q20_percentage": result.q20_percentage,
                    "q30_percentage": result.q30_percentage,
                    "gc_content": result.gc_content,
                    "length": result.length,
                    "ambiguous_count": result.ambiguous_count,
                    "snr": result.snr,
                },
            )
        except Exception as e:
            return ToolResult(success=False, error=str(e))

    registry.register(Tool(
        name="analyze_sequence_quality",
        description=(
            "Analyze the quality_enhanced of a DNA sequence. Returns metrics "
            "including mean quality_enhanced score, percentage of bases with "
            "Q20+ and Q30+ quality_enhanced, GC content percentage, sequence "
            "length, and signal-to-noise ratio if available."
        ),
        parameters=[
            ToolParameter(
                name="sequence",
                type="string",
                description="The DNA sequence to analyze (A, C, G, T characters)",
                required=True,
            ),
            ToolParameter(
                name="quality_scores",
                type="array",
                description="Optional Phred quality_enhanced scores for each base position",
                required=False,
                items_type="integer",
            ),
        ],
        handler=analyze_quality_handler,
        category="analysis",
        examples=[
            {
                "input": {"sequence": "ACGTACGTACGT"},
                "description": "Basic quality_enhanced analysis without quality_enhanced scores",
            },
            {
                "input": {
                    "sequence": "ACGTACGT",
                    "quality_scores": [30, 32, 28, 35, 29, 31, 27, 33],
                },
                "description": "Quality analysis with Phred scores",
            },
        ],
    ))

    # ==================== Trimming ====================
    async def trim_sequence_handler(
        sequence: str,
        quality_scores: list[int],
        algorithm: str = "modified_mott",
        cutoff: float = 0.05,
        window_size: int = 10,
        threshold: int = 20,
    ) -> ToolResult:
        """Handle sequence trimming."""
        client = get_client()
        try:
            algo = TrimmingAlgorithm(algorithm)
            result = await client.trim_sequence(
                sequence=sequence,
                quality_scores=quality_scores,
                algorithm=algo,
                cutoff=cutoff,
                window_size=window_size,
                threshold=threshold,
            )
            return ToolResult(
                success=True,
                data={
                    "original_length": result.original_length,
                    "trimmed_length": result.trimmed_length,
                    "trim_start": result.trim_start,
                    "trim_end": result.trim_end,
                    "trimmed_sequence": result.trimmed_sequence,
                    "algorithm": result.algorithm,
                    "bases_removed": result.original_length - result.trimmed_length,
                },
            )
        except Exception as e:
            return ToolResult(success=False, error=str(e))

    registry.register(Tool(
        name="trim_sequence",
        description=(
            "Trim low-quality_enhanced regions from the ends of a DNA sequence. "
            "Uses quality_enhanced scores to identify and remove unreliable bases. "
            "Three algorithms available: modified_mott (probabilistic), "
            "sliding_window (average quality_enhanced), quality_threshold (per-base cutoff)."
        ),
        parameters=[
            ToolParameter(
                name="sequence",
                type="string",
                description="The DNA sequence to trim",
                required=True,
            ),
            ToolParameter(
                name="quality_scores",
                type="array",
                description="Phred quality_enhanced scores for each base (required for trimming)",
                required=True,
                items_type="integer",
            ),
            ToolParameter(
                name="algorithm",
                type="string",
                description="Trimming algorithm to use",
                required=False,
                default="modified_mott",
                enum=["modified_mott", "sliding_window", "quality_threshold"],
            ),
            ToolParameter(
                name="cutoff",
                type="number",
                description="Error probability cutoff for modified_mott (default: 0.05)",
                required=False,
                default=0.05,
            ),
            ToolParameter(
                name="window_size",
                type="integer",
                description="Window size for sliding_window algorithm (default: 10)",
                required=False,
                default=10,
            ),
            ToolParameter(
                name="threshold",
                type="integer",
                description="Quality threshold for quality_threshold algorithm (default: 20)",
                required=False,
                default=20,
            ),
        ],
        handler=trim_sequence_handler,
        category="analysis",
    ))

    # ==================== Motif Search ====================
    async def search_motifs_handler(
        sequence: str,
        pattern: str,
        search_complement: bool = False,
        use_regex: bool = False,
    ) -> ToolResult:
        """Handle motif search."""
        client = get_client()
        try:
            result = await client.search_motifs(
                sequence=sequence,
                pattern=pattern,
                search_complement=search_complement,
                use_regex=use_regex,
            )
            matches = [
                {
                    "start": m.start,
                    "end": m.end,
                    "matched_sequence": m.matched_sequence,
                    "strand": m.strand,
                }
                for m in result.matches
            ]
            return ToolResult(
                success=True,
                data={
                    "match_count": result.match_count,
                    "matches": matches,
                    "pattern": pattern,
                },
            )
        except Exception as e:
            return ToolResult(success=False, error=str(e))

    registry.register(Tool(
        name="search_motifs",
        description=(
            "Search for DNA motif patterns in a sequence. Supports exact matches, "
            "IUPAC ambiguity codes (e.g., N for any base, R for purine), and regex patterns. "
            "Can optionally search the reverse complement strand."
        ),
        parameters=[
            ToolParameter(
                name="sequence",
                type="string",
                description="The DNA sequence to search in",
                required=True,
            ),
            ToolParameter(
                name="pattern",
                type="string",
                description="The motif pattern to search for (e.g., 'ATAT', 'GAATTC', 'N*N')",
                required=True,
            ),
            ToolParameter(
                name="search_complement",
                type="boolean",
                description="Also search the reverse complement strand",
                required=False,
                default=False,
            ),
            ToolParameter(
                name="use_regex",
                type="boolean",
                description="Treat the pattern as a regular expression",
                required=False,
                default=False,
            ),
        ],
        handler=search_motifs_handler,
        category="analysis",
    ))

    # ==================== ORF Detection ====================
    async def detect_orfs_handler(
        sequence: str,
        min_length: int = 30,
    ) -> ToolResult:
        """Handle ORF detection."""
        client = get_client()
        try:
            result = await client.detect_orfs(sequence, min_length)
            orfs = [
                {
                    "start": o.start,
                    "end": o.end,
                    "length": o.length,
                    "frame": o.frame,
                    "sequence": o.sequence[:50] + "..." if len(o.sequence) > 50 else o.sequence,
                }
                for o in result.orfs
            ]
            return ToolResult(
                success=True,
                data={
                    "total_orfs": result.total_orfs,
                    "longest_orf_length": result.longest_orf_length,
                    "orfs": orfs,
                },
            )
        except Exception as e:
            return ToolResult(success=False, error=str(e))

    registry.register(Tool(
        name="detect_orfs",
        description=(
            "Detect open reading frames (ORFs) in a DNA sequence. "
            "An ORF is a continuous stretch of codons that begins with a start codon (ATG) "
            "and ends with a stop codon (TAA, TAG, TGA). Useful for identifying potential "
            "protein-coding regions."
        ),
        parameters=[
            ToolParameter(
                name="sequence",
                type="string",
                description="The DNA sequence to analyze",
                required=True,
            ),
            ToolParameter(
                name="min_length",
                type="integer",
                description="Minimum ORF length in amino acids (default: 30)",
                required=False,
                default=30,
            ),
        ],
        handler=detect_orfs_handler,
        category="analysis",
    ))

    # ==================== Translation ====================
    async def translate_handler(
        sequence: str,
        frame: int = 1,
    ) -> ToolResult:
        """Handle translation."""
        client = get_client()
        try:
            result = await client.translate(sequence, frame)
            return ToolResult(
                success=True,
                data={
                    "protein_sequence": result.protein_sequence,
                    "protein_length": result.protein_length,
                    "frame": result.frame,
                },
            )
        except Exception as e:
            return ToolResult(success=False, error=str(e))

    registry.register(Tool(
        name="translate_dna",
        description=(
            "Translate a DNA sequence to a protein sequence. "
            "Converts nucleotide triplets (codons) to amino acids using the standard genetic code. "
            "Supports all 6 reading frames (1-3 forward, 4-6 reverse complement)."
        ),
        parameters=[
            ToolParameter(
                name="sequence",
                type="string",
                description="The DNA sequence to translate",
                required=True,
            ),
            ToolParameter(
                name="frame",
                type="integer",
                description="Reading frame: 1-3 for forward strand, 4-6 for reverse complement",
                required=False,
                default=1,
                enum=["1", "2", "3", "4", "5", "6"],
            ),
        ],
        handler=translate_handler,
        category="analysis",
    ))

    # ==================== Restriction Sites ====================
    async def restriction_handler(
        sequence: str,
        enzymes: list[str] | None = None,
    ) -> ToolResult:
        """Handle restriction site analysis."""
        client = get_client()
        try:
            result = await client.find_restriction_sites(sequence, enzymes)
            sites = [
                {
                    "enzyme": s.enzyme,
                    "position": s.position,
                    "cut_sequence": s.cut_sequence,
                }
                for s in result.sites
            ]
            return ToolResult(
                success=True,
                data={
                    "enzyme_count": result.enzyme_count,
                    "total_sites": result.total_sites,
                    "sites": sites,
                },
            )
        except Exception as e:
            return ToolResult(success=False, error=str(e))

    registry.register(Tool(
        name="find_restriction_sites",
        description=(
            "Find restriction enzyme cut sites in a DNA sequence. "
            "Restriction enzymes cut DNA at specific recognition sequences. "
            "Common enzymes include EcoRI (GAATTC), BamHI (GGATCC), HindIII (AAGCTT). "
            "Useful for cloning and molecular biology applications."
        ),
        parameters=[
            ToolParameter(
                name="sequence",
                type="string",
                description="The DNA sequence to analyze",
                required=True,
            ),
            ToolParameter(
                name="enzymes",
                type="array",
                description=(
                    "List of enzyme names to search for "
                    "(optional, default: common enzymes)"
                ),
                required=False,
                items_type="string",
            ),
        ],
        handler=restriction_handler,
        category="analysis",
    ))

    # ==================== Sequence Alignment ====================
    async def align_handler(
        sequences: list[str],
        alignment_type: str = "pairwise",
        build_consensus: bool = True,
    ) -> ToolResult:
        """Handle sequence alignment."""
        client = get_client()
        try:
            align_type = AlignmentType(alignment_type)
            result = await client.align_sequences(
                sequences=sequences,
                alignment_type=align_type,
                build_consensus=build_consensus,
            )
            return ToolResult(
                success=True,
                data={
                    "alignment_id": result.alignment_id,
                    "alignment_type": result.alignment_type,
                    "aligned_sequences": result.aligned_sequences,
                    "score": result.score,
                    "identity": result.identity,
                    "gaps": result.gaps,
                    "consensus": result.consensus,
                },
            )
        except Exception as e:
            return ToolResult(success=False, error=str(e))

    registry.register(Tool(
        name="align_sequences",
        description=(
            "Align two or more DNA sequences. Pairwise alignment compares exactly two sequences. "
            "Multiple sequence alignment handles 3+ sequences using progressive alignment. "
            "Returns aligned sequences with gaps (-) inserted for alignment, along with "
            "alignment score, percent identity, gap count, and optional consensus sequence."
        ),
        parameters=[
            ToolParameter(
                name="sequences",
                type="array",
                description="List of DNA sequences to align (minimum 2)",
                required=True,
                items_type="string",
            ),
            ToolParameter(
                name="alignment_type",
                type="string",
                description=(
                    "Type of alignment: 'pairwise' (2 sequences) "
                    "or 'multiple' (3+ sequences)"
                ),
                required=False,
                default="pairwise",
                enum=["pairwise", "multiple"],
            ),
            ToolParameter(
                name="build_consensus",
                type="boolean",
                description="Build a consensus sequence from the alignment",
                required=False,
                default=True,
            ),
        ],
        handler=align_handler,
        category="alignment",
    ))

    # ==================== GC Content ====================
    async def gc_content_handler(sequence: str) -> ToolResult:
        """Calculate GC content."""
        try:
            sequence = sequence.upper()
            gc_count = sequence.count("G") + sequence.count("C")
            total = len(sequence)
            gc_percentage = (gc_count / total * 100) if total > 0 else 0

            return ToolResult(
                success=True,
                data={
                    "gc_percentage": round(gc_percentage, 2),
                    "gc_count": gc_count,
                    "total_bases": total,
                    "at_percentage": round(100 - gc_percentage, 2),
                },
            )
        except Exception as e:
            return ToolResult(success=False, error=str(e))

    registry.register(Tool(
        name="calculate_gc_content",
        description=(
            "Calculate the GC content (guanine + cytosine percentage) of a DNA sequence. "
            "GC content affects DNA stability and melting temperature. "
            "This is a fast local calculation that doesn't require external services."
        ),
        parameters=[
            ToolParameter(
                name="sequence",
                type="string",
                description="The DNA sequence to analyze",
                required=True,
            ),
        ],
        handler=gc_content_handler,
        category="utilities",
    ))

    # ==================== Reverse Complement ====================
    async def reverse_complement_handler(sequence: str) -> ToolResult:
        """Calculate reverse complement."""
        try:
            complement_map = {"A": "T", "T": "A", "G": "C", "C": "G", "N": "N"}
            sequence = sequence.upper()
            complement = "".join(complement_map.get(base, "N") for base in sequence)
            reverse_complement = complement[::-1]

            return ToolResult(
                success=True,
                data={
                    "original": sequence,
                    "complement": complement,
                    "reverse_complement": reverse_complement,
                    "length": len(sequence),
                },
            )
        except Exception as e:
            return ToolResult(success=False, error=str(e))

    registry.register(Tool(
        name="reverse_complement",
        description=(
            "Calculate the reverse complement of a DNA sequence. "
            "First complements each base (A<->T, G<->C), then reverses the sequence."
            "Useful for working with double-stranded DNA and primer design."
        ),
        parameters=[
            ToolParameter(
                name="sequence",
                type="string",
                description="The DNA sequence",
                required=True,
            ),
        ],
        handler=reverse_complement_handler,
        category="utilities",
    ))

    # ==================== Sequence Statistics ====================
    async def sequence_stats_handler(sequence: str) -> ToolResult:
        """Calculate basic sequence statistics."""
        try:
            sequence = sequence.upper()
            length = len(sequence)

            # Base counts
            counts = {
                "A": sequence.count("A"),
                "C": sequence.count("C"),
                "G": sequence.count("G"),
                "T": sequence.count("T"),
                "N": sequence.count("N"),
            }

            # GC content
            gc_count = counts["G"] + counts["C"]
            gc_percentage = (gc_count / length * 100) if length > 0 else 0

            # Dinucleotide frequencies
            dinucs = {}
            for i in range(len(sequence) - 1):
                dinuc = sequence[i : i + 2]
                dinucs[dinuc] = dinucs.get(dinuc, 0) + 1

            # Most common dinucleotides
            top_dinucs = sorted(dinucs.items(), key=lambda x: -x[1])[:5]

            return ToolResult(
                success=True,
                data={
                    "length": length,
                    "base_counts": counts,
                    "gc_percentage": round(gc_percentage, 2),
                    "top_dinucleotides": dict(top_dinucs),
                    "has_ambiguous": counts["N"] > 0,
                },
            )
        except Exception as e:
            return ToolResult(success=False, error=str(e))

    registry.register(Tool(
        name="sequence_statistics",
        description=(
            "Calculate comprehensive statistics for a DNA sequence including length, "
            "base composition, GC content, and dinucleotide frequencies. "
            "Useful for initial sequence characterization."
        ),
        parameters=[
            ToolParameter(
                name="sequence",
                type="string",
                description="The DNA sequence to analyze",
                required=True,
            ),
        ],
        handler=sequence_stats_handler,
        category="utilities",
    ))
