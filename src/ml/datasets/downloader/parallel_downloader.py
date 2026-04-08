"""Parallel downloader using multiple NCBI API keys."""

import gzip
import json
import logging
from concurrent.futures import ThreadPoolExecutor, as_completed
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from threading import Lock

from .harvest_state import HarvestState
from .ncbi_client import NCBIClient
from .species_discovery import SpeciesDiscovery, SpeciesTarget

logger = logging.getLogger(__name__)

# Lazy import to avoid circular dependencies
_minio_storage = None

def get_minio_storage():
    """Get MinIO storage instance (lazy load)."""
    global _minio_storage
    if _minio_storage is None:
        try:
            from src.storage.minio_client import MinioStorage
            _minio_storage = MinioStorage()
        except Exception as e:
            logger.warning(f"MinIO not available: {e}")
            _minio_storage = False
    return _minio_storage if _minio_storage else None


# Kingdom distribution targets (5 classical kingdoms)
KINGDOM_CONFIG = {
    "animalia": {"percentage": 0.30},
    "plantae": {"percentage": 0.25},
    "fungi": {"percentage": 0.15},
    "monera": {"percentage": 0.20},    # Bacteria + Archaea
    "protista": {"percentage": 0.10},
}


def sanitize_path_component(name: str, max_length: int = 50) -> str:
    """Sanitize a string for use as a path component.

    Removes/replaces characters that are invalid on Windows/Unix filesystems.
    """
    if not name:
        return "unknown"

    # Characters invalid on Windows: < > : " / \ | ? *
    # Also handle other problematic characters
    invalid_chars = '<>:"/\\|?*\r\n\t'
    result = name.lower()

    for char in invalid_chars:
        result = result.replace(char, "_")

    # Replace spaces and other problematic chars
    result = result.replace(" ", "_")

    # Remove leading/trailing dots and spaces (Windows doesn't like them)
    result = result.strip(". ")

    # Collapse multiple underscores
    while "__" in result:
        result = result.replace("__", "_")

    # Truncate
    result = result[:max_length]

    # Remove trailing underscores after truncation
    result = result.rstrip("_")

    return result if result else "unknown"


@dataclass
class DownloadResult:
    """Result of downloading a species."""
    taxon_id: int
    species_name: str
    kingdom: str
    sequences_downloaded: int
    accessions: list
    success: bool
    error: str = ""


