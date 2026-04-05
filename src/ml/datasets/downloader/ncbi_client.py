"""NCBI Entrez client for downloading sequences with quality_enhanced data."""

import time
import logging
import gzip
from dataclasses import dataclass
from typing import Optional
from io import StringIO

from Bio import Entrez, SeqIO

logger = logging.getLogger(__name__)


@dataclass
class SequenceRecord:
    """Downloaded sequence with quality_enhanced and taxonomy."""
    accession: str
    sequence: str
    quality_scores: list[int] | None  # Phred scores if available
    organism: str
    taxonomy: dict  # Full taxonomy lineage
    description: str
    length: int
    format: str  # fasta or fastq


class NCBIClient:
    """Client for downloading sequences from NCBI."""

    # Taxonomic ranks
    RANKS = ["superkingdom", "kingdom", "phylum", "class", "order", "family", "genus", "species"]

    def __init__(self, email: str, api_key: str = "", batch_size: int = 100):
        self.email = email
        self.api_key = api_key
        self.batch_size = batch_size
        self._request_count = 0
        self._last_request = 0

        Entrez.email = email
        if api_key:
            Entrez.api_key = api_key

    def _rate_limit(self):
        """Enforce NCBI rate limits."""
        min_interval = 0.1 if self.api_key else 0.34
        elapsed = time.time() - self._last_request
        if elapsed < min_interval:
            time.sleep(min_interval - elapsed)
        self._last_request = time.time()
        self._request_count += 1

    def search_sra(
        self,
        organism: str = "",
        platform: str = "",
        max_results: int = 1000,
    ) -> list[str]:
        """Search SRA for sequencing runs."""
        self._rate_limit()

        query_parts = []
        if organism:
            query_parts.append(f'"{organism}"[Organism]')
        if platform:
            query_parts.append(f'{platform}[Platform]')

        # Filter for runs with quality_enhanced data
        query_parts.append("biomol_dna[Properties]")

        query = " AND ".join(query_parts) if query_parts else "all[filter]"

        try:
            handle = Entrez.esearch(db="sra", term=query, retmax=max_results, usehistory="y")
            results = Entrez.read(handle)
            handle.close()
            return results.get("IdList", [])
        except Exception as e:
            logger.error(f"SRA search failed: {e}")
            return []

    def search_nucleotide(
        self,
        organism: str = "",
        gene: str = "",
        max_results: int = 1000,
        min_length: int = 100,
        max_length: int = 10000,
    ) -> list[str]:
        """Search nucleotide database for sequences."""
        self._rate_limit()

        query_parts = []
        if organism:
            query_parts.append(f'"{organism}"[Organism]')
        if gene:
            query_parts.append(f'({gene}[Gene] OR {gene}[Title])')
        query_parts.append(f"{min_length}:{max_length}[Sequence Length]")
        query_parts.append("biomol_genomic[Properties]")

        query = " AND ".join(query_parts)

        try:
            handle = Entrez.esearch(db="nucleotide", term=query, retmax=max_results, usehistory="y")
            results = Entrez.read(handle)
            handle.close()
            return results.get("IdList", [])
        except Exception as e:
            logger.error(f"Nucleotide search failed: {e}")
            return []

    def search_by_taxon(
        self,
        taxon_id: int,
        db: str = "nucleotide",
        max_results: int = 500,
        min_length: int = 100,
        max_length: int = 50000,
    ) -> tuple[list[str], dict]:
        """Search by NCBI taxonomy ID - NO gene filter, diverse genomic sequences.

        Returns:
            Tuple of (id_list, history_params) where history_params contains
            WebEnv and query_key for use with efetch.
        """
        self._rate_limit()

        # NO gene filter - we want diverse sequences, not just barcodes
        query = (
            f"txid{taxon_id}[Organism] AND "
            f"biomol_genomic[Properties] AND "
            f"{min_length}:{max_length}[Sequence Length]"
        )

        try:
            handle = Entrez.esearch(db=db, term=query, retmax=max_results, usehistory="y")
            results = Entrez.read(handle)
            handle.close()

            history = {
                "WebEnv": results.get("WebEnv", ""),
                "query_key": results.get("QueryKey", ""),
            }

            return results.get("IdList", []), history
        except Exception as e:
            logger.error(f"Taxon search failed: {e}")
            return [], {}

    def get_taxonomy(self, taxon_id: int) -> dict:
        """Get full taxonomy lineage for a taxon ID."""
        self._rate_limit()

        try:
            handle = Entrez.efetch(db="taxonomy", id=str(taxon_id), retmode="xml")
            records = Entrez.read(handle)
            handle.close()

            if not records:
                return {}

            record = records[0]
            taxonomy = {
                "taxon_id": taxon_id,
                "scientific_name": record.get("ScientificName", ""),
                "rank": record.get("Rank", ""),
            }

            # Parse lineage
            lineage = record.get("LineageEx", [])
            for item in lineage:
                rank = item.get("Rank", "").lower()
                if rank in self.RANKS:
                    taxonomy[rank] = item.get("ScientificName", "")

            # Add self as species if applicable
            if record.get("Rank", "").lower() == "species":
                taxonomy["species"] = record.get("ScientificName", "")

            return taxonomy

        except Exception as e:
            logger.error(f"Taxonomy fetch failed for {taxon_id}: {e}")
            return {}

    def fetch_sequences_with_history(
        self,
        history: dict,
        count: int,
        db: str = "nucleotide",
    ) -> list[SequenceRecord]:
        """Fetch sequences using NCBI history (WebEnv/query_key)."""
        if not history.get("WebEnv") or not history.get("query_key"):
            return []

        records = []
        self._rate_limit()

        try:
            handle = Entrez.efetch(
                db=db,
                query_key=history["query_key"],
                WebEnv=history["WebEnv"],
                rettype="fasta",
                retmode="text",
                retmax=count,
            )

            content = handle.read()
            handle.close()

            if not content or not content.strip():
                logger.warning("Empty response from history fetch")
                return []

            logger.debug(f"History fetch response length: {len(content)}")

            from io import StringIO
            fasta_handle = StringIO(content)
            for seq_record in SeqIO.parse(fasta_handle, "fasta"):
                record = self._parse_fasta_record(seq_record)
                if record:
                    records.append(record)

            logger.debug(f"Parsed {len(records)} records from history fetch")

        except Exception as e:
            logger.error(f"History fetch failed: {e}")

        return records

    def fetch_sequences(
        self,
        accession_ids: list[str],
        db: str = "nucleotide",
        format: str = "fasta",
    ) -> list[SequenceRecord]:
        """Fetch sequences by accession IDs."""
        records = []

        for i in range(0, len(accession_ids), self.batch_size):
            batch = accession_ids[i:i + self.batch_size]
            self._rate_limit()

            try:
                # Use FASTA format - more reliable for sequence content
                handle = Entrez.efetch(
                    db=db,
                    id=",".join(batch),
                    rettype="fasta",
                    retmode="text",
                )

                content = handle.read()
                handle.close()

                logger.debug(f"efetch response length: {len(content) if content else 0}")
                if content:
                    logger.debug(f"efetch response preview: {content[:200]}")

                if not content or not content.strip():
                    logger.warning(f"Empty response for batch starting at {i}")
                    continue

                # Parse FASTA from string
                from io import StringIO
                fasta_handle = StringIO(content)
                parsed_count = 0
                for seq_record in SeqIO.parse(fasta_handle, "fasta"):
                    parsed_count += 1
                    record = self._parse_fasta_record(seq_record)
                    if record:
                        records.append(record)

                logger.debug(f"Batch {i}: parsed {parsed_count} records, kept {len(records)}")

            except Exception as e:
                logger.error(f"Fetch failed for batch: {e}")
                continue

        return records

    def _parse_fasta_record(self, seq_record) -> Optional[SequenceRecord]:
        """Parse a FASTA SeqRecord."""
        try:
            sequence = str(seq_record.seq)
            if not sequence or len(sequence) < 10:
                return None

            # Extract organism from description (format: "accession organism gene...")
            description = seq_record.description
            organism = "Unknown"
            parts = description.split(" ", 1)
            if len(parts) > 1:
                # Try to extract species name (first two words after accession)
                desc_parts = parts[1].split()
                if len(desc_parts) >= 2:
                    organism = f"{desc_parts[0]} {desc_parts[1]}"

            return SequenceRecord(
                accession=seq_record.id,
                sequence=sequence,
                quality_scores=None,
                organism=organism,
                taxonomy={"organism": organism},
                description=description,
                length=len(sequence),
                format="fasta",
            )

        except Exception as e:
            logger.error(f"Parse FASTA failed: {e}")
            return None

    def _parse_genbank_record(self, seq_record) -> Optional[SequenceRecord]:
        """Parse a BioPython SeqRecord from GenBank format."""
        try:
            # Get taxonomy
            taxonomy_list = seq_record.annotations.get("taxonomy", [])
            organism = seq_record.annotations.get("organism", "Unknown")

            # Build taxonomy dict
            taxonomy = {"organism": organism}
            for i, rank in enumerate(self.RANKS):
                if i < len(taxonomy_list):
                    taxonomy[rank] = taxonomy_list[i]

            # Try to get taxon_id from db_xrefs
            for feature in seq_record.features:
                if feature.type == "source":
                    for xref in feature.qualifiers.get("db_xref", []):
                        if xref.startswith("taxon:"):
                            taxonomy["taxon_id"] = int(xref.split(":")[1])
                            break

            sequence = str(seq_record.seq)
            if not sequence or "undefined" in sequence.lower():
                return None

            return SequenceRecord(
                accession=seq_record.id,
                sequence=sequence,
                quality_scores=None,
                organism=organism,
                taxonomy=taxonomy,
                description=seq_record.description,
                length=len(sequence),
                format="fasta",
            )

        except Exception as e:
            logger.error(f"Parse failed: {e}")
            return None

    def download_species_sequences(
        self,
        taxon_id: int,
        target_count: int = 50,
        exclude_accessions: set[str] = None,
    ) -> tuple[list[SequenceRecord], dict]:
        """Download diverse genomic sequences for a species (NO gene filter)."""
        exclude_accessions = exclude_accessions or set()

        logger.debug(f"Downloading sequences for taxon_id={taxon_id}, target={target_count}")

        # Get taxonomy
        taxonomy = self.get_taxonomy(taxon_id)
        if not taxonomy:
            logger.warning(f"No taxonomy found for taxon_id={taxon_id}")

        # Search for sequences - NO gene filter, diverse genomic data
        accession_ids, history = self.search_by_taxon(
            taxon_id,
            max_results=target_count * 2,
            min_length=100,
            max_length=50000,
        )

        logger.debug(f"Found {len(accession_ids)} IDs for taxon_id={taxon_id}: {accession_ids[:5]}")

        # Filter excluded
        accession_ids = [a for a in accession_ids if a not in exclude_accessions][:target_count]

        if not accession_ids:
            logger.warning(f"No IDs to fetch for taxon_id={taxon_id}")
            return [], taxonomy

        # Fetch sequences using history (more reliable than passing IDs directly)
        logger.debug(f"Fetching {len(accession_ids)} sequences...")
        records = self.fetch_sequences_with_history(history, len(accession_ids))

        if not records:
            # Fallback to direct ID fetch
            logger.debug("History fetch failed, trying direct ID fetch...")
            records = self.fetch_sequences(accession_ids)

        logger.debug(f"Fetched {len(records)} records for taxon_id={taxon_id}")

        # Add taxonomy to each record
        for record in records:
            record.taxonomy.update(taxonomy)

        return records, taxonomy

    @property
    def stats(self) -> dict:
        return {"requests": self._request_count}
