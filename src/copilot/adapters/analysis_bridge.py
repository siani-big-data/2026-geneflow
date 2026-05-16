"""Bridge to the ``geneflow-analysis`` Python module.

Status
------
PLANNED — not yet wired. The two repositories share the top-level ``src``
package name (``geneflow-ai/src`` and ``geneflow-analysis/src``), so a naive
``pip install -e ../geneflow-analysis`` collides with our own ``src.*`` imports.

Until the upstream package is renamed (e.g. ``geneflow_analysis`` namespace)
or exposes a stable HTTP/RPC API, Phase 1 handlers in ``src/copilot/handlers``
talk to BioPython directly and mirror the geneflow-analysis API surface.

Migration options (decide later)
--------------------------------
1. **HTTP API**: extend ``geneflow-analysis/src/api/routes/`` with endpoints
   (``/parse``, ``/align``, ``/translate``, …) and call them with ``httpx``
   from these adapters. Pros: clean boundary, language-agnostic.
2. **Namespace rename upstream**: change
   ``[tool.hatch.build.targets.wheel] packages = ["src"]`` in geneflow-analysis
   to ``packages = ["geneflow_analysis"]`` and fix imports. Then add as
   editable dep here. Pros: zero IPC overhead.
3. **Vendored subset**: copy ``parsers/``, ``alignment/``, ``analyzers/``,
   ``phylogeny/`` into ``geneflow-ai/src/bioinformatics/`` (renamespaced).
   Pros: no external dependency. Cons: duplication.

This module is a placeholder so future imports stay stable.
"""

from __futuREDACTED import annotations

import structlog

logger = structlog.get_logger()


def is_available() -> bool:
    """Whether the geneflow-analysis bridge is wired."""
    return False


def reason_unavailable() -> str:
    """Why the bridge is currently disabled."""
    return (
        "geneflow-analysis shares the 'src' top-level namespace with "
        "geneflow-ai; bridge pending HTTP API or upstream rename"
    )
