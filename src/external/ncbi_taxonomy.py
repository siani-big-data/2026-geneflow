"""NCBI Taxonomy client for taxonomic classification."""

from dataclasses import dataclass, field

import httpx
import structlog

logger = structlog.get_logger()

EUTILS_BASE = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils"

# Taxonomic ranks in order
TAXONOMIC_RANKS = [
    "superkingdom",  # Bacteria, Archaea, Eukaryota
    "kingdom",
    "phylum",
    "class",
    "order",
    "family",
    "genus",
    "species",
]


@dataclass
class TaxonomyInfo:
    """Complete taxonomic information for an organism."""

    taxid: str = ""
    scientific_name: str = ""
    common_name: str = ""
    lineage: dict[str, str] = field(default_factory=dict)

    @property
    def superkingdom(self) -> str:
        return self.lineage.get("superkingdom", "unknown")

    @property
    def kingdom(self) -> str:
        return self.lineage.get("kingdom", "")

    @property
    def phylum(self) -> str:
        return self.lineage.get("phylum", "")

    @property
    def class_name(self) -> str:
        return self.lineage.get("class", "")

    @property
    def order(self) -> str:
        return self.lineage.get("order", "")

    @property
    def family(self) -> str:
        return self.lineage.get("family", "")

    @property
    def genus(self) -> str:
        return self.lineage.get("genus", "")

    @property
    def species(self) -> str:
        return self.lineage.get("species", self.scientific_name)

    def get_path(self, include_ranks: list[str] | None = None) -> str:
        """Get filesystem path based on taxonomy."""
        if include_ranks is None:
            include_ranks = [
                "superkingdom", "kingdom", "phylum", "class",
                "order", "family", "genus", "species"
            ]

        parts = []
        for rank in include_ranks:
            value = self.lineage.get(rank, "")
            if value:
                # Sanitize for filesystem
                safe_value = value.lower().replace(" ", "_").replace("/", "_").replace(".", "")
                parts.append(safe_value)

        return "/".join(parts) if parts else "unknown"

    def to_dict(self) -> dict:
        return {
            "taxId": self.taxid,
            "scientificName": self.scientific_name,
            "commonName": self.common_name,
            "lineage": self.lineage,
            "path": self.get_path(),
        }


class NCBITaxonomyClient:
    """Client for NCBI Taxonomy database."""

    def __init__(self, email: str = "", api_key: str = "", timeout: float = 30.0):
        self.email = email
        self.api_key = api_key
        self.timeout = timeout
        self._client: httpx.AsyncClient | None = None
        self._cache: dict[str, TaxonomyInfo] = {}

    async def _get_client(self) -> httpx.AsyncClient:
        if self._client is None:
            self._client = httpx.AsyncClient(
                timeout=self.timeout,
                headers={"User-Agent": "GeneFlow-AI/1.0"},
            )
        return self._client

    async def close(self) -> None:
        if self._client:
            await self._client.aclose()
            self._client = None

    def _build_params(self, **kwargs) -> dict:
        params = dict(kwargs)
        if self.email:
            params["email"] = self.email
        if self.api_key:
            params["api_key"] = self.api_key
        return params

    async def search_taxid(self, organism: str) -> str | None:
        """Search for taxonomy ID by organism name."""
        if organism in self._cache:
            return self._cache[organism].taxid

        client = await self._get_client()

        params = self._build_params(
            db="taxonomy",
            term=f'"{organism}"[Scientific Name]',
            retmode="json",
        )

        try:
            response = await client.get(f"{EUTILS_BASE}/esearch.fcgi", params=params)
            if response.status_code == 200:
                data = response.json()
                id_list = data.get("esearchresult", {}).get("idlist", [])
                if id_list:
                    return id_list[0]
        except Exception as e:
            logger.error("taxid_search_failed", organism=organism, error=str(e))

        return None

    async def get_taxonomy(self, organism: str) -> TaxonomyInfo:
        """Get complete taxonomic information for an organism."""
        # Check cache
        if organism in self._cache:
            return self._cache[organism]

        # Search for taxid
        taxid = await self.search_taxid(organism)
        if not taxid:
            logger.warning("taxid_not_found", organism=organism)
            return TaxonomyInfo(scientific_name=organism)

        client = await self._get_client()

        # Fetch taxonomy details
        params = self._build_params(
            db="taxonomy",
            id=taxid,
            retmode="xml",
        )

        try:
            response = await client.get(f"{EUTILS_BASE}/efetch.fcgi", params=params)
            if response.status_code == 200:
                # Parse XML response
                xml_text = response.text
                info = self._parse_taxonomy_xml(xml_text, taxid)
                self._cache[organism] = info
                logger.info(
                    "taxonomy_fetched",
                    organism=organism,
                    taxid=taxid,
                    lineage_depth=len(info.lineage),
                )
                return info
        except Exception as e:
            logger.error("taxonomy_fetch_failed", taxid=taxid, error=str(e))

        return TaxonomyInfo(taxid=taxid, scientific_name=organism)

    def _parse_taxonomy_xml(self, xml_text: str, taxid: str) -> TaxonomyInfo:
        """Parse taxonomy XML response."""
        import re

        info = TaxonomyInfo(taxid=taxid)

        # Extract scientific name
        match = re.search(r"<ScientificName>([^<]+)</ScientificName>", xml_text)
        if match:
            info.scientific_name = match.group(1)

        # Extract common name
        match = re.search(r"<CommonName>([^<]+)</CommonName>", xml_text)
        if match:
            info.common_name = match.group(1)

        # Extract lineage with ranks
        # Find all Taxon entries in LineageEx
        lineage_match = re.search(r"<LineageEx>(.*?)</LineageEx>", xml_text, re.DOTALL)
        if lineage_match:
            lineage_xml = lineage_match.group(1)

            # Extract each taxon
            taxon_pattern = (
                r"<Taxon>\s*<TaxId>(\d+)</TaxId>\s*"
                r"<ScientificName>([^<]+)</ScientificName>\s*<Rank>([^<]+)</Rank>"
            )
            for match in re.finditer(taxon_pattern, lineage_xml):
                rank = match.group(3).lower()
                name = match.group(2)
                if rank in TAXONOMIC_RANKS:
                    info.lineage[rank] = name

        # Add species (self)
        if info.scientific_name:
            info.lineage["species"] = info.scientific_name

        return info


