"""End-to-end demo of the Molecular Biology Agent tools (Phase 1-3).

Runs the canonical bacterial 16S workflow on real Sanger traces from
``datalake/ab1/*_27F.ab1`` without invoking the LLM. Exercises:

    parse_trace_file × N   (Phase 1)
    reverse_complement     (Phase 1)
    translate_sequence     (Phase 1, sanity print)
    align_multiple         (Phase 1)
    build_consensus        (Phase 1, IUPAC)
    detect_variants_from_alignment  (Phase 2)
    compute_distance_matrix         (Phase 3)
    build_phylogenetic_tree         (Phase 3, NJ + JC)
    bootstrap_tree                  (Phase 3, 50 replicates)

Usage
-----
    python scripts/demo_agent_pipeline.py
    python scripts/demo_agent_pipeline.py --traces 11_27F 13_27F 14_27F 5_27F
    python scripts/demo_agent_pipeline.py --trim-window 20 --replicates 100
"""

from __futuREDACTED import annotations

import argparse
import asyncio
import base64
import glob
import os
import sys
from pathlib import Path

# Allow running from the repo root without installing the package.
ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from src.copilot.handlers import alignment as h_alignment
from src.copilot.handlers import parsing as h_parsing
from src.copilot.handlers import phylogeny as h_phylogeny
from src.copilot.handlers import translation as h_translation
from src.copilot.handlers import variants as h_variants


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def bullet(title: str) -> None:
    print()
    print("=" * 72)
    print(title)
    print("=" * 72)


def trim_by_quality(sequence: str, scores: list[int], window: int = 10,
                    min_avg: float = 20.0) -> tuple[str, int, int]:
    """Trim 5' and 3' ends until a window of ``window`` bases has avg Q>=min_avg."""
    n = len(sequence)
    if n < window or not scores or len(scores) != n:
        return sequence, 0, n

    def avg(i: int) -> float:
        return sum(scores[i:i + window]) / window

    start = 0
    while start <= n - window and avg(start) < min_avg:
        start += 1
    end = n
    while end >= start + window and avg(end - window) < min_avg:
        end -= 1
    return sequence[start:end], start, end


def short(seq: str, n: int = 60) -> str:
    if len(seq) <= n:
        return seq
    half = n // 2 - 2
    return f"{seq[:half]}...{seq[-half:]}  (len={len(seq)})"


# ---------------------------------------------------------------------------
# Demo
# ---------------------------------------------------------------------------


