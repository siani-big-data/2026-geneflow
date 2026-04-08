"""NCBI Trace Archive client for downloading Sanger sequencing data."""

import asyncio
import gzip
import json
from dataclasses import dataclass, field
from datetime import datetime

import httpx
import structlog

from src.storage import minio_storage

logger = structlog.get_logger()

# NCBI Trace Archive endpoints
TRACE_BASE_URL = "https://trace.ncbi.nlm.nih.gov/Traces"
EUTILS_BASE_URL = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils"


@dataclass
class TraceMetadata:
    """Metadata for a trace file."""

    trace_id: str
    organism: str = ""
    center: str = ""
    trace_type: str = ""
    clip_quality_left: int = 0
    clip_quality_right: int = 0
    basecall_length: int = 0
    download_url: str = ""
    downloaded_at: str = ""

    def to_dict(self) -> dict:
        return {
            "traceId": self.trace_id,
            "organism": self.organism,
            "center": self.center,
            "traceType": self.trace_type,
            "clipQualityLeft": self.clip_quality_left,
            "clipQualityRight": self.clip_quality_right,
            "basecallLength": self.basecall_length,
            "downloadUrl": self.download_url,
            "downloadedAt": self.downloaded_at,
        }


@dataclass
class DownloadResult:
    """Result of a trace download."""

    trace_id: str
    success: bool
    minio_path: str = ""
    metadata_path: str = ""
    file_size: int = 0
    error: str = ""


@dataclass
class DownloadStats:
    """Statistics for a download batch."""

    total: int = 0
    successful: int = 0
    failed: int = 0
    total_bytes: int = 0
    results: list[DownloadResult] = field(default_factory=list)

    def to_dict(self) -> dict:
        return {
            "total": self.total,
            "successful": self.successful,
            "failed": self.failed,
            "totalBytes": self.total_bytes,
            "successRate": (
                f"{(self.successful / self.total * 100):.1f}%"
                if self.total > 0 else "0%"
            ),
        }


