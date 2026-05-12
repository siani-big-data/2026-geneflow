# Plan de Refactorización — Módulos Analysis y Usage

> Reglas obligatorias: **una clase por archivo**, **sin comentarios que no sean XML docs**, **sin sobreingeniería**.

---

## 1. Diagnóstico general

Dos módulos cross-cutting orientados a **integración con Python workers** y **read model de uso**. Sin Domain layer en Analysis (correcto — es orquestación), Domain ligero en Usage (`UsageStats` aggregate keyed por UserId, persistido en Redis hashes).

**Analysis:**
- Sistema asíncrono fire-and-forget. Publica jobs (trim, heterozygote, motif, translation, ORF, restriction) a Redis Streams; consume eventos de finalización del worker Python.
- `AnalysisEventProcessor` (BackgroundService, 452 líneas) es el corazón — y el problema.
- Tests fuertes para `RequestAnalysisCommand` (~22 casos).

**Usage:**
- Read model en Redis hashes con auto-reset por billing period.
- `UsageStatsEventProcessor` (305 líneas) consume eventos de Studies/Traces/Alignments y actualiza counters.
- Endpoints admin para sync y init.
- **No es enforcement**, solo reporting — la enforcement está en `Application/Behaviors/SubscriptionLimitBehavior.cs` (cross-cutting).

**Veredicto:** Analysis necesita **descomponer el God BackgroundService**; Usage necesita **clarificar fuentes de verdad** (Redis cache vs live DB) y **cerrar el gap de validación** en `BillingPeriodKey.Parse`.

---

## 2. Code Smells detectados

### Analysis
| # | Smell | Ubicación | Severidad |
|---|---|---|---|
| A1 | God BackgroundService (452 líneas, 3 categorías de eventos, 10+ tipos) | `Infrastructure/Analysis/AnalysisEventProcessor.cs` | Alta |
| A2 | Bare `catch (Exception ex)` que traga errores | `AnalysisEventProcessor.cs:371,383` | Alta |
| A3 | Magic numbers de calidad (80%, 50%, 60%, 30%) hardcoded para estimar Q20/Q30 | `AnalysisEventProcessor.cs:183-189` | Media |
| A4 | Sin validación de estructura de JSON anidado (asume properties) | `AnalysisEventProcessor.cs` | Alta |
| A5 | `await` sobre método sync (`await HandleAnalysisEventAsync()`) | `AnalysisEventProcessor.cs:361,295` | Baja |
| A6 | Comentarios inline no-doc | varios | Media (regla obligatoria) |
| A7 | Sin métricas/observabilidad de eventos consumidos vs fallidos | `AnalysisEventProcessor.cs` | Media |
| A8 | TODOs en producción ("Phase 3 - Update Alignment entity when Alignment domain is created") | `AnalysisEventProcessor.cs:274,308` | Media |

### Usage
| # | Smell | Ubicación | Severidad |
|---|---|---|---|
| U1 | `GetDashboardStatsQuery` bypassa cache Redis y consulta DB directo | `GetDashboardStatsQueryHandler.cs:18-19` | Alta |
| U2 | Bare `catch (Exception ex)` retorna `null` en error Redis | `RedisUsageStatsRepository.cs:103-107` | Alta |
| U3 | Hardcoded Redis key prefix `"usage:{userId}"` | `RedisUsageStatsRepository.cs:230` | Baja |
| U4 | `BillingPeriodKey.Parse` usa `int.Parse` sin try-catch | `BillingPeriodKey.cs:50` | Media |
| U5 | `UsageStatsEventProcessor` mezcla parsing JSON + routing + storage | `UsageStatsEventProcessor.cs` | Media |
| U6 | Probar 10+ variaciones de nombre de propiedad para encontrar `userId` (heurística frágil) | `UsageStatsEventProcessor.cs` | Alta |
| U7 | Sin idempotencia en eventos de increment (si se reentrega, double-count) | `UsageStatsEventProcessor.cs` | Alta |
| U8 | Endpoints admin sin protección visible (`/admin/usage/...`) — verificar autorización | `UsageEndpoints.cs:44-73` | **Crítica seguridad** |
| U9 | Comentarios inline no-doc | varios | Media |

---

## 3. Problemas arquitectónicos

### Analysis
1. **`AnalysisEventProcessor` es el cuello de botella mantenibilidad y observabilidad**: 452 líneas mezclando consumer Redis, parser de Python dicts (gracias a `RedisEventBusSubscriber`), routing por tipo, actualización de Trace aggregate. Un solo bug en parsing tira todo el procesamiento de analysis.
2. **Estimación de Q20/Q30 mezclada con consumo de eventos**: si el worker Python ya manda Q20/Q30 reales, ¿por qué estimamos? Si no los manda, debe mandarlos. La estimación heurística no debería estar en .NET.
3. **Sin DLQ ni retry policy**: un evento mal formado se pierde silenciosamente.
4. **`RequestAnalysisCommand` no recibe correlation-id explícito**: difícil rastrear request → job → completion event.

