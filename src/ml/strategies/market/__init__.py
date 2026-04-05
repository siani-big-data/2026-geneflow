"""Market model strategies - wrappers for external pre-trained models.

These strategies wrap established bioinformatics tools that provide
capabilities beyond what geneflow-analysis offers algorithmically:

- Quality: Tracy (chromatogram quality_enhanced)
- Functional: SIFT (mutation impact), ViennaRNA (RNA structure)

For basic analyses (ORF detection, trimming, SNP calling, diversity),
use geneflow-analysis which has robust algorithmic implementations.
"""

from .functional import SIFTStrategy, ViennaRNAStrategy
from .quality import TracyQualityStrategy

__all__ = [
    "TracyQualityStrategy",
    "SIFTStrategy",
    "ViennaRNAStrategy",
]
