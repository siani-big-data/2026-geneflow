"""Tool definitions for the Molecular Biology Agent."""

from typing import Any

# Tool definitions for Claude API
AGENT_TOOLS = [
    {
        "name": "get_trace_analysis",
        "description": (
            "Obtiene los datos de análisis de una traza de secuenciación Sanger. "
            "Incluye: secuencia, métricas de calidad, resultados BLAST, "
            "variantes detectadas y anotaciones. Usar cuando el usuario "
            "pregunte sobre una traza específica."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "traceId": {
                    "type": "string",
                    "description": "ID de la traza (ej: TR-abc123)",
                }
            },
            "required": ["traceId"],
        },
    },
    {
        "name": "search_blast",
        "description": (
            "Ejecuta una búsqueda BLAST en NCBI para identificar una secuencia. "
            "Retorna los mejores hits con organismo, identidad y e-value. "
            "Usar cuando se necesite identificar un organismo o gen."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "sequence": {
                    "type": "string",
                    "description": "Secuencia de ADN a buscar (ATCG...)",
                },
                "program": {
                    "type": "string",
                    "enum": ["blastn", "blastp", "blastx"],
                    "description": "Programa BLAST a usar (default: blastn)",
                },
                "database": {
                    "type": "string",
                    "enum": ["nt", "nr", "refseq_rna"],
                    "description": "Base de datos a buscar (default: nt)",
                },
            },
            "required": ["sequence"],
        },
    },
    {
        "name": "get_quality_assessment",
        "description": (
            "Obtiene evaluación detallada de calidad de una secuencia. "
            "Incluye: regiones de baja calidad, puntos de recorte sugeridos, "
            "probabilidad de error por posición. Usar para evaluar "
            "confiabilidad de los datos."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "traceId": {
                    "type": "string",
                    "description": "ID de la traza",
                }
            },
            "required": ["traceId"],
        },
    },
    {
        "name": "explain_variant",
        "description": (
            "Proporciona información sobre una variante genética. "
            "Incluye: tipo de variante, posible impacto funcional, "
            "frecuencia poblacional si está disponible. "
            "Usar cuando el usuario pregunte sobre variantes específicas."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "position": {
                    "type": "integer",
                    "description": "Posición de la variante en la secuencia",
                },
                "reference": {
                    "type": "string",
                    "description": "Base de referencia",
                },
                "alternate": {
                    "type": "string",
                    "description": "Base alternativa",
                },
                "gene": {
                    "type": "string",
                    "description": "Gen afectado (opcional)",
                },
            },
            "required": ["position", "reference", "alternate"],
        },
    },
    {
        "name": "compaREDACTED",
        "description": (
            "Compara dos secuencias y retorna diferencias. "
            "Útil para comparar una secuencia con una referencia "
            "o entre dos muestras."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "sequence1": {
                    "type": "string",
                    "description": "Primera secuencia",
                },
                "sequence2": {
                    "type": "string",
                    "description": "Segunda secuencia",
                },
            },
            "required": ["sequence1", "sequence2"],
        },
    },
]


def get_tools_for_api() -> list[dict[str, Any]]:
    """Get tools formatted for Claude API."""
    return AGENT_TOOLS


def get_tool_names() -> list[str]:
    """Get list of available tool names."""
    return [tool["name"] for tool in AGENT_TOOLS]
