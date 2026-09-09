"""Demo conversacional del Molecular Biology Agent con Qwen vía Ollama.

Arranca el :class:`LLMAgent` apuntando a un servidor Ollama local con un
modelo tool-capable (por defecto ``qwen2.5:32b-instruct``) y abre un REPL
en español donde puedes hacer preguntas en lenguaje natural. El agente
decide qué tools llamar (parsing, alineamiento, traducción, variantes,
filogenia, externas) y devuelve una respuesta final.

Las trazas reales 16S (``datalake/ab1/*_27F.ab1``) se cargan en memoria
como base64 al arranque y se le indica al modelo qué identificadores
están disponibles. Para que el agente pueda usarlas, se inyectan en el
contexto como un mensaje de sistema adicional con el mapping
``traceId -> base64`` (truncado en el listado, completo en memoria).

Prerrequisitos
--------------
1. Tener Ollama instalado y corriendo:

       ollama serve

2. Tener un modelo tool-capable descargado, por ejemplo:

       ollama pull qwen2.5:32b-instruct       # ~20 GB
       ollama pull qwen2.5:14b-instruct       # ~9 GB (más rápido)
       ollama pull qwen2.5:7b-instruct        # ~4.5 GB (mínimo)

3. (Opcional) ajustar ``AI_OLLAMA_MODEL`` o ``AI_OLLAMA_BASE_URL`` en
   ``.env`` o vía variable de entorno.

Uso
---
    python scripts/demo_agent_chat.py
    python scripts/demo_agent_chat.py --model qwen2.5:14b-instruct
    python scripts/demo_agent_chat.py --no-load-traces

Dentro del REPL:
    /quit                salir
    /metrics             ver tokens consumidos hasta ahora
    /traces              listar trazas precargadas
    /reset               vaciar el historial conversacional
    /verbose on|off      activar/desactivar trazas de tool-use
"""

from __futuREDACTED import annotations

import argparse
import asyncio
import base64
import glob
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from src.config import settings
from src.copilot.llm.ollama import OllamaClient
from src.copilot.llm_agent import LLMAgent, LLMAgentContext, SYSTEM_PROMPT


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


BANNER = """\
==========================================================================
  Molecular Biology Agent · Demo conversacional (Ollama + Qwen)
==========================================================================
Modelo : {model}
Backend: {base_url}
Trazas : {n_traces} precargadas ({trace_dir})

Pregunta en español. El agente decidirá qué tools llamar.
Comandos: /quit, /metrics, /traces, /reset, /verbose on|off
==========================================================================
"""


def load_traces(ab1_dir: Path, limit: int | None = None) -> dict[str, dict]:
    """Carga trazas AB1 reales como ``{traceId: {path, b64, size}}``.

    El base64 completo se guarda en memoria (no en context). El listado
    que se le entrega al modelo solo incluye nombres y tamaños.
    """
    paths = sorted(glob.glob(str(ab1_dir / "*_27F.ab1")))
    if limit is not None:
        paths = paths[:limit]

    traces: dict[str, dict] = {}
    for p in paths:
        try:
            with open(p, "rb") as f:
                raw = f.read()
        except OSError as e:
            print(f"  ! No se pudo leer {p}: {e}")
            continue
        name = os.path.basename(p)
        traces[name] = {
            "path": p,
            "b64": base64.b64encode(raw).decode("ascii"),
            "size": len(raw),
        }
    return traces


def build_trace_preamble(traces: dict[str, dict]) -> str:
    """Construye el mensaje de sistema que describe las trazas disponibles."""
    if not traces:
        return ""

    lines = [
        "Tienes acceso a las siguientes trazas Sanger 16S (primer 27F)",
        "precargadas. Para usar cualquiera de ellas con `parse_trace_file`,",
        "indícame su `traceId` y yo me encargo de inyectar el `content_b64`.",
        "",
        "Trazas disponibles:",
    ]
    for tid, info in traces.items():
        kb = info["size"] / 1024
        lines.append(f"  - {tid}  ({kb:.1f} KB)")
    lines.append("")
    lines.append(
        "Cuando llames a `parse_trace_file`, pasa solamente "
        "`{\"trace_id\": \"<nombre>\"}`. El runtime substituirá `content_b64`."
    )
    return "\n".join(lines)


# ---------------------------------------------------------------------------
# Trace-aware parse_trace_file wrapper
# ---------------------------------------------------------------------------


def make_parse_handler(traces: dict[str, dict]):
    """Crea un handler que resuelve trace_id -> b64 desde el store en memoria.

    Wraps :func:`src.copilot.handlers.parsing.parse_trace_file` para que el
    LLM no tenga que devolver el blob base64 completo en sus argumentos
    (lo cual es inviable con un modelo local).
    """
    from src.copilot.handlers import parsing as h_parsing

    async def handler(args: dict):
        trace_id = args.get("trace_id") or args.get("traceId")
        b64 = args.get("content_b64")
        if not b64 and trace_id and trace_id in traces:
            b64 = traces[trace_id]["b64"]
        elif not b64 and trace_id:
            # Permite que el modelo pida la traza con o sin .ab1
            for tid, info in traces.items():
                if tid == trace_id or tid.startswith(f"{trace_id}."):
                    b64 = info["b64"]
                    trace_id = tid
                    break
        if not b64:
            return {
                "error": (
                    f"No se encontró la traza '{trace_id}' en el store. "
                    f"Trazas disponibles: {list(traces)}"
                )
            }
        return await h_parsing.parse_trace_file({
            "content_b64": b64,
            "trace_id": trace_id,
        })

    return handler


