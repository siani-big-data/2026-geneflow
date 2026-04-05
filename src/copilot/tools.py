"""Tool definitions for the Molecular Biology Agent.

Defines all tools available to the Claude-powered copilot agent, including:
- Trace analysis tools (get_trace_analysis, get_quality_assessment)
- BLAST search tools
- ML-powered tools (taxonomy, heterozygote, trimming, quality)
- Variant analysis tools
"""

from typing import Any

# =============================================================================
# Core Agent Tools
# =============================================================================

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

# =============================================================================
# ML-Powered Tools (Neural Network Models)
# =============================================================================

ML_TOOLS = [
    {
        "name": "classify_taxonomy",
        "description": (
            "Clasifica una secuencia de ADN taxonómicamente usando un modelo "
            "de red neuronal jerárquica. Predice: reino, filo, clase, orden, "
            "familia y género. Alternativa rápida a BLAST para identificación "
            "de organismos. Usar cuando se tenga una secuencia y se quiera "
            "saber de qué organismo proviene."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "sequence": {
                    "type": "string",
                    "description": "Secuencia de ADN (mínimo 100 bp)",
                },
            },
            "required": ["sequence"],
        },
    },
    {
        "name": "detect_heterozygotes",
        "description": (
            "Detecta posiciones heterocigotas en un cromatograma usando "
            "un clasificador CNN. Identifica posiciones donde hay dos alelos "
            "presentes (doble pico). Útil para detectar SNPs y variantes. "
            "Requiere las señales normalizadas del cromatograma."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "traceId": {
                    "type": "string",
                    "description": "ID de la traza con señales de cromatograma",
                },
                "threshold": {
                    "type": "number",
                    "description": "Umbral de probabilidad (default: 0.5)",
                },
            },
            "required": ["traceId"],
        },
    },
    {
        "name": "predict_trim_points",
        "description": (
            "Predice los puntos óptimos de recorte para una secuencia "
            "basándose en los scores de calidad. Usa un modelo CNN entrenado "
            "para identificar regiones de baja calidad en los extremos. "
            "Retorna posiciones de inicio y fin recomendadas."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "traceId": {
                    "type": "string",
                    "description": "ID de la traza con scores de calidad",
                },
            },
            "required": ["traceId"],
        },
    },
    {
        "name": "classify_quality_ml",
        "description": (
            "Clasifica la calidad de cada posición en bins (Q10, Q20, Q30, "
            "Q40, Q50+) usando un modelo CNN con contexto espacial. "
            "Considera el patrón de señales vecinas para una clasificación "
            "más precisa que usar solo el score Phred."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "traceId": {
                    "type": "string",
                    "description": "ID de la traza",
                },
            },
            "required": ["traceId"],
        },
    },
    {
        "name": "analyze_trace_ml",
        "description": (
            "Análisis completo de una traza usando todos los modelos de ML. "
            "Ejecuta: clasificación taxonómica, detección de heterocigotos, "
            "predicción de recorte y clasificación de calidad. "
            "Ideal para obtener un reporte completo de una muestra."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "traceId": {
                    "type": "string",
                    "description": "ID de la traza a analizar",
                },
            },
            "required": ["traceId"],
        },
    },
]

# =============================================================================
# Analysis Tools (Heuristic/Algorithmic)
# =============================================================================

ANALYSIS_TOOLS = [
    {
        "name": "analyze_quality",
        "description": (
            "Analiza métricas de calidad (Q20, Q30, accuracy) a partir de "
            "scores Phred. Método heurístico rápido."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "quality_scores": {
                    "type": "array",
                    "items": {"type": "integer"},
                    "description": "Lista de scores de calidad Phred",
                }
            },
            "required": ["quality_scores"],
        },
    },
    {
        "name": "find_orfs",
        "description": (
            "Encuentra marcos de lectura abiertos (ORFs) en una secuencia. "
            "Identifica posibles regiones codificantes."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "sequence": {
                    "type": "string",
                    "description": "Secuencia de ADN",
                },
                "min_length": {
                    "type": "integer",
                    "description": "Longitud mínima del ORF en bp (default: 100)",
                },
            },
            "required": ["sequence"],
        },
    },
    {
        "name": "scan_motifs",
        "description": (
            "Busca motivos regulatorios en una secuencia (TATA box, Kozak, "
            "sitios de splicing, etc.)."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "sequence": {
                    "type": "string",
                    "description": "Secuencia de ADN",
                },
            },
            "required": ["sequence"],
        },
    },
    {
        "name": "calculate_gc_content",
        "description": (
            "Calcula el contenido GC de una secuencia y estadísticas "
            "relacionadas (GC skew, AT skew, etc.)."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "sequence": {
                    "type": "string",
                    "description": "Secuencia de ADN",
                },
            },
            "required": ["sequence"],
        },
    },
    {
        "name": "find_restriction_sites",
        "description": (
            "Encuentra sitios de restricción en una secuencia para las "
            "enzimas más comunes (EcoRI, BamHI, HindIII, etc.)."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "sequence": {
                    "type": "string",
                    "description": "Secuencia de ADN",
                },
                "enzymes": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Lista de enzimas (opcional, default: comunes)",
                },
            },
            "required": ["sequence"],
        },
    },
]

# =============================================================================
# External Database Tools
# =============================================================================

EXTERNAL_TOOLS = [
    {
        "name": "lookup_clinvar",
        "description": (
            "Consulta ClinVar para obtener significancia clínica de una "
            "variante. Retorna: clasificación, condiciones asociadas, "
            "nivel de evidencia."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "variant": {
                    "type": "string",
                    "description": "Variante en formato HGVS o rsID (ej: rs123456)",
                },
                "gene": {
                    "type": "string",
                    "description": "Gen asociado (opcional)",
                },
            },
            "required": ["variant"],
        },
    },
    {
        "name": "lookup_ensembl",
        "description": (
            "Consulta Ensembl para información de genes. Retorna: "
            "ubicación genómica, transcritos, dominios proteicos."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "gene_symbol": {
                    "type": "string",
                    "description": "Símbolo del gen (ej: BRCA1)",
                },
                "species": {
                    "type": "string",
                    "description": "Especie (default: human)",
                },
            },
            "required": ["gene_symbol"],
        },
    },
    {
        "name": "lookup_ncbi_taxonomy",
        "description": (
            "Consulta la taxonomía NCBI para un organismo. Retorna: "
            "clasificación completa, nombres comunes, linaje."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "query": {
                    "type": "string",
                    "description": "Nombre del organismo o taxon ID",
                },
            },
            "required": ["query"],
        },
    },
]


def get_tools_for_api() -> list[dict[str, Any]]:
    """Get all tools formatted for Claude API."""
    return AGENT_TOOLS + ML_TOOLS


def get_all_tools() -> list[dict[str, Any]]:
    """Get all available tools including analysis and external."""
    return AGENT_TOOLS + ML_TOOLS + ANALYSIS_TOOLS + EXTERNAL_TOOLS


def get_tool_names() -> list[str]:
    """Get list of available tool names."""
    return [tool["name"] for tool in get_all_tools()]


def get_ml_tool_names() -> list[str]:
    """Get list of ML-powered tool names."""
    return [tool["name"] for tool in ML_TOOLS]


def get_tool_by_name(name: str) -> dict[str, Any] | None:
    """Get a tool definition by name."""
    for tool in get_all_tools():
        if tool["name"] == name:
            return tool
    return None
