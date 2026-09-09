<div align="center">

```
 ██████╗ ███████╗███╗   ██╗███████╗███████╗██╗      ██████╗ ██╗    ██╗
██╔════╝ ██╔════╝████╗  ██║██╔════╝██╔════╝██║     ██╔═══██╗██║    ██║
██║  ███╗█████╗  ██╔██╗ ██║█████╗  █████╗  ██║     ██║   ██║██║ █╗ ██║
██║   ██║██╔══╝  ██║╚██╗██║██╔══╝  ██╔══╝  ██║     ██║   ██║██║███╗██║
╚██████╔╝███████╗██║ ╚████║███████╗██║     ███████╗╚██████╔╝╚███╔███╔╝
 ╚═════╝ ╚══════╝╚═╝  ╚═══╝╚══════╝╚═╝     ╚══════╝ ╚═════╝  ╚══╝╚══╝
             ██████╗  █████╗  ██████╗██╗  ██╗███████╗███╗   ██╗██████╗
             ██╔══██╗██╔══██╗██╔════╝██║ ██╔╝██╔════╝████╗  ██║██╔══██╗
             ██████╔╝███████║██║     █████╔╝ █████╗  ██╔██╗ ██║██║  ██║
             ██╔══██╗██╔══██║██║     ██╔═██╗ ██╔══╝  ██║╚██╗██║██║  ██║
             ██████╔╝██║  ██║╚██████╗██║  ██╗███████╗██║ ╚████║██████╔╝
             ╚═════╝ ╚═╝  ╚═╝ ╚═════╝╚═╝  ╚═╝╚══════╝╚═╝  ╚═══╝╚═════╝
```

