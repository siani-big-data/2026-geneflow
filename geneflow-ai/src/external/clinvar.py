"""ClinVar/NCBI API client.

ClinVar provides clinical significance of genetic variants:
- Pathogenic/Benign classifications
- Disease associations
- Supporting evidence

Uses NCBI E-utilities API.
API Docs: https://www.ncbi.nlm.nih.gov/clinvar/docs/api/
"""

from dataclasses import dataclass

import structlog

from .base import APIResponse, ExternalAPIClient

logger = structlog.get_logger()


@dataclass
class ClinVarVariant:
    """Variant information from ClinVar."""

    accession: str
    title: str
    clinical_significance: str
    review_status: str
    conditions: list[str]
    gene: str | None
    variant_type: str
    last_evaluated: str | None

    def to_dict(self) -> dict:
        return {
            "accession": self.accession,
            "title": self.title,
            "clinicalSignificance": self.clinical_significance,
            "reviewStatus": self.review_status,
            "conditions": self.conditions,
            "gene": self.gene,
            "variantType": self.variant_type,
            "lastEvaluated": self.last_evaluated,
        }


class ClinVarClient(ExternalAPIClient):
    """Client for ClinVar/NCBI E-utilities API."""

    BASE_URL = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils"

    def __init__(self, email: str = "", api_key: str = "", timeout: float = 30.0):
        super().__init__(base_url=self.BASE_URL, timeout=timeout)
        self.email = email
        self.api_key = api_key

    @property
    def name(self) -> str:
        return "clinvar"

    def _add_credentials(self, params: dict) -> dict:
        """Add email and API key to params."""
        if self.email:
            params["email"] = self.email
        if self.api_key:
            params["api_key"] = self.api_key
        return params

    async def search_variant(
        self,
        query: str,
        max_results: int = 10,
    ) -> APIResponse:
        """Search ClinVar for variants.

        Args:
            query: Search query (gene name, rsID, HGVS, etc.)
            max_results: Maximum results to return

        Returns:
            List of variant IDs
        """
        params = self._add_credentials(
            {
                "db": "clinvar",
                "term": query,
                "retmax": max_results,
                "retmode": "json",
            }
        )

        response = await self._get("/esearch.fcgi", params=params)

        if response.success and response.data:
            result = response.data.get("esearchresult", {})
            ids = result.get("idlist", [])
            response.data = {
                "ids": ids,
                "total": int(result.get("count", 0)),
                "query": query,
            }

        return response

    async def get_variant_details(self, variant_id: str) -> APIResponse:
        """Get detailed information for a ClinVar variant.

        Args:
            variant_id: ClinVar variant ID

        Returns:
            Variant details
        """
        params = self._add_credentials(
            {
                "db": "clinvar",
                "id": variant_id,
                "retmode": "json",
            }
        )

        response = await self._get("/esummary.fcgi", params=params)

        if response.success and response.data:
            result = response.data.get("result", {})
            variant_data = result.get(str(variant_id), {})

            if variant_data:
                # Parse clinical significance
                clin_sig = variant_data.get("clinical_significance", {})
                description = clin_sig.get("description", "Unknown")

                # Parse conditions
                conditions = []
                for trait in variant_data.get("trait_set", []):
                    for trait_name in trait.get("trait_name", []):
                        conditions.append(trait_name)

                # Parse genes
                genes = variant_data.get("genes", [])
                gene = genes[0].get("symbol", "") if genes else None

                variant = ClinVarVariant(
                    accession=variant_data.get("accession", ""),
                    title=variant_data.get("title", ""),
                    clinical_significance=description,
                    review_status=clin_sig.get("review_status", ""),
                    conditions=conditions,
                    gene=gene,
                    variant_type=variant_data.get("obj_type", ""),
                    last_evaluated=clin_sig.get("last_evaluated", None),
                )
                response.data = variant.to_dict()
            else:
                response.success = False
                response.error = "Variant not found"

        return response

    async def search_by_rsid(self, rsid: str) -> APIResponse:
        """Search for variant by rsID.

        Args:
            rsid: dbSNP rsID (e.g., rs12345)

        Returns:
            Variant information
        """
        # Clean rsID
        rsid_clean = rsid.lower().replace("rs", "")
        query = f"rs{rsid_clean}[Variant ID]"

        search_result = await self.search_variant(query, max_results=1)

        if not search_result.success:
            return search_result

        ids = search_result.data.get("ids", [])
        if not ids:
            return APIResponse(
                success=False,
                error=f"No ClinVar entry found for {rsid}",
                source=self.name,
            )

        return await self.get_variant_details(ids[0])

    async def search_by_gene(
        self,
        gene_symbol: str,
        significance: str | None = None,
        max_results: int = 20,
    ) -> APIResponse:
        """Search for variants in a gene.

        Args:
            gene_symbol: Gene symbol (e.g., BRCA1)
            significance: Filter by clinical significance (e.g., "pathogenic")
            max_results: Maximum results

        Returns:
            List of variants
        """
        query = f"{gene_symbol}[gene]"
        if significance:
            query += f" AND {significance}[clinical_significance]"

        search_result = await self.search_variant(query, max_results=max_results)

        if not search_result.success:
            return search_result

        ids = search_result.data.get("ids", [])
        if not ids:
            return APIResponse(
                success=True,
                data={"variants": [], "total": 0, "gene": gene_symbol},
                source=self.name,
            )

        # Get details for each variant
        variants = []
        for var_id in ids[:10]:  # Limit to 10 detail requests
            detail = await self.get_variant_details(var_id)
            if detail.success:
                variants.append(detail.data)

        return APIResponse(
            success=True,
            data={
                "variants": variants,
                "total": search_result.data.get("total", 0),
                "gene": gene_symbol,
            },
            source=self.name,
        )

    async def get_clinical_significance(
        self,
        chromosome: str,
        position: int,
        ref: str,
        alt: str,
    ) -> APIResponse:
        """Get clinical significance for a specific variant.

        Args:
            chromosome: Chromosome (1-22, X, Y)
            position: Genomic position (GRCh38)
            ref: Reference allele
            alt: Alternate allele

        Returns:
            Clinical significance if found
        """
        # Build HGVS-like query
        query = f"{chromosome}:{position} {ref}>{alt}"

        search_result = await self.search_variant(query, max_results=5)

        if not search_result.success:
            return search_result

        ids = search_result.data.get("ids", [])
        if not ids:
            return APIResponse(
                success=True,
                data={
                    "found": False,
                    "variant": f"{chromosome}:{position}{ref}>{alt}",
                    "message": "No ClinVar entry found for this variant",
                },
                source=self.name,
            )

        # Get first match
        detail = await self.get_variant_details(ids[0])
        if detail.success:
            detail.data["found"] = True

        return detail
