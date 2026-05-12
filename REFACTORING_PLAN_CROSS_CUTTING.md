# Plan de Refactorización — Infraestructura Cross-Cutting + Alignments

> Reglas obligatorias: **una clase por archivo**, **sin comentarios que no sean XML docs**, **sin sobreingeniería**.
> Cubre: Alignments (módulo abandonado), Events bus (Redis Streams), Jobs (Redis Streams), Redis services, File Storage, DI, MediatR Behaviors.

---

## 1. Diagnóstico general

Capa cross-cutting **funcional pero con deuda técnica concreta**:
- **Alignments**: módulo abandonado/parcial — solo tiene `AlignmentId.cs` + 3 eventos. Sin aggregate, sin repo, sin handlers. TODOs en `AnalysisEventProcessor` confirman "Phase 3 pending".
- **Events bus**: Redis Streams + dual dispatch (MediatR in-process + external pub/sub). Bien diseñado pero con **leaky abstraction de Python dict parsing** (229 líneas de string handling en `RedisEventBusSubscriber.ConvertPythonDictToJson`).
- **Jobs**: Redis Streams custom (no Hangfire/Quartz). 4 tipos de jobs (Trace, Alignment, Analysis, Pipeline). **Inconsistencia de serialización**: events usan snake_case, jobs usan camelCase.
- **Redis**: usado para cache, sequences, eventos, jobs, usage stats. `IConnectionMultiplexer` singleton (correcto).
- **Storage**: dos implementaciones (`LocalFileStorageService` 163 líneas, `MinIOFileStorageService` 189 líneas). Buenas. `GetFileAsync` retorna `byte[]?` (no `Stream`) — limita streams grandes.
- **DI**: `DependencyInjection.cs` 352 líneas, **bien estructurada** en 6 helpers privados. No es God Method.
- **Behaviors MediatR**: 4 behaviors (Authentication, WorkerApiKey, StudyMembership, SubscriptionLimit). Reflection-based generic Result handling — duplicada en 4 sitios.

**Veredicto:** Refactor de mediana prioridad. El código funciona; la deuda es **claridad y consolidación**, no incorrectness. **Decisión clave: qué hacer con Alignments.**

---

## 2. Code Smells detectados

| # | Smell | Ubicación | Severidad |
|---|---|---|---|
| 1 | **Módulo Alignments abandonado** (solo ID + 3 eventos sin aggregate) | `Domain/Alignments/` | Alta (decisión bloqueante) |
| 2 | Leaky abstraction Python dict → JSON (229 líneas de string parsing) | `Infrastructure/Events/RedisEventBusSubscriber.cs:205-227` | Alta |
| 3 | Inconsistencia naming: events snake_case, jobs camelCase | `RedisEventBusPublisher.cs` vs `RedisJobPublisher.cs` | Media |
| 4 | Magic strings en `EventCategoryResolver` (mapping namespace → category) | `EventCategoryResolver.cs:12-23` | Media |
| 5 | Sin retry policy / DLQ en `RedisJobPublisher` | `Infrastructure/Jobs/RedisJobPublisher.cs` | Alta |
| 6 | Lógica reflection-based de Result duplicada en 4 behaviors | `Application/Behaviors/*.cs` (`CreateErrorResult`) | Media |
| 7 | `IFileStorageService.GetFileAsync` devuelve `byte[]?` — no soporta streams grandes | `Application/.../IFileStorageService.cs` | Media |
| 8 | TODOs en código de producción ("Phase 3 - Update Alignment entity") | `AnalysisEventProcessor.cs:274,308` | Media |
| 9 | Sin tests para event publishing, job publishing, Redis subscriber, behaviors | tests | Media |
| 10 | `RedisEventBusSubscriber` 316 líneas — borderline | mismo | Baja |
| 11 | Comentarios inline no-doc | varios | Media (regla obligatoria) |
| 12 | Stub mode silencioso en `WorkerApiKeyValidator` (similar al de Stripe) — verificar | `Infrastructure/Services/WorkerApiKeyValidator.cs` | Media |
| 13 | `SubscriptionLimitBehavior` 239 líneas — borderline | mismo | Media |
| 14 | `AddPersistence()` en DI registra 9 DbContexts — coherente con bounded contexts pero peso de fricción | `DependencyInjection.cs:83-218` | Baja (acceptable) |

---

## 3. Problemas arquitectónicos