**Event-Driven .NET 8 API for the GeneFlow Platform**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-Streams-dc382d?logo=redis&logoColor=white)](https://redis.io/)
[![Docker](https://img.shields.io/badge/Docker-GHCR-2496ed?logo=docker&logoColor=white)](https://github.com/geneflow-app/geneflow-backend/pkgs/container/geneflow-api)
[![License](https://img.shields.io/badge/License-Proprietary-red)]()

</div>

---

GeneFlow Backend is the **core API** that produces **all** domain events to the Redis event bus. It handles authentication, study management, trace processing coordination, and subscription billing — acting as the primary command gateway for the entire GeneFlow platform.

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                    EVENT BUS (Redis Streams)                             │
│  ┌─────────────────────────────────────────────────────────────────────────────────────┐│
│  │                           geneflow:events:{category}                                ││
│  │  users, studies, traces, alignments, subscriptions, plans, ai, blast, system       ││
│  └─────────────────────────────────────────────────────────────────────────────────────┘│
└───────────┬─────────────────────┬─────────────────────┬─────────────────────┬───────────┘
            │                     │                     │                     │
      send/subscribe        send/subscribe        send/subscribe         subscribe
            │                     │                     │                     │
            ▼                     ▼                     ▼                     ▼
┌───────────────────┐  ┌───────────────────┐  ┌───────────────────┐  ┌───────────────────┐
│   GENEFLOW API    │  │  GENEFLOW WORKER  │  │   GENEFLOW AI     │  │ GENEFLOW DATALAKE │
│   (.NET 8)        │  │  (Python)         │  │   (Python)        │  │   (Python)        │
├───────────────────┤  ├───────────────────┤  ├───────────────────┤  ├───────────────────┤
│                   │  │                   │  │                   │  │                   │
│ Produce events    │  │ • TraceProcessor  │  │ • Embeddings      │  │ SOURCE OF TRUTH   │
│ for ALL domain    │  │ • AlignmentProc.  │  │ • Vector Search   │  │                   │
│ operations        │  │ • BlastProcessor  │  │ • Similarity      │  │ Persists ALL      │
│                   │  │                   │  │ • Claude API      │  │ events (JSONL)    │
│                   │  │                   │  │                   │  │                   │
│                   │  │                   │  │                   │  │ Immutable         │
│                   │  │                   │  │                   │  │ Append-only       │
└───────────────────┘  └───────────────────┘  └───────────────────┘  └───────────────────┘
                                                                              │
                                                                              ▼
       ┌──────────────────────────────────────────────────────────────────────────────────┐
       │                                   MOUNTERS                                        │
       │           Read events from Datalake → Transform → Materialize to targets         │
       └──────────┬───────────────────────┬───────────────────────┬───────────────────────┘
                  │                       │                       │
                  ▼                       ▼                       ▼
       ┌───────────────────┐   ┌───────────────────┐   ┌───────────────────┐
       │    PostgreSQL     │   │      Qdrant       │   │   Storage/Files   │
       │    (Datamart)     │   │  (Vector DB)      │   │    (Datamart)     │
       ├───────────────────┤   ├───────────────────┤   ├───────────────────┤
       │                   │   │                   │   │                   │
       │ identity.users    │   │ geneflow_sequences│   │ traces/{id}/      │
       │ studies.studies   │   │ geneflow_annotations│ │   ├─ original.ab1 │
       │ studies.members   │   │ geneflow_traces   │   │   ├─ manifest.json│
       │ traces.traces     │   │                   │   │   └─ chunks/      │
       │ alignments.*      │   │ Semantic search   │   │                   │
       │ billing.*         │   │ Similarity match  │   │ alignments/{id}/  │
       │                   │   │                   │   │   └─ chunks/      │
       │ DISPOSABLE        │   │ DISPOSABLE        │   │                   │
       │ RECONSTRUCTIBLE   │   │ RECONSTRUCTIBLE   │   │ DISPOSABLE        │
       └───────────────────┘   └───────────────────┘   └───────────────────┘
```

---

## How It Works

The API operates with **event-first** architecture and **CQRS** pattern.

### Request Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│  1. RECEIVE REQUEST                                                      │
│     Minimal API endpoint validates JWT token                            │
│     Maps request to Command/Query                                        │
├─────────────────────────────────────────────────────────────────────────┤
│  2. DISPATCH via MediatR                                                 │
│     Pipeline behaviors: Validation, Logging, SubscriptionLimits         │
│     Handler executes business logic                                      │
├─────────────────────────────────────────────────────────────────────────┤
│  3. DOMAIN OPERATIONS                                                    │
│     Aggregate validates invariants                                       │
│     Domain events raised internally                                      │
├─────────────────────────────────────────────────────────────────────────┤
│  4. PERSIST                                                              │
│     Unit of Work saves changes to PostgreSQL                            │
│     Domain events collected from aggregates                              │
├─────────────────────────────────────────────────────────────────────────┤
│  5. DISPATCH EVENTS                                                      │
│     DomainEventDispatcher publishes to MediatR (internal handlers)      │
│     EventBusPublisher publishes to Redis Streams (external systems)     │
└─────────────────────────────────────────────────────────────────────────┘
```

### Architecture Principles

| Principle | Implementation |
|-----------|----------------|
| Event Sourcing | All state changes emit domain events |
| CQRS | Commands mutate, Queries read |
| Clean Architecture | Domain → Application → Infrastructure → API |
| DDD | Aggregates, Value Objects, Domain Events |
| Result Pattern | No exceptions for business logic |

---

## Quick Start

```bash
# Prerequisites: .NET 8 SDK, PostgreSQL, Redis

# Clone and restore
git clone https://github.com/geneflow-app/geneflow-backend.git
cd geneflow-backend/GeneFlow.ApiNet2
dotnet restore

# Run the API
dotnet run --project GeneFlow.ApiNet.API

# Verify
curl http://localhost:5145/health
```

---

## Bounded Contexts

The API is organized into **6 bounded contexts**, each with its own domain:

| BC | Stream | Description |
|----|--------|-------------|
| **Identity** | `geneflow:events:users` | Authentication, 2FA, OAuth, password reset |
| **Plans** | `geneflow:events:plans` | Subscription plans catalog (read-only) |
| **Subscriptions** | `geneflow:events:subscriptions` | User subscriptions, billing, limits |
| **Studies** | `geneflow:events:studies` | Research projects, members, invitations |
| **Traces** | `geneflow:events:traces` | Sequencing files, processing, annotations |
| **Alignments** | `geneflow:events:alignments` | Sequence alignment jobs |

---

## REST API

### Authentication

```bash
POST /api/auth/register              # Register new user
POST /api/auth/login                 # Login (returns JWT + refresh token)
POST /api/auth/refresh               # Refresh access token
POST /api/auth/logout                # Revoke refresh token
POST /api/auth/verify-email          # Verify email with token
POST /api/auth/2fa/enable            # Enable two-factor auth
POST /api/auth/2fa/request-code      # Request 2FA code
POST /api/auth/request-password-reset
POST /api/auth/reset-password
```

### Users

```bash
GET /api/users/me                    # Current user profile
```

### Studies

```bash
GET  /api/studies                    # List user's studies
GET  /api/studies/public             # List public studies
POST /api/studies                    # Create study
GET  /api/studies/{id}               # Get study details
PUT  /api/studies/{id}               # Update study
DELETE /api/studies/{id}             # Delete study
PATCH /api/studies/{id}/status       # Change status (Draft→Active→Published)
```

### Study Members

```bash
GET  /api/studies/{id}/members       # List members
POST /api/studies/{id}/members       # Add member
DELETE /api/studies/{id}/members     # Remove member
PATCH /api/studies/{id}/members/role # Change role
POST /api/studies/{id}/members/transfer-ownership
```

### Traces

```bash
GET  /api/studies/{id}/traces        # List traces in study
GET  /api/studies/{id}/traces/counts # Count by status
POST /api/studies/{id}/traces        # Upload trace file
GET  /api/traces/{id}                # Get trace details
GET  /api/traces/{id}/download       # Download original file
GET  /api/traces/{id}/chromatogram   # Get chromatogram data
PATCH /api/traces/{id}/name          # Update name
DELETE /api/traces/{id}              # Delete trace
POST /api/traces/{id}/trim/auto      # Auto-trim low quality ends
POST /api/traces/{id}/edits          # Create sequence edit
POST /api/traces/{id}/annotations    # Add annotation
```

### Alignments

```bash
GET  /api/studies/{id}/alignments    # List alignments
POST /api/studies/{id}/alignments    # Create alignment job
GET  /api/alignments/{id}            # Get alignment details
POST /api/alignments/{id}/cancel     # Cancel processing
POST /api/alignments/{id}/retry      # Retry failed alignment
```

### Subscriptions

```bash
GET  /api/plans                      # List available plans
GET  /api/subscriptions/current      # Current subscription
POST /api/subscriptions/cancel       # Cancel subscription
PATCH /api/subscriptions/change-plan # Upgrade/downgrade
```

---

## Event Categories

All domain events are published to Redis Streams with the prefix `geneflow:events`:

| Category | Events |
|----------|--------|
| `users` | UserRegistered, EmailVerified, PasswordChanged, TwoFactorEnabled, ... |
| `studies` | StudyCreated, StudyUpdated, MemberAdded, MemberRemoved, StatusChanged, ... |
| `traces` | TraceUploaded, TraceProcessed, TraceFailed, TraceArchived, ... |
| `alignments` | AlignmentCreated, AlignmentStarted, AlignmentCompleted, AlignmentFailed |
| `subscriptions` | SubscriptionCreated, PlanChanged, SubscriptionCancelled, ... |
| `plans` | PlanCreated |
| `ai` | EmbeddingGenerated, SimilaritySearchCompleted, ... |
| `blast` | BlastJobSubmitted, BlastJobCompleted, ... |

### Event Format

```json
{
  "eventId": "550e8400-e29b-41d4-a716-446655440000",
  "type": "UserRegistered",
  "category": "users",
  "timestamp": 1711357800000,
  "data": "{\"userId\":\"user-123\",\"email\":\"scientist@lab.org\"}",
  "source": "geneflow-api",
  "version": "1.0",
  "correlationId": "request-guid"
}
```

---

## Configuration

All settings in `appsettings.json`:

| Section | Variable | Description | Default |
|---------|----------|-------------|---------|
| **ConnectionStrings** | `DefaultConnection` | PostgreSQL connection | - |
| **Redis** | `ConnectionString` | Redis connection | `localhost:6379` |
| **EventBus** | `StreamPrefix` | Redis stream prefix | `geneflow:events` |
| **EventBus** | `Enabled` | Enable event publishing | `true` |
| **Jwt** | `Secret` | JWT signing key | - |
| **Jwt** | `AccessTokenExpirationMinutes` | Access token TTL | `15` |
| **Jwt** | `RefreshTokenExpirationDays` | Refresh token TTL | `7` |

<details>
<summary>Full configuration reference</summary>

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=geneflow;Username=geneflow;Password=secret"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "EventBus": {
    "StreamPrefix": "geneflow:events",
    "MaxStreamLength": 100000,
    "Enabled": true,
    "TimeoutMs": 5000
  },
  "Jwt": {
    "Secret": "your-256-bit-secret-key-here",
    "Issuer": "GeneFlow",
    "Audience": "GeneFlow",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "TwoFactor": {
    "Issuer": "GeneFlow",
    "CodeExpirationMinutes": 5
  },
  "Email": {
    "SmtpHost": "smtp.example.com",
    "SmtpPort": 587,
    "Username": "",
    "Password": "",
    "FromEmail": "noreply@geneflow.io"
  },
  "Storage": {
    "Provider": "supabase",
    "SupabaseUrl": "",
    "SupabaseKey": "",
    "Bucket": "traces"
  }
}
```

</details>

---

## Project Structure

```
GeneFlow.ApiNet2/
├── GeneFlow.ApiNet.sln
├── GeneFlow.ApiNet.API/
│   ├── Program.cs                   # Entry point, DI configuration
│   ├── appsettings.json
│   ├── Contracts/                   # Request/Response DTOs
│   │   ├── Identity/
│   │   ├── Studies/
│   │   └── Traces/
│   ├── Endpoints/                   # Minimal API endpoints
│   │   ├── Identity/
│   │   ├── Studies/
│   │   └── Traces/
│   ├── Extensions/                  # DI, Auth, Swagger config
│   └── Middleware/                  # Exception handling, correlation ID
├── GeneFlow.ApiNet.Application/
│   ├── {BC}/
│   │   ├── Commands/                # Create, Update, Delete handlers
│   │   ├── Queries/                 # GetById, GetList handlers
│   │   ├── DTOs/                    # Internal data transfer objects
│   │   └── Interfaces/              # Service abstractions
│   └── Behaviors/                   # MediatR pipeline behaviors
├── GeneFlow.ApiNet.Domain/
│   ├── {BC}/
│   │   ├── {Aggregate}.cs           # Aggregate root
│   │   ├── {Aggregate}Id.cs         # Strongly-typed ID
│   │   ├── {Aggregate}Errors.cs     # Domain errors
│   │   ├── I{Aggregate}Repository.cs
│   │   ├── Entities/                # Child entities
│   │   ├── Enumerations/            # Smart enums (Status, Role)
│   │   ├── Events/                  # Domain events
│   │   └── ValueObjects/            # Value objects
│   └── Shared/                      # Cross-cutting domain concepts
├── GeneFlow.ApiNet.Infrastructure/
│   ├── {BC}/
│   │   ├── Persistence/
│   │   │   ├── Context/             # DbContext
│   │   │   ├── Configurations/      # EF Core configurations
│   │   │   ├── Repositories/        # Repository implementations
│   │   │   └── Migrations/
│   │   └── Services/                # External service implementations
│   ├── Events/                      # DomainEventDispatcher, EventBusPublisher
│   └── DependencyInjection.cs
├── GeneFlow.ApiNet.SharedKernel/
│   ├── Application/
│   │   ├── CQRS/                    # ICommand, IQuery, handlers
│   │   └── EventNotifications/      # IDomainEvent, dispatcher
│   ├── Domain/
│   │   ├── DDD/                     # Entity, AggregateRoot, ValueObject
│   │   ├── Results/                 # Result<T>, Error
│   │   ├── Guards/                  # Input validation
│   │   ├── Types/                   # StronglyTypedId, Enumeration, Maybe
│   │   └── Pagination/              # PagedList, PagedRequest
│   └── Infrastructure/              # IDateTimeProvider, ICacheService
└── GeneFlow.ApiNet.Tests/
    ├── Domain/                      # Unit tests
    ├── Application/                 # Handler tests
    ├── Infrastructure/              # Integration tests
    └── API/                         # Endpoint tests
```

---

## Docker

```bash
# Build
docker build -t geneflow-api -f GeneFlow.ApiNet.API/Dockerfile .

# Run
docker run -d \
  -p 5145:8080 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;..." \
  -e Redis__ConnectionString="host.docker.internal:6379" \
  geneflow-api
```

### Docker Compose

```yaml
api:
  build:
    context: ./GeneFlow.ApiNet2
    dockerfile: GeneFlow.ApiNet.API/Dockerfile
  ports:
    - "5145:8080"
  environment:
    ConnectionStrings__DefaultConnection: "Host=postgres;Database=geneflow;..."
    Redis__ConnectionString: "redis:6379"
    EventBus__Enabled: "true"
  depends_on:
    postgres:
      condition: service_healthy
    redis:
      condition: service_healthy
  restart: unless-stopped
```

---

## Development

```bash
dotnet restore                       # Install dependencies
dotnet build                         # Build solution
dotnet run --project GeneFlow.ApiNet.API  # Run API
dotnet test                          # Run all tests
dotnet test --collect:"XPlat Code Coverage"  # With coverage
dotnet format                        # Format code
```

---

## CI/CD

This project uses GitHub Actions for continuous integration and deployment.

### Workflows

| Workflow | Trigger | Description |
|----------|---------|-------------|
| **CI** | Push/PR to `main`, `develop` | Build, test, lint |
| **CD** | Push to `main` or tags `v*` | Build & push Docker image, deploy |

### Pipeline Stages

```
┌─────────┐    ┌─────────┐    ┌─────────┐    ┌──────────┐
│ Restore │───►│  Build  │───►│  Test   │───►│  Docker  │
│         │    │         │    │  xUnit  │    │  Build   │
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

---

## Implementation Status

| Phase | Description | Status |
|-------|-------------|--------|
| 1 | Foundation (SharedKernel, CI/CD, Docker) | ⏳ In Progress |
| 2 | Identity BC | ⏳ Pending |
| 3 | Plans BC | ⏳ Pending |
| 4 | Subscriptions BC | ⏳ Pending |
| 5 | Studies BC | ⏳ Pending |
| 6 | Traces BC | ⏳ Pending |
| 7 | Alignments BC | ⏳ Pending |
| 8 | Pipelines BC | ⏳ Pending |
| 9 | Profiles & Settings BC | ⏳ Pending |

---

## Compatibility

### Datalake Event Format

Compatible with GeneFlow Datalake consumer expecting:

```json
{
  "eventId": "guid",
  "type": "EventTypeName",
  "category": "category-name",
  "timestamp": 1711357800000,
  "data": "{\"serialized\":\"json\"}",
  "source": "geneflow-api",
  "version": "1.0",
  "correlationId": "optional-guid"
}
```

### Mounters

Events are projected by the Datalake's Mounters to multiple targets:

| Mounter | Target | Tables/Collections |
|---------|--------|-------------------|
| **PostgresMounter** | PostgreSQL | `identity.users`, `studies.*`, `traces.*`, `alignments.*`, `billing.*` |
| **QdrantMounter** | Qdrant | `geneflow_sequences`, `geneflow_annotations`, `geneflow_traces` |
| **StorageMounter** | Supabase/MinIO | `traces/{id}/chunks/`, `alignments/{id}/chunks/` |

---

<div align="center">

**GeneFlow Platform** · Proprietary

</div>