# Singleton instance
ncbi_taxonomy = NCBITaxonomyClient()


# Pre-defined taxonomy for common organisms (fallback/cache)
KNOWN_TAXONOMY = {
    "Homo sapiens": TaxonomyInfo(
        taxid="9606",
        scientific_name="Homo sapiens",
        common_name="human",
        lineage={
            "superkingdom": "Eukaryota",
            "kingdom": "Metazoa",
            "phylum": "Chordata",
            "class": "Mammalia",
            "order": "Primates",
            "family": "Hominidae",
            "genus": "Homo",
            "species": "Homo sapiens",
        },
    ),
    "Mus musculus": TaxonomyInfo(
        taxid="10090",
        scientific_name="Mus musculus",
        common_name="house mouse",
        lineage={
            "superkingdom": "Eukaryota",
            "kingdom": "Metazoa",
            "phylum": "Chordata",
            "class": "Mammalia",
            "order": "Rodentia",
            "family": "Muridae",
            "genus": "Mus",
            "species": "Mus musculus",
        },
    ),
    "Escherichia coli": TaxonomyInfo(
        taxid="562",
        scientific_name="Escherichia coli",
        common_name="E. coli",
        lineage={
            "superkingdom": "Bacteria",
            "phylum": "Pseudomonadota",
            "class": "Gammaproteobacteria",
            "order": "Enterobacterales",
            "family": "Enterobacteriaceae",
            "genus": "Escherichia",
            "species": "Escherichia coli",
        },
    ),
    "Saccharomyces cerevisiae": TaxonomyInfo(
        taxid="4932",
        scientific_name="Saccharomyces cerevisiae",
        common_name="baker's yeast",
        lineage={
            "superkingdom": "Eukaryota",
            "kingdom": "Fungi",
            "phylum": "Ascomycota",
            "class": "Saccharomycetes",
            "order": "Saccharomycetales",
            "family": "Saccharomycetaceae",
            "genus": "Saccharomyces",
            "species": "Saccharomyces cerevisiae",
        },
    ),
    "Arabidopsis thaliana": TaxonomyInfo(
        taxid="3702",
        scientific_name="Arabidopsis thaliana",
        common_name="thale cress",
        lineage={
            "superkingdom": "Eukaryota",
            "kingdom": "Viridiplantae",
            "phylum": "Streptophyta",
            "class": "Magnoliopsida",
            "order": "Brassicales",
            "family": "Brassicaceae",
            "genus": "Arabidopsis",
            "species": "Arabidopsis thaliana",
        },
    ),
    "Drosophila melanogaster": TaxonomyInfo(
        taxid="7227",
        scientific_name="Drosophila melanogaster",
        common_name="fruit fly",
        lineage={
            "superkingdom": "Eukaryota",
            "kingdom": "Metazoa",
            "phylum": "Arthropoda",
            "class": "Insecta",
            "order": "Diptera",
            "family": "Drosophilidae",
            "genus": "Drosophila",
            "species": "Drosophila melanogaster",
        },
    ),
    "Caenorhabditis elegans": TaxonomyInfo(
        taxid="6239",
        scientific_name="Caenorhabditis elegans",
        common_name="roundworm",
        lineage={
            "superkingdom": "Eukaryota",
            "kingdom": "Metazoa",
            "phylum": "Nematoda",
            "class": "Chromadorea",
            "order": "Rhabditida",
            "family": "Rhabditidae",
            "genus": "Caenorhabditis",
            "species": "Caenorhabditis elegans",
        },
    ),
    "Danio rerio": TaxonomyInfo(
        taxid="7955",
        scientific_name="Danio rerio",
        common_name="zebrafish",
        lineage={
            "superkingdom": "Eukaryota",
            "kingdom": "Metazoa",
            "phylum": "Chordata",
            "class": "Actinopteri",
            "order": "Cypriniformes",
            "family": "Danionidae",
            "genus": "Danio",
            "species": "Danio rerio",
        },
    ),
}