1. **Alignments = código zombie**: existe pero no funciona. Confunde a desarrolladores nuevos. Causa TODOs en código activo. **Decisión bloqueante**: completar o eliminar.
2. **Contrato Python ↔ .NET informal**: el `ConvertPythonDictToJson` es síntoma de que el worker Python no produce JSON válido directamente, sino una representación de `dict` Python (`{'key': 'value'}` con comillas simples). Esto debería resolverse en el lado Python (publicar JSON puro) y eliminar 229 líneas de parsing frágil.
3. **Job queue sin garantías de entrega ni reintentos**: si el worker Python no consume un job, no hay reintento ni alerta. Es fire-and-forget puro.
4. **Behaviors MediatR con reflection duplicada**: cada behavior reimplementa el patrón "convertir Error a Result<T>" via reflection. Debería ser un helper compartido.
5. **`IFileStorageService` con interface limitante**: `byte[]?` fuerza a cargar archivos completos en memoria. Para traces grandes (10MB max actualmente, pero podría crecer) es problema.

---

## 4. Plan de refactorización priorizado

### Fase 0 — Decisión Alignments (BLOQUEANTE)
**Decisión obligatoria antes de Fase 2:**

> ¿Alignments se va a implementar?
>
> A) **SÍ, en próximo trimestre** → mantener `AlignmentId` + eventos como esqueleto, completar dominio.
> B) **NO próximamente** → eliminar `Domain/Alignments/`, eliminar TODOs, eliminar tipo `AlignmentJob` en `RedisJobPublisher`, eliminar mapping en `EventCategoryResolver`.
> C) **Indefinido** → mover a una rama feature/alignments separada del main; mantener main limpio.

**Recomendación: B o C**. Tener código zombie en main es peor que perderlo de vista temporalmente.

### Fase 1 — Cambios seguros
| Tarea | Archivo(s) | Justificación |
|---|---|---|
| 1.1 Eliminar comentarios no-doc en infra cross-cutting | `Infrastructure/{Events,Jobs,Redis,Services,Storage}/`, `Application/Behaviors/` | Regla obligatoria |
| 1.2 Resolver decisión Fase 0 (eliminar / completar / aislar Alignments) | varios | Anti-código zombie |
| 1.3 Constantes para event categories (en lugar de magic strings) | `Infrastructure/Events/EventCategoryResolver.cs` | Mantenibilidad |
| 1.4 Unificar naming JSON: **decisión: snake_case en ambos** (events + jobs) — más alineado con worker Python | `RedisJobPublisher.cs` | Coherencia |
| 1.5 Helper compartido `ResultFactory.CreateError<TResponse>(Error)` para eliminar reflection duplicada en behaviors | `SharedKernel/Application/ResultFactory.cs` (NUEVO) | DRY |
| 1.6 Fail-fast en `WorkerApiKeyValidator` si API key vacía y env != Development | `WorkerApiKeyValidator.cs` | Seguridad consistente con Stripe |

### Fase 2 — Mejoras de diseño
| Tarea | Archivo(s) | Justificación |
|---|---|---|
| 2.1 **Eliminar `ConvertPythonDictToJson`**: negociar con worker Python publicar **JSON estricto** (no Python `repr`). En .NET, `JsonSerializer.Deserialize` directo. Si por algo no se puede tocar Python, **aislar el parser** en clase dedicada `PythonDictNormalizer.cs` con tests exhaustivos | `Infrastructure/Events/RedisEventBusSubscriber.cs:205-227` | Reduce 229 líneas frágiles |
| 2.2 **Retry policy + DLQ para jobs**: tras N intentos fallidos del worker, mover job a `geneflow:jobs:dlq:{type}` y emitir alerta | `Infrastructure/Jobs/`, `RedisJobPublisher.cs` | Sin pérdida silenciosa |
| 2.3 `IFileStorageService.GetFileStreamAsync(relativePath): Task<Stream?>` añadido (mantener `GetFileAsync(byte[])` por compat) | `IFileStorageService.cs`, ambas implementaciones | Streams grandes |
| 2.4 Tests para behaviors MediatR (`AuthenticationBehavior`, `StudyMembershipBehavior`, `SubscriptionLimitBehavior`, `WorkerApiKeyBehavior`) | `Tests/Application/Behaviors/` | Cobertura cero hoy |
| 2.5 Tests para `RedisEventBusPublisher`, `RedisEventBusSubscriber`, `RedisJobPublisher` (mock `IConnectionMultiplexer`) | `Tests/Infrastructure/Events/`, `Tests/Infrastructure/Jobs/` | Cobertura cero hoy |
| 2.6 `EventCategoryResolver` extensible vía `IEventCategoryProvider` registrable por módulo (cada módulo registra sus categorías) | `Infrastructure/Events/` | OCP |

