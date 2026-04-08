"""Training data downloader for GeneFlow ML."""

from .ena_client import ENAClient, ENASequence
from .ena_downloader import ENAParallelDownloader
from .harvest_state import HarvestState
from .ncbi_client import NCBIClient
from .parallel_downloader import ParallelDownloader
from .species_discovery import DiscoveryResult, SpeciesDiscovery, SpeciesTarget

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