class ParallelDownloader:
    """Download sequences using multiple API keys in parallel.

    Key features:
    - Uses species discovery to find species first
    - Adaptive targets (rare species get all, common get capped)
    - NO gene filtering - diverse genomic sequences
    - Parallel download with multiple API keys
    """

    def __init__(
        self,
        email: str,
        api_keys: list[str],
        state_file: Path = Path("harvest_state.json"),
        output_dir: Path = Path("datalake"),
        sequences_per_species: int = 50,
        upload_to_minio: bool = False,
        minio_prefix: str = "training/raw",
    ):
        self.email = email
        self.api_keys = api_keys if api_keys else [""]
        self.state_file = state_file
        self.output_dir = output_dir
        self.sequences_per_species = sequences_per_species
        self.upload_to_minio = upload_to_minio
        self.minio_prefix = minio_prefix
        self._minio = None

        # Initialize MinIO if requested
        if upload_to_minio:
            self._minio = get_minio_storage()
            if self._minio:
                self._minio.ensuREDACTED()
                logger.info(f"MinIO upload enabled, prefix: {minio_prefix}")
            else:
                logger.warning("MinIO requested but not available, saving locally only")

        # Create clients for each API key
        self.clients = [
            NCBIClient(email=email, api_key=key, batch_size=50)
            for key in self.api_keys
        ]

        # Species discovery (uses first API key)
        # Adapt min/max based on sequences_per_species
        self.discovery = SpeciesDiscovery(
            email=email,
            api_key=self.api_keys[0] if self.api_keys else "",
            min_seqs=max(1, min(10, sequences_per_species)),  # At least 1, at most 10
            max_seqs=min(200, max(sequences_per_species, sequences_per_species * 2)),
        )

        # Load state
        self.state = HarvestState.load(state_file)
        self.state.target_sequences_per_species = sequences_per_species

        # Thread safety
        self._lock = Lock()
        self._save_counter = 0

        # Ensure output directory
        self.output_dir.mkdir(parents=True, exist_ok=True)

        num_keys = len(self.clients)
        logger.info(f"Initialized with {num_keys} API keys ({num_keys * 10} req/s)")

    def _get_client(self, worker_id: int) -> NCBIClient:
        """Get client for worker."""
        return self.clients[worker_id % len(self.clients)]

    def download_species(
        self,
        worker_id: int,
        target: SpeciesTarget,
    ) -> DownloadResult:
        """Download sequences for a single species."""
        client = self._get_client(worker_id)
        kingdom = target.kingdom
        taxon_id = target.taxon_id

        # Get already downloaded
        exclude = self.state.get_downloaded_accessions(kingdom, taxon_id)
        needed = target.target_count - len(exclude)

        if needed <= 0:
            return DownloadResult(
                taxon_id=taxon_id,
                species_name=target.species_name,
                kingdom=kingdom,
                sequences_downloaded=0,
                accessions=[],
                success=True,
            )

        try:
            records, taxonomy = client.download_species_sequences(
                taxon_id=taxon_id,
                target_count=needed,
                exclude_accessions=exclude,
            )

            if records:
                # Save to disk
                self._save_sequences(kingdom, taxon_id, target.species_name, taxonomy, records)

                # Update state
                accessions = [r.accession for r in records]
                with self._lock:
                    self.state.record_species_download(
                        kingdom=kingdom,
                        taxon_id=taxon_id,
                        species_name=target.species_name,
                        accessions=accessions,
                        target_count=target.target_count,
                    )
                    self._save_counter += 1
                    if self._save_counter % 10 == 0:
                        self.state.save(self.state_file)

                return DownloadResult(
                    taxon_id=taxon_id,
                    species_name=target.species_name,
                    kingdom=kingdom,
                    sequences_downloaded=len(records),
                    accessions=accessions,
                    success=True,
                )

            return DownloadResult(
                taxon_id=taxon_id,
                species_name=target.species_name,
                kingdom=kingdom,
                sequences_downloaded=0,
                accessions=[],
                success=True,
            )

        except Exception as e:
            logger.error(f"Download failed for {target.species_name}: {e}")
            return DownloadResult(
                taxon_id=taxon_id,
                species_name=target.species_name,
                kingdom=kingdom,
                sequences_downloaded=0,
                accessions=[],
                success=False,
                error=str(e),
            )

    def _save_sequences(
        self,
        kingdom: str,
        taxon_id: int,
        species_name: str,
        taxonomy: dict,
        records: list,
    ):
        """Save sequences organized by taxonomy."""
        # Build path from taxonomy
        path_parts = [sanitize_path_component(kingdom, 20)]
        for rank in ["phylum", "class", "order", "family", "genus"]:
            value = taxonomy.get(rank, "unknown")
            if value and value != "unknown":
                path_parts.append(sanitize_path_component(value, 40))

        # Add species
        path_parts.append(sanitize_path_component(species_name, 50))

        species_dir = self.output_dir / "raw" / "/".join(path_parts)
        species_dir.mkdir(parents=True, exist_ok=True)

        # Save as FASTA.gz
        fasta_path = species_dir / f"{taxon_id}.fasta.gz"
        with gzip.open(fasta_path, "wt") as f:
            for record in records:
                f.write(f">{record.accession} {record.description}\n")
                seq = record.sequence
                for i in range(0, len(seq), 80):
                    f.write(seq[i:i+80] + "\n")

        # Save metadata
        meta_path = species_dir / f"{taxon_id}.json"
        metadata = {
            "taxon_id": taxon_id,
            "species_name": species_name,
            "taxonomy": taxonomy,
            "sequence_count": len(records),
            "accessions": [r.accession for r in records],
            "total_length": sum(r.length for r in records),
            "downloaded_at": datetime.now(timezone.utc).isoformat(),
        }
        with open(meta_path, "w") as f:
            json.dump(metadata, f, indent=2)

        # Upload to MinIO if enabled
        if self._minio:
            try:
                relative_path = "/".join(path_parts)
                fasta_object = f"{self.minio_prefix}/{relative_path}/{taxon_id}.fasta.gz"
                meta_object = f"{self.minio_prefix}/{relative_path}/{taxon_id}.json"

                self._minio.upload_file(fasta_path, fasta_object)
                self._minio.upload_file(meta_path, meta_object)
                logger.debug(f"Uploaded to MinIO: {fasta_object}")
            except Exception as e:
                logger.warning(f"MinIO upload failed for {taxon_id}: {e}")

    def download_kingdom(
        self,
        kingdom: str,
        max_species: int = 10000,
        max_workers: int = None,
        sample_size: int = None,
    ) -> dict:
        """Download sequences for a kingdom using adaptive discovery.

        Args:
            kingdom: Kingdom name
            max_species: Maximum species to download
            max_workers: Number of parallel workers
            sample_size: Number of sequences to sample for discovery (default: auto)
        """
        max_workers = max_workers or len(self.clients)

        # Discover species with adaptive targets
        logger.info(f"\n{'='*60}")
        logger.info(f"DISCOVERING SPECIES: {kingdom.upper()}")
        logger.info("=" * 60)

        discovery = self.discovery.discover_species(kingdom, max_species, sample_size=sample_size)

        if not discovery.species_targets:
            logger.warning(f"No species found for {kingdom}")
            return {"kingdom": kingdom, "species": 0, "sequences": 0}

        # Filter already completed species
        already_complete = sum(
            1 for t in discovery.species_targets
            if self.state.is_species_complete(kingdom, t.taxon_id)
        )
        targets = [
            t for t in discovery.species_targets
            if not self.state.is_species_complete(kingdom, t.taxon_id)
        ]

        logger.info(f"Species discovered: {len(discovery.species_targets):,}")
        logger.info(f"Already complete (skipped): {already_complete:,}")
        logger.info(f"Species to download: {len(targets):,}")
        logger.info(f"Expected sequences: {sum(t.target_count for t in targets):,}")

        if not targets:
            logger.info(f"All species already downloaded for {kingdom}")
            return {"kingdom": kingdom, "species": 0, "sequences": 0}

        # Download in parallel
        logger.info(f"\nDownloading with {max_workers} workers...")

        total_sequences = 0
        species_completed = 0

        with ThreadPoolExecutor(max_workers=max_workers) as executor:
            futures = {}
            for i, target in enumerate(targets):
                future = executor.submit(self.download_species, i % max_workers, target)
                futures[future] = target

            for future in as_completed(futures):
                target = futures[future]
                try:
                    result = future.result()
                    if result.success and result.sequences_downloaded > 0:
                        total_sequences += result.sequences_downloaded
                        species_completed += 1

                        if species_completed % 50 == 0:
                            logger.info(
                                f"[{species_completed}/{len(targets)}] "
                                f"{total_sequences:,} sequences"
                            )

                except Exception as e:
                    logger.error(f"Worker error for {target.species_name}: {e}")

        # Final save
        self.state.save(self.state_file)

        msg = f"\n{kingdom.upper()} complete: "
        msg += f"{species_completed:,} species, {total_sequences:,} sequences"
        logger.info(msg)

        return {
            "kingdom": kingdom,
            "species_discovered": len(discovery.species_targets),
            "species_downloaded": species_completed,
            "sequences": total_sequences,
        }

    def download_all(
        self,
        total_species: int = 50000,
        max_workers: int = None,
    ) -> dict:
        """Download from all kingdoms with proportional distribution."""
        results = {}

        for kingdom, config in KINGDOM_CONFIG.items():
            target = int(total_species * config["percentage"])

            logger.info(f"\n{'='*70}")
            logger.info(f"KINGDOM: {kingdom.upper()} (target: {target:,} species)")
            logger.info("=" * 70)

            result = self.download_kingdom(kingdom, target, max_workers)
            results[kingdom] = result

        # Summary
        total_seqs = sum(r.get("sequences", 0) for r in results.values())
        total_sp = sum(r.get("species_downloaded", 0) for r in results.values())

        logger.info(f"\n{'='*70}")
        logger.info("DOWNLOAD COMPLETE")
        logger.info(f"Total species: {total_sp:,}")
        logger.info(f"Total sequences: {total_seqs:,}")
        logger.info("=" * 70)

        return results

    def get_summary(self) -> str:
        """Get current state summary."""
        return self.state.summary()
