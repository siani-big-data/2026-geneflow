"""ENA Parallel Downloader - Download from European Nucleotide Archive.

Uses the same output format as NCBI downloader for seamless integration.
"""

import gzip
import json
import logging
from concurrent.futures import ThreadPoolExecutor, as_completed
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from threading import Lock

from .ena_client import ENAClient, ENASequence
from .harvest_state import HarvestState
from .parallel_downloader import sanitize_path_component

logger = logging.getLogger(__name__)


@dataclass
class ENADownloadResult:
    """Result of downloading a species from ENA."""
    taxon_id: int
    species_name: str
    kingdom: str
    sequences_downloaded: int
    accessions: list[str]
    success: bool
    error: str = ""


class ENAParallelDownloader:
    """Download sequences from ENA in parallel.

    Advantages over NCBI:
    - No strict rate limits (~15-20 req/s is safe)
    - Simpler REST API
    - Same INSDC data

    Uses same output format as NCBI downloader.
    """

    def __init__(
        self,
        state_file: Path = Path("harvest_state.json"),
        output_dir: Path = Path("datalake"),
        sequences_per_species: int = 50,
        requests_per_second: float = 15.0,
        pool_size: int = 50,
    ):
        self.state_file = state_file
        self.output_dir = output_dir
        self.sequences_per_species = sequences_per_species

        # Initialize client with larger connection pool
        self.client = ENAClient(
            requests_per_second=requests_per_second,
            pool_size=pool_size,
        )

        # Load state (shared with NCBI downloader)
        self.state = HarvestState.load(state_file)
        self.state.target_sequences_per_species = sequences_per_species

        # Thread safety
        self._lock = Lock()
        self._save_counter = 0

        # Ensure output directory
        self.output_dir.mkdir(parents=True, exist_ok=True)

        logger.info(f"ENA Downloader initialized ({requests_per_second} req/s)")

    def download_species(
        self,
        taxon_id: int,
        species_name: str,
        kingdom: str,
        target_count: int,
    ) -> ENADownloadResult:
        """Download sequences for a single species."""
        # Get already downloaded (shared state with NCBI)
        exclude = self.state.get_downloaded_accessions(kingdom, taxon_id)
        needed = target_count - len(exclude)

        if needed <= 0:
            return ENADownloadResult(
                taxon_id=taxon_id,
                species_name=species_name,
                kingdom=kingdom,
                sequences_downloaded=0,
                accessions=[],
                success=True,
            )

        try:
            sequences = self.client.download_species_sequences(
                taxon_id=taxon_id,
                target_count=needed,
                exclude_accessions=exclude,
            )

            if sequences:
                # Get full taxonomy from ENA
                taxonomy = self.client.get_taxonomy(taxon_id)
                taxonomy["kingdom"] = kingdom

                # Save to disk
                self._save_sequences(kingdom, taxon_id, species_name, taxonomy, sequences)

                # Update state
                accessions = [s.accession for s in sequences]
                with self._lock:
                    self.state.record_species_download(
                        kingdom=kingdom,
                        taxon_id=taxon_id,
                        species_name=species_name,
                        accessions=accessions,
                        target_count=target_count,
                    )
                    self._save_counter += 1
                    if self._save_counter % 10 == 0:
                        self.state.save(self.state_file)

                return ENADownloadResult(
                    taxon_id=taxon_id,
                    species_name=species_name,
                    kingdom=kingdom,
                    sequences_downloaded=len(sequences),
                    accessions=accessions,
                    success=True,
                )

            return ENADownloadResult(
                taxon_id=taxon_id,
                species_name=species_name,
                kingdom=kingdom,
                sequences_downloaded=0,
                accessions=[],
                success=True,
            )

        except Exception as e:
            logger.error(f"ENA download failed for {species_name}: {e}")
            return ENADownloadResult(
                taxon_id=taxon_id,
                species_name=species_name,
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
        sequences: list[ENASequence],
    ):
        """Save sequences in same format as NCBI downloader."""
        # Build path from taxonomy (same structure as NCBI)
        path_parts = [sanitize_path_component(kingdom, 20)]

        # Add taxonomy hierarchy
        for rank in ["phylum", "class", "order", "family", "genus"]:
            value = taxonomy.get(rank, "")
            if value and value.lower() != "unknown":
                path_parts.append(sanitize_path_component(value, 40))

        # Add species
        path_parts.append(sanitize_path_component(species_name, 50))

        species_dir = self.output_dir / "raw" / "/".join(path_parts)
        species_dir.mkdir(parents=True, exist_ok=True)

        # Check if file exists (might have NCBI data)
        fasta_path = species_dir / f"{taxon_id}.fasta.gz"
        meta_path = species_dir / f"{taxon_id}.json"

        # Load existing data if any
        existing_accessions = set()
        existing_sequences = []

        if fasta_path.exists():
            try:
                with gzip.open(fasta_path, "rt") as f:
                    current_header = ""
                    current_seq = []
                    for line in f:
                        line = line.strip()
                        if line.startswith(">"):
                            if current_seq:
                                acc = current_header.split()[0]
                                existing_accessions.add(acc)
                                existing_sequences.append((current_header, "".join(current_seq)))
                            current_header = line[1:]
                            current_seq = []
                        else:
                            current_seq.append(line)
                    if current_seq:
                        acc = current_header.split()[0]
                        existing_accessions.add(acc)
                        existing_sequences.append((current_header, "".join(current_seq)))
            except Exception:
                pass

        # Add new sequences (skip duplicates)
        new_sequences = []
        for seq in sequences:
            if seq.accession not in existing_accessions:
                new_sequences.append(seq)
                existing_accessions.add(seq.accession)

        if not new_sequences:
            return

        # Write combined FASTA
        with gzip.open(fasta_path, "wt") as f:
            # Write existing
            for header, seq in existing_sequences:
                f.write(f">{header}\n")
                for i in range(0, len(seq), 80):
                    f.write(seq[i:i+80] + "\n")

            # Write new
            for seq in new_sequences:
                f.write(f">{seq.accession} {seq.description}\n")
                for i in range(0, len(seq.sequence), 80):
                    f.write(seq.sequence[i:i+80] + "\n")

        # Update metadata
        metadata = {}
        if meta_path.exists():
            try:
                with open(meta_path) as f:
                    metadata = json.load(f)
            except Exception:
                pass

        total_count = len(existing_sequences) + len(new_sequences)
        metadata.update({
            "taxon_id": taxon_id,
            "species_name": species_name,
            "taxonomy": taxonomy,
            "sequence_count": total_count,
            "accessions": list(existing_accessions),
            "total_length": (
                sum(len(s[1]) for s in existing_sequences)
                + sum(len(s.sequence) for s in new_sequences)
            ),
            "updated_at": datetime.now(timezone.utc).isoformat(),
            "sources": list(set(metadata.get("sources", []) + ["ENA"])),
        })

        with open(meta_path, "w") as f:
            json.dump(metadata, f, indent=2)

    def download_kingdom(
        self,
        kingdom: str,
        max_species: int = 10000,
        sample_size: int = 100000,
        max_workers: int = 5,
    ) -> dict:
        """Download sequences for a kingdom.

        Args:
            kingdom: Kingdom name
            max_species: Maximum species to download
            sample_size: Sample size for species discovery
            max_workers: Number of parallel workers

        Returns:
            Statistics dict
        """
        logger.info(f"\n{'='*60}")
        logger.info(f"ENA DISCOVERY: {kingdom.upper()}")
        logger.info("=" * 60)

        # Discover species
        species_data = self.client.discover_species(
            kingdom=kingdom,
            sample_size=sample_size,
        )

        if not species_data:
            logger.warning(f"No species found for {kingdom} in ENA")
            return {"kingdom": kingdom, "species": 0, "sequences": 0}

        # Filter already completed species
        targets = []
        already_complete = 0

        for species_name, data in species_data.items():
            taxon_id = data["taxon_id"]
            available = data["count"]

            if self.state.is_species_complete(kingdom, taxon_id):
                already_complete += 1
                continue

            # Calculate target
            target = min(self.sequences_per_species, available)
            if target > 0:
                targets.append({
                    "taxon_id": taxon_id,
                    "species_name": species_name,
                    "available": available,
                    "target": target,
                })

            if len(targets) >= max_species:
                break

        logger.info(f"Species discovered: {len(species_data):,}")
        logger.info(f"Already complete (skipped): {already_complete:,}")
        logger.info(f"Species to download: {len(targets):,}")

        if not targets:
            logger.info(f"All species already downloaded for {kingdom}")
            return {"kingdom": kingdom, "species": 0, "sequences": 0}

        # Download in parallel
        logger.info(f"\nDownloading with {max_workers} workers...")

        total_sequences = 0
        species_completed = 0

        with ThreadPoolExecutor(max_workers=max_workers) as executor:
            futures = {}
            for target in targets:
                future = executor.submit(
                    self.download_species,
                    target["taxon_id"],
                    target["species_name"],
                    kingdom,
                    target["target"],
                )
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
                    logger.error(f"Worker error for {target['species_name']}: {e}")

        # Final save
        self.state.save(self.state_file)

        logger.info(
            f"\n{kingdom.upper()} (ENA) complete: "
            f"{species_completed:,} species, {total_sequences:,} sequences"
        )

        return {
            "kingdom": kingdom,
            "source": "ENA",
            "species_discovered": len(species_data),
            "species_downloaded": species_completed,
            "sequences": total_sequences,
        }

    def get_summary(self) -> str:
        """Get current state summary."""
        return self.state.summary()
