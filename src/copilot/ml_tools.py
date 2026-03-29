"""ML-powered tools for the Molecular Biology Agent.

Connects the ML strategies to the agent's tool system.
"""

import structlog

from ..ml.strategies import (
    ModelKeys,
    StrategyType,
    get_provider,
    init_strategies,
)
from ..ml.strategies.base import StrategyResult

logger = structlog.get_logger()

# Initialize strategies on module load
_initialized = False


def ensuREDACTED() -> None:
    """Ensure ML strategies are initialized."""
    global _initialized
    if not _initialized:
        init_strategies(preferred_type=StrategyType.HEURISTIC)
        _initialized = True
        logger.info("ml_tools_initialized")


async def analyze_quality(quality_scores: list[int]) -> dict:
    """Analyze sequence quality using ML strategy.

    Args:
        quality_scores: List of Phred quality scores

    Returns:
        Quality analysis results
    """
    ensuREDACTED()
    provider = get_provider()
    strategy = provider.get(ModelKeys.QUALITY_PREDICTOR)

    if not strategy:
        return {"error": "Quality predictor not available"}

    result = await strategy.execute(quality_scores=quality_scores)
    return _format_result(result)


async def auto_trim(quality_scores: list[int]) -> dict:
    """Find optimal trim points for sequence.

    Args:
        quality_scores: List of Phred quality scores

    Returns:
        Trim start/end positions
    """
    ensuREDACTED()
    provider = get_provider()
    strategy = provider.get(ModelKeys.AUTO_TRIMMER)

    if not strategy:
        return {"error": "Auto trimmer not available"}

    result = await strategy.execute(quality_scores=quality_scores)
    return _format_result(result)


async def detect_artifacts(
    signal_a: list[float],
    signal_t: list[float],
    signal_c: list[float],
    signal_g: list[float],
) -> dict:
    """Detect chromatogram artifacts.

    Args:
        signal_a/t/c/g: Channel signals

    Returns:
        List of detected artifacts
    """
    ensuREDACTED()
    provider = get_provider()
    strategy = provider.get(ModelKeys.ARTIFACT_DETECTOR)

    if not strategy:
        return {"error": "Artifact detector not available"}

    result = await strategy.execute(
        signal_a=signal_a,
        signal_t=signal_t,
        signal_c=signal_c,
        signal_g=signal_g,
    )
    return _format_result(result)


async def call_snps(
    query_sequence: str,
    reference_sequence: str,
    quality_scores: list[int] | None = None,
) -> dict:
    """Detect SNPs between query and reference.

    Args:
        query_sequence: Query DNA sequence
        reference_sequence: Reference DNA sequence
        quality_scores: Optional quality scores

    Returns:
        List of detected SNPs
    """
    ensuREDACTED()
    provider = get_provider()
    strategy = provider.get(ModelKeys.SNP_CALLER)

    if not strategy:
        return {"error": "SNP caller not available"}

    result = await strategy.execute(
        query_sequence=query_sequence,
        reference_sequence=reference_sequence,
        quality_scores=quality_scores,
    )
    return _format_result(result)


async def detect_heterozygotes(
    signal_a: list[float],
    signal_t: list[float],
    signal_c: list[float],
    signal_g: list[float],
) -> dict:
    """Detect heterozygous positions from chromatogram.

    Args:
        signal_a/t/c/g: Channel signals

    Returns:
        List of heterozygous positions
    """
    ensuREDACTED()
    provider = get_provider()
    strategy = provider.get(ModelKeys.HETEROZYGOTE_DETECTOR)

    if not strategy:
        return {"error": "Heterozygote detector not available"}

    result = await strategy.execute(
        signal_a=signal_a,
        signal_t=signal_t,
        signal_c=signal_c,
        signal_g=signal_g,
    )
    return _format_result(result)


