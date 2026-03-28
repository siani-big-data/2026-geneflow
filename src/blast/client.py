"""NCBI BLAST API client for GeneFlow AI."""

import asyncio
import re
import time
from typing import Optional
from xml.etree import ElementTree

import httpx
import structlog

from src.config import Settings
from src.models import BlastHit

from .models import BlastJob, BlastSearchResult

logger = structlog.get_logger()


class BlastError(Exception):
    """BLAST operation error."""

    pass


class BlastTimeoutError(BlastError):
    """BLAST job timed out."""

    pass


class BlastClient:
    """Async client for NCBI BLAST API."""

    def __init__(self, settings: Settings):
        self._settings = settings
        self._base_url = settings.blast_base_url
        self._email = settings.blast_email
        self._api_key = settings.blast_api_key
        self._client: Optional[httpx.AsyncClient] = None

        self._jobs_submitted = 0
        self._jobs_completed = 0
        self._errors = 0

    @property
    def is_configured(self) -> bool:
        """Check if BLAST is configured (email required)."""
        return bool(self._email)

    @property
    def metrics(self) -> dict:
        """Get client metrics."""
        return {
            "jobsSubmitted": self._jobs_submitted,
            "jobsCompleted": self._jobs_completed,
            "errors": self._errors,
            "configured": self.is_configured,
        }

    async def _get_client(self) -> httpx.AsyncClient:
        """Get or create HTTP client."""
        if self._client is None:
            self._client = httpx.AsyncClient(timeout=60.0)
        return self._client

    async def close(self) -> None:
        """Close HTTP client."""
        if self._client:
            await self._client.aclose()
            self._client = None

    async def submit_search(
        self,
        sequence: str,
        program: str = "blastn",
        database: str = "nt",
    ) -> BlastJob:
        """
        Submit a BLAST search job.

        Args:
            sequence: DNA/protein sequence to search
            program: BLAST program (blastn, blastp, blastx, tblastn, tblastx)
            database: Database to search (nt, nr, refseq_rna, etc.)

        Returns:
            BlastJob with request ID

        Raises:
            BlastError: If submission fails
        """
        if not self.is_configured:
            raise BlastError("BLAST not configured. Set AI_BLAST_EMAIL.")

        client = await self._get_client()

        params = {
            "CMD": "Put",
            "PROGRAM": program,
            "DATABASE": database,
            "QUERY": sequence,
            "FORMAT_TYPE": "XML",
            "EMAIL": self._email,
        }

        if self._api_key:
            params["API_KEY"] = self._api_key

        try:
            response = await client.post(self._base_url, data=params)
            response.raise_for_status()

            # Parse RID from response
            rid = self._parse_rid(response.text)

            self._jobs_submitted += 1

            logger.info(
                "blast_job_submitted",
                rid=rid,
                program=program,
                database=database,
                sequence_length=len(sequence),
            )

            return BlastJob(rid=rid, status="WAITING", program=program, database=database)

        except httpx.HTTPError as e:
            self._errors += 1
            raise BlastError(f"Failed to submit BLAST job: {str(e)}") from e

    async def check_status(self, rid: str) -> str:
        """
        Check status of a BLAST job.

        Args:
            rid: Request ID

        Returns:
            Status string: WAITING, READY, or FAILED
        """
        client = await self._get_client()

        params = {
            "CMD": "Get",
            "RID": rid,
            "FORMAT_OBJECT": "SearchInfo",
        }

        try:
            response = await client.get(self._base_url, params=params)
            response.raise_for_status()

            return self._parse_status(response.text)

        except httpx.HTTPError as e:
            self._errors += 1
            raise BlastError(f"Failed to check job status: {str(e)}") from e

    async def get_results(self, rid: str) -> BlastSearchResult:
        """
        Get results of a completed BLAST job.

        Args:
            rid: Request ID

        Returns:
            BlastSearchResult with hits
        """
        client = await self._get_client()

        params = {
            "CMD": "Get",
            "RID": rid,
            "FORMAT_TYPE": "XML",
        }

        try:
            response = await client.get(self._base_url, params=params)
            response.raise_for_status()

            result = self._parse_xml_results(rid, response.text)
            self._jobs_completed += 1

            logger.info(
                "blast_results_retrieved",
                rid=rid,
                hit_count=result.hitCount,
            )

            return result

        except httpx.HTTPError as e:
            self._errors += 1
            raise BlastError(f"Failed to get results: {str(e)}") from e

    async def search(
        self,
        sequence: str,
        program: str = "blastn",
        database: str = "nt",
        timeout_seconds: int = 300,
        poll_interval: int = 10,
    ) -> BlastSearchResult:
        """
        Submit search and wait for results.

        Args:
            sequence: Sequence to search
            program: BLAST program
            database: Database to search
            timeout_seconds: Maximum wait time
            poll_interval: Seconds between status checks

        Returns:
            BlastSearchResult with hits

        Raises:
            BlastTimeoutError: If job times out
            BlastError: If job fails
        """
        start_time = time.time()

        # Submit job
        job = await self.submit_search(sequence, program, database)

        # Poll for completion
        elapsed = 0
        while elapsed < timeout_seconds:
            await asyncio.sleep(poll_interval)
            elapsed = time.time() - start_time

            status = await self.check_status(job.rid)

            logger.debug(
                "blast_job_polling",
                rid=job.rid,
                status=status,
                elapsed_seconds=int(elapsed),
            )

            if status == "READY":
                result = await self.get_results(job.rid)
                result.searchTimeSeconds = elapsed
                return result

            elif status == "FAILED":
                self._errors += 1
                raise BlastError(f"BLAST job {job.rid} failed")

        # Timeout
        self._errors += 1
        raise BlastTimeoutError(
            f"BLAST job {job.rid} timed out after {timeout_seconds}s"
        )

    def _parse_rid(self, response_text: str) -> str:
        """Parse RID from BLAST response."""
        # Look for RID in QBlastInfo
        match = re.search(r'RID\s*=\s*(\S+)', response_text)
        if match:
            return match.group(1)

        raise BlastError("Could not parse RID from response")

    def _parse_status(self, response_text: str) -> str:
        """Parse status from BLAST response."""
        # Check for Status in response
        match = re.search(r'Status=(\S+)', response_text)
        if match:
            status = match.group(1).upper()
            if status in ("WAITING", "READY", "FAILED"):
                return status

        # Check for ThereAreHits
        if "ThereAreHits=yes" in response_text:
            return "READY"

        return "WAITING"

    def _parse_xml_results(self, rid: str, xml_text: str) -> BlastSearchResult:
        """Parse BLAST XML results."""
        hits = []

        try:
            # Parse XML
            root = ElementTree.fromstring(xml_text)

            # Get query length
            query_len_elem = root.find('.//Iteration_query-len')
            query_length = int(query_len_elem.text) if query_len_elem is not None else 0

            # Get database
            db_elem = root.find('.//BlastOutput_db')
            database = db_elem.text if db_elem is not None else "nt"

            # Get program
            prog_elem = root.find('.//BlastOutput_program')
            program = prog_elem.text if prog_elem is not None else "blastn"

            # Parse hits
            for hit_elem in root.findall('.//Hit'):
                hit = self._parse_hit(hit_elem)
                if hit:
                    hits.append(hit)

            return BlastSearchResult(
                rid=rid,
                hits=hits,
                queryLength=query_length,
                database=database,
                program=program,
            )

        except ElementTree.ParseError as e:
            logger.error("xml_parse_error", error=str(e))
            return BlastSearchResult(rid=rid)

    def _parse_hit(self, hit_elem: ElementTree.Element) -> Optional[BlastHit]:
        """Parse a single hit from XML."""
        try:
            # Get hit info
            accession = hit_elem.findtext('Hit_accession', '')
            description = hit_elem.findtext('Hit_def', '')

            # Get best HSP
            hsp = hit_elem.find('.//Hsp')
            if hsp is None:
                return None

            score = float(hsp.findtext('Hsp_bit-score', '0'))
            e_value = float(hsp.findtext('Hsp_evalue', '1'))

            # Calculate identity
            identity_count = int(hsp.findtext('Hsp_identity', '0'))
            align_len = int(hsp.findtext('Hsp_align-len', '1'))
            identity = (identity_count / align_len * 100) if align_len > 0 else 0

            # Get positions
            query_start = int(hsp.findtext('Hsp_query-from', '0'))
            query_end = int(hsp.findtext('Hsp_query-to', '0'))
            subject_start = int(hsp.findtext('Hsp_hit-from', '0'))
            subject_end = int(hsp.findtext('Hsp_hit-to', '0'))

            # Extract organism from description
            organism = self._extract_organism(description)

            return BlastHit(
                accession=accession,
                description=description[:200],  # Limit length
                score=score,
                eValue=e_value,
                identity=round(identity, 2),
                queryStart=query_start,
                queryEnd=query_end,
                subjectStart=subject_start,
                subjectEnd=subject_end,
                organism=organism,
            )

        except (ValueError, TypeError) as e:
            logger.warning("hit_parse_error", error=str(e))
            return None

    def _extract_organism(self, description: str) -> Optional[str]:
        """Extract organism name from hit description."""
        # Common pattern: [Organism name]
        match = re.search(r'\[([^\]]+)\]', description)
        if match:
            return match.group(1)

        # Try to get first two words (genus species)
        words = description.split()
        if len(words) >= 2:
            return f"{words[0]} {words[1]}"

        return None
