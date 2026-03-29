"""External API clients for bioinformatics databases.

All APIs are free to use:
- Ensembl: Gene annotation, genomic data
- ClinVar: Clinical significance of variants
- InterPro: Protein domains and functions
"""

from .base import APIResponse, ExternalAPIClient
from .clinvar import ClinVarClient
from .ensembl import EnsemblClient
from .interpro import InterProClient

__all__ = [
    "ExternalAPIClient",
    "APIResponse",
    "EnsemblClient",
    "ClinVarClient",
    "InterProClient",
]
