# Plan de Migración: GeneFlow Backend Event-Driven

## Resumen Ejecutivo

Migración completa del backend GeneFlow hacia una arquitectura **Event Sourcing** donde:
- **Datalake** = Source of Truth (inmutable, append-only)
- **PostgreSQL** = Datamart (descartable, reconstruible via Mounter)
- **Redis Streams** = Event Bus central

**Proyecto destino:** `GeneFlow.APINET2`
**Duración estimada:** 8-10 sprints (2 semanas cada uno)

### Decisiones Tomadas
- **SharedKernel:** Copiar y adaptar del proyecto original (GeneFlow.ApiNet)
- **Mounter PostgreSQL:** Ya implementado en `geneflow-datalake/src/mounters/postgres/`
  - Handlers existentes: users, studies, traces, alignments, billing
- **Testing:** Tests junto al código (inmediatamente después de cada feature)

---

## Arquitectura Objetivo

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         REDIS STREAMS (Event Bus)                        │
│   geneflow:events:{users|studies|traces|alignments|subscriptions|...}   │
└────────────┬──────────────────┬──────────────────┬──────────────────────┘
             │                  │                  │
        PUBLISH            PUBLISH            SUBSCRIBE
             │                  │                  │
             ▼                  ▼                  ▼
┌────────────────┐    ┌────────────────┐    ┌────────────────┐
│  GeneFlow API  │    │ GeneFlow Worker│    │GeneFlow Datalake│
│    (.NET 8)    │    │   (Python)     │    │    (Python)     │
│                │    │                │    │                 │
│ Produce ALL    │    │ TraceProcessor │    │ SOURCE OF TRUTH │
│ domain events  │    │ AlignmentProc. │    │ Persiste TODO   │
└───────┬────────┘    └────────────────┘    └────────┬────────┘
        │                                            │
        ▼                                            ▼
┌────────────────┐                          ┌────────────────┐
│  PostgreSQL    │◄─────── MOUNTER ─────────│   JSONL Files  │
│  (Datamart)    │                          │ (Event Store)  │
│  DESCARTABLE   │                          │   INMUTABLE    │
└────────────────┘                          └────────────────┘
```

---

## Bounded Contexts y Orden de Migración

| Fase | BC | Dependencias | Complejidad | Events |
|------|-----|--------------|-------------|--------|
| 1 | SharedKernel | Ninguna | ⭐⭐ | - |
| 2 | Identity | Ninguna | ⭐⭐⭐ | 13 |
| 3 | Plans | Ninguna | ⭐ | 1 |
| 4 | Subscriptions | Identity, Plans | ⭐⭐ | 5 |
| 5 | Studies | Identity, Subscriptions | ⭐⭐⭐ | 11 |
| 6 | Traces | Studies, Subscriptions | ⭐⭐⭐ | 11 |
| 7 | Alignments | Traces, Studies | ⭐⭐ | 4 |
| 8 | Pipelines | Studies, Traces | ⭐⭐ | 5 |
| 9 | Profiles & Settings | Identity | ⭐ | 7 |

---

## Estrategia de Ramas

```
main
└── develop
    ├── feature/phase-1-foundation
    │   ├── feature/skeleton-projects
    │   ├── feature/shared-kernel
    │   ├── feature/ci-cd-pipeline
    │   └── feature/docker-compose
    │
    ├── feature/phase-2-identity
    │   ├── feature/identity-domain
    │   ├── feature/identity-application
    │   ├── feature/identity-infrastructure
    │   ├── feature/identity-api
    │   └── feature/identity-tests
    │
    ├── feature/phase-3-plans
    │   └── ... (mismo patrón)
    │
    ├── feature/phase-4-subscriptions
    ├── feature/phase-5-studies
    ├── feature/phase-6-traces
    ├── feature/phase-7-alignments
    ├── feature/phase-8-pipelines
    └── feature/phase-9-profiles-settings
```

---

## FASE 1: Foundation (Sprint 1)

### 1.1 Skeleton Projects
**Rama:** `feature/skeleton-projects`

**Commits:**
```
feat(skeleton): create solution structure
feat(skeleton): add project references
feat(skeleton): configure NuGet packages
feat(skeleton): add .editorconfig and Directory.Build.props
```

**Archivos:**
```
GeneFlow.APINET2/
├── GeneFlow.ApiNet.sln
├── Directory.Build.props
├── .editorconfig
├── GeneFlow.ApiNet.API/
│   └── GeneFlow.ApiNet.API.csproj
├── GeneFlow.ApiNet.Application/
│   └── GeneFlow.ApiNet.Application.csproj
├── GeneFlow.ApiNet.Domain/
│   └── GeneFlow.ApiNet.Domain.csproj
├── GeneFlow.ApiNet.Infrastructure/
│   └── GeneFlow.ApiNet.Infrastructure.csproj
├── GeneFlow.ApiNet.SharedKernel/
│   └── GeneFlow.ApiNet.SharedKernel.csproj
└── GeneFlow.ApiNet.Tests/
    └── GeneFlow.ApiNet.Tests.csproj
