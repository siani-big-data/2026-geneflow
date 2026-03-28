<div align="center">

```
 ██████╗ ███████╗███╗   ██╗███████╗███████╗██╗      ██████╗ ██╗    ██╗
██╔════╝ ██╔════╝████╗  ██║██╔════╝██╔════╝██║     ██╔═══██╗██║    ██║
██║  ███╗█████╗  ██╔██╗ ██║█████╗  █████╗  ██║     ██║   ██║██║ █╗ ██║
██║   ██║██╔══╝  ██║╚██╗██║██╔══╝  ██╔══╝  ██║     ██║   ██║██║███╗██║
╚██████╔╝███████╗██║ ╚████║███████╗██║     ███████╗╚██████╔╝╚███╔███╔╝
 ╚═════╝ ╚══════╝╚═╝  ╚═══╝╚══════╝╚═╝     ╚══════╝ ╚═════╝  ╚══╝╚══╝
                    ██████╗  █████╗ ████████╗ █████╗ ██╗      █████╗ ██╗  ██╗███████╗
                    ██╔══██╗██╔══██╗╚══██╔══╝██╔══██╗██║     ██╔══██╗██║ ██╔╝██╔════╝
                   ██║  ██║███████║   ██║   ███████║██║     ███████║█████╔╝ █████╗
                   ██║  ██║██╔══██║   ██║   ██╔══██║██║     ██╔══██║██╔═██╗ ██╔══╝
                    ██████╔╝██║  ██║   ██║   ██║  ██║███████╗██║  ██║██║  ██╗███████╗
                    ╚═════╝ ╚═╝  ╚═╝   ╚═╝   ╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝╚═╝  ╚═╝╚══════╝
```

**Immutable Source of Truth for the GeneFlow Platform**

