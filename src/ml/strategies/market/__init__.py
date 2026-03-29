"""Market model strategies - wrappers for external pre-trained models.

These strategies wrap established bioinformatics tools:
- Quality: Tracy, Phred
- Variants: DeepVariant, GATK
- Annotation: Prodigal, AUGUSTUS
- Phylo: FastTree, RAxML
- Functional: SIFT, PolyPhen-2, ViennaRNA
"""

from .functional import SIFTStrategy, ViennaRNAStrategy
from .quality import TracyQualityStrategy
from .variants import DeepVariantStrategy

__all__ = [
    "TracyQualityStrategy",
    "DeepVariantStrategy",
    "SIFTStrategy",
    "ViennaRNAStrategy",
]