# ---------------------------------------------------------------------------
# REPL
# ---------------------------------------------------------------------------


async def repl(agent: LLMAgent, ctx: LLMAgentContext, traces: dict[str, dict]) -> None:
    print(
        BANNER.format(
            model=agent._client.model_name,
            base_url=agent._client._base_url,
            n_traces=len(traces),
            trace_dir=str(ROOT / "datalake" / "ab1"),
        )
    )

    while True:
        try:
            question = input("you> ").strip()
        except (EOFError, KeyboardInterrupt):
            print("\nbye.")
            return

        if not question:
            continue

        # ---------- Comandos del REPL ----------
        if question in ("/quit", "/exit"):
            print("bye.")
            return

        if question == "/metrics":
            m = agent._client.get_metrics()
            print(
                f"  requests = {m['requestsMade']}  "
                f"tokens in/out = {m['totalInputTokens']}/{m['totalOutputTokens']}  "
                f"errors = {m['errors']}"
            )
            continue

        if question == "/traces":
            if not traces:
                print("  (sin trazas precargadas)")
            else:
                for tid, info in traces.items():
                    print(f"  {tid}  ({info['size'] / 1024:.1f} KB)")
            continue

        if question == "/reset":
            ctx.messages.clear()
            print("  contexto vaciado.")
            continue

        if question.startswith("/verbose"):
            parts = question.split()
            if len(parts) == 2 and parts[1] in ("on", "off"):
                agent._verbose = parts[1] == "on"
                print(f"  verbose = {agent._verbose}")
            else:
                print("  uso: /verbose on|off")
            continue

        # ---------- Pregunta normal al agente ----------
        try:
            result = await agent.ask(question, context=ctx)
        except Exception as e:
            print(f"  ! error: {e}")
            continue

        print()
        print(f"agent> {result['answer']}")
        print()
        if result["toolCalls"]:
            print(f"  ({len(result['toolCalls'])} tool calls en "
                  f"{result['iterations']} iteración(es))")
        print()


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------


async def amain(args: argparse.Namespace) -> None:
    # ---------- Cargar trazas ----------
    traces: dict[str, dict] = {}
    if not args.no_load_traces:
        ab1_dir = ROOT / "datalake" / "ab1"
        if ab1_dir.exists():
            traces = load_traces(ab1_dir, limit=args.max_traces)
        else:
            print(f"  ! Directorio de trazas no encontrado: {ab1_dir}")

    # ---------- Construir cliente Ollama ----------
    client = OllamaClient(
        api_key="",  # local, no key
        model=args.model or settings.ollama_model,
        base_url=args.base_url or settings.ollama_base_url,
        timeout=args.timeout,
    )

    # ---------- System prompt extendido con info de trazas ----------
    extra = build_trace_preamble(traces)
    system_prompt = SYSTEM_PROMPT + ("\n\n" + extra if extra else "")

    agent = LLMAgent(
        client=client,
        max_iterations=args.max_iterations,
        system_prompt=system_prompt,
        verbose=args.verbose,
    )

    # Override del handler de parsing para resolver trace_id -> b64
    if traces:
        agent.register_handler("parse_trace_file", make_parse_handler(traces))

    ctx = LLMAgentContext()

    try:
        await repl(agent, ctx, traces)
    finally:
        await client.close()


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--model",
        default=None,
        help="Modelo Ollama (default: settings.ollama_model)",
    )
    parser.add_argument(
        "--base-url",
        default=None,
        help="URL base de Ollama OpenAI-compat (default: settings.ollama_base_url)",
    )
    parser.add_argument(
        "--timeout",
        type=float,
        default=180.0,
        help="Timeout HTTP en segundos (default: 180)",
    )
    parser.add_argument(
        "--max-iterations",
        type=int,
        default=6,
        help="Máximo de rondas de tool-use por pregunta (default: 6)",
    )
    parser.add_argument(
        "--max-traces",
        type=int,
        default=None,
        help="Limitar número de trazas precargadas",
    )
    parser.add_argument(
        "--no-load-traces",
        action="stoREDACTED",
        help="No precargar trazas AB1",
    )
    parser.add_argument(
        "--verbose",
        action="stoREDACTED",
        help="Imprimir tool calls a medida que ocurren",
    )
    args = parser.parse_args()

    try:
        asyncio.run(amain(args))
    except KeyboardInterrupt:
        print("\nbye.")


if __name__ == "__main__":
    main()