[![CI](https://github.com/geneflow-app/GeneFlow-Datalake/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/geneflow-app/GeneFlow-Datalake/actions/workflows/ci.yml)
[![CD](https://github.com/geneflow-app/GeneFlow-Datalake/actions/workflows/cd.yml/badge.svg?branch=master)](https://github.com/geneflow-app/GeneFlow-Datalake/actions/workflows/cd.yml)
[![Python](https://img.shields.io/badge/Python-3.12+-3776ab?logo=python&logoColor=white)](https://www.python.org/)
[![FastAPI](https://img.shields.io/badge/FastAPI-0.110+-009688?logo=fastapi&logoColor=white)](https://fastapi.tiangolo.com/)
[![Redis](https://img.shields.io/badge/Redis-Streams-dc382d?logo=redis&logoColor=white)](https://redis.io/)
[![Docker](https://img.shields.io/badge/Docker-GHCR-2496ed?logo=docker&logoColor=white)](https://github.com/geneflow-app/GeneFlow-Datalake/pkgs/container/geneflow-datalake)
[![License](https://img.shields.io/badge/License-Proprietary-red)]()

</div>

---

GeneFlow Datalake is the **event store** that consumes **all** events from the Redis event bus and persists them in JSONL format. It enables full system replay, audit trails, and analytics — acting as the single source of truth for the entire GeneFlow platform.

```
Redis Streams ──► Consumer ──► Buffer + WAL ──► Storage (JSONL/day)
     │                │                              │
     │                ▼                              │
     │         Deduplication                         │
     │                │                              │
     │                ▼                              │
     │         Retry Handler ──► DLQ (failed)        │
     │                                               │
     └───────────────► REST API ◄───────────────────┘
                    /health /events /replay /dlq
```

---

## How It Works

The Datalake operates with **durability-first** architecture and **at-least-once** delivery guarantees.

### Event Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│  1. CONSUME                                                             │
│     Redis XREADGROUP from 9 category streams                            │
│     Consumer Groups for horizontal scaling                              │
├─────────────────────────────────────────────────────────────────────────┤
│  2. DEDUPLICATE                                                         │
│     Check eventId against in-memory set (24h TTL)                       │
│     Duplicates get immediate XACK, skip processing                      │
├─────────────────────────────────────────────────────────────────────────┤
│  3. BUFFER                                                              │
│     Write to WAL first (durability)                                     │
│     Accumulate in memory buffer                                         │
│     Flush on: size limit OR time interval                               │
├─────────────────────────────────────────────────────────────────────────┤
│  4. PERSIST                                                             │
│     Append batch to JSONL file (category/YYYY-MM-DD.jsonl)              │
│     XACK to Redis only after successful write                           │
│     Clear WAL entries                                                   │
├─────────────────────────────────────────────────────────────────────────┤
│  5. RETRY (on failure)                                                  │
│     Exponential backoff: 1s → 2s → 4s → 8s → 16s                        │
│     After max retries → Dead Letter Queue                               │
└─────────────────────────────────────────────────────────────────────────┘
```

### Durability Guarantees

| Guarantee | Implementation |
|-----------|----------------|
| No data loss | WAL written before buffering |
| No duplicates | eventId deduplication with 24h window |
| Ordered within category | Single consumer per category stream |
| Crash recovery | WAL replay on startup |
| Failed event inspection | DLQ with manual replay API |

---

## Quick Start

```bash
# Install uv (if not installed)
# Windows
powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"
# Linux/macOS
curl -LsSf https://astral.sh/uv/install.sh | sh

# Clone and setup
git clone https://github.com/your-org/geneflow-datalake.git
cd geneflow-datalake
uv sync --dev

# Run (uses localhost Redis by default)
uv run datalake

# Verify
curl http://localhost:8080/health
```

---

## Event Categories

The Datalake consumes from **9 Redis Streams**, one per domain category:

| Category | Stream | Events |
|----------|--------|--------|
| `users` | `geneflow:events:users` | Registration, login, profile updates |
| `studies` | `geneflow:events:studies` | Study CRUD operations |
| `traces` | `geneflow:events:traces` | Trace upload, processing, archival |
| `alignments` | `geneflow:events:alignments` | Alignment creation and results |
| `subscriptions` | `geneflow:events:subscriptions` | Subscription lifecycle |
| `plans` | `geneflow:events:plans` | Plan changes, billing events |
| `ai` | `geneflow:events:ai` | GeneFlow AI interactions |
| `blast` | `geneflow:events:blast` | BLAST job submissions and results |
| `system` | `geneflow:events:system` | System-wide events, maintenance |

---

## REST API

### Health & Metrics

```bash
GET /health                        # Service status + consumer metrics
```

### Query Events

```bash
GET /events/{category}                              # Today's events
GET /events/{category}?date=2026-03-25              # Specific date
GET /events/{category}?start_date=...&end_date=...  # Date range
GET /events/{category}?event_type=UserRegistered    # Filter by type
GET /events/{category}?limit=100&offset=0           # Pagination
```

### Replay

```bash
GET /replay/{category}                # All events, chronologically sorted
GET /replay/{category}?from=2026-03-01  # From specific date
```

### Dead Letter Queue

```bash
GET  /dlq                          # Failed events (today)
GET  /dlq/all                      # All failed events
POST /dlq/retry/{event_id}         # Retry single event
POST /dlq/retry-all                # Retry all failed events
```

---

## Storage Format

Events are persisted in **JSONL** (JSON Lines) files, organized by category and date:

```
data/datalake/events/
├── users/
│   ├── 2026-03-24.jsonl
│   └── 2026-03-25.jsonl
├── traces/
│   └── 2026-03-25.jsonl
├── studies/
│   └── 2026-03-25.jsonl
└── ...
```

Each line is a self-contained JSON event:

```json
{
  "eventId": "550e8400-e29b-41d4-a716-446655440000",
  "type": "UserRegistered",
  "category": "users",
  "timestamp": "2026-03-25T10:30:00.000Z",
  "streamId": "1711357800000-0",
  "data": {
    "userId": "user-123",
    "email": "scientist@lab.org"
  },
  "receivedAt": "2026-03-25T10:30:00.150Z"
}
```

---

## Configuration

All settings use the `DATALAKE_` prefix:

| Variable | Description | Default |
|----------|-------------|---------|
| `REDIS_URL` | Redis connection URL | `redis://localhost:6379` |
| `REDIS_CONSUMER_GROUP` | Consumer group name | `datalake-consumers` |
| `REDIS_CONSUMER_NAME` | This consumer's name | `datalake-1` |
| `STORAGE_PROVIDER` | `local`, `minio`, or `supabase` | `local` |
| `LOCAL_STORAGE_PATH` | Path for local storage | `./data/datalake` |
| `BUFFER_MAX_SIZE` | Events before flush | `100` |
| `BUFFER_FLUSH_INTERVAL` | Flush interval (seconds) | `5.0` |
| `WAL_PATH` | Write-Ahead Log path | `./data/wal` |
| `RETRY_MAX_ATTEMPTS` | Retries before DLQ | `5` |
| `DLQ_PATH` | Dead Letter Queue path | `./data/dlq` |
| `API_PORT` | REST API port | `8080` |

<details>
<summary>Full configuration reference</summary>

| Variable | Description | Default |
|----------|-------------|---------|
| `REDIS_BLOCK_MS` | Read timeout (ms) | `5000` |
| `BATCH_SIZE` | Events per read | `50` |
| `SUPABASE_URL` | Supabase project URL | - |
| `SUPABASE_KEY` | Supabase service key | - |
| `SUPABASE_BUCKET` | Storage bucket name | `geneflow-datalake` |
| `MINIO_ENDPOINT` | MinIO/S3 endpoint (host:port) | - |
| `MINIO_ACCESS_KEY` | MinIO/S3 access key | - |
| `MINIO_SECRET_KEY` | MinIO/S3 secret key | - |
| `MINIO_BUCKET` | MinIO/S3 bucket name | `geneflow-datalake` |
| `MINIO_SECURE` | Use HTTPS | `true` |
| `RETRY_BASE_DELAY` | Initial retry delay (s) | `1.0` |
| `RETRY_MAX_DELAY` | Maximum retry delay (s) | `300.0` |
| `DEDUP_TTL_HOURS` | Dedup window | `24` |
| `DEDUP_MAX_SIZE` | Max events in memory | `100000` |
| `API_HOST` | API bind address | `0.0.0.0` |

</details>

---

## Project Structure

```
geneflow-datalake/
├── .github/
│   ├── workflows/
│   │   ├── ci.yml           # Continuous Integration
│   │   ├── cd.yml           # Continuous Deployment
│   │   └── release.yml      # Release automation
│   ├── dependabot.yml       # Dependency updates
│   ├── CODEOWNERS           # Code ownership
│   └── pull_request_template.md
├── src/
│   ├── main.py              # Entry point, orchestration
│   ├── config.py            # Settings (pydantic-settings)
│   ├── models.py            # Data models
│   ├── consumer.py          # Redis Streams consumer
│   ├── buffer.py            # Event buffer + WAL
│   ├── deduplication.py     # eventId deduplication
│   ├── retry.py             # Retry handler + DLQ
│   ├── api.py               # FastAPI REST endpoints
│   ├── mounters/            # Event projections (Postgres, Qdrant, Storage)
│   └── storage/
│       ├── storage.py       # Abstract interface
│       ├── local.py         # Local filesystem (aiofiles)
│       ├── minio.py         # MinIO / S3-compatible (aiobotocore)
│       └── supabase.py      # Supabase Storage (httpx)
├── tests/
├── docs/
│   ├── API_CONVENTIONS.md   # API design standards
│   └── CONVENTIONS.md       # Development conventions
├── pyproject.toml           # Dependencies (uv)
├── Dockerfile
└── README.md
```

---

## Docker

```bash
# Build
docker build -t geneflow-datalake .

# Run
docker run -d \
  -p 8080:8080 \
  -e DATALAKE_REDIS_URL=redis://host.docker.internal:6379 \
  -v datalake-data:/app/data \
  geneflow-datalake
```

### Docker Compose

```yaml
datalake:
  build: ./geneflow-datalake
  ports:
    - "8082:8080"
  environment:
    DATALAKE_REDIS_URL: redis://redis:6379
    DATALAKE_STORAGE_PROVIDER: local
  volumes:
    - datalake-data:/app/data
  depends_on:
    redis:
      condition: service_healthy
  restart: unless-stopped
```

---

## Development

```bash
uv sync --dev           # Install with dev dependencies
uv run datalake         # Run service
uv run pytest           # Run tests
uv run pytest --cov=src # With coverage
uv run ruff check src/  # Lint
uv run ruff format src/ # Format
```

---

## CI/CD

This project uses GitHub Actions for continuous integration and deployment.

### Workflows

| Workflow | Trigger | Description |
|----------|---------|-------------|
| **CI** | Push/PR to `main`, `master`, `develop` | Lint, test, build Docker image, security scan |
| **CD** | Push to `main`/`master` or tags `v*` | Build & push to GHCR, deploy to staging/production |
| **Release** | Tags `v*` | Auto-generate changelog and GitHub release |

### Pipeline Stages

```
┌─────────┐    ┌─────────┐    ┌─────────┐    ┌──────────┐
│  Lint   │───►│  Test   │───►│  Build  │───►│ Security │
│  ruff   │    │ pytest  │    │ Docker  │    │pip-audit │
└─────────┘    └─────────┘    └─────────┘    └──────────┘
                                  │
                                  ▼
                    ┌─────────────────────────┐
                    │   Push to GHCR          │
                    │   (on main/tags)        │
                    └───────────┬─────────────┘
                                │
              ┌─────────────────┴─────────────────┐
              ▼                                   ▼
     ┌─────────────────┐                ┌─────────────────┐
     │ Deploy Staging  │                │ Deploy Prod     │
     │ (main branch)   │                │ (v* tags)       │
     └─────────────────┘                └─────────────────┘
```

### Docker Images

```bash
# Pull latest
docker pull ghcr.io/geneflow-app/geneflow-datalake:master

# Pull specific version
docker pull ghcr.io/geneflow-app/geneflow-datalake:v1.0.0
```

---

## Mounters: Event Projections

Mounters project events from the datalake to external systems, keeping them synchronized in real-time or via replay.

```
┌─────────────────────────────────────────────────────────────────┐
│                    MounterEngine (Orchestrator)                  │
│  • Registers and coordinates mounters                           │
│  • Dispatches events based on category                          │
│  • Manages modes: REPLAY / LIVE / REBUILD                       │
└───────────┬─────────────────┬─────────────────┬─────────────────┘
            │                 │                 │
            ▼                 ▼                 ▼
   ┌─────────────────┐ ┌─────────────┐ ┌─────────────────┐
   │ PostgresMounter │ │QdrantMounter│ │ StorageMounter  │
   │   Relational    │ │   Vector    │ │  Binary Files   │
   │     Tables      │ │  Embeddings │ │   + Chunking    │
   └─────────────────┘ └─────────────┘ └─────────────────┘
```

### PostgresMounter

Projects structured events to PostgreSQL tables organized by domain:

| Handler | Category | Tables |
|---------|----------|--------|
| **UsersHandler** | `users` | `identity.users` |
| **StudiesHandler** | `studies` | `studies.studies`, `studies.members`, `studies.invitations` |
| **TracesHandler** | `traces` | `traces.traces`, `traces.annotations` |
| **AlignmentsHandler** | `alignments` | `alignments.alignments`, `alignments.alignment_traces` |
| **BillingHandler** | `billing` | `billing.plans`, `billing.subscriptions` |

### QdrantMounter

Stores AI embeddings for semantic similarity search:

| Collection | Vector Size | Content |
|------------|-------------|---------|
| `geneflow_sequences` | 768 | Sequence embeddings |
| `geneflow_annotations` | 1536 | Annotation text embeddings |
| `geneflow_traces` | 256 | Trace summary embeddings |

### StorageMounter

Stores trace files with intelligent chunking for efficient access:

```
bucket: geneflow-traces/
└── traces/{trace_id}/
    ├── original.ab1          # Original file
    ├── manifest.json         # Metadata + chunk index
    └── chunks/
        ├── chunk_0000.json   # Bases 0-9999
        ├── chunk_0001.json   # Bases 10000-19999
        └── ...
```

### Operation Modes

| Mode | Description | Use Case |
|------|-------------|----------|
| `REPLAY` | Process historical events from datalake | Initial sync, recovery |
| `REBUILD` | Wipe everything and reprocess from scratch | Schema changes, corruption |
| `LIVE` | Process events in real-time (planned) | Production operation |

### Usage Example

```python
from src.mounters import MounterEngine, MounterMode, PostgresMounter, QdrantMounter

engine = MounterEngine(datalake_path="/data/datalake")
engine.register(PostgresMounter(dsn="postgresql://..."))
engine.register(QdrantMounter(qdrant_url="http://localhost:6333"))

# Replay all events from March
result = await engine.run(
    mode=MounterMode.REPLAY,
    from_date=datetime(2026, 3, 1),
    to_date=datetime(2026, 3, 27),
    categories=["users", "traces"]  # Optional filter
)
print(f"Processed: {result['events_processed']}, Failed: {result['events_failed']}")
```

---

## Documentation

| Document | Description |
|----------|-------------|
| [API Conventions](docs/API_CONVENTIONS.md) | REST API design patterns and standards |
| [Conventions](docs/CONVENTIONS.md) | Architecture, code, and development conventions |

---

## Implementation Status

| Phase | Description | Status |
|-------|-------------|--------|
| 1 | Project setup (uv, structure) | ✅ Done |
| 2 | Config + Models | ✅ Done |
| 3 | Storage layer (local provider) | ✅ Done |
| 4 | Buffer + WAL | ✅ Done |
| 5 | Deduplication | ✅ Done |
| 6 | Retry + DLQ | ✅ Done |
| 7 | Redis consumer | ✅ Done |
| 8 | REST API | ✅ Done |
| 9 | Entry point + graceful shutdown | ✅ Done |
| 10 | Docker | ✅ Done |
| 11 | Tests | ✅ Done |
| 12 | MinIO provider | ✅ Done |
| 13 | Supabase provider | ✅ Done |
| 14 | CI/CD (GitHub Actions) | ✅ Done |
| 15 | .NET integration test | ⏳ Pending |

---

## Compatibility

### .NET Event Dispatcher

Compatible with GeneFlow's `DomainEventDispatcher` publishing format:

```json
{
  "eventId": "guid",
  "type": "EventTypeName",
  "category": "category-name",
  "timestamp": 1711357800000,
  "data": "{\"serialized\":\"json\"}",
  "source": "service-name",
  "version": "1.0",
  "correlationId": "optional-guid"
}
```

---

<div align="center">

**GeneFlow Platform** · Proprietary

</div>