### Usage
1. **Doble fuente de verdad**: Usage tiene Redis (counters cacheados via events) y `GetDashboardStatsQuery` consulta DB live. **Decisión arquitectónica pendiente**: ¿Redis es la fuente única o un cache?
2. **Sin idempotencia en increments**: Redis Streams con consumer groups ofrece "at least once" — los increments deben ser idempotentes (almacenar `last_processed_event_id` por counter).
3. **Endpoints admin sin protección clara**: peligroso. Sync/init pueden corromper datos si los llama un usuario no autorizado.
4. **`BillingPeriodKey` es un value object pero no cumple invariantes**: `Parse` lanza `FormatException` no manejado.

---

## 4. Plan de refactorización priorizado

### Fase 1 — Cambios seguros
| Tarea | Archivo(s) | Justificación |
|---|---|---|
| 1.1 Eliminar comentarios no-doc en ambos módulos | todos | Regla obligatoria |
| 1.2 Resolver TODOs de Alignments en `AnalysisEventProcessor` (depende del plan de Alignments — ver módulo cross-cutting) | `AnalysisEventProcessor.cs:274,308` | No-TODO en main |
| 1.3 Catch tipados (`JsonException`, `RedisException`, `RedisTimeoutException`) en lugar de `catch(Exception)` | `AnalysisEventProcessor.cs`, `RedisUsageStatsRepository.cs` | Manejo explícito |
| 1.4 `BillingPeriodKey.Parse` retorna `Result<BillingPeriodKey, Error>`, sin lanzar | `Domain/Usage/BillingPeriodKey.cs` | Result Pattern coherente |
| 1.5 Constantes nombradas para magic numbers (`QualityEstimation.MeanScoreToQ20Multiplier = 0.80m`, etc.) | `Infrastructure/Analysis/Constants/` | Si la estimación se mantiene, al menos nombrada |
| 1.6 Verificar autorización en endpoints admin de Usage (`IRequireAdminRole` marker + behavior) | `UsageEndpoints.cs:44-73` | **Seguridad** |
| 1.7 Quitar `await` sobre métodos sync en `AnalysisEventProcessor` | `AnalysisEventProcessor.cs:361` | Compilación correcta |
| 1.8 Logging estructurado con event_id, event_type, correlation_id en ambos processors | ambos | Observabilidad mínima |

### Fase 2 — Mejoras de diseño

#### Analysis
| Tarea | Archivo(s) | Justificación |
|---|---|---|
| 2.A.1 **Descomponer `AnalysisEventProcessor`** en 4 clases:<br>- `AnalysisEventDispatcher` (consumer + routing)<br>- `TraceCompletionEventHandler` (eventos de traces)<br>- `AlignmentCompletionEventHandler` (eventos de alignments — solo si Alignments se completa)<br>- `AnalysisCompletionEventHandler` (eventos de analysis genéricos)<br>Cada handler es una clase < 100 líneas | `Infrastructure/Analysis/Handlers/` | God Service → cohesión |
| 2.A.2 Definir DTOs tipados para eventos del worker Python (no parsear JSON ad-hoc): `TraceProcessedEventPayload`, `AnalysisCompletedEventPayload` | `Infrastructure/Analysis/Contracts/` | Contratos explícitos |
| 2.A.3 Eliminar estimación heurística de Q20/Q30 en .NET. **Negociar con worker Python** que envíe siempre Q20/Q30 reales en `quality_metrics` payload. Si no es posible, dejar la estimación pero aislarla en `IQualityScoreEstimator` | `AnalysisEventProcessor.cs:183-189` | Lógica de bioinformática vive en Python |
| 2.A.4 DLQ para eventos mal formados o que fallan N veces: `geneflow:events:dlq:analysis` | `Infrastructure/Analysis/Handlers/`, infra Redis | Sin pérdida silenciosa |
| 2.A.5 Métrica de eventos consumidos / fallidos / DLQ (`IMetricsRecorder` o Prometheus) | cross-cutting metrics | Observabilidad |
| 2.A.6 Correlation-id en `RequestAnalysisCommand` propagado al `AnalysisJob` y de vuelta al evento de finalización | `Application/Analysis/Commands/RequestAnalysis/`, `RedisJobPublisher` | Trazabilidad end-to-end |