### Fase 3 — Refactor arquitectónico
| Tarea | Recomendación |
|---|---|
| 3.1 Outbox pattern para Domain Events críticos (Subscriptions, Pipelines, Traces processing) | **SÍ** — compartido transversalmente, alto valor |
| 3.2 Reemplazar Redis Streams por broker dedicado (RabbitMQ / Kafka) | **NO** salvo escala que lo justifique |
| 3.3 Mover `BackgroundService`s (UsageStatsEventProcessor, AnalysisEventProcessor) a un proyecto worker dedicado | **NO** mientras quepan en el monolito modular |
| 3.4 Service Bus abstracto (interface `IMessageBus`) sobre el que Redis Streams es solo una impl | **NO** salvo migración real planeada |

---

## 5. Patrones aplicables

| Patrón | Dónde | Por qué SÍ |
|---|---|---|
| **DLQ + Retry** | Jobs | Sin pérdida silenciosa |
| **Outbox** | Domain Events críticos (compartido) | Atomicidad DB ↔ bus |
| **Strategy / Registry** | `IEventCategoryProvider` | OCP para nuevos módulos |
| **Factory helper** | `ResultFactory` para reducir reflection | DRY |

**NO aplicar:**
- Service Bus abstraction layer — premature.
- Event Sourcing.
- CQRS DBs separadas.
- Reactive Extensions.

---

## 6. Estrategia de testing

**Antes:**
- `DomainEventVerificationTests` (990 líneas) ya valida payloads de eventos — bueno.
- `RedisCacheServiceTests`, `JwtTokenGeneratorTests`, `PasswordHasherTests` existen.
- Coverage cero para: behaviors, event publisher/subscriber, job publisher, file storage.

**Durante Fase 2.4/2.5:**
- Cobertura objetivo > 70% en `Application/Behaviors/`.
- Cobertura objetivo > 60% en `Infrastructure/Events/`.
- Mocks de `IConnectionMultiplexer` con StackExchange.Redis fakes (Redis-Internal).

**Para Fase 2.1 (eliminar Python dict parser):**
- Si se elimina: tests de regression confirman que JSON estricto se deserializa correctamente.
- Si se aísla: tests exhaustivos de `PythonDictNormalizer` con casos edge (escaped quotes, nested dicts, None, True/False, etc.).

**Para Fase 2.2 (DLQ):**
- Test: job que falla 5 veces va a DLQ.
- Test: job en DLQ no se reprocesa automáticamente.

---

## 7. Orden de ejecución

```
Fase 0:
  Decisión Alignments ← BLOQUEANTE

Fase 1:
  1.6 (fail-fast WorkerApiKey) ← seguridad
  1.1 (comentarios)
  1.2 (aplicar decisión Alignments)
  1.3 (constantes event categories)
  1.4 (snake_case unificado)
  1.5 (ResultFactory helper)

Fase 2:
  2.4 (tests behaviors) ← preparación red de seguridad
  2.5 (tests event/job publisher) ← preparación
  2.1 (eliminar ConvertPythonDictToJson) ← gran simplificación
  2.6 (IEventCategoryProvider extensible)
  2.3 (GetFileStreamAsync)
  2.2 (DLQ + retry jobs)

Fase 3:
  3.1 (Outbox compartido) ← solo si Pipelines/Traces/Billing lo demandan en sus planes
```

---

## 8. Cambios NO recomendados

- Migrar a Hangfire / Quartz — Redis Streams basta y ya está integrado.
- Migrar a MassTransit / NServiceBus — overkill.
- Migrar a Kafka / RabbitMQ — sin justificación de escala.
- Service Bus abstraction layer — over-engineering preventivo.
- Reactive Extensions.
- Multi-broker support (Redis + RabbitMQ + Kafka) — feature flag hell.
- Event store como base de datos — Redis Streams no lo es ni necesita serlo.
- Mover `BackgroundService`s a proyecto separado prematuramente.
- Reemplazar StackExchange.Redis por otro cliente.
- Reemplazar MinIO por otro provider sin razón.
- Encriptación at-rest custom para archivos — usar features nativas de S3/MinIO/disco.

