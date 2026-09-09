<div align="center">

```
 ██████╗ ███████╗███╗   ██╗███████╗███████╗██╗      ██████╗ ██╗    ██╗
██╔════╝ ██╔════╝████╗  ██║██╔════╝██╔════╝██║     ██╔═══██╗██║    ██║
██║  ███╗█████╗  ██╔██╗ ██║█████╗  █████╗  ██║     ██║   ██║██║ █╗ ██║
██║   ██║██╔══╝  ██║╚██╗██║██╔══╝  ██╔══╝  ██║     ██║   ██║██║███╗██║
╚██████╔╝███████╗██║ ╚████║███████╗██║     ███████╗╚██████╔╝╚███╔███╔╝
 ╚═════╝ ╚══════╝╚═╝  ╚═══╝╚══════╝╚═╝     ╚══════╝ ╚═════╝  ╚══╝╚══╝
   █████╗ ███╗   ██╗ █████╗ ██╗  ██╗   ██╗███████╗██╗███████╗
  ██╔══██╗████╗  ██║██╔══██╗██║  ╚██╗ ██╔╝██╔════╝██║██╔════╝
  ███████║██╔██╗ ██║███████║██║   ╚████╔╝ ███████╗██║███████╗
  ██╔══██║██║╚██╗██║██╔══██║██║    ╚██╔╝  ╚════██║██║╚════██║
  ██║  ██║██║ ╚████║██║  ██║███████╗██║   ███████║██║███████║
  ╚═╝  ╚═╝╚═╝  ╚═══╝╚═╝  ╚═╝╚══════╝╚═╝   ╚══════╝╚═╝╚══════╝
```

**Bioinformatics Analysis Engine for the GeneFlow Platform**

