"""Species Discovery - Discover species and calculate adaptive download targets.

Unlike the barcode datalake, we do NOT filter by specific genes.
We want diverse genomic sequences with quality_enhanced data.
"""

import logging
import time
from collections import defaultdict
from dataclasses import dataclass, field
from typing import Optional

from Bio import Entrez

logger = logging.getLogger(__name__)


@dataclass
class SpeciesTarget:
    """Target for downloading a species."""
    taxon_id: int
    species_name: str
    kingdom: str
    available_count: int
    target_count: int
    taxonomy: dict = field(default_factory=dict)


@dataclass
class DiscoveryResult:
    """Result of species discovery."""
    kingdom: str
    total_species: int
    species_with_sequences: int
    total_available: int
    total_target: int
    species_targets: list[SpeciesTarget] = field(default_factory=list)

    def summary(self) -> str:
        return (
            f"Discovery: {self.kingdom}\n"
            f"  Species found: {self.total_species:,}\n"
            f"  With sequences: {self.species_with_sequences:,}\n"
            f"  Total available: {self.total_available:,}\n"
            f"  Total target: {self.total_target:,}"
        )


class SpeciesDiscovery:
    """Discovers species in NCBI and calculates adaptive download targets.

    Key differences from barcode datalake:
    - NO gene filtering - we want diverse genomic sequences
    - Focus on sequences with quality_enhanced data when possible
    - Adaptive targets to ensure diversity (not too many from common species)
    """

    # Kingdom search terms (5 classical kingdoms)
    # NO gene filtering - we want diverse genomic sequences
    KINGDOM_TERMS = {
        "animalia": "Metazoa[Organism]",
        "plantae": "Viridiplantae[Organism]",
        "fungi": "Fungi[Organism]",
        "protista": "(Eukaryota[Organism] NOT Metazoa[Organism] NOT Viridiplantae[Organism] NOT Fungi[Organism])",
        "monera": "(Bacteria[Organism] OR Archaea[Organism])",  # Prokaryotes
    }

    def __init__(
        self,
        email: str,
        api_key: str = "",
        min_seqs: int = 10,
        max_seqs: int = 200,
        common_threshold: int = 1000,
    ):
        self.email = email
        self.api_key = api_key
        self.min_seqs = min_seqs
        self.max_seqs = max_seqs
        self.common_threshold = common_threshold

        Entrez.email = email
        if api_key:
            Entrez.api_key = api_key

    def _rate_limit(self):
        """Enforce NCBI rate limits."""
        time.sleep(0.1 if self.api_key else 0.34)

    def discover_species(
        self,
        kingdom: str,
        max_species: int = 10000,
        min_length: int = 100,
        max_length: int = 50000,
        sample_size: int | None = None,
    ) -> DiscoveryResult:
        """Discover species with genomic sequences (NO gene filter).

        Args:
            kingdom: Biological kingdom
            max_species: Maximum species to return
            min_length: Minimum sequence length
            max_length: Maximum sequence length

        Returns:
            DiscoveryResult with species targets
        """
        kingdom_term = self.KINGDOM_TERMS.get(kingdom.lower(), f"{kingdom}[Organism]")

        # Build query - NO gene filter, just genomic sequences
        query = (
            f"{kingdom_term} AND "
            f"{min_length}:{max_length}[Sequence Length] AND "
            f"biomol_genomic[Properties]"
        )

        logger.info(f"Discovering species for {kingdom}...")
        logger.info(f"Query: {query}")

        # Get total count
        try:
            self._rate_limit()
            handle = Entrez.esearch(db="nucleotide", term=query, retmax=0)
            results = Entrez.read(handle)
            handle.close()
            total_count = int(results.get("Count", 0))
        except Exception as e:
            logger.error(f"Search failed: {e}")
            return DiscoveryResult(kingdom=kingdom, total_species=0, species_with_sequences=0, total_available=0, total_target=0)

        logger.info(f"Total sequences found: {total_count:,}")

        if total_count == 0:
            return DiscoveryResult(kingdom=kingdom, total_species=0, species_with_sequences=0, total_available=0, total_target=0)

        # Sample sequences to discover species
        # Use LARGE sample size to discover more species (key insight from GeneFlowDatalake)
        # 200,000 samples discovers 60k+ species per kingdom
        # For kingdoms with many sequences (like monera), use larger sample
        default_sample = 200000
        if sample_size is None:
            # Auto-scale based on total count
            if total_count > 50_000_000:  # >50M sequences (like monera)
                default_sample = 500000
            elif total_count > 10_000_000:  # >10M sequences
                default_sample = 300000
            sample_size = default_sample
        sample_size = min(total_count, sample_size)
        logger.info(f"Sampling {sample_size:,} sequences to discover species...")
        species_counts = self._sample_species_counts(query, sample_size)

        logger.info(f"Unique species discovered: {len(species_counts):,}")

        # Create targets with adaptive counts
        species_targets = []
        total_available = 0
        total_target = 0

        skipped_no_taxid = 0
        skipped_no_target = 0

        # For small profiles, iterate from most common (reliable data)
        # For large profiles, iterate from rarest (diversity)
        reverse_order = max_species <= 10
        for species_key, count in sorted(species_counts.items(), key=lambda x: x[1], reverse=reverse_order):
            if len(species_targets) >= max_species:
                break

            # Parse species key (organism|taxon_id)
            parts = species_key.split("|")
            organism = parts[0]
            taxon_id = int(parts[1]) if len(parts) > 1 and parts[1].isdigit() else 0

            # Skip invalid taxon_ids - NCBI returns 400 for taxon_id=0
            if taxon_id == 0:
                skipped_no_taxid += 1
                continue

            # Calculate adaptive target
            target = self.calculate_adaptive_target(count)

            if target <= 0:
                skipped_no_target += 1
                continue

            if target > 0:
                species_targets.append(SpeciesTarget(
                    taxon_id=taxon_id,
                    species_name=organism,
                    kingdom=kingdom,
                    available_count=count,
                    target_count=target,
                ))
                total_available += count
                total_target += target

        logger.debug(f"Species skipped - no taxon_id: {skipped_no_taxid}, no target: {skipped_no_target}")

        # For small targets, prioritize common species (more likely to have data)
        # For large targets, prioritize rare species (diversity)
        if max_species <= 10:
            # Small profile: most common species first (most reliable)
            species_targets.sort(key=lambda x: -x.available_count)
            logger.debug("Sorted by most common first (small profile)")
        else:
            # Large profile: rarest first for diversity
            species_targets.sort(key=lambda x: x.available_count)
            logger.debug("Sorted by rarest first (large profile)")

        result = DiscoveryResult(
            kingdom=kingdom,
            total_species=len(species_counts),
            species_with_sequences=len(species_targets),
            total_available=total_available,
            total_target=total_target,
            species_targets=species_targets,
        )

        logger.info(result.summary())
        return result

    def calculate_adaptive_target(self, available: int) -> int:
        """Calculate adaptive download target.

        Strategy:
        - Rare species (< min_seqs): download ALL
        - Uncommon (< 100): download min_seqs
        - Moderate (< 1000): download 50
        - Common (>= 1000): download max 100-200

        This ensures diversity - we don't over-sample common species.
        """
        if available <= 0:
            return 0

        if available < self.min_seqs:
            return available  # Rare: take all

        if available < 100:
            return self.min_seqs

        if available < self.common_threshold:
            return min(50, available)

        # Common species: cap at max_seqs
        return min(self.max_seqs, max(100, available // 10))

    def _sample_species_counts(self, query: str, sample_size: int) -> dict[str, int]:
        """Sample sequences and count by species."""
        species_counts = defaultdict(int)
        batch_size = 500
        fetch_batch = 10000

        try:
            # Fetch IDs
            all_ids = []
            for start in range(0, sample_size, fetch_batch):
                self._rate_limit()
                try:
                    handle = Entrez.esearch(
                        db="nucleotide",
                        term=query,
                        retstart=start,
                        retmax=min(fetch_batch, sample_size - start),
                    )
                    results = Entrez.read(handle)
                    handle.close()
                    all_ids.extend(results.get("IdList", []))

                    if start % 50000 == 0:
                        logger.info(f"  Fetched {len(all_ids):,} IDs...")

                except Exception as e:
                    logger.warning(f"Error fetching IDs at {start}: {e}")
                    time.sleep(1)

            logger.info(f"Fetched {len(all_ids):,} IDs, extracting species...")

            # Fetch summaries to extract species
            logged_sample = False
            for i in range(0, len(all_ids), batch_size):
                batch = all_ids[i:i + batch_size]
                self._rate_limit()

                try:
                    handle = Entrez.esummary(db="nucleotide", id=",".join(batch))
                    summaries = Entrez.read(handle)
                    handle.close()

                    # Debug: log first summary structure to understand format
                    if not logged_sample and summaries:
                        first = summaries[0] if isinstance(summaries, list) else summaries
                        if hasattr(first, "keys"):
                            logger.debug(f"Summary fields: {list(first.keys())}")
                        logged_sample = True

                    for summary in summaries:
                        organism = self._extract_organism(summary)
                        if organism:
                            taxon_id = self._extract_taxon_id(summary)
                            if taxon_id > 0:  # Only count if we have valid taxon_id
                                key = f"{organism}|{taxon_id}"
                                species_counts[key] += 1

                    if i % 10000 == 0 and i > 0:
                        logger.info(f"  Processed {i:,} summaries, {len(species_counts):,} species...")

                except Exception as e:
                    logger.warning(f"Error at batch {i}: {e}")

        except Exception as e:
            logger.error(f"Sampling failed: {e}")

        return dict(species_counts)

    # Invalid patterns to filter out
    INVALID_PATTERNS = {
        "unknown", "uncultured", "unidentified", "environmental",
        "predicted", "hypothetical", "synthetic", "artificial",
        "clone", "vector", "plasmid", "metagenome", "unclassified",
    }

    def _extract_taxon_id(self, summary: dict) -> int:
        """Extract taxon ID from NCBI summary."""
        # Try multiple field names (NCBI varies between API versions)
        for field in ["TaxId", "Taxid", "taxid", "TaxID"]:
            value = summary.get(field)
            # Check if value exists and is not None/empty
            # Note: Biopython returns IntegerElement which needs explicit int() conversion
            if value is not None:
                try:
                    tid = int(value)
                    if tid > 0:
                        return tid
                except (ValueError, TypeError):
                    continue

        # Try DocumentSummarySet format (newer NCBI API)
        if hasattr(summary, "items"):
            for key, value in summary.items():
                key_lower = key.lower()
                if "tax" in key_lower and ("id" in key_lower or key_lower == "taxid"):
                    if value is not None:
                        try:
                            tid = int(value)
                            if tid > 0:
                                return tid
                        except (ValueError, TypeError):
                            continue

        return 0

    def _extract_organism(self, summary: dict) -> Optional[str]:
        """Extract organism name from NCBI summary."""
        title = summary.get("Title", "")
        if not title:
            return None

        title_lower = title.lower()
        for invalid in self.INVALID_PATTERNS:
            if invalid in title_lower:
                return None

        parts = title.split()
        if len(parts) < 2:
            return None

        genus = parts[0]
        species = parts[1]

        # Validate
        if not genus[0].isupper() or len(genus) < 3:
            return None
        if species[0].isupper() or species[0].isdigit():
            return genus

        return f"{genus} {species}"
