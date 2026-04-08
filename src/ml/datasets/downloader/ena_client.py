"""ENA (European Nucleotide Archive) client for sequence downloads.

ENA has the same data as NCBI (INSDC synchronization) but with:
- No strict rate limits
- REST API
- Faster bulk downloads
"""

import logging
import time
from dataclasses import dataclass
from typing import Iterator

import requests

logger = logging.getLogger(__name__)

# ENA API endpoints
ENA_PORTAL_API = "https://www.ebi.ac.uk/ena/portal/api"
ENA_BROWSER_API = "https://www.ebi.ac.uk/ena/browser/api"


@dataclass
class ENASequence:
    """A sequence from ENA."""
    accession: str
    sequence: str
    description: str
    taxon_id: int
    scientific_name: str
    length: int


class ENAClient:
    """Client for ENA REST API.

    ENA advantages over NCBI:
    - No rate limits (be respectful, ~20 req/s is fine)
    - Simpler REST API
    - Same INSDC data
    """

    # Taxonomy IDs for kingdoms
    KINGDOM_TAXIDS = {
        "animalia": 33208,      # Metazoa
        "plantae": 33090,       # Viridiplantae
        "fungi": 4751,          # Fungi
        "monera": [2, 2157],    # Bacteria (2) + Archaea (2157)
        "protista": 2759,       # Eukaryota (will exclude others)
    }

    # Taxonomic subdivisions for better coverage (ENA limits offsets to ~5000)
    # DEEP mode: includes orders and major families for maximum species discovery
    KINGDOM_SUBDIVISIONS = {
        "animalia": [
            # Chordata - Mammals (orders)
            9443,    # Primates
            9989,    # Rodentia
            91561,   # Cetartiodactyla (whales, pigs, deer)
            33554,   # Carnivora
            9397,    # Chiroptera (bats)
            9362,    # Lagomorpha (rabbits)
            9255,    # Monotremata
            9263,    # Marsupialia
            314145,  # Afrotheria
            311790,  # Xenarthra
            9392,    # Eulipotyphla (shrews, moles)
            # Chordata - Birds (orders)
            8825,    # Passeriformes
            8932,    # Galliformes
            8783,    # Anseriformes
            9126,    # Accipitriformes
            8948,    # Columbiformes
            8939,    # Psittaciformes
            30449,   # Charadriiformes
            # Chordata - Reptiles
            8459,    # Squamata (lizards, snakes)
            8492,    # Testudines (turtles)
            8493,    # Crocodylia
            # Chordata - Amphibians
            8342,    # Anura (frogs)
            8291,    # Caudata (salamanders)
            # Chordata - Fish
            7898,    # Actinopterygii (ray-finned fish)
            7777,    # Chondrichthyes (sharks, rays)
            # Arthropoda - Insects (orders)
            7041,    # Coleoptera (beetles)
            7399,    # Hymenoptera (ants, bees, wasps)
            7088,    # Lepidoptera (butterflies, moths)
            7147,    # Diptera (flies)
            6960,    # Hemiptera (true bugs)
            6962,    # Orthoptera (grasshoppers)
            6973,    # Odonata (dragonflies)
            30263,   # Trichoptera (caddisflies)
            # Arthropoda - Other
            6854,    # Arachnida (spiders, scorpions)
            6657,    # Crustacea
            7496,    # Myriapoda (centipedes, millipedes)
            # Mollusca
            6544,    # Bivalvia
            6448,    # Gastropoda
            6605,    # Cephalopoda
            # Other invertebrates
            6231,    # Nematoda
            6340,    # Annelida
            6073,    # Cnidaria
            7586,    # Echinodermata
            6157,    # Platyhelminthes
            6040,    # Porifera
            10197,   # Ctenophora
            10172,   # Rotifera
            6253,    # Tardigrada
        ],
        "plantae": [
            # Eudicots (major families)
            3745,    # Fabaceae (legumes)
            4070,    # Solanaceae (nightshades)
            3726,    # Brassicaceae (mustards)
            4345,    # Asteraceae (daisies)
            3814,    # Rosaceae (roses)
            4345,    # Lamiaceae (mints)
            23513,   # Apiaceae (carrots)
            3629,    # Euphorbiaceae
            3689,    # Malvaceae (mallows)
            3584,    # Cucurbitaceae (gourds)
            3381,    # Caryophyllaceae
            22276,   # Rubiaceae (coffee family)
            # Monocots
            4479,    # Poaceae (grasses)
            4668,    # Orchidaceae
            4681,    # Liliaceae
            4618,    # Arecaceae (palms)
            4734,    # Bromeliaceae
            40552,   # Zingiberaceae (gingers)
            # Gymnosperms
            3312,    # Coniferopsida
            1446289, # Cycadopsida
            3380,    # Ginkgoaceae
            # Ferns and allies
            3242,    # Pteridophyta
            3274,    # Lycopodiophyta
            # Bryophytes
            3208,    # Bryophyta (mosses)
            3195,    # Marchantiophyta (liverworts)
            3243,    # Anthocerotophyta (hornworts)
            # Algae
            3041,    # Chlorophyta (green algae)
            2763,    # Rhodophyta (red algae)
            2870,    # Charophyta
        ],
        "fungi": [
            # Basidiomycota (orders)
            5338,    # Agaricales (mushrooms)
            5234,    # Polyporales (bracket fungi)
            452335,  # Boletales
            5257,    # Russulales
            28988,   # Tremellales (jelly fungi)
            5204,    # Ustilaginales (smuts)
            5258,    # Pucciniales (rusts)
            # Ascomycota (orders)
            4891,    # Saccharomycetales (yeasts)
            5125,    # Eurotiales (Aspergillus, Penicillium)
            5042,    # Hypocreales (Fusarium)
            34395,   # Pleosporales
            28583,   # Xylariales
            5139,    # Sordariales
            37990,   # Capnodiales
            4827,    # Pezizales (cup fungi)
            # Other fungi
            4761,    # Mucoromycota (former Zygomycota)
            4762,    # Glomeromycota (mycorrhizal)
            4764,    # Chytridiomycota
            451459,  # Microsporidia
            214504,  # Blastocladiomycota
        ],
        "monera": [
            # Proteobacteria (classes)
            28211,   # Alphaproteobacteria
            28216,   # Betaproteobacteria
            1236,    # Gammaproteobacteria
            28221,   # Deltaproteobacteria
            29547,   # Epsilonproteobacteria
            # Firmicutes (classes)
            91061,   # Bacilli
            186801,  # Clostridia
            909932,  # Negativicutes
            # Actinobacteria (orders)
            85004,   # Actinomycetales
            85006,   # Bifidobacteriales
            85009,   # Corynebacteriales
            # Bacteroidetes
            200643,  # Bacteroidia
            117743,  # Flavobacteriia
            768503,  # Cytophagia
            # Cyanobacteria
            1117,    # Cyanobacteria
            1150,    # Nostocales
            1161,    # Oscillatoriales
            # Other bacteria
            203691,  # Spirochaetes
            200930,  # Tenericutes (Mycoplasma)
            200795,  # Chlamydiae
            1297,    # Deinococcus-Thermus
            32066,   # Fusobacteria
            74201,   # Verrucomicrobia
            57723,   # Acidobacteria
            200918,  # Thermotogae
            200940,  # Aquificae
            # Archaea
            183925,  # Methanobacteria
            183963,  # Methanomicrobia
            183967,  # Halobacteria
            183988,  # Thermoplasmata
            2281,    # Thermococcales
            2266,    # Sulfolobales
            114380,  # Nitrososphaerales
            651137,  # Thaumarchaeota
        ],
        "protista": [
            # Alveolata
            5794,    # Apicomplexa (Plasmodium, Toxoplasma)
            5878,    # Ciliophora
            2864,    # Dinoflagellata
            # Stramenopiles
            2836,    # Bacillariophyta (diatoms)
            2870,    # Phaeophyceae (brown algae)
            4762,    # Oomycota (water molds)
            33634,   # Stramenopiles other
            # Amoebozoa
            5752,    # Amoebozoa
            142796,  # Discosea
            555280,  # Tubulinea
            # Rhizaria
            543769,  # Foraminifera
            6029,    # Radiolaria
            28009,   # Cercozoa
            # Excavata
            5719,    # Kinetoplastida (Trypanosoma, Leishmania)
            5738,    # Diplomonadida (Giardia)
            66288,   # Parabasalia (Trichomonas)
            136087,  # Euglenozoa
            # Other protists
            2830,    # Haptophyta
            3027,    # Cryptophyta
            38254,   # Choanoflagellata
            33682,   # Euglenophyceae
        ],
    }

    def __init__(self, requests_per_second: float = 15.0, pool_size: int = 50):
        self.session = requests.Session()

        # Increase connection pool size for parallel downloads
        adapter = requests.adapters.HTTPAdapter(
            pool_connections=pool_size,
            pool_maxsize=pool_size,
            max_retries=3,
        )
        self.session.mount("https://", adapter)
        self.session.mount("http://", adapter)

        self.min_interval = 1.0 / requests_per_second
        self._last_request = 0.0

        # Cache for taxonomy lookups
        self._taxonomy_cache: dict[int, dict] = {}

    def _rate_limit(self):
        """Simple rate limiting."""
        elapsed = time.time() - self._last_request
        if elapsed < self.min_interval:
            time.sleep(self.min_interval - elapsed)
        self._last_request = time.time()

    # Known taxa mapped to their ranks (for taxonomy parsing)
    KNOWN_TAXA_RANKS = {
        # Phyla
        "chordata": "phylum", "arthropoda": "phylum",
        "mollusca": "phylum", "annelida": "phylum",
        "nematoda": "phylum", "cnidaria": "phylum",
        "echinodermata": "phylum", "platyhelminthes": "phylum",
        "porifera": "phylum", "streptophyta": "phylum",
        "chlorophyta": "phylum", "rhodophyta": "phylum",
        "ascomycota": "phylum", "basidiomycota": "phylum",
        "mucoromycota": "phylum", "proteobacteria": "phylum",
        "firmicutes": "phylum", "actinobacteria": "phylum",
        "bacteroidetes": "phylum", "cyanobacteria": "phylum",
        "spirochaetes": "phylum", "tenericutes": "phylum",
        "euryarchaeota": "phylum", "crenarchaeota": "phylum",
        "apicomplexa": "phylum", "ciliophora": "phylum",
        "bacillariophyta": "phylum",
        # Classes
        "mammalia": "class", "aves": "class",
        "reptilia": "class", "amphibia": "class",
        "actinopteri": "class", "chondrichthyes": "class",
        "insecta": "class", "arachnida": "class",
        "malacostraca": "class", "maxillopoda": "class",
        "bivalvia": "class", "gastropoda": "class",
        "cephalopoda": "class", "magnoliopsida": "class",
        "liliopsida": "class", "pinopsida": "class",
        "polypodiopsida": "class", "bryopsida": "class",
        "agaricomycetes": "class", "eurotiomycetes": "class",
        "sordariomycetes": "class", "saccharomycetes": "class",
        "leotiomycetes": "class", "dothideomycetes": "class",
        "gammaproteobacteria": "class", "alphaproteobacteria": "class",
        "betaproteobacteria": "class", "deltaproteobacteria": "class",
        "bacilli": "class", "clostridia": "class",
        "actinomycetia": "class",
        # Orders (common ones)
        "primates": "order", "rodentia": "order",
        "carnivora": "order", "chiroptera": "order",
        "cetartiodactyla": "order", "lagomorpha": "order",
        "passeriformes": "order", "galliformes": "order",
        "anseriformes": "order", "squamata": "order",
        "testudines": "order", "crocodylia": "order",
        "anura": "order", "caudata": "order",
        "coleoptera": "order", "lepidoptera": "order",
        "diptera": "order", "hymenoptera": "order",
        "hemiptera": "order", "orthoptera": "order",
        "fabales": "order", "brassicales": "order",
        "solanales": "order", "poales": "order",
        "asterales": "order", "rosales": "order",
        "agaricales": "order", "polyporales": "order",
        "eurotiales": "order", "hypocreales": "order",
        "saccharomycetales": "order", "enterobacterales": "order",
        "pseudomonadales": "order", "lactobacillales": "order",
        # Families (common ones)
        "hominidae": "family", "muridae": "family",
        "felidae": "family", "canidae": "family",
        "bovidae": "family", "cervidae": "family",
        "equidae": "family", "fabaceae": "family",
        "poaceae": "family", "brassicaceae": "family",
        "solanaceae": "family", "rosaceae": "family",
        "asteraceae": "family", "enterobacteriaceae": "family",
        "pseudomonadaceae": "family", "staphylococcaceae": "family",
        "streptococcaceae": "family",
    }

    def get_taxonomy(self, taxon_id: int) -> dict:
        """Get full taxonomy lineage for a taxon ID.

        Uses ENA Taxonomy REST API to fetch the complete lineage.

        Args:
            taxon_id: NCBI/ENA taxonomy ID

        Returns:
            Dict with taxonomy ranks: {phylum, class, order, family, genus}
        """
        # Check cache
        if taxon_id in self._taxonomy_cache:
            return self._taxonomy_cache[taxon_id]

        self._rate_limit()

        taxonomy = {}
        try:
            response = self.session.get(
                f"https://www.ebi.ac.uk/ena/taxonomy/rest/tax-id/{taxon_id}",
                timeout=30,
            )

            if response.ok:
                data = response.json()
                lineage = data.get("lineage", "")

                if lineage:
                    parts = [p.strip() for p in lineage.split(";") if p.strip()]

                    # First pass: use known taxa mapping
                    for part in parts:
                        part_lower = part.lower()
                        if part_lower in self.KNOWN_TAXA_RANKS:
                            rank = self.KNOWN_TAXA_RANKS[part_lower]
                            if rank not in taxonomy:
                                taxonomy[rank] = part

                    # Second pass: use suffix patterns for remaining ranks
                    for part in parts:
                        part_lower = part.lower()

                        # Skip already assigned or superkingdom/kingdom
                        if part_lower in ["eukaryota", "bacteria", "archaea", "metazoa",
                                          "viridiplantae", "fungi", "cellular organisms"]:
                            continue

                        # Phylum suffixes
                        if "phylum" not in taxonomy:
                            if part_lower.endswith(("phyta", "mycota")):
                                taxonomy["phylum"] = part
                                continue

                        # Class suffixes
                        if "class" not in taxonomy:
                            if part_lower.endswith(("opsida", "mycetes", "phyceae")):
                                taxonomy["class"] = part
                                continue

                        # Order suffixes
                        if "order" not in taxonomy:
                            if (part_lower.endswith("ales")
                                or part_lower.endswith("formes")):
                                taxonomy["order"] = part
                                continue

                        # Family suffixes
                        if "family" not in taxonomy:
                            if (part_lower.endswith("aceae")
                                or part_lower.endswith("idae")):
                                taxonomy["family"] = part
                                continue

                    # Last element before species is usually genus
                    if len(parts) >= 2 and "genus" not in taxonomy:
                        potential_genus = parts[-1]
                        # Genus is typically single word, capitalized
                        if " " not in potential_genus and potential_genus[0].isupper():
                            taxonomy["genus"] = potential_genus

        except Exception as e:
            logger.debug(f"Taxonomy lookup failed for {taxon_id}: {e}")

        # Cache result (even if empty)
        self._taxonomy_cache[taxon_id] = taxonomy
        return taxonomy

    def get_taxonomy_batch(self, taxon_ids: list[int]) -> dict[int, dict]:
        """Get taxonomy for multiple taxon IDs efficiently.

        Args:
            taxon_ids: List of taxon IDs

        Returns:
            Dict mapping taxon_id to taxonomy dict
        """
        results = {}
        uncached = [tid for tid in taxon_ids if tid not in self._taxonomy_cache]

        # Fetch uncached in batches
        for tid in uncached:
            results[tid] = self.get_taxonomy(tid)

        # Add cached results
        for tid in taxon_ids:
            if tid in self._taxonomy_cache:
                results[tid] = self._taxonomy_cache[tid]

        return results

    def search_sequences(
        self,
        kingdom: str,
        limit: int = 10000,
        offset: int = 0,
        min_length: int = 100,
        max_length: int = 50000,
    ) -> list[dict]:
        """Search for sequences in a kingdom.

        Args:
            kingdom: Kingdom name
            limit: Maximum results to return
            offset: Starting offset
            min_length: Minimum sequence length
            max_length: Maximum sequence length

        Returns:
            List of sequence metadata dicts
        """
        self._rate_limit()

        # Build taxonomy query
        taxids = self.KINGDOM_TAXIDS.get(kingdom.lower())
        if taxids is None:
            raise ValueError(f"Unknown kingdom: {kingdom}")

        if isinstance(taxids, list):
            tax_query = " OR ".join(f"tax_tree({t})" for t in taxids)
            tax_query = f"({tax_query})"
        else:
            if kingdom.lower() == "protista":
                # Protista = Eukaryota excluding animals, plants, fungi
                tax_query = (
                    f"tax_tree({taxids}) AND "
                    f"NOT tax_tree(33208) AND "  # NOT Metazoa
                    f"NOT tax_tree(33090) AND "  # NOT Viridiplantae
                    f"NOT tax_tree(4751)"        # NOT Fungi
                )
            else:
                tax_query = f"tax_tree({taxids})"

        # Simple query - ENA doesn't support length filter in query
        query = tax_query

        params = {
            "query": query,
            "result": "sequence",
            "fields": "accession,tax_id,scientific_name,description,base_count",
            "format": "json",
            "limit": limit,
            "offset": offset,
        }

        try:
            response = self.session.get(
                f"{ENA_PORTAL_API}/search",
                params=params,
                timeout=60,
            )
            response.raise_for_status()

            data = response.json()
            if not isinstance(data, list):
                return []

            # Filter by length (base_count)
            filtered = []
            for record in data:
                try:
                    length = int(record.get("base_count", 0))
                    if min_length <= length <= max_length:
                        record["length"] = length
                        filtered.append(record)
                except (ValueError, TypeError):
                    pass

            return filtered

        except requests.exceptions.RequestException as e:
            logger.warning(f"ENA search failed: {e}")
            return []

    def count_sequences(
        self,
        kingdom: str,
        min_length: int = 100,
        max_length: int = 50000,
    ) -> int:
        """Count sequences in a kingdom.

        Note: ENA doesn't support length filter in count, so this returns
        total count without length filtering.
        """
        self._rate_limit()

        taxids = self.KINGDOM_TAXIDS.get(kingdom.lower())
        if taxids is None:
            return 0

        if isinstance(taxids, list):
            tax_query = " OR ".join(f"tax_tree({t})" for t in taxids)
            tax_query = f"({tax_query})"
        else:
            if kingdom.lower() == "protista":
                tax_query = (
                    f"tax_tree({taxids}) AND "
                    f"NOT tax_tree(33208) AND "
                    f"NOT tax_tree(33090) AND "
                    f"NOT tax_tree(4751)"
                )
            else:
                tax_query = f"tax_tree({taxids})"

        params = {
            "query": tax_query,
            "result": "sequence",
        }

        try:
            response = self.session.get(
                f"{ENA_PORTAL_API}/count",
                params=params,
                timeout=30,
            )
            response.raise_for_status()
            # Response is "count\nNNNN\n"
            lines = response.text.strip().split("\n")
            if len(lines) >= 2:
                return int(lines[1])
            return int(lines[0]) if lines[0].isdigit() else 0

        except Exception as e:
            logger.warning(f"ENA count failed: {e}")
            return 0

    def get_sequence_fasta(self, accession: str) -> str | None:
        """Get FASTA sequence by accession."""
        self._rate_limit()

        try:
            # Use the data view endpoint with FASTA format
            response = self.session.get(
                f"https://www.ebi.ac.uk/ena/browser/api/fasta/{accession}",
                headers={"Accept": "text/x-fasta"},
                timeout=30,
            )

            if response.status_code == 406:
                # Try alternative: embl format and convert
                response = self.session.get(
                    f"https://www.ebi.ac.uk/ena/browser/api/embl/{accession}",
                    timeout=30,
                )
                if response.ok:
                    # Extract sequence from EMBL format
                    return self._embl_to_fasta(accession, response.text)
                return None

            response.raise_for_status()
            return response.text

        except requests.exceptions.RequestException as e:
            logger.warning(f"Failed to get sequence {accession}: {e}")
            return None

    def _embl_to_fasta(self, accession: str, embl_text: str) -> str | None:
        """Convert EMBL format to FASTA."""
        try:
            lines = embl_text.split("\n")
            description = ""
            sequence_lines = []
            in_sequence = False

            for line in lines:
                if line.startswith("DE   "):
                    description += line[5:].strip() + " "
                elif line.startswith("SQ   "):
                    in_sequence = True
                elif in_sequence and not line.startswith("//"):
                    # Sequence lines have numbers at the end
                    seq = "".join(c for c in line if c.isalpha())
                    sequence_lines.append(seq.upper())

            if sequence_lines:
                sequence = "".join(sequence_lines)
                return f">{accession} {description.strip()}\n{sequence}\n"
            return None
        except Exception:
            return None

    def get_sequences_batch(
        self,
        accessions: list[str],
        batch_size: int = 10,
    ) -> Iterator[ENASequence]:
        """Get multiple sequences in batches.

        Args:
            accessions: List of accession numbers
            batch_size: Accessions per request (smaller for reliability)

        Yields:
            ENASequence objects
        """
        for i in range(0, len(accessions), batch_size):
            batch = accessions[i:i + batch_size]

            for accession in batch:
                self._rate_limit()

                try:
                    # Try to get FASTA directly
                    fasta = self.get_sequence_fasta(accession)
                    if fasta:
                        # Parse FASTA
                        lines = fasta.strip().split("\n")
                        if lines and lines[0].startswith(">"):
                            header = lines[0][1:]
                            sequence = "".join(lines[1:]).replace(" ", "")
                            yield self._parse_fasta_entry(header, sequence)

                except Exception as e:
                    logger.debug(f"Failed to fetch {accession}: {e}")

    def _parse_fasta_entry(self, header: str, sequence: str) -> ENASequence:
        """Parse a FASTA header and sequence."""
        # ENA FASTA header format: >accession description
        parts = header.split(" ", 1)
        accession = parts[0].split(".")[0]  # Remove version
        description = parts[1] if len(parts) > 1 else ""

        # Try to extract taxon_id and species from description
        taxon_id = 0
        scientific_name = ""

        # Common pattern: "Homo sapiens gene..."
        desc_parts = description.split()
        if len(desc_parts) >= 2:
            # First two words often are genus species
            if desc_parts[0][0].isupper() and desc_parts[1][0].islower():
                scientific_name = f"{desc_parts[0]} {desc_parts[1]}"

        return ENASequence(
            accession=accession,
            sequence=sequence,
            description=description,
            taxon_id=taxon_id,
            scientific_name=scientific_name,
            length=len(sequence),
        )

    def discover_species(
        self,
        kingdom: str,
        sample_size: int = 100000,
        min_length: int = 100,
        max_length: int = 50000,
    ) -> dict[str, dict]:
        """Discover species by sampling sequences from taxonomic subdivisions.

        Uses subdivisions to work around ENA's offset limit (~5000).

        Args:
            kingdom: Kingdom name
            sample_size: Target number of valid sequences to sample
            min_length: Minimum sequence length
            max_length: Maximum sequence length

        Returns:
            Dict of {species_name: {taxon_id, count}}
        """
        logger.info(f"Discovering species for {kingdom} from ENA...")

        species_counts: dict[str, dict] = {}
        total_valid = 0
        total_fetched = 0

        # Get subdivisions for this kingdom
        subdivisions = self.KINGDOM_SUBDIVISIONS.get(kingdom.lower(), [])
        if not subdivisions:
            # Fallback to main taxid
            main_taxid = self.KINGDOM_TAXIDS.get(kingdom.lower())
            if isinstance(main_taxid, list):
                subdivisions = main_taxid
            else:
                subdivisions = [main_taxid] if main_taxid else []

        if not subdivisions:
            logger.warning(f"No taxonomy IDs for {kingdom}")
            return species_counts

        # Calculate samples per subdivision
        samples_per_subdivision = max(sample_size // len(subdivisions), 5000)
        logger.info(f"  Querying {len(subdivisions)} taxonomic subdivisions...")

        for i, taxid in enumerate(subdivisions):
            if total_valid >= sample_size:
                break

            subdivision_valid = 0
            subdivision_fetched = 0
            offset = 0
            batch_size = 5000  # ENA's effective limit
            max_offset = 50000  # Don't go too far in any subdivision

            while subdivision_valid < samples_per_subdivision and offset < max_offset:
                self._rate_limit()

                tax_query = f"tax_tree({taxid})"

                params = {
                    "query": tax_query,
                    "result": "sequence",
                    "fields": "accession,tax_id,scientific_name,base_count",
                    "format": "json",
                    "limit": batch_size,
                    "offset": offset,
                }

                try:
                    response = self.session.get(
                        f"{ENA_PORTAL_API}/search",
                        params=params,
                        timeout=120,
                        headers={"Accept": "application/json"},
                    )

                    # Handle 400 errors (offset too large) gracefully
                    if response.status_code == 400:
                        logger.debug(
                            f"  Subdivision {taxid}: offset {offset} hit limit, "
                            "moving to next"
                        )
                        break

                    response.raise_for_status()
                    results = response.json()

                    if not results or not isinstance(results, list):
                        break

                    subdivision_fetched += len(results)
                    total_fetched += len(results)

                    for record in results:
                        # Filter by length
                        try:
                            length = int(record.get("base_count", 0))
                            if not (min_length <= length <= max_length):
                                continue
                        except (ValueError, TypeError):
                            continue

                        species = record.get("scientific_name", "")
                        taxon_id = record.get("tax_id", 0)

                        if not species or not taxon_id:
                            continue

                        # Skip invalid species names
                        species_lower = species.lower()
                        if any(x in species_lower for x in [
                            "uncultured", "unknown", "unidentified",
                            "environmental", "metagenome", "unclassified",
                            "synthetic", "vector", "plasmid"
                        ]):
                            continue

                        if species not in species_counts:
                            species_counts[species] = {
                                "taxon_id": int(taxon_id),
                                "count": 0,
                            }
                        species_counts[species]["count"] += 1
                        subdivision_valid += 1
                        total_valid += 1

                    offset += batch_size

                    # If we got fewer than requested, exhausted this subdivision
                    if len(results) < batch_size:
                        break

                except Exception as e:
                    logger.debug(f"  Subdivision {taxid} error at offset {offset}: {e}")
                    break

            if subdivision_valid > 0:
                logger.info(
                    f"  [{i+1}/{len(subdivisions)}] Taxid {taxid}: "
                    f"{subdivision_valid:,} valid, {len(species_counts):,} species total"
                )

        logger.info(
            f"Discovered {len(species_counts):,} species from {total_valid:,} "
            f"valid samples ({total_fetched:,} fetched)"
        )
        return species_counts

    def download_species_sequences(
        self,
        taxon_id: int,
        target_count: int = 50,
        exclude_accessions: set[str] | None = None,
        min_length: int = 100,
        max_length: int = 50000,
    ) -> list[ENASequence]:
        """Download sequences for a specific species.

        Args:
            taxon_id: NCBI taxonomy ID
            target_count: Target number of sequences
            exclude_accessions: Accessions to skip
            min_length: Minimum sequence length
            max_length: Maximum sequence length

        Returns:
            List of ENASequence objects
        """
        exclude = exclude_accessions or set()

        # Simple query by taxon
        query = f"tax_eq({taxon_id})"

        params = {
            "query": query,
            "result": "sequence",
            "fields": "accession,tax_id,scientific_name,description,base_count",
            "format": "json",
            "limit": target_count * 3,  # Get extra for filtering
        }

        self._rate_limit()

        try:
            response = self.session.get(
                f"{ENA_PORTAL_API}/search",
                params=params,
                timeout=60,
            )
            response.raise_for_status()
            results = response.json()

            if not results or not isinstance(results, list):
                return []

            # Filter by length and collect accessions
            accessions = []
            acc_to_meta = {}
            for record in results:
                acc = record.get("accession", "")
                if not acc or acc in exclude:
                    continue

                # Check length
                try:
                    length = int(record.get("base_count", 0))
                    if not (min_length <= length <= max_length):
                        continue
                except (ValueError, TypeError):
                    continue

                accessions.append(acc)
                acc_to_meta[acc] = record

                if len(accessions) >= target_count:
                    break

            if not accessions:
                return []

            # Fetch actual sequences
            sequences = list(self.get_sequences_batch(accessions))

            # Update with metadata from search
            for seq in sequences:
                if seq.accession in acc_to_meta:
                    meta = acc_to_meta[seq.accession]
                    seq.taxon_id = int(meta.get("tax_id", 0))
                    seq.scientific_name = meta.get("scientific_name", "")

            return sequences[:target_count]

        except Exception as e:
            logger.warning(f"Failed to download species {taxon_id}: {e}")
            return []
