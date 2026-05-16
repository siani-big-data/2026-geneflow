# Molecular Biologist Agent — Design Plan

## 1. Objetivo

Construir un agente LLM capaz de realizar todo el flujo de trabajo de un
biólogo molecular en torno a secuenciación Sanger y análisis de secuencias,
combinando:

- **Modelos ML propios** (`geneflow-ai/checkpoints/`)
- **Módulos analíticos** (`../geneflow-analysis/src/`)
- **APIs externas** (NCBI, Ensembl, ClinVar, InterPro)
- **Estrategias ML "market"** (SIFT, PolyPhen2, ViennaRNA, ESM, Tracy)

## 2. Inventario disponible

### 2.1 Modelos ML (`geneflow-ai`)
| Modelo | Función | Checkpoint |
|---|---|---|
| `TaxonomyClassifier` (CNN, multi-head) | Reino→Género desde secuencia | `taxonomy_cnn/` |
| `TaxonomyRF` (Random Forest) | Idem, baseline interpretable | `taxonomy_rf/` |
| `HeterozygoteClassifier` | SNPs / doble-pico en cromatograma | `heterozygote/` |
| `TrimmingPredictor` | Puntos de recorte 5′/3′ | `trimming/` |
| `QualityClassifierCNN` | Bins Q10–Q50+ por posición | `quality_classifier/` |

### 2.2 Estrategias ML (`src/ml/strategies/`)
- `market/`: SIFT, PolyPhen2, ViennaRNA, ESMEmbedding, Tracy, Phred
- `custom/`: CustomQuality
- `heuristic/`: Motif, MutationImpact, Artifact

### 2.3 Módulos `../geneflow-analysis/src/`
| Módulo | Clases públicas |
|---|---|
| `parsers/` | AB1Parser, FASTAParser, FASTQParser, SCFParser, synthesize_chromatogram |
| `analyzers/` | Heterozygote, Motif, ORF, Quality, Restriction, Translation, Trimming |
| `alignment/` | PairwiseAligner, MultipleAligner, ConsensusBuilder, VariantDetector |
| `phylogeny/` | PhylogenyAnalyzer, BootstrapAnalyzer, DistanceCalculator, TreeBuilder |
| `workers/` | TraceWorker, AlignmentWorker, AnalysisWorker, PhylogenyWorker |

### 2.4 Tools ya expuestas al agente (`src/copilot/tools.py`, 18 tools)
Datos: `get_trace_analysis`, `get_quality_assessment` ·
ML: `classify_taxonomy`, `detect_heterozygotes`, `predict_trim_points`, `classify_quality_ml`, `analyze_trace_ml` ·
Secuencia: `analyze_quality`, `find_orfs`, `scan_motifs`, `calculate_gc_content`, `find_restriction_sites`, `compaREDACTED` ·
Variantes: `explain_variant`, `search_blast` ·
Externas: `lookup_clinvar`, `lookup_ensembl`, `lookup_ncbi_taxonomy`.

## 3. Capacidades esperadas de un biólogo molecular vs. cobertura actual

| Capacidad | Estado | Componente a usar |
|---|---|---|
| Parsear AB1 / FASTA / FASTQ / SCF | ❌ no expuesto | `parsers/*` |
| Control de calidad por posición | ✅ ML + heurístico | `QualityClassifier` + `QualityAnalyzer` |
| Recorte por calidad | ✅ ML | `TrimmingPredictor` + `TrimmingAnalyzer` |
| Detección de heterocigotos (Sanger) | ✅ ML | `HeterozygoteClassifier` |
| Ensamblado consenso fwd/rev | ❌ falta | `ConsensusBuilder` |
| Alineamiento pareado | ❌ falta | `PairwiseAligner` |
| Alineamiento múltiple (MSA) | ❌ falta | `MultipleAligner` |
| Detección de variantes desde alineamiento | ❌ falta | `VariantDetector` |
| BLAST remoto NCBI | ✅ | `search_blast` |
| Clasificación taxonómica ML | ✅ | `TaxonomyClassifier` |
| Búsqueda en taxonomía NCBI | ✅ | `lookup_ncbi_taxonomy` |
| ORFs / traducción a proteína | parcial | `ORFAnalyzer`, `TranslationAnalyzer` |
| Búsqueda de motivos | ✅ | `MotifAnalyzer` |
| Mapa de restricción | ✅ | `RestrictionAnalyzer` |
| Contenido GC, complemento, reverso | parcial | helper en `parsers/utils` |
| Impacto funcional de variantes (SIFT, PolyPhen2) | ❌ tool faltante | `strategies/market/functional` |
| Estructura secundaria RNA | ❌ tool faltante | `ViennaRNAStrategy` |
| Embeddings proteicos (ESM) | ❌ tool faltante | `ESMEmbeddingStrategy` |
| Anotación ClinVar / Ensembl | ✅ | externas |
| Dominios proteicos (InterPro) | ❌ falta tool | nueva integración |
| Filogenia: matriz distancia | ❌ falta tool | `DistanceCalculator` |
| Filogenia: construcción de árbol | ❌ falta tool | `TreeBuilder` |
| Filogenia: bootstrap | ❌ falta tool | `BootstrapAnalyzer` |
| Diseño de primers | ❌ falta | nuevo (Primer3 / heurístico) |
| Codon usage / optimización | ❌ falta | extender `TranslationAnalyzer` |
| Búsqueda en PubMed | ❌ falta | nueva integración NCBI eutils |
| Workflows multi-paso (trace → consenso → variantes → BLAST) | ❌ falta | orquestar workers |