#### Usage
| Tarea | Archivo(s) | Justificación |
|---|---|---|
| 2.U.1 **Decidir fuente de verdad**: Redis es cache, DB es source of truth → `GetDashboardStatsQuery` consulta Redis con fallback a DB. **O bien**: Redis es la única fuente, eliminar query a DB y rellenar Redis vía `UsageStatsEventProcessor` autoritativamente | `GetDashboardStatsQueryHandler.cs`, `RedisUsageStatsRepository.cs` | Coherencia |
| 2.U.2 **Idempotencia de increments**: añadir `last_event_id` por counter en hash Redis. Si event_id ≤ last_event_id → ignorar. | `RedisUsageStatsRepository.cs`, `UsageStatsEventProcessor.cs` | Sin double-counting |
| 2.U.3 Eliminar heurística "10 nombres posibles para userId" en `UsageStatsEventProcessor`. Establecer **contrato de evento** con campo `user_id` obligatorio (ya existe en eventos .NET, validar en Python) | `UsageStatsEventProcessor.cs` | Contratos firmes |
| 2.U.4 Constante para Redis key prefix: `RedisKeys.Usage(userId)` | `Infrastructure/Usage/RedisKeys.cs` (NUEVO) | Centralización |
| 2.U.5 `IUsageStatsCache` interface (Application) para decoupling de Redis específico | `Application/Usage/Abstractions/` | Testabilidad |
| 2.U.6 Endpoints admin de Usage detrás de behavior `IRequireAdminRole` con auth real | `UsageEndpoints.cs` | Seguridad |
| 2.U.7 Validación: `BillingPeriodKey` debe ser VO inmutable con `TryParse` y `Result` | `Domain/Usage/BillingPeriodKey.cs` | Robustez |

### Fase 3 — Refactor arquitectónico
| Tarea | Recomendación |
|---|---|
| 3.1 Mover `AnalysisEventProcessor` y `UsageStatsEventProcessor` a un proyecto separado `GeneFlow.ApiNet2.Workers` | **Solo si crecen significativamente** — hoy un BackgroundService basta |
| 3.2 Reemplazar Redis Streams por broker dedicado (RabbitMQ, Kafka) | **NO** — Redis Streams es suficiente para este volumen |
| 3.3 Saga / Process Manager para flujo Analysis (request → progress → completion → trace update) | **NO** — flujo es lineal |
| 3.4 Materializar `BillingDashboard` como read model en lugar de calcular on-demand | **Solo bajo evidencia de slow query** |

---

## 5. Patrones aplicables

| Patrón | Dónde | Por qué SÍ |
|---|---|---|
| **Event Handler Decomposition** | Split de `AnalysisEventProcessor` | God Service evidente |
| **DLQ** | Eventos mal formados de Python | Pérdida silenciosa hoy |
| **Idempotent Consumer** | `UsageStatsEventProcessor` increments | At-least-once delivery |
| **Adapter / DTO** | Contratos tipados con worker Python | Heurística frágil hoy |
| **Cache-aside** (con decisión) | Usage Redis vs DB | Coherencia |
| **Correlation ID** | Analysis request → job → event | Trazabilidad |

**NO aplicar:**
- Saga/Process Manager — overkill.
- Event Sourcing.
- Broker dedicado (Kafka/RabbitMQ) — Redis Streams es suficiente.
- Microservicio de Workers separado — premature.

---

## 6. Estrategia de testing

**Antes:**
- Tests para `RequestAnalysisCommand` ya cubren ~22 casos.
- Tests para `GetBillingUsageQuery` y `GetDashboardStatsQuery` existen.
- Faltan tests para:
  - `AnalysisEventProcessor` con eventos malformados (rechazo o DLQ).
  - `UsageStatsEventProcessor` con eventos duplicados (idempotencia).
  - `BillingPeriodKey.Parse` con strings inválidos.
  - Redis fault simulation en `RedisUsageStatsRepository`.

**Durante Fase 2.A.1 (split processor):**
- Tests unitarios por handler (TraceCompletion, AnalysisCompletion).
- Tests de routing (dispatcher selecciona handler correcto).

**Durante Fase 2.U.2 (idempotencia):**
- Test: enviar mismo evento 5 veces → counter incrementa 1 vez.
- Test: enviar evento con event_id menor que last_event_id → ignora.

**Después:**
- Mutation testing en handlers de eventos.
- Test de carga: 1000 eventos analysis paralelos → 0 perdidos, 0 doble-procesados.

---

## 7. Orden de ejecución