```

### 1.2 SharedKernel (Copiar y Adaptar)
**Rama:** `feature/shared-kernel`
**Fuente:** `GeneFlow.ApiNet/GeneFlow.ApiNet.SharedKernel/`

**Estrategia:** Copiar archivos del proyecto original y ajustar namespaces.

**Commits:**
```
feat(kernel): copy DDD base classes from original project
feat(kernel): copy Result pattern classes
feat(kernel): copy CQRS interfaces
feat(kernel): copy Domain Events interfaces
feat(kernel): copy Guards framework
feat(kernel): copy StronglyTypedId and Enumeration
feat(kernel): copy Maybe<T> monad
feat(kernel): copy Pagination classes
feat(kernel): copy Validation framework
feat(kernel): copy Infrastructure interfaces
refactor(kernel): update namespaces to GeneFlow.ApiNet
test(kernel): copy and adapt unit tests
```

**Pasos de copia:**
```bash
# Copiar SharedKernel completo
cp -r GeneFlow.ApiNet/GeneFlow.ApiNet.SharedKernel/* \
      GeneFlow.APINET2/GeneFlow.ApiNet.SharedKernel/

# Ajustar namespaces (si es necesario)
# Los archivos ya usan GeneFlow.ApiNet.SharedKernel
```

**Estructura:**
```
SharedKernel/
├── Application/
│   ├── CQRS/
│   │   ├── ICommand.cs
│   │   ├── ICommandHandler.cs
│   │   ├── IQuery.cs
│   │   └── IQueryHandler.cs
│   └── EventNotifications/
│       ├── IDomainEvent.cs
│       ├── DomainEvent.cs
│       ├── IDomainEventHandler.cs
│       └── IDomainEventDispatcher.cs
├── Domain/
│   ├── DDD/
│   ├── Results/
│   ├── Guards/
│   ├── Types/
│   ├── Validation/
│   ├── Pagination/
│   └── Auditing/
└── Infrastructure/
    ├── IDateTimeProvider.cs
    ├── IMessageBroker.cs
    └── ICacheService.cs
```

### 1.3 CI/CD Pipeline
**Rama:** `feature/ci-cd-pipeline`

**Commits:**
```
ci: add GitHub Actions workflow for build
ci: add test workflow with coverage
ci: add Docker build workflow
ci: add deployment workflow to Hetzner
ci: add dependabot configuration
```

**Archivos:**
```
.github/
├── workflows/
│   ├── build.yml
│   ├── test.yml
│   ├── docker.yml
│   └── deploy.yml
├── dependabot.yml
└── CODEOWNERS
```

**build.yml:**
```yaml
name: Build
on:
  push:
    branches: [main, develop, 'feature/**']
  pull_request:
    branches: [main, develop]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build --verbosity normal
```

### 1.4 Docker & Compose
**Rama:** `feature/docker-compose`

**Commits:**
```
docker: add Dockerfile for API
docker: add docker-compose.yml with all services
docker: add docker-compose.override.yml for dev
docker: add health checks configuration
```

**docker-compose.yml:**
```yaml
services:
  api:
    build: ./GeneFlow.APINET2/GeneFlow.ApiNet.API
    ports: ["5145:8080"]
    depends_on: [postgres, redis]

  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: geneflow
      POSTGRES_USER: geneflow
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    volumes:
      - postgres-data:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    ports: ["6379:6379"]

  datalake:
    build: ../geneflow-datalake
    ports: ["8082:8080"]
    depends_on: [redis]

  worker:
    build: ../geneflow-analysis
    depends_on: [redis, api]

  ai:
    build: ../geneflow-ai
    ports: ["8090:8090"]
    depends_on: [redis]
```

### 1.5 README y Documentación
**Rama:** `feature/documentation`

**Commits:**
```
docs: add README.md with architecture overview
docs: add CONTRIBUTING.md
docs: add API documentation structure
docs: add architecture decision records (ADR)
```

---

## FASE 2: Identity BC (Sprint 2)

### 2.1 Domain Layer
**Rama:** `feature/identity-domain`

**Commits:**
```
feat(identity): add UserId strongly-typed ID
feat(identity): add User aggregate root
feat(identity): add value objects (Email, Username, PasswordHash)
feat(identity): add value objects (RefreshToken, TwoFactorAuth)
feat(identity): add entities (ExternalLogin, TwoFactorCode)
feat(identity): add enumerations (Role, ExternalProvider)
feat(identity): add domain events (13 events)
feat(identity): add UserErrors static class
feat(identity): add IUserRepository interface
test(identity): add domain unit tests
```

**Estructura:**
```
Domain/Identity/
├── User.cs (Aggregate Root)
├── UserId.cs
├── UserErrors.cs
├── IUserRepository.cs
├── Entities/
│   ├── ExternalLogin.cs
│   └── TwoFactorCode.cs
├── Enumerations/
│   ├── Role.cs
│   └── ExternalProvider.cs
├── Events/
│   ├── UserRegisteredEvent.cs
│   ├── UserEmailVerifiedEvent.cs
│   ├── UserPasswordChangedEvent.cs
│   └── ... (13 total)
└── ValueObjects/
    ├── Email.cs
    ├── Username.cs
    ├── PasswordHash.cs
    ├── RefreshToken.cs
    ├── TwoFactorAuth.cs
    ├── TwoFactorSecret.cs
    └── AccountLockout.cs
```

### 2.2 Application Layer
**Rama:** `feature/identity-application`

**Commits:**
```
feat(identity): add DTOs (UserDto, AuthTokensDto, LoginResultDto)
feat(identity): add interfaces (IPasswordHasher, IJwtTokenGenerator)
feat(identity): add Register command and handler
feat(identity): add Login command and handler
feat(identity): add RefreshToken command and handler
feat(identity): add Logout command and handler
feat(identity): add VerifyEmail command and handler
feat(identity): add TwoFactor commands (Enable, Disable, RequestCode)
feat(identity): add PasswordReset commands
feat(identity): add GetCurrentUser query
feat(identity): add GetUserById query
feat(identity): add domain event handlers
feat(identity): add mapping extensions
test(identity): add application unit tests
```

### 2.3 Infrastructure Layer
**Rama:** `feature/identity-infrastructure`

**Commits:**
```
feat(identity): add UserContext DbContext
feat(identity): add UserConfiguration (EF Core)
feat(identity): add UserRepository implementation
feat(identity): add UnitOfWork implementation
feat(identity): add PasswordHasher service (BCrypt)
feat(identity): add JwtTokenGenerator service
feat(identity): add TwoFactorAuthenticator service (TOTP)
feat(identity): add EmailService for notifications
feat(identity): add initial migration
test(identity): add infrastructure integration tests
```

### 2.4 API Layer
**Rama:** `feature/identity-api`

**Commits:**
```
feat(identity): add request contracts (Register, Login, etc.)
feat(identity): add response contracts (UserResponse, AuthTokensResponse)
feat(identity): add AuthEndpoints (Minimal API)
feat(identity): add UserEndpoints
feat(identity): add JWT authentication configuration
feat(identity): add CurrentUserService
test(identity): add API integration tests
```

### 2.5 Event Bus Integration
**Rama:** `feature/identity-eventbus`

**Commits:**
```
feat(eventbus): add IEventBusPublisher interface
feat(eventbus): add EventBusMessage model
feat(eventbus): add EventCategoryResolver
feat(eventbus): add RedisEventBusPublisher
feat(eventbus): add NullEventBusPublisher (for testing)
feat(eventbus): integrate with DomainEventDispatcher
feat(identity): configure Identity events to publish to bus
test(eventbus): add event bus integration tests
```

---

## FASE 3-9: Bounded Contexts Restantes

Cada BC sigue el mismo patrón de commits:

### Patrón por BC
```
feature/phase-N-{bc-name}
├── feature/{bc}-domain
│   ├── feat({bc}): add {Bc}Id strongly-typed ID
│   ├── feat({bc}): add {Bc} aggregate root
│   ├── feat({bc}): add value objects
│   ├── feat({bc}): add domain events
│   ├── feat({bc}): add {Bc}Errors
│   ├── feat({bc}): add I{Bc}Repository
│   └── test({bc}): add domain tests
│
├── feature/{bc}-application
│   ├── feat({bc}): add DTOs
│   ├── feat({bc}): add commands and handlers
│   ├── feat({bc}): add queries and handlers
│   ├── feat({bc}): add event handlers
│   ├── feat({bc}): add mappings
│   └── test({bc}): add application tests
│
├── feature/{bc}-infrastructure
│   ├── feat({bc}): add {Bc}Context DbContext
│   ├── feat({bc}): add EF configurations
│   ├── feat({bc}): add repository implementation
│   ├── feat({bc}): add unit of work
│   ├── feat({bc}): add migrations
│   └── test({bc}): add infrastructure tests
│
└── feature/{bc}-api
    ├── feat({bc}): add request/response contracts
    ├── feat({bc}): add endpoints
    └── test({bc}): add API tests
```

---

## Especificaciones por Fase

### FASE 3: Plans BC
- **Archivos Domain:** 10
- **Commands:** 0 (read-only)
- **Queries:** 3 (GetAll, GetById, GetActive)
- **Events:** 1 (PlanCreatedEvent)

### FASE 4: Subscriptions BC
- **Archivos Domain:** 15
- **Commands:** 4 (Create, ChangePlan, Cancel, Renew)
- **Queries:** 3 (GetCurrent, GetById, GetHistory)
- **Events:** 5
- **Dependencias:** Identity (user owner), Plans (plan reference)

### FASE 5: Studies BC (⭐⭐⭐ CRÍTICO)
- **Archivos Domain:** 51
- **Commands:** 15 (Create, Update, Delete, AddMember, RemoveMember, etc.)
- **Queries:** 9 (GetById, GetMine, GetPublic, GetMembers, etc.)
- **Events:** 11
- **Entities:** StudyMember, StudyInvitation
- **Dependencias:** Identity, Subscriptions (SubscriptionLimitBehavior)

### FASE 6: Traces BC (⭐⭐⭐ CRÍTICO)
- **Archivos Domain:** 108
- **Commands:** 26 (Upload, Process, Complete, Fail, Trim, Archive, etc.)
- **Queries:** 17 (GetById, GetByStudy, GetChromatogram, etc.)
- **Events:** 11
- **Entities:** SequenceEdit, TraceAnnotation
- **Dependencias:** Studies, Subscriptions
- **Integración:** Redis queue para Python workers

### FASE 7: Alignments BC
- **Archivos Domain:** 20
- **Commands:** 6 (Create, Start, Complete, Fail, Cancel, Retry)
- **Queries:** 3 (GetById, GetByStudy, GetPending)
- **Events:** 4
- **Dependencias:** Studies, Traces, Subscriptions

### FASE 8: Pipelines BC
- **Archivos Domain:** 20
- **Commands:** 6 (Create, Start, UpdateProgress, Complete, Fail, Cancel)
- **Queries:** 3 (GetById, GetByStudy, GetActive)
- **Events:** 5
- **Dependencias:** Studies, Traces

### FASE 9: Profiles & Settings BC
- **Archivos Domain:** 41
- **Commands:** 11 (UpdateProfile, UpdatePhoto, UpdateSettings, etc.)
- **Queries:** 5
- **Events:** 7
- **Dependencias:** Identity

---

## Componentes Cross-Cutting

### PostgreSQL Mounter (YA IMPLEMENTADO)
**Ubicación:** `geneflow-datalake/src/mounters/postgres/`

El Mounter ya existe y proyecta eventos a PostgreSQL:
```
src/mounters/postgres/
├── mounter.py          # PostgresMounter class
├── connection.py       # PostgresConnection
├── handlers/
│   ├── users.py        # UserRegistered, UserUpdated, etc.
│   ├── studies.py      # StudyCreated, MemberAdded, etc.
│   ├── traces.py       # TraceUploaded, TraceProcessed, etc.
│   ├── alignments.py   # AlignmentCreated, AlignmentCompleted, etc.
│   └── billing.py      # SubscriptionCreated, PlanChanged, etc.
└── schemas/            # SQL schemas
```

**IMPORTANTE:** Los eventos que publique la API .NET deben coincidir con los handlers existentes.

### SubscriptionLimitBehavior (MediatR Pipeline)
**Implementar en Fase 4 (Subscriptions)**

```csharp
public class SubscriptionLimitBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequireSubscriptionLimit
{
    // Valida límites de: maxStudies, maxTracesPerStudy, maxStorageGb
    // Se ejecuta en: CreateStudy, UploadTrace, CreateAlignment
}
```

### DomainEventDispatcher
**Implementar en Fase 1 (SharedKernel Infrastructure)**

```csharp
public class DomainEventDispatcher : IDomainEventDispatcher
{
    public async Task DispatchEventsAsync(IEnumerable<IDomainEvent> events, CancellationToken ct)
    {
        // 1. Dispatch via MediatR (handlers internos)
        foreach (var evt in events)
            await _mediator.Publish(evt, ct);

        // 2. Publish to Event Bus (Redis Streams)
        await _eventBusPublisher.PublishBatchAsync(
            events.Select(e => (e, _categoryResolver.Resolve(e))), ct);
    }
}
```

### Event Bus Configuration
**appsettings.json:**
```json
{
  "EventBus": {
    "RedisConnectionString": "localhost:6379",
    "StreamPrefix": "geneflow:events",
    "MaxStreamLength": 100000,
    "Enabled": true,
    "TimeoutMs": 5000
  }
}
```

---

## Testing Strategy

### Enfoque: Tests Junto al Código
Cada feature se implementa con sus tests inmediatamente después:
1. Implementar feature (Domain → Application → Infrastructure → API)
2. Escribir tests correspondientes en el mismo commit o siguiente
3. No se considera "done" sin tests

### Por Capa
| Capa | Framework | Tipo | Cobertura Target |
|------|-----------|------|------------------|
| Domain | xUnit + FluentAssertions | Unit | 95%+ |
| Application | xUnit + NSubstitute | Unit | 80%+ |
| Infrastructure | xUnit + Testcontainers | Integration | 70%+ |
| API | xUnit + WebApplicationFactory | Integration | 60%+ |

### Estructura de Tests
```
Tests/
├── Domain/
│   └── {BC}/
│       ├── {Aggregate}Tests.cs
│       ├── {ValueObject}Tests.cs
│       └── Enumerations/
├── Application/
│   └── {BC}/
│       ├── Commands/
│       │   └── {Command}HandlerTests.cs
│       └── Queries/
│           └── {Query}HandlerTests.cs
├── Infrastructure/
│   └── {BC}/
│       └── {Repository}Tests.cs
└── API/
    └── {BC}/
        └── {Endpoints}Tests.cs
```

### Naming Convention
```
[MethodName]_[Scenario]_Should[ExpectedResult]

Examples:
- Create_WithValidData_ShouldReturnSuccess
- Create_WithEmptyTitle_ShouldFail
- Handle_WithInvalidCredentials_ShouldReturnUnauthorized
```

---

## Verificación End-to-End

### 1. Build y Tests
```bash
cd GeneFlow.APINET2
dotnet restore
dotnet build
dotnet test --verbosity normal
```

### 2. Docker Local
```bash
docker-compose up -d
curl http://localhost:5145/health
curl http://localhost:8082/health  # Datalake
```

### 3. Verificar Event Bus
```bash
# Ver eventos en Redis
redis-cli XLEN geneflow:events:users
redis-cli XRANGE geneflow:events:users - + COUNT 5

# Ver eventos en Datalake
curl http://localhost:8082/categories
curl http://localhost:8082/events/users?date=$(date +%Y-%m-%d)
```

### 4. Flujo Completo
```bash
# 1. Registrar usuario
curl -X POST http://localhost:5145/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!","username":"testuser"}'

# 2. Verificar evento en Datalake
curl http://localhost:8082/events/users | jq '.[-1]'

# 3. Login
curl -X POST http://localhost:5145/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!"}'
```

---

## Archivos Críticos a Crear

### Fase 1
- [ ] `GeneFlow.ApiNet.sln`
- [ ] `Directory.Build.props`
- [ ] `SharedKernel/**` (50+ archivos)
- [ ] `.github/workflows/*.yml`
- [ ] `docker-compose.yml`
- [ ] `README.md`

### Por cada BC
- [ ] `Domain/{BC}/{Aggregate}.cs`
- [ ] `Domain/{BC}/I{BC}Repository.cs`
- [ ] `Domain/{BC}/Events/*.cs`
- [ ] `Application/{BC}/Commands/**`
- [ ] `Application/{BC}/Queries/**`
- [ ] `Infrastructure/{BC}/Persistence/**`
- [ ] `API/Endpoints/{BC}/*.cs`
- [ ] `Tests/**/{BC}/**`

---

## Milestones

| Milestone | Fecha Target | Entregables |
|-----------|--------------|-------------|
| M1: Foundation | Sprint 1 | SharedKernel, CI/CD, Docker |
| M2: Auth Ready | Sprint 2 | Identity BC completo |
| M3: Core Ready | Sprint 4 | Plans + Subscriptions + Studies |
| M4: Analysis Ready | Sprint 6 | Traces + Alignments |
| M5: Full Feature | Sprint 8 | Todos los BCs |
| M6: Production | Sprint 10 | Optimización + Deploy |

---

## Notas Importantes

1. **Event Bus Prefix:** Debe coincidir con Datalake (`geneflow:events`)
2. **SubscriptionLimitBehavior:** Implementar temprano (Fase 4)
3. **Tests primero:** Escribir tests junto con cada feature
4. **Commits atómicos:** Un commit = un cambio lógico
5. **PR por feature branch:** Merge a develop via PR con review