async def find_genes(sequence: str, min_orf_length: int = 100) -> dict:
    """Find genes/ORFs in sequence.

    Args:
        sequence: DNA sequence
        min_orf_length: Minimum ORF length in bp

    Returns:
        List of detected ORFs
    """
    ensuREDACTED()
    provider = get_provider()
    strategy = provider.get(ModelKeys.GENE_FINDER)

    if not strategy:
        return {"error": "Gene finder not available"}

    result = await strategy.execute(sequence=sequence, min_orf_length=min_orf_length)
    return _format_result(result)


async def scan_motifs(sequence: str, motifs: list[str] | None = None) -> dict:
    """Scan for regulatory motifs.

    Args:
        sequence: DNA sequence
        motifs: Specific motifs to search (or all)

    Returns:
        List of found motifs
    """
    ensuREDACTED()
    provider = get_provider()
    strategy = provider.get(ModelKeys.MOTIF_SCANNER)

    if not strategy:
        return {"error": "Motif scanner not available"}

    result = await strategy.execute(sequence=sequence, motifs=motifs)
    return _format_result(result)


async def cluster_sequences(
    sequences: list[str],
    threshold: float = 0.7,
) -> dict:
    """Cluster sequences by similarity.

    Args:
        sequences: List of DNA sequences
        threshold: Similarity threshold (0-1)

    Returns:
        Cluster assignments
    """
    ensuREDACTED()
    provider = get_provider()
    strategy = provider.get(ModelKeys.SEQUENCE_CLUSTERER)

    if not strategy:
        return {"error": "Sequence clusterer not available"}

    result = await strategy.execute(sequences=sequences, threshold=threshold)
    return _format_result(result)


async def calculate_diversity(sequences: list[str]) -> dict:
    """Calculate genetic diversity metrics.

    Args:
        sequences: List of aligned sequences

    Returns:
        Diversity metrics (Pi, Theta, Tajima's D)
    """
    ensuREDACTED()
    provider = get_provider()
    strategy = provider.get(ModelKeys.DIVERSITY_CALCULATOR)

    if not strategy:
        return {"error": "Diversity calculator not available"}

    result = await strategy.execute(sequences=sequences)
    return _format_result(result)


async def predict_mutation_impact(
    reference_codon: str,
    alternate_codon: str,
    position: int = 0,
) -> dict:
    """Predict functional impact of mutation.

    Args:
        reference_codon: Original codon (3 bases)
        alternate_codon: Mutated codon (3 bases)
        position: Position in sequence

    Returns:
        Impact prediction (benign/damaging)
    """
    ensuREDACTED()
    provider = get_provider()
    strategy = provider.get(ModelKeys.MUTATION_IMPACT)

    if not strategy:
        return {"error": "Mutation impact predictor not available"}

    result = await strategy.execute(
        reference_codon=reference_codon,
        alternate_codon=alternate_codon,
        position=position,
    )
    return _format_result(result)


async def predict_rna_structure(sequence: str) -> dict:
    """Predict RNA secondary structure.

    Args:
        sequence: RNA/DNA sequence

    Returns:
        Structure in dot-bracket notation
    """
    ensuREDACTED()
    provider = get_provider()
    strategy = provider.get(ModelKeys.RNA_STRUCTURE)

    if not strategy:
        return {"error": "RNA structure predictor not available"}

    result = await strategy.execute(sequence=sequence)
    return _format_result(result)


def _format_result(result: StrategyResult) -> dict:
    """Format strategy result for agent consumption."""
    return {
        "data": result.data,
        "confidence": result.confidence,
        "model": {
            "name": result.model_name,
            "version": result.model_version,
            "type": result.strategy_used.value,
        },
    }


