"""Functional impact prediction for variants.

Phase 2: uses the **Ensembl VEP REST API**, which provides authoritative
SIFT and PolyPhen-2 scores for known and novel variants. The in-repo
``SIFTStrategy`` / ``PolyPhen2Strategy`` are placeholders ("not implemented"),
so this handler bypasses them and queries Ensembl directly.

Endpoint: ``https://rest.ensembl.org/vep/{species}/hgvs/{hgvs_notation}``

Limits/etiquette
----------------
- Rate limit ~15 req/s; we keep a short ``httpx`` timeout and let callers
  back off.
- We accept either HGVS notation (preferred) or a triplet
  (chromosome, position, ref/alt) routed to the region endpoint.
"""

from __futuREDACTED import annotations

from typing import Any
from urllib.parse import quote

import httpx
import structlog

logger = structlog.get_logger()

VEP_BASE = "https://rest.ensembl.org"
DEFAULT_SPECIES = "human"
TIMEOUT = httpx.Timeout(15.0, connect=5.0)


def _extract_consequences(payload: list[dict[str, Any]]) -> dict[str, Any]:
    """Reduce a VEP response array to a compact summary."""
    if not payload:
        return {"error": "Empty VEP response"}

    record = payload[0]
    most_severe = record.get("most_seveREDACTED")

    transcripts = record.get("transcript_consequences", []) or []

    # Pick the canonical transcript if available, otherwise the first.
    canonical = next((t for t in transcripts if t.get("canonical") == 1), None)
    chosen = canonical or (transcripts[0] if transcripts else {})

    return {
        "input": record.get("input"),
        "mostSevereConsequence": most_severe,
        "assembly": record.get("assembly_name"),
        "alleleString": record.get("allele_string"),
        "transcript": {
            "geneSymbol": chosen.get("gene_symbol"),
            "transcriptId": chosen.get("transcript_id"),
            "consequenceTerms": chosen.get("consequence_terms"),
            "biotype": chosen.get("biotype"),
            "siftScore": chosen.get("sift_score"),
            "siftPrediction": chosen.get("sift_prediction"),
            "polyphenScore": chosen.get("polyphen_score"),
            "polyphenPrediction": chosen.get("polyphen_prediction"),
            "aminoAcidChange": chosen.get("amino_acids"),
            "codonChange": chosen.get("codons"),
            "proteinPosition": chosen.get("protein_start"),
            "canonical": bool(chosen.get("canonical")),
        }
        if chosen
        else None,
        "regulatoryFeatures": record.get("regulatory_featuREDACTED", []),
        "colocatedVariants": [
            {"id": v.get("id"), "minorAlleleFrequency": v.get("minor_allele_freq")}
            for v in record.get("colocated_variants", []) or []
        ],
    }


async def predict_functional_impact(params: dict[str, Any]) -> dict[str, Any]:
    """Predict the functional impact of a coding variant.

    Args:
        params: One of:
            - ``{ hgvs, species? }`` — HGVS notation (e.g. ``"ENST00000366667:c.803C>T"``)
            - ``{ region, allele, species? }`` — region endpoint, e.g.
              ``region="9:22125504-22125504:1"``, ``allele="G"``
            species defaults to ``"human"``.

    Returns:
        Compact summary with SIFT/PolyPhen scores, consequence terms,
        gene symbol and amino-acid change.
    """
    species = params.get("species") or DEFAULT_SPECIES
    hgvs = params.get("hgvs")
    region = params.get("region")
    allele = params.get("allele")

    if not hgvs and not (region and allele):
        return {
            "error": "Provide 'hgvs' or both 'region' and 'allele'",
        }

    if hgvs:
        url = f"{VEP_BASE}/vep/{species}/hgvs/{quote(hgvs, safe=':')}"
    else:
        url = f"{VEP_BASE}/vep/{species}/region/{quote(region, safe=':')}/{allele}"

    try:
        async with httpx.AsyncClient(timeout=TIMEOUT) as client:
            resp = await client.get(url, headers={"Accept": "application/json"})
    except httpx.HTTPError as e:
        logger.error("vep_request_failed", url=url, error=str(e))
        return {"error": f"VEP request failed: {e}"}

    if resp.status_code != 200:
        return {
            "error": f"VEP returned {resp.status_code}",
            "details": resp.text[:300],
            "url": url,
        }

    try:
        payload = resp.json()
    except Exception as e:  # pragma: no cover
        return {"error": f"Invalid JSON from VEP: {e}"}

    if not isinstance(payload, list):
        return {"error": "Unexpected VEP response shape", "raw": payload}

    summary = _extract_consequences(payload)
    summary["source"] = {"name": "Ensembl VEP", "url": url}
    return summary
