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

[![Python 3.12+](https://img.shields.io/badge/Python-3.12+-3776ab?logo=python&logoColor=white)](https://www.python.org/)
[![FastAPI](https://img.shields.io/badge/FastAPI-0.110+-009688?logo=fastapi&logoColor=white)](https://fastapi.tiangolo.com/)
[![Redis](https://img.shields.io/badge/Redis-Streams-dc382d?logo=redis&logoColor=white)](https://redis.io/)
[![License: Proprietary](https://img.shields.io/badge/License-Proprietary-red)]()
[![Status: In Development](https://img.shields.io/badge/Status-In%20Development-orange)]()

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
| `STORAGE_PROVIDER` | `local` or `supabase` | `local` |
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
| `SUPABASE_BUCKET` | Storage bucket name | `datalake` |
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
├── src/
│   ├── main.py              # Entry point, orchestration
│   ├── config.py            # Settings (pydantic-settings)
│   ├── models.py            # Data models
│   ├── consumer.py          # Redis Streams consumer
│   ├── buffer.py            # Event buffer + WAL
│   ├── deduplication.py     # eventId deduplication
│   ├── retry.py             # Retry handler + DLQ
│   ├── api.py               # FastAPI REST endpoints
│   └── storage/
│       ├── base.py          # Abstract interface
│       ├── local.py         # Local filesystem (aiofiles)
│       └── supabase.py      # Supabase Storage
├── tests/
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

## Implementation Status

| Phase | Description | Status |
|-------|-------------|--------|
| 1 | Project setup (uv, structure) | ✅ Done |
| 2 | Config + Models | ⏳ Pending |
| 3 | Storage layer (local provider) | ⏳ Pending |
| 4 | Buffer + WAL | ⏳ Pending |
| 5 | Deduplication | ⏳ Pending |
| 6 | Retry + DLQ | ⏳ Pending |
| 7 | Redis consumer | ⏳ Pending |
| 8 | REST API | ⏳ Pending |
| 9 | Entry point + graceful shutdown | ⏳ Pending |
| 10 | Docker | ⏳ Pending |
| 11 | Tests | ⏳ Pending |
| 12 | Supabase provider | ⏳ Pending |
| 13 | .NET integration test | ⏳ Pending |

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
