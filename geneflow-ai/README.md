<div align="center">

```
 ██████╗ ███████╗███╗   ██╗███████╗███████╗██╗      ██████╗ ██╗    ██╗
██╔════╝ ██╔════╝████╗  ██║██╔════╝██╔════╝██║     ██╔═══██╗██║    ██║
██║  ███╗█████╗  ██╔██╗ ██║█████╗  █████╗  ██║     ██║   ██║██║ █╗ ██║
██║   ██║██╔══╝  ██║╚██╗██║██╔══╝  ██╔══╝  ██║     ██║   ██║██║███╗██║
╚██████╔╝███████╗██║ ╚████║███████╗██║     ███████╗╚██████╔╝╚███╔███╔╝
 ╚═════╝ ╚══════╝╚═╝  ╚═══╝╚══════╝╚═╝     ╚══════╝ ╚═════╝  ╚══╝╚══╝
                           █████╗ ██╗
                          ██╔══██╗██║
                          ███████║██║
                          ██╔══██║██║
                          ██║  ██║██║
                          ╚═╝  ╚═╝╚═╝
```

**Intelligent Sequence Analysis for the GeneFlow Platform**

[![Python](https://img.shields.io/badge/Python-3.12+-3776ab?logo=python&logoColor=white)](https://www.python.org/)
[![FastAPI](https://img.shields.io/badge/FastAPI-0.115+-009688?logo=fastapi&logoColor=white)](https://fastapi.tiangolo.com/)
[![Redis](https://img.shields.io/badge/Redis-Streams-dc382d?logo=redis&logoColor=white)](https://redis.io/)
[![Anthropic](https://img.shields.io/badge/Claude-API-cc785c?logo=anthropic&logoColor=white)](https://anthropic.com/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ed?logo=docker&logoColor=white)]()
[![License](https://img.shields.io/badge/License-Proprietary-red)]()

</div>

---

GeneFlow AI is the **intelligent analysis layer** that provides AI-powered sequence analysis, automated annotations, and conversational assistance. It integrates Claude API for natural language interactions, NCBI BLAST for sequence similarity search, and custom ML models for quality prediction.

```
                    ┌─────────────────────────────────────┐
                    │           GeneFlow AI               │
                    │                                     │
                    │   Copilot  │  BLAST  │  ML Models   │
                    │                                     │
                    └──────────────────┬──────────────────┘
                                       │
                 ┌─────────────────────┼─────────────────────┐
                 │                     │                     │
                 ▼                     ▼                     ▼
        ┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐
        │  Redis Streams  │   │    REST API     │   │     Qdrant      │
        │   (Event Bus)   │   │   (Endpoints)   │   │   (Embeddings)  │
        └─────────────────┘   └─────────────────┘   └─────────────────┘
```

---

## Features

### Copilot (Claude API)

| Feature | Description |
|---------|-------------|
| **Chat Assistant** | Natural language Q&A about sequence analysis results |
| **Report Generation** | Automated technical reports in Markdown |
| **Interpretation** | Plain-language explanations of complex findings |
| **Recommendations** | Context-aware suggestions for next steps |

### BLAST Integration

| Feature | Description |
|---------|-------------|
| **NCBI BLAST Search** | Async job submission with polling |
| **Multiple Databases** | Support for nt, nr, refseq, and custom databases |
| **Result Parsing** | Structured hit extraction with scores and alignments |
| **Organism Identification** | Automatic species prediction from top hits |

### Quality Analysis

| Feature | Description |
|---------|-------------|
| **Quality Prediction** | ML-based Q20/Q30 estimation |
| **Artifact Detection** | CNN detection of dye blobs and pull-ups |
| **Auto-Trimming** | Intelligent trim point suggestion |
| **Region Flagging** | Identification of low-quality regions |

### Variant Detection

| Feature | Description |
|---------|-------------|
| **SNP Calling** | Automatic polymorphism detection |
| **Heterozygote Detection** | Double-peak identification |
| **Clinical Lookup** | ClinVar/dbSNP integration for significance |
| **Impact Prediction** | Functional effect annotation |

### Annotations

| Feature | Description |
|---------|-------------|
| **Gene Identification** | Ensembl API integration |
| **ORF Detection** | Open reading frame prediction |
| **Motif Search** | Promoter and regulatory element detection |
| **Domain Analysis** | InterPro/Pfam protein domain lookup |

---

## How It Works

GeneFlow AI integrates with the platform via **Redis Streams**, automatically triggering analysis when traces are processed.

```
┌─────────────────────────────────────────────────────────────────────────┐
│  1. TRIGGER                                                             │
│     Consume TraceProcessedEvent from geneflow:events:traces             │
│     Extract sequence, quality scores, metadata                          │
├─────────────────────────────────────────────────────────────────────────┤
│  2. ANALYZE                                                             │
│     Run requested analysis: Quality, BLAST, Variants, Annotations       │
│     Call external APIs as needed (NCBI, Ensembl, ClinVar)               │
├─────────────────────────────────────────────────────────────────────────┤
│  3. INTERPRET                                                           │
│     Copilot generates natural language summary                          │
│     Recommendations based on findings                                   │
├─────────────────────────────────────────────────────────────────────────┤
│  4. PUBLISH                                                             │
│     Emit AIAnalysisCompletedEvent to geneflow:events:ai                 │
│     Store embeddings in Qdrant via Datalake                             │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Quick Start

```bash
# Install uv (if not installed)
# Windows
powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"
# Linux/macOS
curl -LsSf https://astral.sh/uv/install.sh | sh

# Clone and setup
git clone https://github.com/your-org/geneflow-ai.git
cd geneflow-ai
uv sync --dev

# Configure API keys
cp .env.example .env
# Edit .env with your CLAUDE_API_KEY and BLAST_EMAIL

# Run (uses localhost Redis by default)
uv run python -m src.main

# Verify
curl http://localhost:8090/health
```

---

## REST API

### Health & Metrics

```bash
GET /health                        # Service status + component health
GET /models                        # List available AI models
```

### Analysis

```bash
POST /analyze                      # Full AI analysis
GET  /analyze/{analysis_id}        # Get analysis result
GET  /analyze/{analysis_id}/status # Check analysis status
POST /analyze/blast                # BLAST search only
POST /analyze/quality              # Quality prediction only
```

### Copilot

```bash
POST /copilot/ask                  # Ask question about analysis
POST /copilot/report               # Generate analysis report
GET  /copilot/conversations/{id}   # Get conversation history
DELETE /copilot/conversations/{id} # Clear conversation
```

### Search (Semantic)

```bash
POST /search/sequences             # Find similar sequences
POST /search/annotations           # Search by annotation text
GET  /search/similar/{trace_id}    # Find similar to specific trace
```

---

## Request/Response Examples

### Full Analysis

```bash
POST /analyze
Content-Type: application/json
X-API-Key: your-api-key

{
  "traceId": "tr-123",
  "studyId": "st-456",
  "ownerId": "user-789",
  "sequence": "ATGCGATCGATCG...",
  "qualityScores": [35, 38, 40, ...],
  "analysisTypes": ["quality", "blast", "variants", "copilot"],
  "organism": "Homo sapiens"
}
```

```json
{
  "analysisId": "ana-abc-123",
  "traceId": "tr-123",
  "status": "completed",
  "processingTimeMs": 12500,
  "overallConfidence": 0.89,
  "quality": {
    "predictedAccuracy": 0.96,
    "suggestedTrimStart": 25,
    "suggestedTrimEnd": 850,
    "lowQualityRegions": [[0, 25], [850, 875]]
  },
  "blastHits": [
    {
      "accession": "NM_001234.5",
      "description": "Homo sapiens gene XYZ",
      "eValue": 1e-150,
      "identity": 99.2,
      "organism": "Homo sapiens"
    }
  ],
  "variants": [
    {
      "position": 342,
      "referenceBase": "A",
      "alternateBase": "G",
      "variantType": "SNP",
      "clinicalSignificance": "benign"
    }
  ],
  "summary": "High-quality sequence with 99.2% identity to human gene XYZ. One benign SNP detected at position 342.",
  "recommendations": [
    "Trim first 25 bases for optimal quality",
    "Consider Sanger validation for the SNP at position 342"
  ]
}
```

### Copilot Chat

```bash
POST /copilot/ask
Content-Type: application/json
X-API-Key: your-api-key

{
  "conversationId": "conv-123",
  "analysisId": "ana-abc-123",
  "question": "What is the significance of the SNP found at position 342?"
}
```

```json
{
  "answer": "The SNP at position 342 (A→G) is classified as benign in ClinVar. This is a synonymous variant that doesn't change the amino acid sequence. It has been observed in 12% of the general population and is not associated with any known disease phenotype.",
  "sources": ["ClinVar", "dbSNP"],
  "confidence": 0.95
}
```

---

## Event Bus Integration

### Events Consumed

| Stream | Event | Action |
|--------|-------|--------|
| `geneflow:events:traces` | `TraceProcessedEvent` | Trigger automatic AI analysis |
| `geneflow:events:alignments` | `AlignmentCompletedEvent` | Analyze alignment results |

### Events Published

| Event | Description |
|-------|-------------|
| `AIAnalysisStartedEvent` | Analysis job started |
| `AIAnalysisCompletedEvent` | Analysis completed successfully |
| `AIAnalysisFailedEvent` | Analysis failed with error |
| `AIBlastCompletedEvent` | BLAST search completed |
| `AIEmbeddingGeneratedEvent` | Embedding stored in Qdrant |

### Event Payload Example

```json
{
  "eventId": "evt-789",
  "type": "AIAnalysisCompletedEvent",
  "category": "ai",
  "timestamp": 1711357800000,
  "data": {
    "traceId": "tr-123",
    "studyId": "st-456",
    "analysisId": "ana-abc-123",
    "overallConfidence": 0.89,
    "processingTimeMs": 12500,
    "summary": "High-quality sequence identified as human gene XYZ"
  },
  "source": "geneflow-ai",
  "version": "1.0"
}
```

---

## Configuration

All settings use the `AI_` prefix:

| Variable | Description | Default |
|----------|-------------|---------|
| `REDIS_URL` | Redis connection URL | `redis://localhost:6379` |
| `REDIS_CONSUMER_GROUP` | Consumer group name | `ai-consumers` |
| `API_PORT` | REST API port | `8090` |
| `CLAUDE_API_KEY` | Anthropic API key | - |
| `CLAUDE_MODEL` | Claude model to use | `claude-sonnet-4-20250514` |
| `BLAST_EMAIL` | Email for NCBI BLAST | - |
| `BLAST_API_KEY` | NCBI API key (optional) | - |

<details>
<summary>Full configuration reference</summary>

| Variable | Description | Default |
|----------|-------------|---------|
| `REDIS_CONSUMER_NAME` | This consumer's name | `ai-1` |
| `REDIS_BLOCK_MS` | Read timeout (ms) | `5000` |
| `API_HOST` | API bind address | `0.0.0.0` |
| `API_KEY` | API authentication key | - |
| `CORS_ORIGINS` | Allowed CORS origins | `["*"]` |
| `LOG_LEVEL` | Logging level | `INFO` |
| `LOG_REQUESTS` | Log HTTP requests | `true` |
| `CLAUDE_MAX_TOKENS` | Max response tokens | `4096` |
| `BLAST_BASE_URL` | NCBI BLAST endpoint | `https://blast.ncbi.nlm.nih.gov/Blast.cgi` |
| `EVENTBUS_STREAM_PREFIX` | Redis stream prefix | `geneflow:events` |
| `EVENTBUS_ENABLED` | Enable event bus | `true` |
| `SUBSCRIBED_CATEGORIES` | Categories to consume | `["traces", "alignments"]` |
| `ANALYSIS_TIMEOUT_SECONDS` | Max analysis time | `300` |
| `MAX_SEQUENCE_LENGTH` | Max sequence length | `100000` |
| `QDRANT_URL` | Qdrant server URL | `http://localhost:6333` |
| `CACHE_TTL_HOURS` | Result cache TTL | `24` |

</details>

---

## Project Structure

```
geneflow-ai/
├── src/
│   ├── __init__.py
│   ├── main.py              # Entry point, AIService
│   ├── api.py               # FastAPI REST endpoints
│   ├── config.py            # Settings (pydantic-settings)
│   ├── models.py            # Domain models (dataclasses)
│   │
│   ├── eventbus/            # Redis Streams integration
│   │   ├── consumer.py      # Event consumer
│   │   ├── publisher.py     # Event publisher
│   │   └── events.py        # AI event definitions
│   │
│   ├── orchestrator/        # Analysis coordination
│   │   ├── orchestrator.py  # Main orchestrator
│   │   └── context.py       # Analysis context
│   │
│   ├── copilot/             # Claude API integration
│   │   ├── client.py        # Anthropic client
│   │   ├── chat.py          # Conversation handler
│   │   ├── prompts.py       # Prompt templates
│   │   └── reports.py       # Report generator
│   │
│   ├── blast/               # NCBI BLAST integration
│   │   ├── client.py        # BLAST API client
│   │   ├── parser.py        # Result parser
│   │   └── models.py        # BLAST models
│   │
│   ├── external/            # External API clients
│   │   ├── base.py          # Base client class
│   │   ├── ensembl.py       # Ensembl REST
│   │   ├── clinvar.py       # ClinVar/dbSNP
│   │   ├── interpro.py      # InterPro/Pfam
│   │   └── viennarna.py     # ViennaRNA wrapper
│   │
│   ├── analysis/            # Analysis services
│   │   ├── quality.py       # Quality prediction
│   │   ├── variants.py      # Variant detection
│   │   ├── annotations.py   # Annotation service
│   │   └── clustering.py    # Clustering/phylogenetics
│   │
│   ├── ml/                  # Custom ML models
│   │   ├── base.py          # Model interface
│   │   ├── quality_predictor.py
│   │   ├── artifact_detector.py
│   │   └── auto_trimmer.py
│   │
│   └── storage/             # Storage services
│       ├── cache.py         # Redis cache
│       └── embeddings.py    # Qdrant embeddings
│
├── tests/
│   ├── conftest.py
│   ├── test_api.py
│   ├── test_config.py
│   ├── copilot/
│   └── blast/
│
├── docs/
│   ├── geneflow-ecosystem-context.md
│   ├── MODULO_AI.md
│   ├── CONVENTIONS.md
│   └── API.md
│
├── pyproject.toml
├── pytest.ini
├── Dockerfile
├── .env.example
└── README.md
```

---

## Docker

```bash
# Build
docker build -t geneflow-ai .

# Run
docker run -d \
  -p 8090:8090 \
  -e AI_REDIS_URL=redis://host.docker.internal:6379 \
  -e AI_CLAUDE_API_KEY=your-key \
  -e AI_BLAST_EMAIL=your@email.com \
  geneflow-ai
```

### Docker Compose

```yaml
ai:
  build: ./geneflow-ai
  ports:
    - "8090:8090"
  environment:
    AI_REDIS_URL: redis://redis:6379
    AI_CLAUDE_API_KEY: ${CLAUDE_API_KEY}
    AI_BLAST_EMAIL: ${BLAST_EMAIL}
    AI_QDRANT_URL: http://qdrant:6333
  depends_on:
    redis:
      condition: service_healthy
    qdrant:
      condition: service_healthy
  restart: unless-stopped
```

---

## Development

```bash
uv sync --dev              # Install with dev dependencies
uv run python -m src.main  # Run service
uv run pytest              # Run tests
uv run pytest --cov=src    # With coverage
uv run ruff check src/     # Lint
uv run ruff format src/    # Format
```

---

## Implementation Status

| Phase | Description | Status |
|-------|-------------|--------|
| 0 | Project setup, config, models | ⏳ In Progress |
| 1 | MVP: Copilot + BLAST | ⏳ Pending |
| 2 | External APIs: Ensembl, ClinVar, InterPro | ⏳ Pending |
| 3 | Custom ML models | ⏳ Pending |
| 4 | Clustering and phylogenetics | ⏳ Pending |
| 5 | Embeddings and semantic search | ⏳ Pending |
| 6 | Optimization and production | ⏳ Pending |

---

## Dependencies

### Core (Phase 0-1)

```toml
dependencies = [
    "fastapi>=0.115.0",
    "uvicorn[standard]>=0.32.0",
    "pydantic>=2.0.0",
    "pydantic-settings>=2.0.0",
    "redis>=5.0.0",
    "structlog>=24.0.0",
    "httpx>=0.27.0",
    "anthropic>=0.40.0",
]
```

### Extended (Phase 2+)

```toml
# Bioinformatics
"biopython>=1.84",

# ML (optional)
"scikit-learn>=1.5.0",
"xgboost>=2.1.0",
"torch>=2.4.0",
"scipy>=1.14.0",

# Vector DB
"qdrant-client>=1.12.0",
```

---

## Architecture Decision Records

| ADR | Decision |
|-----|----------|
| ADR-001 | Use Claude API for conversational AI (vs. local LLM) |
| ADR-002 | Use NCBI BLAST API (vs. local BLAST+) |
| ADR-003 | Event-driven triggers (vs. polling) |
| ADR-004 | Qdrant for vector similarity search |

---

## Documentation

| Document | Description |
|----------|-------------|
| [Ecosystem Context](src/docs/geneflow-ecosystem-context.md) | Platform architecture overview |
| [Module Specification](src/docs/MODULO_AI.md) | AI module requirements |
| [Conventions](src/docs/CONVENTIONS.md) | Development standards |
| [Development Plan](PLAN_DESARROLLO.md) | Implementation roadmap |

---

<div align="center">

**GeneFlow Platform** · Proprietary

</div>