[![Python 3.12+](https://img.shields.io/badge/Python-3.12+-3776ab?logo=python&logoColor=white)](https://www.python.org/)
[![Redis](https://img.shields.io/badge/Redis-Streams-dc382d?logo=redis&logoColor=white)](https://redis.io/)
[![Biopython](https://img.shields.io/badge/Biopython-1.84+-3776ab)](https://biopython.org/)
[![License: Proprietary](https://img.shields.io/badge/License-Proprietary-red)]()

</div>

---

GeneFlow Analysis is the **bioinformatics analysis engine** that processes sequencing traces, performs alignments, and executes advanced DNA analysis. It consumes jobs from Redis Streams and publishes results as events.

```
Redis Streams ──► Workers ──► Parsers/Analyzers ──► Storage
     │               │                                 │
     │               ▼                                 │
     │         TraceWorker                             │
     │         AlignmentWorker                         │
     │         AnalysisWorker                          │
     │               │                                 │
     │               ▼                                 │
     └───────► Event Bus (results) ◄──────────────────┘
           :events:traces :events:alignments :events:analysis
                           │
                           ▼
                    GET /health ◄── Docker/K8s healthcheck
```

---

## How It Works

The Worker operates with **job-based** architecture consuming from Redis Streams.

### Processing Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│  1. CONSUME                                                             │
│     Redis XREADGROUP from job streams                                   │
│     Consumer Groups for horizontal scaling                              │
├─────────────────────────────────────────────────────────────────────────┤
│  2. DOWNLOAD                                                            │
│     Fetch trace file from Storage Provider                              │
│     Support: Local, HTTP, Supabase                                      │
├─────────────────────────────────────────────────────────────────────────┤
│  3. PARSE                                                               │
│     Extract sequence + quality scores                                   │
│     AB1/SCF: chromatogram data                                          │
│     FASTQ: quality scores                                               │
│     FASTA: sequence only                                                │
├─────────────────────────────────────────────────────────────────────────┤
│  4. ANALYZE                                                             │
│     Quality metrics (Q20, Q30, GC%)                                     │
│     Trimming (Modified Mott, Sliding Window)                            │
│     Heterozygote detection (chromatogram)                               │
│     Motif search, Translation, ORFs, Restriction                        │
├─────────────────────────────────────────────────────────────────────────┤
│  5. PUBLISH                                                             │
│     Emit result events to Redis Streams                                 │
│     XACK job only after successful processing                           │
└─────────────────────────────────────────────────────────────────────────┘
```

### Supported Formats

| Format | Extensions | Chromatogram | Quality | Parser |
|--------|------------|--------------|---------|--------|
| AB1 | `.ab1`, `.abi` | Yes | Yes | AB1Parser |
| SCF | `.scf` | Yes | Yes | SCFParser |
| FASTQ | `.fastq`, `.fq` | No | Yes | FASTQParser |
| FASTA | `.fasta`, `.fa` | No | No | FASTAParser |

---

## Quick Start

```bash
# Install uv (if not installed)
# Windows
powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"
# Linux/macOS
curl -LsSf https://astral.sh/uv/install.sh | sh

# Clone and setup
git clone https://github.com/your-org/geneflow-worker.git
cd geneflow-worker
uv sync --dev

# Run (uses localhost Redis by default)
uv run python -m src.main

# Verify health
curl http://localhost:8080/health

# Publish a test job
redis-cli XADD geneflow:jobs:traces '*' data '{"traceId":"test","fileName":"sample.ab1"}'
```

---

## Workers

The system has **3 specialized workers**, each consuming from its own stream:

| Worker | Stream | Description |
|--------|--------|-------------|
| `TraceWorker` | `geneflow:jobs:traces` | Parse files, extract sequences, compute quality |
| `AlignmentWorker` | `geneflow:jobs:alignments` | Pairwise/Multiple alignment, consensus, variants |
| `AnalysisWorker` | `geneflow:jobs:analysis` | Trimming, heterozygotes, motifs, ORFs, restriction |

### Job Examples

```bash
# Trace processing
redis-cli XADD geneflow:jobs:traces '*' data '{
  "traceId": "trace-123",
  "studyId": "study-456",
  "fileName": "sample.ab1",
  "storagePath": "traces/sample.ab1",
  "formatName": "AB1"
}'

# Pairwise alignment
redis-cli XADD geneflow:jobs:alignments '*' data '{
  "alignmentId": "align-123",
  "typeId": 1,
  "traceIds": ["trace-1", "trace-2"],
  "options": {"generateConsensus": true}
}'

# Trimming analysis
redis-cli XADD geneflow:jobs:analysis '*' data '{
  "traceId": "trace-123",
  "analysisType": "trimming",
  "options": {"algorithm": "modified_mott"}
}'
```

---

## Analyzers

### Available Analyzers

| Analyzer | Description |
|----------|-------------|
| QualityAnalyzer | Q20/Q30 percentages, GC content, average quality |
| TrimmingAnalyzer | Modified Mott, Sliding Window, Quality Threshold |
| HeterozygoteDetector | Peak ratio analysis, IUPAC codes |
| MotifSearcher | Exact, IUPAC, Regex pattern matching |
| TranslationAnalyzer | 6 reading frames, multiple genetic codes |
| ORFDetector | Configurable start/stop codons |
| RestrictionAnalyzer | Common enzymes, digest simulation |

### Trimming Algorithms

| Algorithm | Description |
|-----------|-------------|
| `modified_mott` | Error probability based (recommended) |
| `sliding_window` | Window average quality threshold |
| `quality_threshold` | Simple per-base cutoff |

---

## Alignment

### Alignment Types

| Type | Aligner | Algorithm |
|------|---------|-----------|
| Pairwise | PairwiseAligner | Needleman-Wunsch |
| Multiple | MultipleAligner | Progressive alignment |

### Additional Components

| Component | Description |
|-----------|-------------|
| ConsensusBuilder | Generate consensus from aligned sequences |
| VariantDetector | Detect SNPs and variations |

---

## Event Bus

Events are published to Redis Streams after processing:

| Category | Stream | Events |
|----------|--------|--------|
| `traces` | `geneflow:events:traces` | `TraceProcessed`, `TraceProcessingFailed` |
| `alignments` | `geneflow:events:alignments` | `AlignmentCompleted`, `AlignmentFailed` |
| `analysis` | `geneflow:events:analysis` | `TrimmingCompleted`, `HeterozygoteDetectionCompleted`, ... |
| `system` | `geneflow:events:system` | `WorkerStarted`, `WorkerStopped` |

---

## Health API

Minimal HTTP endpoint for Docker/Kubernetes health checks:

```bash
GET /health
```

### Response

```json
{
  "status": "healthy",
  "workers": {
    "trace": {"running": true, "jobsProcessed": 142},
    "alignment": {"running": true, "jobsProcessed": 38},
    "analysis": {"running": true, "jobsProcessed": 56}
  },
  "redis": "connected",
  "storage": "healthy",
  "uptime": 3600
}
```

### Status Values

| Status | Description |
|--------|-------------|
| `healthy` | All workers running, Redis connected |
| `degraded` | Some workers stopped or storage issues |
| `unhealthy` | Redis disconnected or critical failure |

---

## Configuration

All settings use the `WORKER_` prefix:

| Variable | Description | Default |
|----------|-------------|---------|
| `REDIS_URL` | Redis connection URL | `redis://localhost:6379` |
| `REDIS_CONSUMER_GROUP` | Consumer group name | `geneflow-worker-consumers` |
| `REDIS_CONSUMER_NAME` | This consumer's name | `worker-1` |
| `STORAGE_PROVIDER` | `local`, `http`, or `supabase` | `local` |
| `LOCAL_STORAGE_PATH` | Path for local storage | `./data/worker` |
| `API_HOST` | Health API bind address | `0.0.0.0` |
| `API_PORT` | Health API port | `8080` |
| `TRACE_WORKER_ENABLED` | Enable trace processing | `true` |
| `ALIGNMENT_WORKER_ENABLED` | Enable alignment processing | `true` |
| `ANALYSIS_WORKER_ENABLED` | Enable analysis processing | `true` |
| `LOG_LEVEL` | Logging level | `INFO` |

<details>
<summary>Full configuration reference</summary>

| Variable | Description | Default |
|----------|-------------|---------|
| `REDIS_BLOCK_MS` | Read timeout (ms) | `5000` |
| `SUPABASE_URL` | Supabase project URL | - |
| `SUPABASE_KEY` | Supabase service key | - |
| `EVENTBUS_ENABLED` | Publish events | `true` |
| `EVENTBUS_STREAM_PREFIX` | Event stream prefix | `geneflow:events` |
| `EVENTBUS_MAX_STREAM_LENGTH` | Max events per stream | `100000` |
| `MAX_RETRIES` | Job retry attempts | `3` |
| `RETRY_DELAY_SECONDS` | Delay between retries | `5` |
| `TEMP_DIR` | Temporary file directory | `/tmp/geneflow-worker` |

</details>

---

## Project Structure

```
geneflow-worker/
├── src/
│   ├── main.py              # Entry point, orchestration
│   ├── config.py            # Settings (pydantic-settings)
│   ├── models.py            # Domain models (dataclasses)
│   ├── api.py               # Health API (FastAPI)
│   ├── parsers/
│   │   ├── parser.py        # BaseParser ABC
│   │   ├── ab1.py           # AB1Parser
│   │   ├── scf.py           # SCFParser
│   │   ├── fastq.py         # FASTQParser
│   │   └── fasta.py         # FASTAParser
│   ├── analyzers/
│   │   ├── analyzer.py      # BaseAnalyzer ABC
│   │   ├── quality.py       # QualityAnalyzer
│   │   ├── trimming.py      # TrimmingAnalyzer
│   │   ├── heterozygote.py  # HeterozygoteDetector
│   │   ├── motif.py         # MotifSearcher
│   │   ├── translation.py   # TranslationAnalyzer
│   │   ├── orf.py           # ORFDetector
│   │   └── restriction.py   # RestrictionAnalyzer
│   ├── alignment/
│   │   ├── aligner.py       # BaseAligner ABC
│   │   ├── pairwise.py      # PairwiseAligner
│   │   ├── multiple.py      # MultipleAligner
│   │   ├── consensus.py     # ConsensusBuilder
│   │   └── variants.py      # VariantDetector
│   ├── workers/
│   │   ├── worker.py        # BaseWorker ABC
│   │   ├── trace.py         # TraceWorker
│   │   ├── alignment.py     # AlignmentWorker
│   │   └── analysis.py      # AnalysisWorker
│   ├── events/
│   │   ├── events.py        # Event definitions
│   │   └── publisher.py     # EventBusPublisher
│   └── storage/
│       ├── storage.py       # StorageProvider ABC
│       ├── local.py         # LocalStorageProvider
│       ├── http.py          # HttpStorageProvider
│       └── supabase.py      # SupabaseStorageProvider
├── tests/
│   ├── conftest.py          # Shared fixtures
│   ├── test_parsers.py
│   ├── test_analyzers.py
│   ├── test_alignment.py
│   ├── test_workers.py
│   └── fixtures/            # Test files
├── pyproject.toml           # Dependencies (uv)
├── Dockerfile
└── README.md
```

---

## Docker

```bash
# Build
docker build -t geneflow-worker .

# Run
docker run -d \
  -p 8080:8080 \
  -e WORKER_REDIS_URL=redis://host.docker.internal:6379 \
  -v worker-data:/app/data \
  geneflow-worker

# Verify
curl http://localhost:8080/health
```

### Docker Compose

```yaml
worker:
  build: ./geneflow-worker
  ports:
    - "8083:8080"
  environment:
    WORKER_REDIS_URL: redis://redis:6379
    WORKER_STORAGE_PROVIDER: local
  volumes:
    - worker-data:/app/data
  depends_on:
    redis:
      condition: service_healthy
  healthcheck:
    test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
    interval: 30s
    timeout: 10s
    retries: 3
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
| 0 | Project setup (uv, structure) | ✅ Complete |
| 1 | Config + Models + Events | ✅ Complete |
| 2 | Parsers (AB1, SCF, FASTQ, FASTA) | ✅ Complete |
| 3 | Analyzers basic (Quality, Trimming) | ✅ Complete |
| 4 | Alignment module | ✅ Complete |
| 5 | Workers + Health API | ✅ Complete |
| 6 | Analyzers advanced | ✅ Complete |
| 7 | Storage providers | ✅ Complete |
| 8 | Release v2.0.0 | ✅ Complete |

**213 tests passing**

---

<div align="center">

**GeneFlow Platform** · Proprietary

</div>
