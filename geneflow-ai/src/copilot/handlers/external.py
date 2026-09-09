"""External database lookups: InterPro (protein domains) and PubMed (literature).

InterPro (EBI)
    GET https://www.ebi.ac.uk/interpro/api/entry/InterPro/protein/UniProt/{accession}
    Returns the InterPro entries (domains/families/sites) matched against a
    UniProt accession.

PubMed (NCBI E-utilities)
    GET https://eutils.ncbi.nlm.nih.gov/entrez/eutils/esearch.fcgi?db=pubmed&term=...
    GET https://eutils.ncbi.nlm.nih.gov/entrez/eutils/esummary.fcgi?db=pubmed&id=...

Etiquette
---------
NCBI requests should include ``tool`` and ``email`` params (recommended). We
read the email from settings if available, otherwise omit.
"""

from __futuREDACTED import annotations

from typing import Any
from urllib.parse import quote

import httpx
import structlog

logger = structlog.get_logger()

INTERPRO_BASE = "https://www.ebi.ac.uk/interpro/api"
EUTILS_BASE = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils"
TIMEOUT = httpx.Timeout(20.0, connect=5.0)


# ---------------------------------------------------------------------------
# InterPro
# ---------------------------------------------------------------------------


def _summarise_interpro_entry(item: dict[str, Any]) -> dict[str, Any]:
    metadata = item.get("metadata", {}) or {}
    proteins = item.get("proteins", []) or []
    locations: list[dict[str, Any]] = []
    if proteins:
        # First protein hit holds the location entries for the queried accession.
        for entry in proteins[0].get("entry_protein_locations", []) or []:
            for loc in entry.get("fragments", []) or []:
                locations.append(
                    {
                        "start": loc.get("start"),
                        "end": loc.get("end"),
                    }
                )
    return {
        "accession": metadata.get("accession"),
        "name": metadata.get("name"),
        "type": metadata.get("type"),
        "memberDatabases": metadata.get("member_databases"),
        "goTerms": metadata.get("go_terms"),
        "locations": locations,
    }


async def lookup_interpro(params: dict[str, Any]) -> dict[str, Any]:
    """Look up InterPro entries (domains, families, sites) for a UniProt
    accession.

    Args:
        params: ``{ accession }`` — UniProt accession, e.g. ``"P38398"`` (BRCA1)

    Returns:
        ``{ accession, count, entries: [{accession, name, type, locations, goTerms}] }``
    """
    accession = (params.get("accession") or "").strip()
    if not accession:
        return {"error": "accession is required (UniProt ID)"}

    url = (
        f"{INTERPRO_BASE}/entry/InterPro/protein/UniProt/{quote(accession)}"
        "?page_size=200"
    )

    try:
        async with httpx.AsyncClient(timeout=TIMEOUT) as client:
            resp = await client.get(url, headers={"Accept": "application/json"})
    except httpx.HTTPError as e:
        logger.error("interpro_request_failed", url=url, error=str(e))
        return {"error": f"InterPro request failed: {e}"}

    if resp.status_code == 204:
        return {"accession": accession, "count": 0, "entries": []}
    if resp.status_code != 200:
        return {
            "error": f"InterPro returned {resp.status_code}",
            "details": resp.text[:300],
            "url": url,
        }

    try:
        payload = resp.json()
    except Exception as e:  # pragma: no cover
        return {"error": f"Invalid JSON from InterPro: {e}"}

    results = payload.get("results", []) or []
    entries = [_summarise_interpro_entry(item) for item in results]
    return {
        "accession": accession,
        "count": payload.get("count", len(entries)),
        "entries": entries,
        "source": {"name": "EBI InterPro", "url": url},
    }


# ---------------------------------------------------------------------------
# PubMed
# ---------------------------------------------------------------------------


async def _eutils_get(
    client: httpx.AsyncClient,
    path: str,
    params: dict[str, str],
) -> httpx.Response:
    return await client.get(f"{EUTILS_BASE}/{path}", params=params)


async def search_pubmed(params: dict[str, Any]) -> dict[str, Any]:
    """Search PubMed and return summaries for the top hits.

    Args:
        params: ``{ query, max_results?, email? }``
            query: free-text PubMed query (e.g. ``"BRCA1 variant pathogenicity"``)
            max_results: 1..50 (default 10)
            email: optional contact email per NCBI guidelines

    Returns:
        ``{ query, total, articles: [{pmid, title, authors, journal, year, doi}] }``
    """
    query = (params.get("query") or "").strip()
    if not query:
        return {"error": "query is required"}

    max_results = max(1, min(50, int(params.get("max_results", 10))))
    email = params.get("email") or ""

    common: dict[str, str] = {
        "db": "pubmed",
        "retmode": "json",
        "tool": "geneflow-ai",
    }
    if email:
        common["email"] = email

    try:
        async with httpx.AsyncClient(timeout=TIMEOUT) as client:
            search_resp = await _eutils_get(
                client,
                "esearch.fcgi",
                {**common, "term": query, "retmax": str(max_results)},
            )
            if search_resp.status_code != 200:
                return {
                    "error": f"esearch returned {search_resp.status_code}",
                    "details": search_resp.text[:300],
                }
            search_data = search_resp.json().get("esearchresult", {}) or {}
            id_list = search_data.get("idlist", []) or []
            total = int(search_data.get("count", "0") or 0)

            if not id_list:
                return {"query": query, "total": total, "articles": []}

            summary_resp = await _eutils_get(
                client,
                "esummary.fcgi",
                {**common, "id": ",".join(id_list)},
            )
            if summary_resp.status_code != 200:
                return {
                    "error": f"esummary returned {summary_resp.status_code}",
                    "details": summary_resp.text[:300],
                }
            summary_data = summary_resp.json().get("result", {}) or {}
    except httpx.HTTPError as e:
        logger.error("pubmed_request_failed", error=str(e))
        return {"error": f"PubMed request failed: {e}"}

    articles: list[dict[str, Any]] = []
    for pmid in id_list:
        rec = summary_data.get(pmid)
        if not rec:
            continue
        doi = next(
            (a.get("value") for a in rec.get("articleids", []) if a.get("idtype") == "doi"),
            None,
        )
        pubdate = rec.get("pubdate") or ""
        year = pubdate.split(" ", 1)[0] if pubdate else None
        articles.append(
            {
                "pmid": pmid,
                "title": rec.get("title"),
                "authors": [a.get("name") for a in rec.get("authors", []) or []],
                "journal": rec.get("fulljournalname") or rec.get("source"),
                "year": year,
                "doi": doi,
                "url": f"https://pubmed.ncbi.nlm.nih.gov/{pmid}/",
            }
        )

    return {
        "query": query,
        "total": total,
        "returned": len(articles),
        "articles": articles,
        "source": {"name": "NCBI E-utilities (PubMed)"},
    }