## 4. Arquitectura propuesta

```
src/copilot/
├── agent.py                   # MolecularBiologyAgent (existente)
├── tools.py                   # AGENT_TOOLS (registro)
├── tool_runner.py             # despacho name -> handler
├── handlers/                  # NUEVO: handlers por dominio
│   ├── parsing.py             # parse_ab1, parse_fasta, parse_fastq
│   ├── quality.py             # ML + heurístico
│   ├── alignment.py           # pairwise, multiple, consensus
│   ├── variants.py            # VariantDetector + SIFT/PolyPhen2
│   ├── phylogeny.py           # distance, tree, bootstrap
│   ├── annotation.py          # ORF, traducción, motivos, restricción
│   ├── structure.py           # ViennaRNA, ESM
│   ├── workflows.py           # pipelines compuestos
│   └── external.py            # NCBI, Ensembl, ClinVar, InterPro, PubMed
├── adapters/                  # NUEVO: import de geneflow-analysis
│   └── analysis_bridge.py     # carga lazy de parsers/analyzers/alignment/phylogeny
└── prompts.py                 # SYSTEM_PROMPT enriquecido
```

### 4.1 Bridge a `geneflow-analysis`

Añadir `geneflow-analysis` como dependencia de path en `pyproject.toml`:

```toml
[tool.uv.sources]
geneflow-analysis = { path = "../geneflow-analysis", editable = true }
```

Encapsular imports en `adapters/analysis_bridge.py` con fallback amable
para no romper si la ruta cambia.

## 5. Nuevas tools a registrar (prioridad alta)

1. **`parse_trace_file`** – AB1/SCF → señales + bases + scores
2. **`parse_fasta` / `parse_fastq`**
3. **`align_pairwise`** – Smith-Waterman / Needleman-Wunsch
4. **`align_multiple`** – MSA (3..N secuencias)
5. **`build_consensus`** – fwd/rev reads → contig + Q
6. **`detect_variants_from_alignment`** – produce VCF-like
7. **`translate_sequence`** – ORF + proteína
8. **`predict_functional_impact`** – SIFT/PolyPhen2 vía strategies
9. **`predict_rna_structure`** – ViennaRNA
10. **`build_phylogeny`** – matriz distancia + NJ/UPGMA + bootstrap
11. **`search_pubmed`** – literatura
12. **`lookup_interpro`** – dominios proteicos
13. **`run_sanger_pipeline`** *(workflow)* – parse → QC → trim → heterocigotos → consenso → BLAST → taxonomía

## 6. Mejoras al SYSTEM_PROMPT

Añadir secciones:
- Catálogo explícito de herramientas agrupadas por workflow (QC, alineamiento, anotación, variantes, filogenia, literatura)
- Política de citado (siempre indicar fuente: NCBI, ClinVar, modelo ML + versión, % confianza)
- Política de coste: preferir herramientas locales sobre externas cuando equivalentes
- Guía de selección ML vs. heurístico vs. market (Tracy/Phred)
- Plantilla de informe estructurado (JSON-friendly + resumen humano)

## 7. Plan de ejecución por fases

**Fase 1 — Cableado de análisis local (sin modelos nuevos)**
- Bridge a `geneflow-analysis`
- Tools: parsers, pairwise, multiple, consensus, translate
- Ampliar SYSTEM_PROMPT

**Fase 2 — Variantes y anotación**
- `detect_variants_from_alignment`
- Integrar `SIFTStrategy` / `PolyPhen2Strategy` como tool
- `lookup_interpro`, `search_pubmed`

**Fase 3 — Filogenia**
- Tools de distancia, árbol y bootstrap
- Renderizado Newick + visual ASCII

**Fase 4 — Workflows compuestos**
- `run_sanger_pipeline` orquestando los workers de `geneflow-analysis`
- Persistencia de informes (`reports.py`)
- Streaming de progreso al frontend

**Fase 5 — Estructura y embeddings (opcional)**
- ViennaRNA y ESM si hay demanda

## 8. Riesgos y mitigaciones

- **Acoplamiento con `geneflow-analysis`**: aislar en `adapters/` con interfaz mínima.
- **Latencia en tools externas (BLAST, eutils)**: caché LRU + timeouts.
- **Explosión de tools**: agrupar por workflow en el prompt y permitir descubrimiento por categoría.
- **Versionado de modelos**: cada tool ML debe devolver `model_version` y `confidence`.

## 9. Próximos pasos accionables

1. Confirmar que `geneflow-analysis` puede importarse como librería (o exponer su API por HTTP).
2. Crear `src/copilot/adapters/analysis_bridge.py` con imports lazy.
3. Implementar Fase 1 (parsers + alignment + consensus + translation).
4. Refactorizar `tools.py` → registro por categoría.
5. Actualizar `SYSTEM_PROMPT` con catálogo y workflows.
