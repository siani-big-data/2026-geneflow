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
# Bioinformatics Tools (Phase 1 — parsing, alignment, translation)
# =============================================================================

BIOINFORMATICS_TOOLS = [
    {
        "name": "parse_trace_file",
        "description": (
            "Parsea un fichero de traza Sanger (AB1/SCF) y devuelve secuencia, "
            "scores Phred y métricas de calidad. Opcionalmente incluye las señales "
            "del cromatograma (DATA9-DATA12 = G,A,T,C)."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "content_b64": {
                    "type": "string",
                    "description": "Contenido del fichero AB1 en base64",
                },
                "trace_id": {
                    "type": "string",
                    "description": "Identificador opcional para la traza",
                },
                "include_chromatogram": {
                    "type": "boolean",
                    "description": "Si true, retorna también las señales (default: false)",
                },
            },
            "required": ["content_b64"],
        },
    },
    {
        "name": "parse_fasta",
        "description": (
            "Parsea texto FASTA. Por defecto devuelve la primera secuencia; "
            "con all=true devuelve todas las secuencias."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "content": {"type": "string", "description": "Texto FASTA"},
                "trace_id": {"type": "string"},
                "all": {"type": "boolean", "description": "Devolver todas las secuencias"},
            },
            "required": ["content"],
        },
    },
    {
        "name": "parse_fastq",
        "description": (
            "Parsea texto FASTQ y devuelve secuencia(s) con scores de calidad."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "content": {"type": "string", "description": "Texto FASTQ"},
                "trace_id": {"type": "string"},
                "all": {"type": "boolean"},
            },
            "required": ["content"],
        },
    },
    {
        "name": "align_pairwise",
        "description": (
            "Alineamiento pareado global (Needleman-Wunsch) o local (Smith-Waterman) "
            "entre dos secuencias usando BioPython. Retorna alineamiento, score, "
            "%identidad y gaps."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "seq1": {"type": "string", "description": "Primera secuencia"},
                "seq2": {"type": "string", "description": "Segunda secuencia"},
                "mode": {
                    "type": "string",
                    "enum": ["global", "local"],
                    "description": "Modo de alineamiento (default: global)",
                },
                "match": {"type": "number", "description": "Score match (default: 2)"},
                "mismatch": {"type": "number", "description": "Score mismatch (default: -1)"},
                "gap_open": {"type": "number", "description": "Penalización gap open (default: -10)"},
                "gap_extend": {"type": "number", "description": "Penalización gap extend (default: -0.5)"},
            },
            "required": ["seq1", "seq2"],
        },
    },
    {
        "name": "align_multiple",
        "description": (
            "Alineamiento múltiple progresivo de 3 o más secuencias. "
            "Retorna alineamiento, identidad media y gaps totales."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "sequences": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Lista de secuencias a alinear",
                },
            },
            "required": ["sequences"],
        },
    },
    {
        "name": "build_consensus",
        "description": (
            "Construye una secuencia consenso a partir de un alineamiento. "
            "Métodos: majority, threshold, iupac (códigos de ambigüedad)."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "aligned_sequences": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Secuencias alineadas (misma longitud)",
                },
                "method": {
                    "type": "string",
                    "enum": ["majority", "threshold", "iupac"],
                    "description": "Método de consenso (default: majority)",
                },
                "threshold": {
                    "type": "number",
                    "description": "Frecuencia mínima para método threshold (0-1)",
                },
                "min_coverage": {
                    "type": "integer",
                    "description": "Cobertura mínima por posición (default: 1)",
                },
            },
            "required": ["aligned_sequences"],
        },
    },
    {
        "name": "translate_sequence",
        "description": (
            "Traduce ADN a proteína en un marco concreto (1/2/3/-1/-2/-3) o "
            "en los seis marcos si all_frames=true. Reporta ATG, codones stop y "
            "composición aminoacídica."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "sequence": {"type": "string", "description": "Secuencia de ADN"},
                "frame": {"type": "integer", "description": "Marco (default: 1)"},
                "all_frames": {"type": "boolean", "description": "Traducir los 6 marcos"},
            },
            "required": ["sequence"],
        },
    },
    {
        "name": "reverse_complement",
        "description": "Devuelve la complementaria reversa de una secuencia de ADN.",
        "input_schema": {
            "type": "object",
            "properties": {
                "sequence": {"type": "string", "description": "Secuencia de ADN"},
            },
            "required": ["sequence"],
        },
    },
]


