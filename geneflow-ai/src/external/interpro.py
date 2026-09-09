"""InterPro API client.

InterPro provides protein domain and family information:
- Domain annotations
- Protein family classification
- GO terms
- Structural predictions

API Docs: https://www.ebi.ac.uk/interpro/api/
"""

from dataclasses import dataclass

import structlog

from .base import APIResponse, ExternalAPIClient

logger = structlog.get_logger()


@dataclass
class ProteinDomain:
    """Protein domain from InterPro."""

    accession: str
    name: str
    description: str
    type: str  # domain, family, repeat, site, etc.
    start: int
    end: int
    score: float | None
    database: str  # Pfam, SMART, etc.

    def to_dict(self) -> dict:
        return {
            "accession": self.accession,
            "name": self.name,
            "description": self.description,
            "type": self.type,
            "location": {
                "start": self.start,
                "end": self.end,
            },
            "score": self.score,
            "database": self.database,
        }


@dataclass
class ProteinEntry:
    """Protein entry from InterPro."""

    accession: str
    name: str
    description: str
    length: int
    domains: list[ProteinDomain]
    go_terms: list[dict]

    def to_dict(self) -> dict:
        return {
            "accession": self.accession,
            "name": self.name,
            "description": self.description,
            "length": self.length,
            "domains": [d.to_dict() for d in self.domains],
            "goTerms": self.go_terms,
            "totalDomains": len(self.domains),
        }


class InterProClient(ExternalAPIClient):
    """Client for InterPro REST API."""

    BASE_URL = "https://www.ebi.ac.uk/interpro/api"

    def __init__(self, timeout: float = 30.0):
        super().__init__(base_url=self.BASE_URL, timeout=timeout)

    @property
    def name(self) -> str:
        return "interpro"

    async def lookup_entry(self, accession: str) -> APIResponse:
        """Look up InterPro entry by accession.

        Args:
            accession: InterPro accession (e.g., IPR000001)

        Returns:
            Entry information
        """
        response = await self._get(f"/entry/interpro/{accession}")

        if response.success and response.data:
            data = response.data.get("metadata", response.data)
            response.data = {
                "accession": data.get("accession", accession),
                "name": data.get("name", {}).get("short", ""),
                "description": data.get("description", [{}])[0].get("text", "")
                if data.get("description")
                else "",
                "type": data.get("type", ""),
                "memberDatabases": list(data.get("member_databases", {}).keys()),
                "goTerms": data.get("go_terms", []),
            }

        return response

    async def search_protein(self, uniprot_id: str) -> APIResponse:
        """Search for protein domains by UniProt ID.

        Args:
            uniprot_id: UniProt accession (e.g., P12345)

        Returns:
            Protein with domain annotations
        """
        response = await self._get(f"/protein/uniprot/{uniprot_id}")

        if response.success and response.data:
            data = response.data.get("metadata", response.data)

            # Get domain matches
            domains_response = await self._get(f"/protein/uniprot/{uniprot_id}/entry/interpro")

            domains = []
            if domains_response.success and domains_response.data:
                for entry in domains_response.data.get("results", []):
                    entry_meta = entry.get("metadata", {})
                    for protein in entry.get("proteins", []):
                        for location in protein.get("entry_protein_locations", []):
                            for fragment in location.get("fragments", []):
                                domains.append(
                                    ProteinDomain(
                                        accession=entry_meta.get("accession", ""),
                                        name=entry_meta.get("name", ""),
                                        description="",
                                        type=entry_meta.get("type", ""),
                                        start=fragment.get("start", 0),
                                        end=fragment.get("end", 0),
                                        score=None,
                                        database="InterPro",
                                    )
                                )

            protein = ProteinEntry(
                accession=data.get("accession", uniprot_id),
                name=data.get("name", ""),
                description=data.get("description", ""),
                length=data.get("length", 0),
                domains=domains,
                go_terms=data.get("go_terms", []),
            )
            response.data = protein.to_dict()

        return response

    async def search_by_sequence(
        self,
        sequence: str,
        applications: list[str] | None = None,
    ) -> APIResponse:
        """Search InterPro by protein sequence.

        Note: This uses InterProScan which may take time for long sequences.
        For quick lookups, use search_protein with UniProt ID.

        Args:
            sequence: Protein sequence (amino acids)
            applications: Specific databases to search (e.g., ["Pfam", "SMART"])

        Returns:
            Domain predictions
        """
        # InterProScan requires job submission
        # For simplicity, we'll use the direct sequence endpoint
        params = {"sequence": sequence[:100]}  # Limit for quick search

        response = await self._get("/protein/sequence", params=params)

        if response.success and response.data:
            results = response.data.get("results", [])
            if results:
                # Return first match
                return await self.search_protein(results[0].get("accession", ""))

        return APIResponse(
            success=True,
            data={
                "message": "No exact match found. Use InterProScan for full analysis.",
                "sequenceLength": len(sequence),
            },
            source=self.name,
        )

    async def get_pfam_domains(self, pfam_id: str) -> APIResponse:
        """Get Pfam domain information.

        Args:
            pfam_id: Pfam accession (e.g., PF00001)

        Returns:
            Domain information
        """
        response = await self._get(f"/entry/pfam/{pfam_id}")

        if response.success and response.data:
            data = response.data.get("metadata", response.data)
            response.data = {
                "accession": data.get("accession", pfam_id),
                "name": data.get("name", ""),
                "description": data.get("description", [{}])[0].get("text", "")
                if data.get("description")
                else "",
                "type": data.get("type", ""),
                "interproAccession": data.get("integrated", ""),
            }

        return response

    async def search_by_name(
        self,
        query: str,
        entry_type: str | None = None,
        limit: int = 10,
    ) -> APIResponse:
        """Search InterPro by name or keyword.

        Args:
            query: Search term
            entry_type: Filter by type (domain, family, etc.)
            limit: Maximum results

        Returns:
            Matching entries
        """
        params = {"search": query, "page_size": limit}
        if entry_type:
            params["type"] = entry_type

        response = await self._get("/entry/interpro", params=params)

        if response.success and response.data:
            results = []
            for entry in response.data.get("results", []):
                meta = entry.get("metadata", entry)
                results.append(
                    {
                        "accession": meta.get("accession", ""),
                        "name": meta.get("name", ""),
                        "type": meta.get("type", ""),
                    }
                )
            response.data = {
                "results": results,
                "total": response.data.get("count", len(results)),
                "query": query,
            }

        return response

    async def get_go_terms(self, interpro_id: str) -> APIResponse:
        """Get GO terms associated with an InterPro entry.

        Args:
            interpro_id: InterPro accession

        Returns:
            Associated GO terms
        """
        response = await self.lookup_entry(interpro_id)

        if response.success and response.data:
            go_terms = response.data.get("goTerms", [])
            categorized = {
                "biological_process": [],
                "molecular_function": [],
                "cellular_component": [],
            }

            for term in go_terms:
                category = term.get("category", {}).get("name", "").lower().replace(" ", "_")
                if category in categorized:
                    categorized[category].append(
                        {
                            "id": term.get("identifier", ""),
                            "name": term.get("name", ""),
                        }
                    )

            response.data = {
                "interproId": interpro_id,
                "goTerms": categorized,
                "totalTerms": len(go_terms),
            }

        return response
