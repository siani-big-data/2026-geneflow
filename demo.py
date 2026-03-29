#!/usr/bin/env python3
"""
Demo del MolecularBiologyAgent de GeneFlow AI.

Ejecutar:
    uv run python demo.py

Requiere:
    - AI_CLAUDE_API_KEY en .env o variable de entorno
    - (Opcional) AI_BLAST_EMAIL para busquedas BLAST reales
"""

import asyncio
import os
import sys

# Fix Windows console encoding
if sys.platform == "win32":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

# Colores para terminal
class Colors:
    HEADER = "\033[95m"
    BLUE = "\033[94m"
    CYAN = "\033[96m"
    GREEN = "\033[92m"
    YELLOW = "\033[93m"
    RED = "\033[91m"
    END = "\033[0m"
    BOLD = "\033[1m"


def print_header(text: str) -> None:
    print(f"\n{Colors.HEADER}{Colors.BOLD}{'='*60}{Colors.END}")
    print(f"{Colors.HEADER}{Colors.BOLD}  {text}{Colors.END}")
    print(f"{Colors.HEADER}{Colors.BOLD}{'='*60}{Colors.END}\n")


def print_tool(name: str, desc: str) -> None:
    print(f"  {Colors.CYAN}▸ {name}{Colors.END}")
    print(f"    {Colors.YELLOW}{desc[:70]}...{Colors.END}")


def print_question(q: str) -> None:
    print(f"\n{Colors.BLUE}{Colors.BOLD}[Usuario]:{Colors.END} {q}")


def print_answer(a: str) -> None:
    print(f"\n{Colors.GREEN}{Colors.BOLD}[Agente]:{Colors.END}")
    # Indent answer lines
    for line in a.split("\n"):
        print(f"   {line}")


def print_error(e: str) -> None:
    print(f"\n{Colors.RED}[Error] {e}{Colors.END}")


def print_info(i: str) -> None:
    print(f"{Colors.YELLOW}> {i}{Colors.END}")


async def run_demo():
    """Ejecutar demo interactiva."""
    print_header("GeneFlow AI - Demo del Agente de Biología Molecular")

    # Check API key
    api_key = os.environ.get("AI_CLAUDE_API_KEY", "")
    if not api_key:
        # Try loading from .env
        env_file = os.path.join(os.path.dirname(__file__), ".env")
        if os.path.exists(env_file):
            with open(env_file) as f:
                for line in f:
                    if line.startswith("AI_CLAUDE_API_KEY="):
                        api_key = line.split("=", 1)[1].strip().strip('"')
                        os.environ["AI_CLAUDE_API_KEY"] = api_key
                        break

    if not api_key:
        print_error("No se encontró AI_CLAUDE_API_KEY")
        print_info("Configura la variable de entorno o créala en .env")
        print_info("Ejemplo: AI_CLAUDE_API_KEY=sk-ant-...")
        print()
        print_info("Ejecutando demo en modo simulado...")
        await run_simulated_demo()
        return

    # Import after env is set
    from src.config import Settings
    from src.copilot import MolecularBiologyAgent
    from src.copilot.tools import AGENT_TOOLS
    from src.models import AnalysisResult, AnalysisStatus, BlastHit

    settings = Settings()

    print_info(f"API Key configurada: {api_key[:10]}...")
    print_info(f"Modelo: {settings.claude_model}")
    print()

    # Show available tools
    print(f"{Colors.BOLD}Herramientas disponibles:{Colors.END}")
    for tool in AGENT_TOOLS:
        print_tool(tool["name"], tool["description"])
    print()

    # Create mock trace data
    mock_analysis = AnalysisResult(
        traceId="TR-DEMO-001",
        studyId="STUDY-2024-001",
        analysisId="AN-001",
        status=AnalysisStatus.COMPLETED,
        overallConfidence=0.92,
        blastHits=[
            BlastHit(
                accession="NC_000001.11",
                description="Homo sapiens chromosome 1, GRCh38.p14",
                score=1850.5,
                eValue=0.0,
                identity=99.2,
                queryStart=1,
                queryEnd=500,
                subjectStart=1000,
                subjectEnd=1500,
                organism="Homo sapiens",
            ),
            BlastHit(
                accession="NC_000002.12",
                description="Homo sapiens chromosome 2, GRCh38.p14",
                score=450.2,
                eValue=1e-120,
                identity=95.5,
                queryStart=1,
                queryEnd=500,
                subjectStart=2000,
                subjectEnd=2500,
                organism="Homo sapiens",
            ),
        ],
    )

    # Trace provider
    def get_trace(trace_id: str):
        if trace_id == "TR-DEMO-001":
            return mock_analysis
        return None

    # Create agent
    agent = MolecularBiologyAgent(
        settings=settings,
        trace_provider=get_trace,
    )

    if not agent.is_available:
        print_error("El agente no está disponible (API key inválida?)")
        return

    print_info("Agente inicializado correctamente")
    print()

    # Demo questions
    questions = [
        ("TR-DEMO-001", "¿Qué organismo corresponde a esta traza?"),
        ("TR-DEMO-001", "¿Cuál es la confianza del análisis?"),
        (None, "Explica qué es una variante SNP en posición 150 donde A cambia a G"),
        (None, "Compara estas secuencias: ATCGATCGATCG y ATCGTTCGATCG"),
    ]

    print(f"{Colors.BOLD}Iniciando demo interactiva...{Colors.END}")
    print(f"{Colors.YELLOW}(Las respuestas vienen de Claude en tiempo real){Colors.END}")

    for trace_id, question in questions:
        print_question(question)
        if trace_id:
            print(f"   {Colors.YELLOW}[Contexto: {trace_id}]{Colors.END}")

        try:
            result = await agent.ask(
                question=question,
                trace_id=trace_id,
            )

            if result.get("error"):
                print_error(result.get("answer", "Error desconocido"))
            else:
                print_answer(result["answer"])

        except Exception as e:
            print_error(str(e))

        print(f"\n{Colors.YELLOW}{'─'*60}{Colors.END}")

        # Small delay between questions
        await asyncio.sleep(1)

    # Show metrics
    print_header("Métricas del Agente")
    metrics = agent.metrics
    print(f"  Requests: {metrics['requests']}")
    print(f"  Tool calls: {metrics['toolCalls']}")
    print(f"  Errors: {metrics['errors']}")
    print(f"  Active contexts: {metrics['activeContexts']}")


