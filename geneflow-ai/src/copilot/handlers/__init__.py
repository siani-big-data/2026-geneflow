"""Domain-specific handlers for the Molecular Biology Agent.

Each module exposes pure async functions that take a dict of arguments
and return a JSON-serializable dict. They are registered into the agent's
tool dispatcher in ``agent.py``.

Phase 1: parsing, alignment, translation
Phase 2: variants, functional, external (InterPro, PubMed)
Phase 3: phylogeny (distance matrix, NJ/UPGMA trees, bootstrap)
"""

from . import (
    alignment,
    external,
    functional,
    parsing,
    phylogeny,
    translation,
    variants,
)

__all__ = [
    "alignment",
    "external",
    "functional",
    "parsing",
    "phylogeny",
    "translation",
    "variants",
]