# =============================================================================
# Variant + Functional Tools (Phase 2)
# =============================================================================

VARIANT_TOOLS = [
    {
        "name": "detect_variants_from_alignment",
        "description": (
            "Detecta SNPs, inserciones y deleciones a partir de un alineamiento "
            "múltiple ya construido. Devuelve un reporte tipo VCF con posiciones "
            "1-based, tipo de variante, frecuencia y cobertura por columna."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "aligned_sequences": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Secuencias alineadas (misma longitud, con '-' como gap)",
                },
                "reference_index": {
                    "type": "integer",
                    "description": "Índice de la secuencia de referencia (default: 0)",
                },
                "min_frequency": {
                    "type": "number",
                    "description": "Frecuencia mínima (0-1) para reportar una variante (default: 0)",
                },
                "min_coverage": {
                    "type": "integer",
                    "description": "Cobertura mínima por posición (default: 1)",
                },
            },
            "required": ["aligned_sequences"],
        },
    },
    {
        "name": "predict_functional_impact",
        "description": (
            "Predice el impacto funcional de una variante codificante usando la API "
            "REST de Ensembl VEP. Devuelve scores SIFT y PolyPhen-2, consecuencias "
            "biológicas (missense, stop_gained, etc.), cambio aminoacídico y "
            "símbolo del gen. Acepta HGVS o coordenada genómica."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "hgvs": {
                    "type": "string",
                    "description": "Notación HGVS, p.ej. 'ENST00000366667:c.803C>T'",
                },
                "region": {
                    "type": "string",
                    "description": "Coordenada genómica, p.ej. '9:22125504-22125504:1'",
                },
                "allele": {
                    "type": "string",
                    "description": "Alelo alternativo cuando se usa 'region'",
                },
                "species": {
                    "type": "string",
                    "description": "Especie (default: human)",
                },
            },
        },
    },
]


# =============================================================================
# Phylogeny Tools (Phase 3)
# =============================================================================

PHYLOGENY_TOOLS = [
    {
        "name": "compute_distance_matrix",
        "description": (
            "Calcula la matriz de distancias evolutivas pareadas entre secuencias "
            "alineadas. Métodos: p-distance (proporción de diferencias), "
            "jukes_cantor (corrección por sustituciones múltiples) y kimura_2p "
            "(distingue transiciones/transversiones)."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "aligned_sequences": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Secuencias alineadas de igual longitud",
                },
                "labels": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Etiquetas de las secuencias (opcional)",
                },
                "method": {
                    "type": "string",
                    "enum": ["p_distance", "jukes_cantor", "kimura_2p"],
                    "description": "Método de distancia (default: jukes_cantor)",
                },
            },
            "required": ["aligned_sequences"],
        },
    },
    {
        "name": "build_phylogenetic_tree",
        "description": (
            "Construye un árbol filogenético a partir de secuencias alineadas. "
            "Algoritmos: NJ (Neighbor-Joining, sin asumir reloj molecular) y "
            "UPGMA (ultramétrico). Devuelve el árbol en formato Newick y como "
            "estructura jerárquica."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "aligned_sequences": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Secuencias alineadas",
                },
                "labels": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Etiquetas de las secuencias (opcional)",
                },
                "distance_method": {
                    "type": "string",
                    "enum": ["p_distance", "jukes_cantor", "kimura_2p"],
                    "description": "Método de distancia (default: jukes_cantor)",
                },
                "tree_method": {
                    "type": "string",
                    "enum": ["nj", "upgma"],
                    "description": "Algoritmo del árbol (default: nj)",
                },
            },
            "required": ["aligned_sequences"],
        },
    },
    {
        "name": "bootstrap_tree",
        "description": (
            "Calcula soporte por bootstrap para un árbol filogenético, "
            "remuestreando columnas del alineamiento. Devuelve el árbol "
            "original y un mapa de soporte (0-100%) para cada clado interno."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "aligned_sequences": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Secuencias alineadas (mínimo 3)",
                },
                "labels": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Etiquetas de las secuencias (opcional)",
                },
                "replicates": {
                    "type": "integer",
                    "description": "Réplicas bootstrap (10-500, default: 100)",
                },
                "distance_method": {
                    "type": "string",
                    "enum": ["p_distance", "jukes_cantor", "kimura_2p"],
                },
                "tree_method": {
                    "type": "string",
                    "enum": ["nj", "upgma"],
                },
                "seed": {
                    "type": "integer",
                    "description": "Semilla aleatoria para reproducibilidad",
                },
            },
            "required": ["aligned_sequences"],
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