async def run_simulated_demo():
    """Demo simulada sin API key."""
    from src.copilot.tools import AGENT_TOOLS

    print()
    print(f"{Colors.BOLD}Herramientas disponibles:{Colors.END}")
    for tool in AGENT_TOOLS:
        print_tool(tool["name"], tool["description"])
    print()

    # Simulated Q&A
    demos = [
        (
            "¿Qué organismo corresponde a la traza TR-DEMO-001?",
            """Basándome en los resultados del análisis BLAST de la traza TR-DEMO-001:

**Organismo identificado:** Homo sapiens (humano)

**Evidencia:**
- Top hit: NC_000001.11 (Cromosoma 1 humano)
- Identidad: 99.2%
- E-value: 0.0 (altamente significativo)

La alta identidad y el e-value de cero indican una coincidencia
muy confiable con el genoma humano de referencia (GRCh38.p14)."""
        ),
        (
            "Explica la variante A>G en posición 150",
            """**Análisis de variante:**

- **Posición:** 150
- **Cambio:** A → G (transición purina-purina)
- **Tipo:** SNP (Single Nucleotide Polymorphism)

**Interpretación:**
Esta es una transición, el tipo más común de sustitución de bases.
Las transiciones A↔G son menos propensas a causar cambios en
aminoácidos debido a la redundancia del código genético.

**Recomendación:**
Para determinar significancia clínica, consultar ClinVar o gnomAD."""
        ),
        (
            "Compara: ATCGATCGATCG vs ATCGTTCGATCG",
            """**Comparación de secuencias:**

| Métrica | Valor |
|---------|-------|
| Longitud seq1 | 12 bp |
| Longitud seq2 | 12 bp |
| Identidad | 91.7% |
| Diferencias | 1 |

**Diferencia detectada:**
- Posición 5: A → T

Las secuencias son altamente similares con una única sustitución."""
        ),
    ]

    print(f"{Colors.BOLD}Demo simulada (sin Claude API):{Colors.END}\n")

    for question, answer in demos:
        print_question(question)
        await asyncio.sleep(0.5)
        print_answer(answer)
        print(f"\n{Colors.YELLOW}{'─'*60}{Colors.END}")
        await asyncio.sleep(1)

    print()
    print_info("Para ejecutar con Claude real, configura AI_CLAUDE_API_KEY")


def main():
    """Entry point."""
    try:
        asyncio.run(run_demo())
    except KeyboardInterrupt:
        print(f"\n{Colors.YELLOW}Demo cancelada.{Colors.END}")
        sys.exit(0)


if __name__ == "__main__":
    main()