class NCBITraceClient:
    """Client for downloading traces from NCBI Trace Archive."""

    def __init__(
        self,
        email: str = "",
        api_key: str = "",
        timeout: float = 60.0,
        max_concurrent: int = 3,
    ):
        self.email = email
        self.api_key = api_key
        self.timeout = timeout
        self.max_concurrent = max_concurrent
        self._semaphore = asyncio.Semaphore(max_concurrent)
        self._client: httpx.AsyncClient | None = None

    async def _get_client(self) -> httpx.AsyncClient:
        if self._client is None:
            self._client = httpx.AsyncClient(
                timeout=self.timeout,
                headers={
                    "User-Agent": "GeneFlow-AI/1.0",
                },
                follow_redirects=True,
            )
        return self._client

    async def close(self) -> None:
        if self._client:
            await self._client.aclose()
            self._client = None

    async def search_traces(
        self,
        organism: str = "Homo sapiens",
        trace_type: str = "SANGER",
        max_results: int = 10,
    ) -> list[str]:
        """Search for trace IDs using NCBI Entrez."""
        client = await self._get_client()

        # Build search query
        query = f'"{organism}"[ORGANISM] AND {trace_type}[TRACE_TYPE]'

        params = {
            "db": "nuccore",  # Using nuccore as trace db is deprecated
            "term": query,
            "retmax": max_results,
            "retmode": "json",
            "usehistory": "n",
        }

        if self.email:
            params["email"] = self.email
        if self.api_key:
            params["api_key"] = self.api_key

        try:
            response = await client.get(f"{EUTILS_BASE_URL}/esearch.fcgi", params=params)
            if response.status_code == 200:
                data = response.json()
                id_list = data.get("esearchresult", {}).get("idlist", [])
                logger.info("trace_search_complete", organism=organism, count=len(id_list))
                return id_list
        except Exception as e:
            logger.error("trace_search_failed", error=str(e))

        return []

    async def download_trace_by_tid(
        self,
        tid: str,
        organism: str = "unknown",
    ) -> DownloadResult:
        """Download a trace file by TID from NCBI Trace Archive."""
        async with self._semaphore:
            client = await self._get_client()

            # NCBI Trace Archive download URL
            # Format: https://trace.ncbi.nlm.nih.gov/Traces/trace.fcgi?cmd=raw&val=TID
            download_url = f"{TRACE_BASE_URL}/trace.fcgi?cmd=raw&val={tid}"

            try:
                response = await client.get(download_url)

                if response.status_code != 200:
                    return DownloadResult(
                        trace_id=tid,
                        success=False,
                        error=f"HTTP {response.status_code}",
                    )

                data = response.content

                # Check if it's gzipped
                if data[:2] == b'\x1f\x8b':
                    data = gzip.decompress(data)

                # Validate AB1 magic number
                if data[:4] != b'ABIF':
                    return DownloadResult(
                        trace_id=tid,
                        success=False,
                        error="Not a valid AB1 file",
                    )

                # Sanitize organism name for path
                safe_organism = organism.lower().replace(" ", "_").replace("/", "_")[:50]

                # Upload to MinIO
                minio_path = f"raw/sanger/{safe_organism}/{tid}.ab1"
                if not minio_storage.upload_bytes(data, minio_path, "application/octet-stream"):
                    return DownloadResult(
                        trace_id=tid,
                        success=False,
                        error="MinIO upload failed",
                    )

                # Create and upload metadata
                metadata = TraceMetadata(
                    trace_id=tid,
                    organism=organism,
                    download_url=download_url,
                    downloaded_at=datetime.utcnow().isoformat(),
                )

                metadata_path = f"metadata/{tid}.json"
                metadata_json = json.dumps(metadata.to_dict(), indent=2)
                minio_storage.upload_bytes(
                    metadata_json.encode(),
                    metadata_path,
                    "application/json"
                )

                logger.info(
                    "trace_downloaded",
                    tid=tid,
                    organism=organism,
                    size=len(data),
                    path=minio_path,
                )

                return DownloadResult(
                    trace_id=tid,
                    success=True,
                    minio_path=minio_path,
                    metadata_path=metadata_path,
                    file_size=len(data),
                )

            except Exception as e:
                logger.error("trace_download_failed", tid=tid, error=str(e))
                return DownloadResult(
                    trace_id=tid,
                    success=False,
                    error=str(e),
                )

    async def download_sample_traces(
        self,
        organism: str = "Homo sapiens",
        count: int = 5,
    ) -> DownloadStats:
        """Download a sample of traces for testing."""
        stats = DownloadStats(total=count)

        # Use known working TIDs for testing
        # These are example TIDs from NCBI Trace Archive
        sample_tids = [
            "2274376821",
            "2274376822",
            "2274376823",
            "2274376824",
            "2274376825",
            "2274376826",
            "2274376827",
            "2274376828",
            "2274376829",
            "2274376830",
        ][:count]

        tasks = [
            self.download_trace_by_tid(tid, organism)
            for tid in sample_tids
        ]

        results = await asyncio.gather(*tasks, return_exceptions=True)

        for result in results:
            if isinstance(result, Exception):
                stats.failed += 1
                stats.results.append(DownloadResult(
                    trace_id="unknown",
                    success=False,
                    error=str(result),
                ))
            elif result.success:
                stats.successful += 1
                stats.total_bytes += result.file_size
                stats.results.append(result)
            else:
                stats.failed += 1
                stats.results.append(result)

        return stats

    async def download_traces_by_query(
        self,
        organism: str = "Homo sapiens",
        trace_type: str = "SANGER",
        count: int = 10,
    ) -> DownloadStats:
        """Search and download traces by query."""
        stats = DownloadStats(total=count)

        # Search for trace IDs
        trace_ids = await self.search_traces(organism, trace_type, count)

        if not trace_ids:
            logger.warning("no_traces_found", organism=organism, trace_type=trace_type)
            return stats

        stats.total = len(trace_ids)

        tasks = [
            self.download_trace_by_tid(tid, organism)
            for tid in trace_ids
        ]

        results = await asyncio.gather(*tasks, return_exceptions=True)

        for result in results:
            if isinstance(result, Exception):
                stats.failed += 1
            elif result.success:
                stats.successful += 1
                stats.total_bytes += result.file_size
                stats.results.append(result)
            else:
                stats.failed += 1
                stats.results.append(result)

        return stats


# Singleton instance
ncbi_trace_client = NCBITraceClient()
