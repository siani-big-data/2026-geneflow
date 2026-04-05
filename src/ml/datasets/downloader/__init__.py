"""Training data downloader for GeneFlow ML."""

from .ncbi_client import NCBIClient
from .parallel_downloader import ParallelDownloader
from .harvest_state import HarvestState
from .species_discovery import SpeciesDiscovery, SpeciesTarget, DiscoveryResult
from .ena_client import ENAClient, ENASequence
from .ena_downloader import ENAParallelDownloader

__all__ = [
    # NCBI
    "NCBIClient",
    "ParallelDownloader",
    "HarvestState",
    "SpeciesDiscovery",
    "SpeciesTarget",
    "DiscoveryResult",
    # ENA
    "ENAClient",
    "ENASequence",
    "ENAParallelDownloader",
]
