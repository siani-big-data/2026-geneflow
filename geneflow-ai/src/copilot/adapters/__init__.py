"""Adapters to external GeneFlow modules.

Currently provides a documented bridge path to ``geneflow-analysis`` which
shares the ``src`` namespace and therefore cannot be imported as a library
without namespace collisions. Phase 1 handlers fall back to direct
BioPython usage; this adapter is the place where the bridge will be
implemented (HTTP API, subprocess, or vendored sub-package).
"""

from . import analysis_bridge

__all__ = ["analysis_bridge"]
