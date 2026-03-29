"""Ensembl REST API client.

Ensembl provides genomic data including:
- Gene annotations
- Variant information
- Sequence retrieval
- Cross-references

API Docs: https://rest.ensembl.org/
Rate limit: 15 requests/second (55,000/hour)
"""

from dataclasses import dataclass

import structlog

from .base import APIResponse, ExternalAPIClient

logger = structlog.get_logger()


@dataclass
class GeneInfo:
    """Gene information from Ensembl."""

    id: str
    name: str
    description: str
    biotype: str
    chromosome: str
    start: int
    end: int
    strand: int
    species: str

    def to_dict(self) -> dict:
        return {
            "id": self.id,
            "name": self.name,
            "description": self.description,
            "biotype": self.biotype,
            "location": {
                "chromosome": self.chromosome,
                "start": self.start,
                "end": self.end,
                "strand": "+" if self.strand == 1 else "-",
            },
            "species": self.species,
        }


@dataclass
class VariantInfo:
    """Variant information from Ensembl."""

    id: str
    consequence: str
    impact: str
    gene_id: str | None
    gene_name: str | None
    alleles: str

    def to_dict(self) -> dict:
        return {
            "id": self.id,
            "consequence": self.consequence,
            "impact": self.impact,
            "geneId": self.gene_id,
            "geneName": self.gene_name,
            "alleles": self.alleles,
        }


class EnsemblClient(ExternalAPIClient):
    """Client for Ensembl REST API."""

    BASE_URL = "https://rest.ensembl.org"

    def __init__(self, timeout: float = 30.0):
        super().__init__(base_url=self.BASE_URL, timeout=timeout)

    @property
    def name(self) -> str:
        return "ensembl"

    def _default_headers(self) -> dict[str, str]:
        return {
            "Accept": "application/json",
            "Content-Type": "application/json",
            "User-Agent": "GeneFlow-AI/1.0",
        }

    async def lookup_gene(self, gene_id: str, species: str = "human") -> APIResponse:
        """Look up gene by Ensembl ID.

        Args:
            gene_id: Ensembl gene ID (e.g., ENSG00000139618)
            species: Species name (default: human)

        Returns:
            Gene information
        """
        response = await self._get(f"/lookup/id/{gene_id}")

        if response.success and response.data:
            data = response.data
            gene = GeneInfo(
                id=data.get("id", ""),
                name=data.get("display_name", ""),
                description=data.get("description", ""),
                biotype=data.get("biotype", ""),
                chromosome=data.get("seq_region_name", ""),
                start=data.get("start", 0),
                end=data.get("end", 0),
                strand=data.get("strand", 1),
                species=data.get("species", species),
            )
            response.data = gene.to_dict()

        return response

    async def lookup_symbol(self, symbol: str, species: str = "human") -> APIResponse:
        """Look up gene by symbol (e.g., BRCA1).

        Args:
            symbol: Gene symbol
            species: Species name

        Returns:
            Gene information
        """
        response = await self._get(f"/lookup/symbol/{species}/{symbol}")

        if response.success and response.data:
            data = response.data
            gene = GeneInfo(
                id=data.get("id", ""),
                name=data.get("display_name", ""),
                description=data.get("description", ""),
                biotype=data.get("biotype", ""),
                chromosome=data.get("seq_region_name", ""),
                start=data.get("start", 0),
                end=data.get("end", 0),
                strand=data.get("strand", 1),
                species=species,
            )
            response.data = gene.to_dict()

        return response

    async def get_sequence(
        self,
        id: str,
        seq_type: str = "genomic",
        expand_5prime: int = 0,
        expand_3prime: int = 0,
    ) -> APIResponse:
        """Get sequence for a gene or transcript.

        Args:
            id: Ensembl ID
            seq_type: Type of sequence (genomic, cds, cdna, protein)
            expand_5prime: Expand 5' flank (bp)
            expand_3prime: Expand 3' flank (bp)

        Returns:
            Sequence data
        """
        params = {"type": seq_type}
        if expand_5prime > 0:
            params["expand_5prime"] = expand_5prime
        if expand_3prime > 0:
            params["expand_3prime"] = expand_3prime

        response = await self._get(f"/sequence/id/{id}", params=params)

        if response.success and response.data:
            data = response.data
            response.data = {
                "id": data.get("id", ""),
                "sequence": data.get("seq", ""),
                "molecule": data.get("molecule", ""),
                "length": len(data.get("seq", "")),
            }

        return response

    async def get_variant_consequences(
        self,
        chromosome: str,
        position: int,
        allele: str,
        species: str = "human",
    ) -> APIResponse:
        """Get consequences of a variant.

        Args:
            chromosome: Chromosome name
            position: Genomic position
            allele: Alternate allele
            species: Species name

        Returns:
            Variant consequences
        """
        # Format: species/region/allele
        region = f"{chromosome}:{position}:{position}/{allele}"
        response = await self._get(f"/vep/{species}/region/{region}")

        if response.success and response.data:
            consequences = []
            for item in response.data:
                for tc in item.get("transcript_consequences", []):
                    consequences.append(
                        VariantInfo(
                            id=item.get("id", ""),
                            consequence=",".join(tc.get("consequence_terms", [])),
                            impact=tc.get("impact", ""),
                            gene_id=tc.get("gene_id"),
                            gene_name=tc.get("gene_symbol"),
                            alleles=f"{item.get('allele_string', '')}",
                        ).to_dict()
                    )
            response.data = {
                "consequences": consequences,
                "totalConsequences": len(consequences),
            }

        return response

    async def search_genes(
        self,
        query: str,
        species: str = "human",
        limit: int = 10,
    ) -> APIResponse:
        """Search for genes by name or description.

        Args:
            query: Search term
            species: Species name
            limit: Max results

        Returns:
            List of matching genes
        """
        # Note: Ensembl doesn't have a search endpoint, using symbol lookup
        response = await self._get(f"/lookup/symbol/{species}/{query}")

        # Single result from symbol lookup
        if response.success:
            response.data = {"results": [response.data], "total": 1}

        return response

    async def get_homologs(
        self,
        gene_id: str,
        target_species: str | None = None,
    ) -> APIResponse:
        """Get homologous genes.

        Args:
            gene_id: Ensembl gene ID
            target_species: Filter by species (optional)

        Returns:
            List of homologs
        """
        params = {}
        if target_species:
            params["target_species"] = target_species

        response = await self._get(f"/homology/id/{gene_id}", params=params)

        if response.success and response.data:
            homologs = []
            for h in response.data.get("data", [{}])[0].get("homologies", []):
                target = h.get("target", {})
                homologs.append(
                    {
                        "type": h.get("type", ""),
                        "targetId": target.get("id", ""),
                        "targetSpecies": target.get("species", ""),
                        "percentIdentity": h.get("target", {}).get("perc_id", 0),
                    }
                )
            response.data = {
                "homologs": homologs,
                "totalHomologs": len(homologs),
            }

        return response