# Tool definitions for the agent
ML_TOOLS = [
    {
        "name": "analyze_quality",
        "description": "Analyze sequence quality metrics (Q20, Q30, accuracy) from Phred scores",
        "input_schema": {
            "type": "object",
            "properties": {
                "quality_scores": {
                    "type": "array",
                    "items": {"type": "integer"},
                    "description": "List of Phred quality scores",
                }
            },
            "required": ["quality_scores"],
        },
    },
    {
        "name": "auto_trim",
        "description": "Find optimal trim points to remove low-quality regions from sequence ends",
        "input_schema": {
            "type": "object",
            "properties": {
                "quality_scores": {
                    "type": "array",
                    "items": {"type": "integer"},
                    "description": "List of Phred quality scores",
                }
            },
            "required": ["quality_scores"],
        },
    },
    {
        "name": "call_snps",
        "description": "Detect SNPs by comparing query sequence against reference",
        "input_schema": {
            "type": "object",
            "properties": {
                "query_sequence": {
                    "type": "string",
                    "description": "Query DNA sequence",
                },
                "reference_sequence": {
                    "type": "string",
                    "description": "Reference DNA sequence",
                },
            },
            "required": ["query_sequence", "reference_sequence"],
        },
    },
    {
        "name": "find_genes",
        "description": "Find ORFs and coding regions in DNA sequence",
        "input_schema": {
            "type": "object",
            "properties": {
                "sequence": {
                    "type": "string",
                    "description": "DNA sequence to analyze",
                },
                "min_orf_length": {
                    "type": "integer",
                    "description": "Minimum ORF length in bp (default 100)",
                },
            },
            "required": ["sequence"],
        },
    },
    {
        "name": "scan_motifs",
        "description": "Scan for regulatory motifs (TATA box, Kozak, splice sites, etc.)",
        "input_schema": {
            "type": "object",
            "properties": {
                "sequence": {
                    "type": "string",
                    "description": "DNA sequence to scan",
                },
            },
            "required": ["sequence"],
        },
    },
    {
        "name": "calculate_diversity",
        "description": "Calculate genetic diversity metrics (Pi, Theta, Tajima's D)",
        "input_schema": {
            "type": "object",
            "properties": {
                "sequences": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "List of aligned DNA sequences",
                },
            },
            "required": ["sequences"],
        },
    },
    {
        "name": "predict_mutation_impact",
        "description": "Predict functional impact of a codon mutation",
        "input_schema": {
            "type": "object",
            "properties": {
                "reference_codon": {
                    "type": "string",
                    "description": "Original codon (3 bases, e.g., 'ATG')",
                },
                "alternate_codon": {
                    "type": "string",
                    "description": "Mutated codon (3 bases, e.g., 'ATT')",
                },
            },
            "required": ["reference_codon", "alternate_codon"],
        },
    },
    {
        "name": "predict_rna_structure",
        "description": "Predict RNA secondary structure using minimum free energy folding",
        "input_schema": {
            "type": "object",
            "properties": {
                "sequence": {
                    "type": "string",
                    "description": "RNA or DNA sequence",
                },
            },
            "required": ["sequence"],
        },
    },
]


async def handle_ml_tool(tool_name: str, arguments: dict) -> dict:
    """Handle ML tool calls from the agent.

    Args:
        tool_name: Name of the ML tool
        arguments: Tool arguments

    Returns:
        Tool result
    """
    handlers = {
        "analyze_quality": lambda args: analyze_quality(args["quality_scores"]),
        "auto_trim": lambda args: auto_trim(args["quality_scores"]),
        "call_snps": lambda args: call_snps(
            args["query_sequence"],
            args["reference_sequence"],
            args.get("quality_scores"),
        ),
        "find_genes": lambda args: find_genes(
            args["sequence"],
            args.get("min_orf_length", 100),
        ),
        "scan_motifs": lambda args: scan_motifs(
            args["sequence"],
            args.get("motifs"),
        ),
        "calculate_diversity": lambda args: calculate_diversity(args["sequences"]),
        "predict_mutation_impact": lambda args: predict_mutation_impact(
            args["reference_codon"],
            args["alternate_codon"],
            args.get("position", 0),
        ),
        "predict_rna_structure": lambda args: predict_rna_structure(args["sequence"]),
    }

    handler = handlers.get(tool_name)
    if not handler:
        return {"error": f"Unknown ML tool: {tool_name}"}

    try:
        return await handler(arguments)
    except Exception as e:
        logger.error("ml_tool_error", tool=tool_name, error=str(e))
        return {"error": str(e)}