# =============================================================================
# External Database Tools — Phase 2 (live API integrations)
# =============================================================================

EXTERNAL_PHASE2_TOOLS = [
    {
        "name": "lookup_interpro",
        "description": (
            "Consulta la API de EBI InterPro para obtener los dominios, familias y "
            "sitios funcionales de una proteína a partir de su accession UniProt "
            "(p.ej. 'P38398' para BRCA1). Devuelve los hits con nombre, tipo, "
            "GO terms y localización en la proteína."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "accession": {
                    "type": "string",
                    "description": "Accession UniProt (p.ej. P38398)",
                },
            },
            "required": ["accession"],
        },
    },
    {
        "name": "search_pubmed",
        "description": (
            "Busca artículos en PubMed mediante las E-utilities de NCBI. "
            "Devuelve PMID, título, autores, revista, año y DOI de los mejores hits. "
            "Útil para sustanciar interpretaciones de variantes o anotaciones."
        ),
        "input_schema": {
            "type": "object",
            "properties": {
                "query": {
                    "type": "string",
                    "description": "Consulta libre de PubMed (p.ej. 'BRCA1 variant pathogenicity')",
                },
                "max_results": {
                    "type": "integer",
                    "description": "Número máximo de resultados (1-50, default: 10)",
                },
                "email": {
                    "type": "string",
                    "description": "Email de contacto (recomendado por NCBI)",
                },
            },
            "required": ["query"],
        },
    },
]


def get_tools_for_api() -> list[dict[str, Any]]:
    """Get all tools formatted for Claude API."""
    return (
        AGENT_TOOLS
        + ML_TOOLS
        + BIOINFORMATICS_TOOLS
        + VARIANT_TOOLS
        + PHYLOGENY_TOOLS
        + EXTERNAL_PHASE2_TOOLS
    )


def get_all_tools() -> list[dict[str, Any]]:
    """Get all available tools including analysis and external."""
    return (
        AGENT_TOOLS
        + ML_TOOLS
        + ANALYSIS_TOOLS
        + BIOINFORMATICS_TOOLS
        + VARIANT_TOOLS
        + PHYLOGENY_TOOLS
        + EXTERNAL_TOOLS
        + EXTERNAL_PHASE2_TOOLS
    )


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


# =============================================================================
# Schema conversion (Anthropic <-> OpenAI / Ollama)
# =============================================================================
#
# Our canonical tool definitions follow the Anthropic shape:
#     { "name", "description", "input_schema": {...} }
#
# OpenAI-compatible APIs (DeepSeek, Ollama, OpenAI, vLLM with function calling)
# require:
#     { "type": "function",
#       "function": { "name", "description", "parameters": {...} } }
#
# The two are otherwise identical (JSON Schema for parameters).


def _to_openai_tool(tool: dict[str, Any]) -> dict[str, Any]:
    """Convert one Anthropic-shaped tool to OpenAI function-tool format."""
    return {
        "type": "function",
        "function": {
            "name": tool["name"],
            "description": tool.get("description", ""),
            "parameters": tool.get("input_schema") or {"type": "object", "properties": {}},
        },
    }


def get_tools_for_openai() -> list[dict[str, Any]]:
    """Get all API-exposed tools formatted for OpenAI-compatible providers
    (DeepSeek, Ollama, OpenAI, vLLM).
    """
    return [_to_openai_tool(t) for t in get_tools_for_api()]


def get_tools_for_provider(provider: str) -> list[dict[str, Any]]:
    """Return the tool catalog formatted for the given LLM provider.

    Args:
        provider: "claude" | "deepseek" | "ollama" | "openai"
    """
    p = provider.lower()
    if p == "claude":
        return get_tools_for_api()
    if p in ("deepseek", "ollama", "openai"):
        return get_tools_for_openai()
    raise ValueError(f"Unknown LLM provider: {provider}")