---

## 9. Métricas de calidad sugeridas

| Métrica | Hoy | Objetivo |
|---|---|---|
| Líneas de `ConvertPythonDictToJson` | ~229 | 0 (eliminadas) o < 80 (aisladas y testeadas) |
| Behaviors con reflection duplicada | 4 | 0 (helper compartido) |
| Tests de behaviors | 0 | ≥ 70% cobertura |
| Tests de event publisher/subscriber | 0 | ≥ 60% cobertura |
| Jobs perdidos por fallo silencioso | desconocido | 0 (con DLQ) |
| Módulos zombie en main | 1 (Alignments) | 0 |
| TODOs en producción | 2+ | 0 |
| Inconsistencia JSON casing | 2 (snake/camel) | 1 (snake_case) |
| Comentarios no-doc | varios | 0 |
| Stub silencioso de WorkerApiKey en prod | posible | imposible |
| `IFileStorageService` soporta streams | No | Sí (sin romper API existente) |

---

## 10. Resultado final esperado

```
Domain/Alignments/                     (eliminado en Fase 0 si decisión = B/C)
                                       (o completado con aggregate Alignment.cs si decisión = A)

Infrastructure/Events/
├── DomainEventDispatcher.cs           (sin cambios estructurales)
├── EventCategoryResolver.cs           (delega a IEventCategoryProvider registrables)
├── IEventCategoryProvider.cs          (NUEVO interface)
├── Categories/
│   ├── IdentityEventCategoryProvider.cs
│   ├── StudiesEventCategoryProvider.cs
│   ├── TracesEventCategoryProvider.cs
│   └── ... (uno por módulo)
├── RedisEventBusPublisher.cs          (snake_case unificado)
├── RedisEventBusSubscriber.cs         (~150 líneas, sin Python parser)
└── PythonDictNormalizer.cs            (NUEVO si decisión 2.1 = aislar; o eliminado)

Infrastructure/Jobs/
├── RedisJobPublisher.cs               (snake_case)
├── JobRetryPolicy.cs                  (NUEVO)
├── JobDeadLetterQueue.cs              (NUEVO)
└── Contracts/                         (DTOs por tipo de job)
    ├── TraceProcessingJob.cs
    ├── AnalysisJob.cs
    └── PipelineJob.cs                 (sin AlignmentJob si decisión = B)

Infrastructure/Storage/
├── IFileStorageService.cs             (con GetFileStreamAsync añadido)
├── LocalFileStorageService.cs
└── MinIOFileStorageService.cs

Infrastructure/Services/
└── WorkerApiKeyValidator.cs           (fail-fast en prod)

Application/Behaviors/
├── AuthenticationBehavior.cs
├── WorkerApiKeyBehavior.cs
├── StudyMembershipBehavior.cs
└── SubscriptionLimitBehavior.cs       (todos usan ResultFactory)

SharedKernel/Application/
└── ResultFactory.cs                   (NUEVO — helper para CreateError<TResponse>)

Tests/
├── Application/Behaviors/             (NUEVO — tests para 4 behaviors)
├── Infrastructure/Events/             (NUEVO — RedisEventBusPublisher/Subscriber)
└── Infrastructure/Jobs/               (NUEVO — RedisJobPublisher + DLQ)
```

**Beneficios concretos:**
- 0 código zombie en `Domain/Alignments` (post Fase 0).
- 229 líneas de string parsing → 0 o < 80 aisladas y testeadas.
- 4 behaviors con reflection duplicada → 0 (helper compartido).
- DLQ funcionando para jobs perdidos.
- `IFileStorageService` soporta archivos grandes vía streams.
- Cobertura de tests en cross-cutting > 60%.
- Naming JSON unificado (snake_case) entre events y jobs.
- Fail-fast en WorkerApiKey y otras integraciones.
- `EventCategoryResolver` extensible sin modificar (cada módulo registra).

**Lo que NO cambia:**
- Redis Streams como bus de eventos y jobs.
- StackExchange.Redis como cliente.
- LocalFileStorageService + MinIOFileStorageService.
- DependencyInjection structure (ya bien estructurada).
- 4 behaviors MediatR existentes (se mejoran, no se reemplazan).
- DomainEventDispatcher dual dispatch (MediatR + Redis).
- Smart enumeration `Enumeration<T>` base.
- `Result<T>` pattern.
