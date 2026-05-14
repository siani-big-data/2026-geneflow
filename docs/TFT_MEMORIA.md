# GeneFlow Analysis: Motor de Análisis Bioinformático

## Documento Técnico para Memoria de TFT

---

## Índice

1. [Introducción](#1-introducción)
2. [Arquitectura del Sistema](#2-arquitectura-del-sistema)
3. [Stack Tecnológico](#3-stack-tecnológico)
4. [Módulos del Sistema](#4-módulos-del-sistema)
5. [Flujo de Datos](#5-flujo-de-datos)
6. [Patrones de Diseño](#6-patrones-de-diseño)
7. [Modelo de Datos](#7-modelo-de-datos)
8. [Estrategia de Testing](#8-estrategia-de-testing)
9. [Despliegue y Operaciones](#9-despliegue-y-operaciones)
10. [Decisiones Técnicas](#10-decisiones-técnicas)
11. [Conclusiones](#11-conclusiones)

---

## 1. Introducción

### 1.1 Contexto

GeneFlow Analysis es el motor de análisis bioinformático de la plataforma GeneFlow, diseñado para procesar trazas de secuenciación de ADN en tiempo real. El sistema consume trabajos desde Redis Streams, ejecuta análisis complejos y publica resultados como eventos, siguiendo una arquitectura orientada a eventos (EDA).

### 1.2 Problemática

El análisis de secuencias de ADN presenta varios desafíos técnicos:

```
┌─────────────────────────────────────────────────────────────────┐
│                    DESAFÍOS DEL DOMINIO                        │
├─────────────────────────────────────────────────────────────────┤
│  1. Formatos Heterogéneos                                       │
│     └─ AB1 (Applied Biosystems), SCF, FASTQ, FASTA             │
│                                                                 │
│  2. Volumen de Datos                                            │
│     └─ Miles de trazas por estudio, cromatogramas de ~10KB     │
│                                                                 │
│  3. Análisis Complejos                                          │
│     └─ Alineamiento, detección de variantes, calidad           │
│                                                                 │
│  4. Escalabilidad                                               │
│     └─ Múltiples estudios concurrentes, picos de carga         │
│                                                                 │
│  5. Tiempo Real                                                 │
│     └─ Resultados inmediatos para decisiones de laboratorio    │
└─────────────────────────────────────────────────────────────────┘
```

### 1.3 Objetivos

| Objetivo | Descripción | Métrica |
|----------|-------------|---------|
| **Procesamiento** | Soportar todos los formatos de secuenciación estándar | 4 formatos |
| **Análisis** | Implementar métricas de calidad y análisis avanzados | 7 analizadores |
| **Escalabilidad** | Permitir escalado horizontal mediante workers | N workers |
| **Fiabilidad** | Garantizar procesamiento at-least-once | 0 pérdidas |
| **Mantenibilidad** | Código testeable y documentado | >90% coverage |

---

## 2. Arquitectura del Sistema

### 2.1 Visión General

GeneFlow Analysis sigue una arquitectura de **microservicio worker** que consume mensajes de colas y produce eventos:

```
                           ┌─────────────────────────────────────────────┐
                           │            GENEFLOW PLATFORM                │
                           └─────────────────────────────────────────────┘
                                              │
                    ┌─────────────────────────┼─────────────────────────┐
                    │                         │                         │
                    ▼                         ▼                         ▼
           ┌─────────────┐           ┌─────────────┐           ┌─────────────┐
           │  Frontend   │           │   Datalake  │           │  Analysis   │
           │   (React)   │           │    (API)    │           │  (Worker)   │◄── Este módulo
           └─────────────┘           └─────────────┘           └─────────────┘
                    │                         │                         │
                    └─────────────────────────┼─────────────────────────┘
                                              │
                                              ▼
                                     ┌─────────────┐
                                     │    Redis    │
                                     │   Streams   │
                                     └─────────────┘
```

### 2.2 Arquitectura Interna

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                          GENEFLOW ANALYSIS                                    │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                        CAPA DE ORQUESTACIÓN                            │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                     │  │
│  │  │   main.py   │  │  bootstrap  │  │  lifecycle  │                     │  │
│  │  │ Entry Point │─►│  .py        │─►│  .py        │                     │  │
│  │  │  (~50 LOC)  │  │ Components  │  │ Start/Stop  │                     │  │
│  │  └─────────────┘  └─────────────┘  └─────────────┘                     │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                           CAPA DE ENTRADA                              │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌──────────────┐   │  │
│  │  │ TraceWorker │  │  Alignment  │  │  Analysis   │  │  Health API  │   │  │
│  │  │             │  │   Worker    │  │   Worker    │  │   (FastAPI)  │   │  │
│  │  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘  └──────────────┘   │  │
│  └─────────┼────────────────┼────────────────┼────────────────────────────┘  │
│            │                │                │                               │
│            ▼                ▼                ▼                               │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                         CAPA DE NEGOCIO                                │  │
│  │                                                                        │  │
│  │   ┌──────────────┐    ┌──────────────┐    ┌──────────────┐            │  │
│  │   │   PARSERS    │    │  ANALYZERS   │    │  ALIGNMENT   │            │  │
│  │   ├──────────────┤    ├──────────────┤    ├──────────────┤            │  │
│  │   │ • AB1Parser  │    │ • Quality    │    │ • Pairwise   │            │  │
│  │   │ • SCFParser  │    │ • Trimming   │    │ • Multiple   │            │  │
│  │   │ • FASTQParser│    │ • Heterozygot│    │ • Consensus  │            │  │
│  │   │ • FASTAParser│    │ • Motif      │    │ • Variants   │            │  │
│  │   │              │    │ • Translation│    │              │            │  │
│  │   │              │    │ • ORF        │    │              │            │  │
│  │   │              │    │ • Restriction│    │              │            │  │
│  │   └──────────────┘    └──────────────┘    └──────────────┘            │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                      CAPA DE INFRAESTRUCTURA                           │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌──────────────┐   │  │
│  │  │   Storage   │  │   Events    │  │   Config    │  │    Models    │   │  │
│  │  │  Providers  │  │  Publisher  │  │  (Pydantic) │  │ (Dataclass)  │   │  │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └──────────────┘   │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

### 2.3 Comunicación con Redis Streams

El sistema utiliza Redis Streams como broker de mensajes, aprovechando las características de Consumer Groups para escalado horizontal:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         REDIS STREAMS                                    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  JOB STREAMS (entrada)                                                  │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │ geneflow:jobs:traces      ──► TraceWorker                      │    │
│  │ geneflow:jobs:alignments  ──► AlignmentWorker                  │    │
│  │ geneflow:jobs:analysis    ──► AnalysisWorker                   │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  EVENT STREAMS (salida)                                                 │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │ geneflow:events:traces     ◄── TraceProcessed/Failed           │    │
│  │ geneflow:events:alignments ◄── AlignmentCompleted/Failed       │    │
│  │ geneflow:events:analysis   ◄── AnalysisCompleted/Failed        │    │
│  │ geneflow:events:system     ◄── WorkerStarted/Stopped           │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  CONSUMER GROUPS                                                        │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │                                                                 │    │
│  │   Consumer Group: "geneflow-workers"                           │    │
│  │   ┌──────────┐  ┌──────────┐  ┌──────────┐                     │    │
│  │   │worker-1  │  │worker-2  │  │worker-N  │   ◄── Escalado      │    │
│  │   └──────────┘  └──────────┘  └──────────┘       horizontal    │    │
│  │                                                                 │    │
│  │   • Cada mensaje se entrega a UN solo consumer                 │    │
│  │   • XACK confirma procesamiento exitoso                        │    │
│  │   • Pending entries se re-procesan si el worker falla          │    │
│  │                                                                 │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Stack Tecnológico

### 3.1 Lenguaje y Runtime

| Tecnología | Versión | Justificación |
|------------|---------|---------------|
| **Python** | 3.12+ | Ecosistema bioinformático maduro (BioPython), tipado estático, async nativo |
| **uv** | Latest | Gestor de paquetes ultrarrápido, reemplazo moderno de pip/poetry |

### 3.2 Dependencias Principales

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        DEPENDENCIAS                                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  CORE                                                                   │
│  ├── biopython>=1.84        Parsing AB1/SCF, alineamiento              │
│  ├── pydantic>=2.10         Validación de datos y configuración        │
│  ├── pydantic-settings      Gestión de variables de entorno            │
│  └── structlog>=24.4        Logging estructurado JSON                  │
│                                                                         │
│  ASYNC/NETWORKING                                                       │
│  ├── redis>=5.2             Cliente Redis async con Streams            │
│  ├── httpx>=0.28            Cliente HTTP async                         │
│  ├── aiofiles>=24.1         I/O de archivos async                      │
│  ├── fastapi>=0.115         API HTTP (health checks)                   │
│  └── uvicorn>=0.32          Servidor ASGI                              │
│                                                                         │
│  STORAGE                                                                │
│  └── minio>=7.2             Cliente S3-compatible (MinIO)              │
│                                                                         │
│  DEV                                                                    │
│  ├── pytest>=8.3            Testing framework                          │
│  ├── pytest-asyncio         Soporte para tests async                   │
│  ├── pytest-cov             Coverage reporting (line + branch)         │
│  ├── ruff>=0.8              Linter y formatter                         │
│  └── mypy>=1.13             Static type checker                        │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 3.3 Justificación de BioPython

BioPython fue seleccionado por su madurez en el ecosistema bioinformático:

```python
# Parsing de archivos AB1 con BioPython
from Bio import SeqIO

record = SeqIO.read(file_handle, "abi")

# Acceso directo a:
# - Secuencia consenso
# - Quality scores (Phred)
# - Datos de cromatograma (traces ACGT)
# - Metadatos del secuenciador
```

**Alternativas consideradas:**
- **ab1-tools**: Solo AB1, sin mantenimiento activo
- **pyABIF**: Menos features, comunidad pequeña
- **Implementación propia**: Costoso en tiempo, propenso a errores

---

## 4. Módulos del Sistema

### 4.1 Parsers

Los parsers transforman archivos de secuenciación en estructuras de datos normalizadas:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           PARSERS                                        │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│                        ┌──────────────┐                                 │
│                        │  BaseParser  │ (ABC)                           │
│                        │──────────────│                                 │
│                        │ + parse()    │                                 │
│                        │ + validate() │                                 │
│                        └──────┬───────┘                                 │
│               ┌───────────────┼───────────────┐                         │
│               │               │               │                         │
│       ┌───────┴───────┐ ┌─────┴─────┐ ┌───────┴───────┐                │
│       │   AB1Parser   │ │ SCFParser │ │  FASTQParser  │                │
│       │───────────────│ │───────────│ │───────────────│                │
│       │ Chromatogram  │ │Chromatogram│ │ Quality only │                │
│       │ Quality       │ │ Quality   │ │               │                │
│       │ Metadata      │ │           │ │               │                │
│       └───────────────┘ └───────────┘ └───────────────┘                │
│               │                               │                         │
│       ┌───────┴───────┐               ┌───────┴───────┐                │
│       │  FASTAParser  │               │ ParserFactory │                │
│       │───────────────│               │───────────────│                │
│       │ Sequence only │               │ get_parser()  │                │
│       │               │               │ by extension  │                │
│       └───────────────┘               └───────────────┘                │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

**Formato de salida unificado:**

```python
@dataclass
class ParsedTrace:
    traceId: str
    format: TraceFormat           # AB1, SCF, FASTQ, FASTA
    sequence: Sequence            # Secuencia + quality scores
    chromatogram: ChromatogramData | None  # Solo AB1/SCF
    qualityMetrics: QualityMetrics | None
    metadata: dict
```

### 4.2 Analyzers

Los analyzers implementan algoritmos de análisis de secuencias:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          ANALYZERS                                       │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────────┐                                                    │
│  │  BaseAnalyzer   │ (ABC)                                              │
│  │─────────────────│                                                    │
│  │ + analyze()     │                                                    │
│  │ + validate()    │                                                    │
│  └────────┬────────┘                                                    │
│           │                                                             │
│  ┌────────┴────────┬────────────────┬────────────────┐                 │
│  │                 │                │                │                 │
│  ▼                 ▼                ▼                ▼                 │
│ ┌───────────┐ ┌───────────┐ ┌────────────┐ ┌─────────────┐            │
│ │  Quality  │ │ Trimming  │ │Heterozygote│ │    Motif    │            │
│ │ Analyzer  │ │ Analyzer  │ │  Analyzer  │ │   Analyzer  │            │
│ ├───────────┤ ├───────────┤ ├────────────┤ ├─────────────┤            │
│ │• Q20/Q30  │ │• Mod.Mott │ │• Peak ratio│ │• Exact      │            │
│ │• GC%      │ │• Sliding  │ │• IUPAC     │ │• IUPAC      │            │
│ │• SNR      │ │• Threshold│ │• Confidence│ │• Regex      │            │
│ └───────────┘ └───────────┘ └────────────┘ └─────────────┘            │
│                                                                         │
│ ┌───────────┐ ┌───────────┐ ┌────────────┐                             │
│ │Translation│ │    ORF    │ │Restriction │                             │
│ │ Analyzer  │ │  Analyzer │ │  Analyzer  │                             │
│ ├───────────┤ ├───────────┤ ├────────────┤                             │
│ │• 6 frames │ │• Start/   │ │• 20+ enzym │                             │
│ │• Codon    │ │  Stop     │ │• Cut sites │                             │
│ │  tables   │ │• Min len  │ │• Fragments │                             │
│ └───────────┘ └───────────┘ └────────────┘                             │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

#### 4.2.1 QualityAnalyzer

Calcula métricas de calidad de secuenciación:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     MÉTRICAS DE CALIDAD                                  │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Q-SCORE (Phred)                                                        │
│  ───────────────                                                        │
│  Q = -10 × log₁₀(P)    donde P = probabilidad de error                 │
│                                                                         │
│  ┌─────────┬─────────────────┬──────────────┐                          │
│  │ Q-Score │ Prob. Error     │ Precisión    │                          │
│  ├─────────┼─────────────────┼──────────────┤                          │
│  │   Q10   │ 1 en 10         │    90%       │                          │
│  │   Q20   │ 1 en 100        │    99%       │                          │
│  │   Q30   │ 1 en 1,000      │    99.9%     │                          │
│  │   Q40   │ 1 en 10,000     │    99.99%    │                          │
│  └─────────┴─────────────────┴──────────────┘                          │
│                                                                         │
│  SNR (Signal-to-Noise Ratio)                                            │
│  ───────────────────────────                                            │
│  SNR = mean(peak_heights) / std(background_noise)                       │
│                                                                         │
│  Interpretación:                                                        │
│  • SNR > 50  : Excelente (señal clara)                                 │
│  • SNR 20-50 : Buena                                                    │
│  • SNR < 20  : Pobre (ruido significativo)                             │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

#### 4.2.2 TrimmingAnalyzer

Implementa tres algoritmos de recorte de extremos de baja calidad:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    ALGORITMOS DE TRIMMING                                │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  1. MODIFIED MOTT (Recomendado)                                         │
│  ──────────────────────────────                                         │
│  Basado en probabilidades de error acumuladas.                          │
│                                                                         │
│  Score(i) = Score(i-1) + (cutoff - P_error(i))                         │
│                                                                         │
│  Quality  ████████▓▓▓▓▓▓▓▓████████████▓▓▓▓▓▓████                       │
│  Score    ────────────────/‾‾‾‾‾‾‾‾‾‾‾‾\────────                       │
│                          │              │                               │
│                        start           end                              │
│                                                                         │
│  2. SLIDING WINDOW                                                      │
│  ─────────────────                                                      │
│  Ventana deslizante que busca regiones sobre umbral.                   │
│                                                                         │
│  [──window──]                                                           │
│  Quality  ████████▓▓▓▓▓▓▓▓████████████▓▓▓▓▓▓████                       │
│           ▲ avg >= threshold                                            │
│                                                                         │
│  3. QUALITY THRESHOLD                                                   │
│  ────────────────────                                                   │
│  Corte simple por base con Q < threshold.                              │
│                                                                         │
│  Quality  ████████▓▓▓▓▓▓▓▓████████████▓▓▓▓▓▓████                       │
│           │      │        │          │      │                          │
│           ▲──────┘        ▲──────────┘      │                          │
│         Trim start      Trim end                                        │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

#### 4.2.3 HeterozygoteAnalyzer

Detecta posiciones heterocigotas analizando picos del cromatograma:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                   DETECCIÓN DE HETEROCIGOTOS                             │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Cromatograma en posición heterocigota (A/G):                          │
│                                                                         │
│  Intensidad                                                             │
│      │                                                                  │
│  100 │    ╱╲         A (verde)                                         │
│      │   ╱  ╲                                                           │
│   80 │  ╱    ╲  ╱╲   G (negro)                                         │
│      │ ╱      ╲╱  ╲                                                     │
│   60 │╱            ╲                                                    │
│      │              ╲                                                   │
│   40 │               ╲                                                  │
│      │                ╲                                                 │
│   20 │                 ╲                                                │
│      │                  ╲                                               │
│    0 └──────────────────────────► posición                             │
│                                                                         │
│  Criterios de detección:                                                │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │ ratio = peak_secundario / peak_primario                        │    │
│  │                                                                 │    │
│  │ if 0.3 ≤ ratio ≤ 0.7:                                          │    │
│  │     → Heterocigoto detectado                                    │    │
│  │     → Asignar código IUPAC (R, Y, S, W, K, M)                  │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  Códigos IUPAC:                                                         │
│  ┌─────┬───────────┬─────┬───────────┐                                 │
│  │  R  │   A + G   │  Y  │   C + T   │                                 │
│  │  S  │   G + C   │  W  │   A + T   │                                 │
│  │  K  │   G + T   │  M  │   A + C   │                                 │
│  └─────┴───────────┴─────┴───────────┘                                 │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 4.3 Alignment

El módulo de alineamiento implementa algoritmos clásicos de bioinformática:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         ALIGNMENT MODULE                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  PAIRWISE ALIGNMENT (Needleman-Wunsch)                                  │
│  ─────────────────────────────────────                                  │
│                                                                         │
│  Seq1: A T G C A T                                                      │
│  Seq2: A T - C G T                                                      │
│              │                                                          │
│            gap                                                          │
│                                                                         │
│  Matriz de puntuación:                                                  │
│  ┌─────────────────────────────────────────┐                           │
│  │  Match:    +2  (bases iguales)          │                           │
│  │  Mismatch: -1  (bases diferentes)       │                           │
│  │  Gap open: -10 (penalización apertura)  │                           │
│  │  Gap ext:  -0.5 (penalización extensión)│                           │
│  └─────────────────────────────────────────┘                           │
│                                                                         │
│  MULTIPLE ALIGNMENT (Progressive)                                       │
│  ────────────────────────────────                                       │
│                                                                         │
│     Seq1 ──┐                                                            │
│            ├──► Align ──┐                                               │
│     Seq2 ──┘            │                                               │
│                         ├──► Align ──► Consenso                         │
│     Seq3 ───────────────┘                                               │
│                                                                         │
│  CONSENSUS BUILDING                                                     │
│  ──────────────────                                                     │
│                                                                         │
│  Métodos disponibles:                                                   │
│  • MAJORITY  : Base más frecuente en cada posición                     │
│  • THRESHOLD : Base sobre % umbral                                      │
│  • IUPAC     : Código de ambigüedad si no hay mayoría                  │
│                                                                         │
│  Ejemplo:                                                               │
│  Pos:    1  2  3  4  5                                                 │
│  Seq1:   A  T  G  C  A                                                 │
│  Seq2:   A  T  G  C  A                                                 │
│  Seq3:   A  T  A  C  G                                                 │
│  ────────────────────                                                   │
│  Cons:   A  T  R  C  R    (R = A o G)                                  │
│                                                                         │
│  VARIANT DETECTION                                                      │
│  ─────────────────                                                      │
│                                                                         │
│  Tipos detectados:                                                      │
│  • SNP       : Sustitución de una base                                 │
│  • Insertion : Base extra en algunas secuencias                        │
│  • Deletion  : Base faltante en algunas secuencias                     │
│                                                                         │
│  Ti/Tv Ratio (Transiciones/Transversiones):                            │
│  • Transiciones:   A↔G, C↔T (purina↔purina, pirimidina↔pirimidina)    │
│  • Transversiones: A↔C, A↔T, G↔C, G↔T                                  │
│  • Ratio típico en evolución neutral: ~2.0                             │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 4.4 Workers

Los workers son procesos async que consumen jobs de Redis Streams:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           WORKERS                                        │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│                       ┌──────────────┐                                  │
│                       │  BaseWorker  │ (ABC)                            │
│                       │──────────────│                                  │
│                       │ + start()    │                                  │
│                       │ + stop()     │                                  │
│                       │ + process()  │ (abstract)                       │
│                       └──────┬───────┘                                  │
│              ┌───────────────┼───────────────┐                          │
│              │               │               │                          │
│      ┌───────┴───────┐ ┌─────┴─────┐ ┌───────┴───────┐                 │
│      │  TraceWorker  │ │ Alignment │ │  Analysis     │                 │
│      │               │ │  Worker   │ │   Worker      │                 │
│      ├───────────────┤ ├───────────┤ ├───────────────┤                 │
│      │ Stream:       │ │ Stream:   │ │ Stream:       │                 │
│      │ jobs:traces   │ │ jobs:     │ │ jobs:analysis │                 │
│      │               │ │ alignments│ │               │                 │
│      │ Parsea y      │ │ Alinea    │ │ Ejecuta       │                 │
│      │ analiza       │ │ secuencias│ │ analyzers     │                 │
│      └───────────────┘ └───────────┘ └───────────────┘                 │
│                                                                         │
│  CICLO DE VIDA DEL WORKER:                                              │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │                                                                 │    │
│  │  ┌─────────┐    ┌─────────┐    ┌─────────┐    ┌─────────┐     │    │
│  │  │ STOPPED │───►│STARTING │───►│ RUNNING │───►│STOPPING │     │    │
│  │  └─────────┘    └─────────┘    └────┬────┘    └────┬────┘     │    │
│  │       ▲                             │              │           │    │
│  │       │                             │              │           │    │
│  │       └─────────────────────────────┴──────────────┘           │    │
│  │                                                                 │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 4.5 Storage

Abstracción para múltiples backends de almacenamiento:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        STORAGE PROVIDERS                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│                    ┌─────────────────────┐                              │
│                    │ BaseStorageProvider │ (ABC)                        │
│                    │─────────────────────│                              │
│                    │ + get(path)         │                              │
│                    │ + put(path, data)   │                              │
│                    │ + delete(path)      │                              │
│                    │ + exists(path)      │                              │
│                    │ + list(prefix)      │                              │
│                    │ + health_check()    │                              │
│                    └──────────┬──────────┘                              │
│       ┌──────────┬─────────────┼─────────────┬──────────┐               │
│       │          │             │             │          │               │
│   ┌───┴────┐ ┌───┴────┐  ┌─────┴─────┐ ┌─────┴────┐ ┌───┴────┐         │
│   │ Local  │ │  HTTP  │  │ Supabase  │ │  MinIO   │ │ Memory │         │
│   │Provider│ │Provider│  │ Provider  │ │ Provider │ │ (test) │         │
│   ├────────┤ ├────────┤  ├───────────┤ ├──────────┤ ├────────┤         │
│   │FS+path │ │Read-   │  │ Signed    │ │ S3-      │ │ In-mem │         │
│   │traversal│ │only    │  │ URLs      │ │ compat   │ │ buffer │         │
│   │protect │ │fetching│  │ Buckets   │ │ Buckets  │ │ fixt.  │         │
│   └────────┘ └────────┘  └───────────┘ └──────────┘ └────────┘         │
│                                                                         │
│   ┌─────────────────────────────────────────────────────────────────┐  │
│   │                      StorageFactory                              │  │
│   │─────────────────────────────────────────────────────────────────│  │
│   │  create(settings) → Provider                                     │  │
│   │  create_local(path)        → LocalStorageProvider                │  │
│   │  create_http(timeout)      → HTTPStorageProvider                 │  │
│   │  create_supabase(...)      → SupabaseStorageProvider             │  │
│   │  create_minio(endpoint,...) → MinioStorageProvider               │  │
│   └─────────────────────────────────────────────────────────────────┘  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

### 4.6 Phylogeny

Módulo de análisis filogenético: cálculo de matrices de distancia entre
secuencias, construcción de árboles y evaluación por bootstrap.

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          PHYLOGENY MODULE                                │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│   ┌────────────────────┐    ┌────────────────────┐                     │
│   │ DistanceCalculator │    │    TreeBuilder     │                     │
│   ├────────────────────┤    ├────────────────────┤                     │
│   │ + calculate(seqs)  │    │ + build(matrix)    │                     │
│   │   → DistanceMatrix │    │   → PhylogeneticTree│                    │
│   │                    │    │                    │                     │
│   │ Métodos:           │    │ Métodos:           │                     │
│   │ - p-distance       │    │ - UPGMA            │                     │
│   │ - Jukes-Cantor     │    │ - Neighbor-Joining │                     │
│   │ - Kimura-2P        │    │                    │                     │
│   └─────────┬──────────┘    └─────────┬──────────┘                     │
│             │                         │                                 │
│             └───────────┬─────────────┘                                 │
│                         ▼                                               │
│              ┌──────────────────────┐                                   │
│              │  BootstrapAnalyzer   │                                   │
│              ├──────────────────────┤                                   │
│              │ + run(seqs, n=100)   │                                   │
│              │   → BootstrapResult  │                                   │
│              │                      │                                   │
│              │ Resampling con       │                                   │
│              │ reemplazo + soporte  │                                   │
│              │ por rama (%)         │                                   │
│              └──────────┬───────────┘                                   │
│                         │                                               │
│                         ▼                                               │
│              ┌──────────────────────┐                                   │
│              │  PhylogenyAnalyzer   │  (façade)                         │
│              ├──────────────────────┤                                   │
│              │ Compone distancia +  │                                   │
│              │ árbol + bootstrap en │                                   │
│              │ un PhylogenyResult   │                                   │
│              └──────────────────────┘                                   │
│                                                                         │
│   CONSUMO:                                                              │
│   PhylogenyWorker escucha stream "jobs:phylogeny" y publica             │
│   PhylogenyCompleted / PhylogenyFailed con el árbol en formato Newick.  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

**Salida estandarizada (Newick):**

```
(((seq_a:0.12,seq_b:0.14):0.08,seq_c:0.21):0.0,seq_d:0.30);
```

Los nodos internos pueden anotarse con soporte bootstrap (porcentaje sobre
N réplicas), permitiendo al frontend renderizar el árbol y resaltar ramas
con baja confianza.

---

## 5. Flujo de Datos

### 5.1 Procesamiento de Traza

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    FLUJO: TRACE PROCESSING                               │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  1. JOB ARRIVES                                                         │
│  ───────────────                                                        │
│  Redis Stream: geneflow:jobs:traces                                     │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │ {                                                                 │  │
│  │   "traceId": "trace-123",                                        │  │
│  │   "studyId": "study-456",                                        │  │
│  │   "fileName": "sample.ab1",                                      │  │
│  │   "storagePath": "traces/sample.ab1",                            │  │
│  │   "format": "ab1"                                                │  │
│  │ }                                                                 │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                              │                                          │
│                              ▼                                          │
│  2. DOWNLOAD FILE                                                       │
│  ────────────────                                                       │
│  StorageProvider.get("traces/sample.ab1") → bytes                      │
│                              │                                          │
│                              ▼                                          │
│  3. PARSE                                                               │
│  ────────                                                               │
│  AB1Parser.parse(bytes) → ParsedTrace                                  │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │ ParsedTrace(                                                      │  │
│  │   sequence = "ATGCATGC...",                                      │  │
│  │   quality = [35, 38, 40, ...],                                   │  │
│  │   chromatogram = ChromatogramData(traceA, traceC, traceG, traceT)│  │
│  │ )                                                                 │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                              │                                          │
│                              ▼                                          │
│  4. ANALYZE                                                             │
│  ──────────                                                             │
│  QualityAnalyzer.analyze(sequence, chromatogram) → QualityMetrics      │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │ QualityMetrics(                                                   │  │
│  │   meanQuality = 35.5,                                            │  │
│  │   q20Percentage = 92.3,                                          │  │
│  │   q30Percentage = 78.1,                                          │  │
│  │   gcContent = 48.5,                                              │  │
│  │   snr = 45.2                                                     │  │
│  │ )                                                                 │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                              │                                          │
│                              ▼                                          │
│  5. PUBLISH EVENT                                                       │
│  ────────────────                                                       │
│  Redis Stream: geneflow:events:traces                                   │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │ TraceProcessedEvent(                                              │  │
│  │   traceId = "trace-123",                                         │  │
│  │   studyId = "study-456",                                         │  │
│  │   sequence = "ATGCATGC...",                                      │  │
│  │   qualityMetrics = {...},                                        │  │
│  │   chromatogram = {...}                                           │  │
│  │ )                                                                 │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                              │                                          │
│                              ▼                                          │
│  6. ACKNOWLEDGE                                                         │
│  ──────────────                                                         │
│  XACK geneflow:jobs:traces consumer-group message-id                   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 5.2 Flujo de Alineamiento

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    FLUJO: ALIGNMENT                                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  INPUT                                                                  │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │ {                                                                 │  │
│  │   "alignmentId": "align-123",                                    │  │
│  │   "type": "multiple",                                            │  │
│  │   "sequences": ["ATGCAT", "ATGCGT", "ATGCCT"]                   │  │
│  │ }                                                                 │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                              │                                          │
│                              ▼                                          │
│  PROGRESSIVE ALIGNMENT                                                  │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                                                                   │  │
│  │  Seq1: ATGCAT    Seq2: ATGCGT                                    │  │
│  │         │              │                                          │  │
│  │         └──────┬───────┘                                          │  │
│  │                ▼                                                  │  │
│  │         ┌────────────┐                                            │  │
│  │         │  Pairwise  │                                            │  │
│  │         │   Align    │                                            │  │
│  │         └─────┬──────┘                                            │  │
│  │               │              Seq3: ATGCCT                         │  │
│  │               │                    │                              │  │
│  │               └────────┬───────────┘                              │  │
│  │                        ▼                                          │  │
│  │                 ┌────────────┐                                    │  │
│  │                 │   Align    │                                    │  │
│  │                 │  Profile   │                                    │  │
│  │                 └─────┬──────┘                                    │  │
│  │                       │                                           │  │
│  │                       ▼                                           │  │
│  │                 ATGC-T (aligned)                                  │  │
│  │                 ATGCGT                                            │  │
│  │                 ATGCCT                                            │  │
│  │                                                                   │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                              │                                          │
│                              ▼                                          │
│  CONSENSUS + VARIANTS                                                   │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │  Position:  1  2  3  4  5  6                                     │  │
│  │  Seq1:      A  T  G  C  -  T                                     │  │
│  │  Seq2:      A  T  G  C  G  T                                     │  │
│  │  Seq3:      A  T  G  C  C  T                                     │  │
│  │  ─────────────────────────────                                   │  │
│  │  Consensus: A  T  G  C  S  T      (S = G or C)                   │  │
│  │                                                                   │  │
│  │  Variants:                                                        │  │
│  │  • Position 5: G→C (SNP, freq=0.33)                              │  │
│  │  • Position 5: - (deletion in Seq1)                               │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                              │                                          │
│                              ▼                                          │
│  OUTPUT EVENT                                                           │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │ AlignmentCompletedEvent(                                          │  │
│  │   alignmentId = "align-123",                                     │  │
│  │   alignedSequences = [...],                                      │  │
│  │   consensus = "ATGCST",                                          │  │
│  │   identity = 83.3,                                               │  │
│  │   variants = [...]                                               │  │
│  │ )                                                                 │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 6. Patrones de Diseño

### 6.1 Patrones Utilizados

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      PATRONES DE DISEÑO                                  │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  1. FACTORY PATTERN                                                     │
│  ──────────────────                                                     │
│  Uso: Creación de parsers y storage providers                          │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ class ParserFactory:                                             │   │
│  │     @classmethod                                                 │   │
│  │     def get_parser(cls, format: TraceFormat) -> BaseParser:     │   │
│  │         match format:                                            │   │
│  │             case TraceFormat.AB1:                                │   │
│  │                 return AB1Parser()                               │   │
│  │             case TraceFormat.SCF:                                │   │
│  │                 return SCFParser()                               │   │
│  │             ...                                                  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  2. STRATEGY PATTERN                                                    │
│  ───────────────────                                                    │
│  Uso: Algoritmos de trimming intercambiables                           │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ class TrimmingAnalyzer:                                          │   │
│  │     def analyze(self, seq, algorithm: TrimmingAlgorithm):       │   │
│  │         match algorithm:                                         │   │
│  │             case TrimmingAlgorithm.MODIFIED_MOTT:               │   │
│  │                 return self._trim_modified_mott(seq)            │   │
│  │             case TrimmingAlgorithm.SLIDING_WINDOW:              │   │
│  │                 return self._trim_sliding_window(seq)           │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  3. TEMPLATE METHOD                                                     │
│  ──────────────────                                                     │
│  Uso: BaseWorker define flujo, subclases implementan proceso           │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ class BaseWorker(ABC):                                           │   │
│  │     async def _consume_loop(self):     # Template                │   │
│  │         while self._running:                                     │   │
│  │             messages = await self._read_messages()              │   │
│  │             for msg in messages:                                 │   │
│  │                 await self.process_job(msg)  # Hook abstracto   │   │
│  │                 await self._ack_message(msg)                    │   │
│  │                                                                  │   │
│  │     @abstractmethod                                              │   │
│  │     async def process_job(self, job_data):                      │   │
│  │         pass                                                     │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  4. PUBLISHER-SUBSCRIBER                                                │
│  ───────────────────────                                                │
│  Uso: Comunicación desacoplada via Redis Streams                       │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ # Publisher                                                      │   │
│  │ await event_bus.publish("traces", TraceProcessedEvent(...))     │   │
│  │                                                                  │   │
│  │ # Subscribers (otros servicios)                                  │   │
│  │ XREADGROUP GROUP consumers CONSUMER api-1 STREAMS events:traces │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  5. DEPENDENCY INJECTION                                                │
│  ───────────────────────                                                │
│  Uso: Workers reciben dependencias en constructor                      │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ class TraceWorker(BaseWorker):                                   │   │
│  │     def __init__(                                                │   │
│  │         self,                                                    │   │
│  │         redis: Redis,              # Inyectado                  │   │
│  │         publisher: EventPublisher, # Inyectado                  │   │
│  │         settings: Settings         # Inyectado                  │   │
│  │     ):                                                           │   │
│  │         ...                                                      │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  6. BOOTSTRAP PATTERN                                                   │
│  ────────────────────                                                   │
│  Uso: Creación y cableado de componentes en un solo lugar             │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ class ApplicationComponents:                                     │   │
│  │     """Contenedor de todos los componentes."""                  │   │
│  │     settings: Settings                                           │   │
│  │     redis: Redis                                                 │   │
│  │     publisher: EventBusPublisher                                 │   │
│  │     storage: BaseStorageProvider                                 │   │
│  │     workers: dict[str, BaseWorker]                              │   │
│  │     api: AnalysisAPI                                             │   │
│  │                                                                  │   │
│  │ def bootstrap(settings: Settings = None) -> ApplicationComponents│   │
│  │     """Crea y conecta todos los componentes."""                 │   │
│  │     redis = create_redis(settings)                              │   │
│  │     publisher = create_publisher(redis, settings)               │   │
│  │     storage = create_storage(settings)                          │   │
│  │     workers = create_workers(redis, publisher, settings)        │   │
│  │     api = create_api(settings)                                  │   │
│  │     return ApplicationComponents(...)                           │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  7. LIFECYCLE PATTERN                                                   │
│  ────────────────────                                                   │
│  Uso: Gestión centralizada de startup y shutdown                       │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ class ApplicationLifecycle:                                      │   │
│  │     def __init__(self, components: ApplicationComponents):      │   │
│  │         self.components = components                             │   │
│  │                                                                  │   │
│  │     async def startup(self):                                     │   │
│  │         await self.components.redis.ping()                      │   │
│  │         await self.publisher.publish(WorkerStarted(...))        │   │
│  │         for worker in self.workers.values():                    │   │
│  │             asyncio.create_task(worker.start())                 │   │
│  │                                                                  │   │
│  │     async def shutdown(self):                                    │   │
│  │         for worker in self.workers.values():                    │   │
│  │             await worker.stop()                                 │   │
│  │         await self.publisher.publish(WorkerStopped(...))        │   │
│  │         await self.redis.close()                                │   │
│  │                                                                  │   │
│  │     async def run(self):                                         │   │
│  │         self._setup_signal_handlers()                           │   │
│  │         await self.startup()                                    │   │
│  │         await self._wait_for_shutdown()                         │   │
│  │         await self.shutdown()                                   │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 7. Modelo de Datos

### 7.1 Diagrama de Clases

```
┌─────────────────────────────────────────────────────────────────────────┐
│                       MODELO DE DOMINIO                                  │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────────┐         ┌─────────────────────┐                   │
│  │    Sequence     │         │  ChromatogramData   │                   │
│  │─────────────────│         │─────────────────────│                   │
│  │ id: str         │         │ traceA: list[int]   │                   │
│  │ sequence: str   │    1──1 │ traceC: list[int]   │                   │
│  │ quality: [int]  │◄────────│ traceG: list[int]   │                   │
│  │ name: str?      │         │ traceT: list[int]   │                   │
│  │ description: str│         │ peakLocations: [int]│                   │
│  └────────┬────────┘         └─────────────────────┘                   │
│           │                                                             │
│           │ 1                                                           │
│           │                                                             │
│           ▼                                                             │
│  ┌─────────────────┐         ┌─────────────────────┐                   │
│  │   ParsedTrace   │         │   QualityMetrics    │                   │
│  │─────────────────│    1──1 │─────────────────────│                   │
│  │ traceId: str    │◄────────│ meanQuality: float  │                   │
│  │ format: Enum    │         │ q20Percentage: float│                   │
│  │ sequence: Seq   │         │ q30Percentage: float│                   │
│  │ chromatogram?   │         │ gcContent: float    │                   │
│  │ qualityMetrics? │         │ snr: float?         │                   │
│  │ metadata: dict  │         │ length: int         │                   │
│  └─────────────────┘         └─────────────────────┘                   │
│                                                                         │
│  ┌─────────────────┐         ┌─────────────────────┐                   │
│  │ AlignmentResult │         │      Variant        │                   │
│  │─────────────────│    1──* │─────────────────────│                   │
│  │ alignmentId: str│◄────────│ position: int       │                   │
│  │ type: Enum      │         │ type: VariantType   │                   │
│  │ sequences: [str]│         │ reference: str      │                   │
│  │ alignedSeqs:[st]│         │ alternate: str      │                   │
│  │ score: float    │         │ frequency: float    │                   │
│  │ identity: float │         └─────────────────────┘                   │
│  │ gaps: int       │                                                    │
│  │ consensus: str? │                                                    │
│  └─────────────────┘                                                    │
│                                                                         │
│  ┌─────────────────┐         ┌─────────────────────┐                   │
│  │  TrimmingResult │         │  HeterozygoteCall   │                   │
│  │─────────────────│         │─────────────────────│                   │
│  │ originalLen: int│         │ position: int       │                   │
│  │ trimmedLen: int │         │ base1: str          │                   │
│  │ trimStart: int  │         │ base2: str          │                   │
│  │ trimEnd: int    │         │ iupacCode: str      │                   │
│  │ trimmedSeq: str │         │ ratio: float        │                   │
│  │ algorithm: str  │         │ confidence: float   │                   │
│  └─────────────────┘         └─────────────────────┘                   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 7.2 Convenciones de Nomenclatura

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     CONVENCIONES                                         │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ATRIBUTOS: camelCase                                                   │
│  ─────────────────────                                                  │
│  Razón: Compatibilidad con frontend JavaScript/TypeScript              │
│                                                                         │
│  @dataclass                                                             │
│  class QualityMetrics:                                                  │
│      meanQuality: float      # ✓ camelCase                             │
│      q20Percentage: float    # ✓ camelCase                             │
│      gc_content: float       # ✗ snake_case (evitar)                   │
│                                                                         │
│  VARIABLES DE ENTORNO: WORKER_ prefix                                   │
│  ────────────────────────────────────                                   │
│  Razón: Evitar colisiones, claridad en docker-compose                  │
│                                                                         │
│  WORKER_REDIS_URL=redis://localhost:6379                               │
│  WORKER_STORAGE_PROVIDER=local                                          │
│  WORKER_API_PORT=8080                                                   │
│                                                                         │
│  STREAMS: geneflow: prefix                                              │
│  ─────────────────────────                                              │
│  Razón: Namespace en Redis compartido                                  │
│                                                                         │
│  geneflow:jobs:traces        # Jobs de procesamiento                   │
│  geneflow:events:traces      # Eventos publicados                      │
│  geneflow:events:system      # Eventos de sistema                      │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 8. Estrategia de Testing

### 8.1 Pirámide de Tests

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      ESTRATEGIA DE TESTING                               │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│                           ╱╲                                            │
│                          ╱  ╲                                           │
│                         ╱ E2E╲         Pocos, lentos, frágiles         │
│                        ╱──────╲                                         │
│                       ╱        ╲                                        │
│                      ╱Integration╲     Redis real, archivos reales     │
│                     ╱────────────╲                                      │
│                    ╱              ╲                                     │
│                   ╱   Unit Tests   ╲   Mayoría, rápidos, aislados      │
│                  ╱──────────────────╲                                   │
│                                                                         │
│  DISTRIBUCIÓN ACTUAL: 476 tests (1 skipped, <2s end-to-end)             │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │ test_models.py             ███████░░░░░░░░░░░░░░░░  28 tests │    │
│  │ test_constants.py          ███░░░░░░░░░░░░░░░░░░░░  12 tests │    │
│  │ test_parsers.py            █████████░░░░░░░░░░░░░░  35 tests │    │
│  │ test_scf_parser.py         ████░░░░░░░░░░░░░░░░░░░  14 tests │    │
│  │ test_analyzers.py          ██████░░░░░░░░░░░░░░░░░  23 tests │    │
│  │ test_analyzers_advanced.py ██████████░░░░░░░░░░░░░  42 tests │    │
│  │ test_alignment.py          █████████░░░░░░░░░░░░░░  38 tests │    │
│  │ test_workers.py            ████░░░░░░░░░░░░░░░░░░░  16 tests │    │
│  │ test_workers_extended.py   ████░░░░░░░░░░░░░░░░░░░  15 tests │    │
│  │ test_analysis_worker.py    ███░░░░░░░░░░░░░░░░░░░░  10 tests │    │
│  │ test_storage.py            ████░░░░░░░░░░░░░░░░░░░  18 tests │    │
│  │ test_storage_minio.py      ███░░░░░░░░░░░░░░░░░░░░  12 tests │    │
│  │ test_storage_remote.py     ███░░░░░░░░░░░░░░░░░░░░  11 tests │    │
│  │ test_events.py             █████░░░░░░░░░░░░░░░░░░  20 tests │    │
│  │ test_publisher.py          ███░░░░░░░░░░░░░░░░░░░░  10 tests │    │
│  │ test_api.py                ███░░░░░░░░░░░░░░░░░░░░  12 tests │    │
│  │ test_bootstrap.py          ███░░░░░░░░░░░░░░░░░░░░  12 tests │    │
│  │ test_lifecycle.py          ████░░░░░░░░░░░░░░░░░░░  16 tests │    │
│  │ test_phylogeny.py          ████░░░░░░░░░░░░░░░░░░░  14 tests │    │
│  │ test_coverage_boost.py     ████████████░░░░░░░░░░░  47 tests │    │
│  │ test_coverage_boost_br.py  ███████████░░░░░░░░░░░░  43 tests │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 8.2 Ejemplos de Tests

```python
# tests/test_analyzers.py

class TestQualityAnalyzer:
    """Tests for QualityAnalyzer."""

    def test_analyze_with_quality(self):
        """Test quality metrics calculation."""
        analyzer = QualityAnalyzer()
        seq = Sequence(
            id="test",
            sequence="ATGCATGC",
            quality=[30, 35, 40, 25, 30, 35, 40, 25],
        )

        result = analyzer.analyze(seq)

        assert result.length == 8
        assert result.gcContent == 50.0
        assert result.meanQuality == 32.5
        assert result.ambiguousCount == 0

    def test_snr_with_chromatogram(self):
        """Test SNR calculation with chromatogram data."""
        analyzer = QualityAnalyzer()

        # Create mock chromatogram with clear peaks
        chromatogram = ChromatogramData(
            traceA=[10]*50,
            traceC=[10]*50,
            traceG=[10]*50,
            traceT=[10]*50,
            baseCalls=[65, 84, 71, 67],
            peakLocations=[10, 20, 30, 40],
        )
        chromatogram.traceA[10] = 1000  # Clear peak

        result = analyzer.analyze(seq, chromatogram=chromatogram)

        assert result.snr is not None
        assert result.snr > 50  # Good quality signal
```

### 8.3 Fixtures Compartidos

```python
# tests/conftest.py

@pytest.fixture
def sample_sequence():
    """Create a sample sequence for testing."""
    return Sequence(
        id="test-seq",
        sequence="ATGCATGCATGC",
        quality=[30, 35, 40, 35, 30, 35, 40, 35, 30, 35, 40, 35],
    )

@pytest.fixture
def mock_redis():
    """Create a mock Redis client."""
    redis = AsyncMock()
    redis.xreadgroup = AsyncMock(return_value=[])
    redis.xack = AsyncMock()
    redis.xadd = AsyncMock()
    return redis

@pytest.fixture
def settings():
    """Create test settings."""
    return Settings(
        redis_url="redis://localhost:6379",
        storage_provider="local",
        local_storage_path="/tmp/test",
    )
```

### 8.4 Cobertura de Código (Line + Branch)

El proyecto mide cobertura **de línea** y **de rama** por separado, exigiendo
ambos en el CI mediante un script propio (`scripts/coverage_summary.py`) que
parsea el `coverage.json` generado por `coverage.py`.

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      COBERTURA ACTUAL                                    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Lines    : 93.77 %  (3175 / 3386 sentencias)                          │
│  Branches : 85.05 %  (677 / 796 ramas, 87 parciales)                   │
│  Combined : 92.11 %                                                     │
│                                                                         │
│  Gates exigidos en CI:                                                  │
│  ├── --min-line   = 80  (objetivo interno ≥ 90)                        │
│  └── --min-branch = 70  (objetivo interno ≥ 85)                        │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

**Por qué medir rama separadamente:**

La cobertura de línea ignora caminos no tomados en una condición. Una línea
con `if cond:` cuenta como cubierta aunque sólo se haya probado el camino
verdadero. La cobertura de rama exige ejercitar ambos caminos, exponiendo
ramas falsas no probadas (manejo de errores, casos límite).

**Configuración (`pyproject.toml`):**

```toml
[tool.coverage.run]
branch = true
source = ["src"]
omit = ["src/main.py", "src/config.py", "src/*/proving.py"]

[tool.coverage.report]
show_missing = true
exclude_lines = ["pragma: no cover", "if TYPE_CHECKING:", "@abstractmethod"]

[tool.coverage.json]
output = "coverage.json"
```

**Reporte amigable en CI:** el step de breakdown publica una tabla
Markdown en `GITHUB_STEP_SUMMARY` para que cada PR muestre línea/rama en
su check.

---

## 9. Despliegue y Operaciones

### 9.1 Arquitectura de Despliegue

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    ARQUITECTURA DE DESPLIEGUE                            │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  KUBERNETES / DOCKER COMPOSE                                            │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │                                                                    │ │
│  │   ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │ │
│  │   │   Worker    │  │   Worker    │  │   Worker    │              │ │
│  │   │  Replica 1  │  │  Replica 2  │  │  Replica N  │              │ │
│  │   │             │  │             │  │             │              │ │
│  │   │ :8080/health│  │ :8080/health│  │ :8080/health│              │ │
│  │   └──────┬──────┘  └──────┬──────┘  └──────┬──────┘              │ │
│  │          │                │                │                      │ │
│  │          └────────────────┼────────────────┘                      │ │
│  │                           │                                        │ │
│  │                           ▼                                        │ │
│  │                    ┌─────────────┐                                 │ │
│  │                    │    Redis    │                                 │ │
│  │                    │   Cluster   │                                 │ │
│  │                    │             │                                 │ │
│  │                    │  Streams +  │                                 │ │
│  │                    │  Consumer   │                                 │ │
│  │                    │   Groups    │                                 │ │
│  │                    └─────────────┘                                 │ │
│  │                                                                    │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  HEALTHCHECKS                                                           │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │                                                                    │ │
│  │  GET /health                                                       │ │
│  │  ┌────────────────────────────────────────────────────────────┐   │ │
│  │  │ {                                                           │   │ │
│  │  │   "status": "healthy",                                     │   │ │
│  │  │   "workers": {                                              │   │ │
│  │  │     "trace": {"running": true, "jobsProcessed": 142},      │   │ │
│  │  │     "alignment": {"running": true, "jobsProcessed": 38},   │   │ │
│  │  │     "analysis": {"running": true, "jobsProcessed": 56}     │   │ │
│  │  │   },                                                        │   │ │
│  │  │   "redis": "connected",                                     │   │ │
│  │  │   "uptime": 3600                                            │   │ │
│  │  │ }                                                           │   │ │
│  │  └────────────────────────────────────────────────────────────┘   │ │
│  │                                                                    │ │
│  │  GET /ready  → 200 si puede procesar jobs                         │ │
│  │  GET /live   → 200 si el proceso está vivo                        │ │
│  │                                                                    │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 9.2 Dockerfile

```dockerfile
FROM python:3.12-slim

# Install uv
COPY --from=ghcr.io/astral-sh/uv:latest /uv /usr/local/bin/uv

# Install curl for healthchecks
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy project files
COPY pyproject.toml uv.lock README.md ./
COPY src/ ./src/

# Create venv and install dependencies
RUN uv venv /app/.venv && \
    uv sync --frozen --no-dev

# Security: non-root user
RUN useradd --create-home appuser && \
    chown -R appuser:appuser /app
USER appuser

ENV PATH="/app/.venv/bin:$PATH"
ENV WORKER_API_HOST=0.0.0.0
ENV WORKER_API_PORT=8080

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

CMD ["python", "-m", "src.main"]
```

### 9.3 CI/CD Pipeline

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        CI/CD PIPELINE                                    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  TRIGGER: push a main/master/develop, pull request                      │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                          CI WORKFLOW                             │   │
│  │                                                                  │   │
│  │   ┌────────┐                                                     │   │
│  │   │  Lint  │  ruff check                                         │   │
│  │   └───┬────┘                                                     │   │
│  │       │                                                          │   │
│  │   ┌───┴────┐                                                     │   │
│  │   │ Format │  ruff format --check                                │   │
│  │   └───┬────┘                                                     │   │
│  │       │                                                          │   │
│  │   ┌───┴──────┐                                                   │   │
│  │   │TypeCheck │  mypy src/   (baseline, continue-on-error)        │   │
│  │   └───┬──────┘                                                   │   │
│  │       │                                                          │   │
│  │       ├────────────────────────────────┐                         │   │
│  │       ▼                                ▼                         │   │
│  │  ┌────────┐    ┌─────────────┐   ┌──────────┐                    │   │
│  │  │  Test  │───►│  Coverage   │   │ Security │                    │   │
│  │  │ pytest │    │  Breakdown  │   │ pip-audit│                    │   │
│  │  │  +cov  │    │ line/branch │   │          │                    │   │
│  │  └───┬────┘    │  + gates    │   └──────────┘                    │   │
│  │      │         └──────┬──────┘                                   │   │
│  │      ▼                ▼                                          │   │
│  │ ┌─────────┐    ┌──────────────┐                                  │   │
│  │ │ Codecov │    │ Artifacts:   │                                  │   │
│  │ │ upload  │    │ coverage.xml │                                  │   │
│  │ │         │    │ coverage.json│                                  │   │
│  │ └────┬────┘    └──────────────┘                                  │   │
│  │      ▼                                                           │   │
│  │ ┌────────┐                                                       │   │
│  │ │ Build  │  docker buildx (cache GHA)                            │   │
│  │ │ Docker │                                                       │   │
│  │ └────────┘                                                       │   │
│  │                                                                  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                              │                                          │
│                              ▼ (on tag v*)                              │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                          CD WORKFLOW                             │   │
│  │                                                                  │   │
│  │  ┌────────────┐    ┌────────────┐    ┌────────────┐            │   │
│  │  │   Build    │───►│   Push     │───►│   Deploy   │            │   │
│  │  │  Multi-    │    │   GHCR     │    │  Staging/  │            │   │
│  │  │   arch     │    │            │    │   Prod     │            │   │
│  │  │amd64/arm64 │    │            │    │            │            │   │
│  │  └────────────┘    └────────────┘    └────────────┘            │   │
│  │                                                                  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

### 9.4 Quality Gates y Tooling

El pipeline aplica **cinco puertas de calidad** que deben pasar antes de
fusionar a `master`:

| Gate          | Herramienta            | Comando                                  | Bloqueante |
|---------------|------------------------|------------------------------------------|------------|
| Lint          | `ruff check`           | `uv run ruff check src/ tests/`          | Sí         |
| Format        | `ruff format --check`  | `uv run ruff format --check src/ tests/` | Sí         |
| Type check    | `mypy`                 | `uv run mypy src/`                       | Baseline*  |
| Tests         | `pytest --cov-branch`  | `uv run pytest --cov=src --cov-branch`   | Sí         |
| Coverage      | `coverage_summary.py`  | `--min-line 80 --min-branch 70`          | Sí         |
| Security      | `pip-audit`            | `uv run pip-audit`                       | Aviso      |

*El type check arranca con `continue-on-error: true` mientras se sanean
los 44 errores preexistentes; se endurece progresivamente.

### 9.5 Docker Compose para Desarrollo Local

Para reproducir las dependencias del worker en local se incluye un
`docker-compose.yml` con Redis y MinIO, más un
`docker-compose.override.yml` para parámetros específicos del entorno
(puertos, credenciales, paths). Los desarrolladores arrancan todo con
`docker compose up -d` y pueden ejecutar el worker en modo nativo
apuntando a estos servicios.

---

## 10. Decisiones Técnicas

### 10.1 Decisiones Clave

| Decisión | Alternativas | Justificación |
|----------|--------------|---------------|
| **Python 3.12** | Go, Rust | Ecosistema bioinformático maduro (BioPython), productividad |
| **Redis Streams** | RabbitMQ, Kafka | Simplicidad, ya usado en la plataforma, consumer groups nativos |
| **Async/await** | Threads, multiprocessing | I/O bound workload, mejor escalabilidad por instancia |
| **BioPython** | Implementación propia | Madurez, soporte AB1/SCF, comunidad activa |
| **Dataclasses** | Pydantic models | Simplicidad para dominio, Pydantic solo para config |
| **uv** | pip, poetry | Velocidad (10-100x), lockfile determinista |
| **structlog** | logging stdlib | JSON estructurado, contexto automático |

### 10.2 Trade-offs

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         TRADE-OFFS                                       │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  1. PYTHON vs LENGUAJES COMPILADOS                                      │
│  ─────────────────────────────────                                      │
│  ✓ Desarrollo rápido                                                    │
│  ✓ Ecosistema bioinformático                                            │
│  ✗ Menor rendimiento en CPU-bound                                       │
│  Mitigación: Análisis pesados usan NumPy (C underneath)                │
│                                                                         │
│  2. REDIS STREAMS vs MESSAGE BROKERS DEDICADOS                          │
│  ──────────────────────────────────────────────                         │
│  ✓ Stack simplificado (Redis ya existe)                                 │
│  ✓ Consumer groups para escalado                                        │
│  ✗ Menor throughput que Kafka                                           │
│  ✗ No hay dead letter queues nativas                                    │
│  Mitigación: Volumen esperado es manejable (<10k jobs/min)             │
│                                                                         │
│  3. MONOLITO WORKER vs MICROSERVICIOS SEPARADOS                         │
│  ───────────────────────────────────────────────                        │
│  ✓ Deployment simple                                                    │
│  ✓ Código compartido entre analyzers                                    │
│  ✗ Un fallo puede afectar todos los workers                            │
│  Mitigación: Cada worker es independiente internamente                 │
│                                                                         │
│  4. BIOPYTHON vs IMPLEMENTACIÓN PROPIA                                  │
│  ─────────────────────────────────────                                  │
│  ✓ Años de desarrollo y testing                                         │
│  ✓ Soporte comunitario                                                  │
│  ✗ Dependencia externa                                                  │
│  ✗ Overhead para features no usados                                     │
│  Decisión: Beneficios superan costos significativamente                │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 11. Conclusiones

### 11.1 Objetivos Cumplidos

| Objetivo | Estado | Evidencia |
|----------|--------|-----------|
| Soporte multi-formato | ✅ | AB1, SCF, FASTQ, FASTA implementados |
| Análisis completo | ✅ | 7 analyzers + alignment module |
| Escalabilidad | ✅ | Consumer groups, stateless workers |
| Fiabilidad | ✅ | XACK after processing, retry logic |
| Mantenibilidad | ✅ | 241 tests, >90% coverage estimado |
| Documentación | ✅ | README, docstrings, CLAUDE.md |
| Arquitectura limpia | ✅ | Bootstrap + Lifecycle patterns, main.py ~50 LOC |

### 11.2 Métricas del Proyecto

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      MÉTRICAS DEL PROYECTO                               │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  CÓDIGO                                                                 │
│  ├── Líneas de código (src/):     ~6,200                               │
│  ├── Líneas de tests:             ~5,800                               │
│  ├── Archivos Python (src):       65                                    │
│  ├── Cobertura de línea:          93.77 %                              │
│  └── Cobertura de rama:           85.05 %                              │
│                                                                         │
│  TESTS                                                                  │
│  ├── Total tests:                 476 (1 skipped)                       │
│  ├── Tests unitarios:             ~420                                  │
│  ├── Tests de integración:        ~55                                   │
│  └── Tiempo de ejecución:         <2 s                                  │
│                                                                         │
│  DEPENDENCIAS                                                           │
│  ├── Producción:                  10 (incl. minio)                      │
│  ├── Desarrollo:                  5  (incl. mypy)                       │
│  └── Vulnerabilidades conocidas:  0                                     │
│                                                                         │
│  CALIDAD                                                                │
│  ├── ruff check / format:         ✓ All checks passed                  │
│  ├── mypy baseline:               44 errores (gate no bloqueante)      │
│  └── Quality gates en CI:         lint + format + typecheck + tests    │
│                                                                         │
│  DOCKER                                                                 │
│  ├── Tamaño imagen:               ~250MB                               │
│  ├── Tiempo de build:             ~30s                                 │
│  └── Plataformas:                 amd64, arm64                         │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 11.3 Trabajo Futuro

1. **BLAST Integration**: Búsqueda de similitud contra bases de datos públicas
2. **GPU Acceleration**: Alineamiento múltiple con CUDA para secuencias largas
3. **Batch Processing**: Optimización para procesar múltiples trazas en paralelo
4. **ML Quality Prediction**: Modelo entrenado para predecir calidad pre-secuenciación
5. **WebSocket Streaming**: Progreso en tiempo real para análisis largos

---

## Anexo A: Estructura del Proyecto

```
geneflow-analysis/
├── src/
│   ├── __init__.py
│   ├── main.py              # Entry point simplificado (~50 LOC)
│   ├── bootstrap.py         # ApplicationComponents + factories
│   ├── lifecycle.py         # ApplicationLifecycle (startup/shutdown)
│   ├── models.py            # Domain dataclasses
│   ├── constants.py         # IUPAC codes, enzymes, codons
│   │
│   ├── config/              # Configuración modular
│   │   ├── __init__.py      # Exports Settings, settings
│   │   └── settings.py      # Pydantic Settings class
│   │
│   ├── api/                 # API modular con middleware
│   │   ├── __init__.py      # Exports AnalysisAPI, create_app
│   │   ├── app.py           # FastAPI factory create_app()
│   │   ├── api.py           # AnalysisAPI wrapper class
│   │   ├── middleware/
│   │   │   ├── __init__.py
│   │   │   ├── correlation.py  # X-Correlation-ID middleware
│   │   │   └── logging.py      # Request logging middleware
│   │   ├── routes/
│   │   │   ├── __init__.py
│   │   │   └── health.py    # /health, /ready, /live endpoints
│   │   └── responses/
│   │       ├── __init__.py
│   │       └── health_response.py  # Pydantic response models
│   │
│   ├── analyzers/
│   │   ├── __init__.py
│   │   ├── analyzer.py      # BaseAnalyzer ABC
│   │   ├── quality.py       # QualityAnalyzer (Q20/Q30/SNR)
│   │   ├── trimming.py      # TrimmingAnalyzer (3 algorithms)
│   │   ├── heterozygote.py  # HeterozygoteAnalyzer
│   │   ├── motif.py         # MotifAnalyzer
│   │   ├── translation.py   # TranslationAnalyzer
│   │   ├── orf.py           # ORFAnalyzer
│   │   └── restriction.py   # RestrictionAnalyzer
│   │
│   ├── alignment/
│   │   ├── __init__.py
│   │   ├── aligner.py       # BaseAligner ABC
│   │   ├── pairwise.py      # Needleman-Wunsch
│   │   ├── multiple.py      # Progressive alignment
│   │   ├── consensus.py     # ConsensusBuilder
│   │   └── variants.py      # VariantDetector
│   │
│   ├── parsers/
│   │   ├── __init__.py
│   │   ├── parser.py        # BaseParser + ParserFactory
│   │   ├── ab1.py           # AB1Parser (BioPython)
│   │   ├── scf.py           # SCFParser (BioPython)
│   │   ├── fastq.py         # FASTQParser
│   │   ├── fasta.py         # FASTAParser
│   │   └── synthetic_chromatogram.py  # Generador para tests
│   │
│   ├── phylogeny/           # NEW: análisis filogenético
│   │   ├── __init__.py
│   │   ├── analyzer.py      # PhylogenyAnalyzer (façade)
│   │   ├── distance.py      # DistanceCalculator (p, JC, K2P)
│   │   ├── tree.py          # TreeBuilder (UPGMA, NJ)
│   │   └── bootstrap.py     # BootstrapAnalyzer
│   │
│   ├── workers/
│   │   ├── __init__.py
│   │   ├── base.py          # BaseWorker (consume loop)
│   │   ├── trace.py         # TraceWorker
│   │   ├── alignment.py     # AlignmentWorker
│   │   ├── analysis.py      # AnalysisWorker
│   │   └── phylogeny.py     # NEW: PhylogenyWorker
│   │
│   ├── events/
│   │   ├── __init__.py
│   │   ├── events.py        # Event definitions
│   │   └── publisher.py     # Redis Streams publisher
│   │
│   ├── storage/
│   │   ├── __init__.py
│   │   ├── base.py          # BaseStorageProvider ABC
│   │   ├── local.py         # LocalStorageProvider
│   │   ├── http.py          # HTTPStorageProvider
│   │   ├── supabase.py      # SupabaseStorageProvider
│   │   ├── minio.py         # NEW: MinioStorageProvider (S3)
│   │   └── factory.py       # StorageFactory
│   │
│   └── utils/               # NEW: utilidades transversales
│       ├── __init__.py
│       └── chunking.py      # Chunking de cromatogramas y trazas
│
├── tests/                       # 476 tests, <2 s
│   ├── __init__.py
│   ├── conftest.py              # Shared fixtures
│   ├── test_models.py
│   ├── test_constants.py
│   ├── test_parsers.py
│   ├── test_scf_parser.py
│   ├── test_analyzers.py
│   ├── test_analyzers_advanced.py
│   ├── test_alignment.py
│   ├── test_workers.py
│   ├── test_workers_extended.py
│   ├── test_analysis_worker.py
│   ├── test_storage.py
│   ├── test_storage_minio.py
│   ├── test_storage_remote.py
│   ├── test_events.py
│   ├── test_publisher.py
│   ├── test_api.py
│   ├── test_bootstrap.py
│   ├── test_lifecycle.py
│   ├── test_phylogeny.py
│   ├── test_coverage_boost.py          # +47 tests (line coverage)
│   └── test_coverage_boost_branches.py # +43 tests (branch coverage)
│
├── scripts/
│   └── coverage_summary.py  # Reporte line/branch + gates en CI
│
├── docs/
│   ├── TFT_MEMORIA.md       # Este documento
│   ├── CONVENTIONS.md
│   └── BUILD_PLAN.md
│
├── .github/
│   ├── workflows/
│   │   ├── ci.yml           # lint + format + typecheck + test + build + security
│   │   ├── cd.yml
│   │   └── release.yml
│   ├── dependabot.yml
│   └── CODEOWNERS
│
├── docker-compose.yml          # Redis + MinIO para desarrollo
├── docker-compose.override.yml # Overrides por entorno
├── pyproject.toml              # mypy + coverage + ruff config
├── uv.lock
├── Dockerfile
├── README.md
└── CLAUDE.md
```

---

**Documento generado para la memoria del Trabajo de Fin de Título**

*GeneFlow Analysis v2.0.0*