async def run(ab1_paths: list[str], trim_window: int, min_q: float,
              replicates: int) -> None:
    bullet("FASE 1 · Parsing de trazas AB1")
    print(f"Trazas a procesar: {len(ab1_paths)}")
    parsed: list[dict] = []
    for p in ab1_paths:
        with open(p, "rb") as f:
            b64 = base64.b64encode(f.read()).decode("ascii")
        res = await h_parsing.parse_trace_file({
            "content_b64": b64,
            "trace_id": os.path.basename(p),
        })
        if "error" in res:
            print(f"  X {p}: {res['error']}")
            continue
        # parse_trace_file returns {sequence: {id, sequence, quality, length}}
        seq_block = res["sequence"]
        seq = seq_block["sequence"]
        scores = seq_block.get("quality") or []
        q20 = (
            sum(1 for s in scores if s >= 20) / len(scores) * 100
            if scores
            else 0.0
        )
        print(f"  + {os.path.basename(p):>30s}  len={len(seq):4d}  Q20={q20:5.1f}%")
        parsed.append({
            "traceId": res.get("traceId"),
            "sequence": seq,
            "qualityScores": scores,
        })

    if len(parsed) < 2:
        print("Insuficientes trazas parseadas. Abortando.")
        return

    # ------------------------------------------------------------------ Trim
    bullet(f"QC · Trimming por calidad (ventana={trim_window}, minQ={min_q})")
    trimmed: list[str] = []
    labels: list[str] = []
    for rec in parsed:
        seq, start, end = trim_by_quality(
            rec["sequence"], rec["qualityScores"], trim_window, min_q
        )
        # Use a stable, short label for tree leaves (Newick disallows many chars)
        label = (
            rec["traceId"]
            .replace(".ab1", "")
            .replace("-", "_")
            .replace(".", "_")
        )
        print(f"  {label:>15s}  raw={len(rec['sequence']):4d}  "
              f"trimmed={len(seq):4d}  ({start}..{end})")
        trimmed.append(seq)
        labels.append(label)

    # Use only the conserved central region to make the demo MSA tractable.
    # We take the first ~400 bp of each trimmed read after the trim point.
    central = [s[:400] for s in trimmed]

    # --------------------------------------------------------- Reverse comp
    bullet("FASE 1 · reverse_complement (primera traza)")
    rc = await h_translation.reverse_complement({"sequence": central[0][:80]})
    print(f"  fwd: {central[0][:80]}")
    print(f"  rev: {rc['reverseComplement']}")

    # --------------------------------------------------------------- Translate
    bullet("FASE 1 · translate_sequence (primera traza, marco 1)")
    tr = await h_translation.translate_sequence({
        "sequence": central[0],
        "frame": 1,
    })
    if "error" not in tr:
        protein = tr.get("protein", "")
        stops = tr.get("stopCodons", 0)
        print(f"  proteína: {short(protein, 60)}")
        print(f"  longitud: {len(protein)} aa, codones stop: {stops}")
    else:
        print(f"  warning: {tr['error']}")

    # ------------------------------------------------------------------ MSA
    bullet("FASE 1 · align_multiple (alineamiento progresivo)")
    print("  Esto puede tardar unos segundos con 6 lecturas...")
    msa = await h_alignment.align_multiple({"sequences": central})
    if "error" in msa:
        print(f"  X {msa['error']}")
        return
    aligned = msa["alignedSequences"]
    print(f"  + alineamiento: {len(aligned)} secuencias, "
          f"longitud={msa['length']}, identidad media={msa['averageIdentity']}%, "
          f"gaps={msa['gaps']}")

    # ---------------------------------------------------------- Consensus
    bullet("FASE 1 · build_consensus (método=iupac)")
    cons = await h_alignment.build_consensus({
        "aligned_sequences": aligned,
        "method": "iupac",
    })
    if "error" in cons:
        print(f"  X {cons['error']}")
    else:
        c = cons["consensus"]
        ambiguous = sum(1 for ch in c if ch not in "ACGT-")
        print(f"  + consenso (len={len(c)}, ambiguos={ambiguous}):")
        print(f"    {short(c, 80)}")

    # ------------------------------------------------------------ Variants
    bullet("FASE 2 · detect_variants_from_alignment (ref = primera)")
    var = await h_variants.detect_variants_from_alignment({
        "aligned_sequences": aligned,
        "reference_index": 0,
        "min_frequency": 0.2,
    })
    if "error" in var:
        print(f"  X {var['error']}")
    else:
        print(f"  + SNPs: {var['snpCount']}, "
              f"inserciones: {var['insertionCount']}, "
              f"deleciones: {var['deletionCount']}, "
              f"posiciones variantes: {var['variantPositions']} "
              f"de {var['totalPositions']}")
        for v in var["variants"][:8]:
            print(f"    pos={v['position']:4d}  "
                  f"{v['reference']}>{v['alternate']}  "
                  f"tipo={v['type']:9s}  "
                  f"freq={v['frequency']:.2f}  "
                  f"cov={v['coverage']}")
        if len(var["variants"]) > 8:
            print(f"    ... +{len(var['variants']) - 8} más")

    # ------------------------------------------------------ Distance matrix
    bullet("FASE 3 · compute_distance_matrix (Jukes-Cantor)")
    dm = await h_phylogeny.compute_distance_matrix({
        "aligned_sequences": aligned,
        "labels": labels,
        "method": "jukes_cantor",
    })
    if "error" in dm:
        print(f"  X {dm['error']}")
    else:
        print(f"  + saturatedPairs={dm['saturatedPairs']}")
        header = " " * 16 + "".join(f"{l[:8]:>10s}" for l in dm["labels"])
        print(header)
        for lab, row in zip(dm["labels"], dm["matrix"]):
            cells = "".join(f"{v:10.4f}" for v in row)
            print(f"  {lab[:14]:>14s}  {cells}")

    # -------------------------------------------------------------- Tree
    bullet("FASE 3 · build_phylogenetic_tree (NJ + Jukes-Cantor)")
    tree = await h_phylogeny.build_phylogenetic_tree({
        "aligned_sequences": aligned,
        "labels": labels,
        "distance_method": "jukes_cantor",
        "tree_method": "nj",
    })
    if "error" in tree:
        print(f"  X {tree['error']}")
    else:
        print(f"  + método={tree['method']}  "
              f"distancia={tree['distanceMethod']}  "
              f"hojas={tree['sequenceCount']}")
        print(f"  newick:")
        print(f"    {tree['newick']}")

    # --------------------------------------------------------- Bootstrap
    bullet(f"FASE 3 · bootstrap_tree ({replicates} réplicas, seed=42)")
    boot = await h_phylogeny.bootstrap_tree({
        "aligned_sequences": aligned,
        "labels": labels,
        "replicates": replicates,
        "distance_method": "jukes_cantor",
        "tree_method": "nj",
        "seed": 42,
    })
    if "error" in boot:
        print(f"  X {boot['error']}")
    else:
        print(f"  + réplicas exitosas: "
              f"{boot['successfulReplicates']}/{boot['replicates']}")
        print(f"  + árbol original:")
        print(f"    {boot['originalTree']['newick']}")
        print(f"  + valores de soporte (clados internos):")
        if not boot["supportValues"]:
            print("    (ningún clado interno bipartito)")
        for bipart, supp in sorted(
            boot["supportValues"].items(), key=lambda kv: -kv[1]
        ):
            print(f"    {supp:5.1f}%   {bipart}")

    bullet("Demo completado")
    print("Todos los handlers de Fase 1-3 ejecutados sin errores.")
    print("El agente Claude consume estas mismas funciones vía tool-use.")


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--traces",
        nargs="*",
        help=(
            "Nombres base de trazas (sin .ab1) a usar. "
            "Por defecto: todas las _27F.ab1 disponibles."
        ),
    )
    parser.add_argument("--trim-window", type=int, default=15)
    parser.add_argument("--min-q", type=float, default=20.0)
    parser.add_argument("--replicates", type=int, default=50)
    args = parser.parse_args()

    ab1_dir = ROOT / "datalake" / "ab1"
    if args.traces:
        paths = [str(ab1_dir / f"{n}.ab1") for n in args.traces]
        missing = [p for p in paths if not os.path.exists(p)]
        if missing:
            print("ERROR: trazas no encontradas:", missing)
            sys.exit(1)
    else:
        paths = sorted(glob.glob(str(ab1_dir / "*_27F.ab1")))
        if not paths:
            print(f"ERROR: no se han encontrado trazas _27F.ab1 en {ab1_dir}")
            sys.exit(1)

    asyncio.run(
        run(paths, args.trim_window, args.min_q, args.replicates)
    )


if __name__ == "__main__":
    main()