```
Fase 1 (orden estricto):
  1.6 (autorización admin Usage) ← URGENTE seguridad
  1.1 (comentarios)
  1.2 (TODOs Alignments — depende plan Alignments)
  1.3 (catch tipados)
  1.4 (BillingPeriodKey con Result)
  1.7 (await sobre sync)
  1.5 (constantes Q20/Q30)
  1.8 (logging estructurado)

Fase 2 — Analysis:
  2.A.6 (correlation-id)
  2.A.2 (DTOs tipados)
  2.A.1 (split processor)
  2.A.4 (DLQ)
  2.A.3 (eliminar estimación Q20/Q30)
  2.A.5 (métricas)

Fase 2 — Usage (paralelo):
  2.U.1 (decidir fuente de verdad)
  2.U.4 (RedisKeys constantes)
  2.U.5 (IUsageStatsCache)
  2.U.7 (BillingPeriodKey VO robusto)
  2.U.3 (contrato evento user_id)
  2.U.2 (idempotencia increments)
  2.U.6 (auth admin endpoints — refuerzo Fase 1.6)

Fase 3:
  No ejecutar salvo evidencia.
```

---

## 8. Cambios NO recomendados

- Reescribir orchestration de Analysis como Saga.
- Migrar a Kafka/RabbitMQ.
- Mover counters de Usage a tabla SQL pura — perdería atomicidad de Redis.
- Calcular bioinformática en .NET.
- Implementar UI de admin para Usage en este backend (dashboard separado).
- Generic `IEventHandler<T>` framework — los handlers actuales son pocos y específicos.
- Reactive Extensions (`IObservable<TraceEvent>`) — paradigma fuera del estilo del codebase.
- Outbox para eventos de Usage — son cache, no críticos.

---

## 9. Métricas de calidad sugeridas

| Métrica | Hoy | Objetivo |
|---|---|---|
| `AnalysisEventProcessor.cs` líneas | 452 | n/a (split) |
| Líneas máx por handler post-split | n/a | < 120 |
| `UsageStatsEventProcessor.cs` líneas | 305 | < 200 |
| Bare catches en módulos | 3+ | 0 |
| Eventos de Usage idempotentes | No | Sí |
| Endpoints admin sin auth | 3+ | 0 |
| TODOs de Alignments en producción | 2 | 0 |
| Métricas/observabilidad de eventos | 0 | sí (count, fail, DLQ) |
| Heurística userId en Python events | 10 variants | 1 (`user_id` requerido) |
| Comentarios no-doc | varios | 0 |

---

## 10. Resultado final esperado

```
Application/Analysis/
├── Commands/
│   ├── RequestAnalysis/
│   │   ├── RequestAnalysisCommand.cs           (con CorrelationId)
│   │   └── RequestAnalysisCommandHandler.cs
│   └── RequestAlignment/                       (sin cambios)
└── ... (sin Domain layer, correcto)

Infrastructure/Analysis/
├── AnalysisEventDispatcher.cs                  (~80 líneas — consumer + routing)
├── Handlers/
│   ├── TraceCompletionEventHandler.cs          (~100 líneas)
│   ├── AlignmentCompletionEventHandler.cs      (~80 líneas — solo si Alignments completo)
│   └── AnalysisCompletionEventHandler.cs       (~80 líneas)
├── Contracts/
│   ├── TraceProcessedEventPayload.cs           (DTO tipado del worker)
│   ├── AnalysisCompletedEventPayload.cs
│   └── QualityMetricsPayload.cs
└── Constants/
    └── QualityEstimation.cs                    (si aplica)

Domain/Usage/
├── UsageStats.cs                               (sin cambios estructurales)
├── BillingPeriodKey.cs                         (con TryParse + Result)
├── Errors/
│   └── BillingPeriodErrors.cs

Application/Usage/
├── Abstractions/
│   └── IUsageStatsCache.cs                     (NUEVO — desacopla Redis)
├── Queries/
│   ├── GetBillingUsage/
│   └── GetDashboardStats/                      (con decisión Fase 2.U.1 aplicada)

Infrastructure/Usage/
├── RedisUsageStatsRepository.cs                (sin bare catch, idempotente)
├── RedisKeys.cs                                (constantes)
└── UsageStatsEventProcessor.cs                 (~180 líneas, idempotente, contrato firme)

API/Endpoints/Usage/
└── UsageEndpoints.cs                           (admin endpoints con IRequireAdminRole)

API/Endpoints/Analysis/
└── AnalysisEndpoints.cs                        (sin cambios)
```

**Beneficios concretos:**
- `AnalysisEventProcessor` 452 → 4 archivos < 120 líneas cada uno.
- DLQ para eventos malformados (sin pérdida silenciosa).
- Idempotencia real en counters de Usage (no double-counting).
- Endpoints admin de Usage con autorización efectiva.
- Contratos tipados con worker Python (sin heurística).
- Observabilidad: métricas de eventos consumidos / fallidos.
- Trazabilidad end-to-end vía correlation-id.
- `BillingPeriodKey.Parse` no lanza, retorna Result.

**Lo que NO cambia:**
- Decisión de Python como worker.
- Redis Streams como bus de eventos y jobs.
- Decisión de no tener Domain en Analysis.
- Modelo de `UsageStats` aggregate.
- Endpoints públicos (solo admin se refuerza).
