# Plan Maestro de Refactorización — GeneFlow.ApiNet2

> Documento orquestador. Cada módulo tiene su plan detallado en `REFACTORING_PLAN_<MODULO>.md`.
> **Reglas globales obligatorias:** una clase por archivo · sin comentarios que no sean XML docs · sin sobreingeniería · sin breaking changes en contratos públicos.

---

## 1. Inventario de planes por módulo

| Módulo | Archivo de plan | Tamaño relativo | Riesgo refactor | Prioridad |
|---|---|---|---|---|
| **Identity** | `REFACTORING_PLAN_IDENTITY.md` | Grande | Medio | Alta |
| **Profiles** | `REFACTORING_PLAN_PROFILES.md` | Pequeño | Bajo | Media |
| **Studies** | `REFACTORING_PLAN_STUDIES.md` | Grande | Medio | Alta |
| **Pipelines** | `REFACTORING_PLAN_PIPELINES.md` | Mediano | Medio | Media-Alta |
| **Traces** | `REFACTORING_PLAN_TRACES.md` | Muy grande | Alto | Alta |
| **Billing (Subs+Plans+PMs)** | `REFACTORING_PLAN_BILLING.md` | Mediano | **Crítico** (financiero) | **Crítica** |
| **Analysis + Usage** | `REFACTORING_PLAN_ANALYSIS_USAGE.md` | Mediano | Medio | Alta |
| **Cross-cutting + Alignments** | `REFACTORING_PLAN_CROSS_CUTTING.md` | Mediano | Medio | Alta |

---

## 2. Decisiones bloqueantes (Fase 0 transversal)

Las siguientes decisiones **deben tomarse antes** de tocar código. Cada una bloquea Fase 2 de su módulo:

| # | Decisión | Bloquea | Recomendación |
|---|---|---|---|
| D1 | **Billing**: ¿Stripe Billing gestiona cobro o backend orquesta? | Billing Fase 2 | Stripe Billing (Opción A) |
| D2 | **Alignments**: ¿completar / eliminar / aislar? | Cross-cutting Fase 2, Analysis Fase 1.2, Usage referencias | Eliminar de main + branch feature/ si interesa futuro |
| D3 | **Usage source of truth**: ¿Redis es cache o autoritativo? | Usage Fase 2 | Redis autoritativo + DB para reconciliación periódica |
| D4 | **Worker Python contract**: ¿se puede negociar JSON estricto + `user_id` obligatorio? | Cross-cutting Fase 2.1, Analysis Fase 2, Usage Fase 2.U.3 | Sí — negociar con equipo Python |
| D5 | **Outbox compartido**: ¿se introduce ahora o se espera evidencia? | Pipelines Fase 2.2, Traces Fase 2 (eventos), Billing Fase 2.A.3, Cross-cutting Fase 3.1 | Introducir ahora — beneficia múltiples módulos |

---

## 3. Orden de ejecución global

### Bloque 0 — Decisiones (sin código)
1. Resolver D1, D2, D3, D4, D5 con stakeholders técnicos y producto.
2. Documentar decisiones en `docs/architecture-decisions/` (formato ADR).
3. Establecer baseline de métricas (líneas, complejidad, cobertura) para comparar después.

### Bloque 1 — Higiene global (paralelizable, riesgo bajo)
**Cumplir reglas obligatorias en TODO el codebase, no solo Identity.**

| Paso | Tarea | Aplica a |
|---|---|---|
| 1.1 | Eliminar todos los comentarios no-doc | Todos los módulos |
| 1.2 | Garantizar una clase pública por archivo | Identity (OAuthEndpoints), revisar otros |
| 1.3 | Particionar archivos `XxxErrors.cs` > 100 líneas por subdominio | Identity, Profiles, Studies, Traces |
| 1.4 | Constantes nombradas para magic numbers | Todos |
| 1.5 | Catch tipados (no `catch(Exception)`) | Identity, Pipelines (StepConfig), Traces (TraceAnalysisService), Analysis, Usage, Cross-cutting |
| 1.6 | XML docs en métodos / clases públicas sin documentar | Todos |
| 1.7 | Fail-fast en stubs de integración (Stripe, WorkerApiKey) | Billing, Cross-cutting |

**Checkpoint:** ejecutar `dotnet build` con 0 warnings, `dotnet test` 100% verde.

### Bloque 2 — Seguridad y atomicidad (urgente)
**Cierra brechas de seguridad y consistencia descubiertas.**

| Paso | Tarea | Módulo |
|---|---|---|
| 2.1 | **Email enumeration** en login → respuesta genérica | Identity |
| 2.2 | **Endpoints admin de Usage** detrás de `IRequireAdminRole` | Usage |
| 2.3 | **Idempotency-key en operaciones de Trace processing** (Start/Complete/Fail) | Traces |
| 2.4 | **Idempotency-key en operaciones de Subscription** (ChangePlan/Cancel) | Billing |
| 2.5 | **Race condition en SetAsDefault PaymentMethod** → transaction wrap + advisory lock | Billing |
| 2.6 | **Idempotencia de Usage increments** (last_event_id por counter) | Usage |
| 2.7 | **Validación de PaymentMethod activo** antes de Subscription paid | Billing |
| 2.8 | **Webhook signature verification de Stripe** (si Opción A) | Billing |

**Checkpoint:** revisión de seguridad por par antes de mergear.

### Bloque 3 — Descomposición de God Classes/Methods
**Reduce los archivos grandes identificados.**

| Tarea | De | A | Módulo |
|---|---|---|---|
| 3.1 | `User.cs` 554 líneas → ~300 (extraer LoginAttemptTracker, enriquecer TwoFactorAuth) | 554 | < 350 | Identity |
| 3.2 | `OAuthLoginCommandHandler.cs` 256 → ~80 (pipeline interno) | 256 | < 80 | Identity |
| 3.3 | `Trace.cs` 513 → ~330 (VO collections para edits/annotations/trims) | 513 | < 350 | Traces |
| 3.4 | `TraceEditingEndpoints.cs` 402 → 3 archivos < 200 | 402 | n/a (split) | Traces |
| 3.5 | `StudyRepository.cs` 500 (46 métodos) → < 350 (Specifications) | 500/46 | < 350/<25 | Studies |
| 3.6 | `Study.cs` 447 → ~330 (StudyTagSet, StudyMetrics enriched) | 447 | < 350 | Studies |
| 3.7 | `PipelineEndpoints.cs` 514 → 3 archivos < 200 | 514 | n/a (split) | Pipelines |
| 3.8 | `AnalysisEventProcessor.cs` 452 → 4 archivos < 120 | 452 | n/a (split) | Analysis |
| 3.9 | `RedisEventBusSubscriber.cs` 316 → ~150 (eliminar Python parser) | 316 | < 200 | Cross-cutting |
| 3.10 | `OAuthEndpoints.cs` (3 DTOs inline) → split por archivo | 1 | 4 | Identity |

### Bloque 4 — Patrones cross-module
**Introduce abstracciones compartidas que benefician múltiples módulos.**

| Paso | Tarea | Beneficia |
|---|---|---|
| 4.1 | `Infrastructure/Idempotency/` con `IIdempotencyStore` | Traces, Billing |
| 4.2 | `Infrastructure/Outbox/` con `OutboxMessage` + `OutboxRelay` | Pipelines, Traces, Billing |
| 4.3 | `Infrastructure/Locking/` con `IDistributedLock` (Redis) | Billing, Usage |
| 4.4 | `SharedKernel/Application/ResultFactory.cs` para Behaviors | Cross-cutting (4 behaviors) |
| 4.5 | `SharedKernel/Application/PagedRequestDefaults.cs` | Pipelines, Studies, Traces, Profiles |
| 4.6 | `IUserProfileLookup` para enriquecer DTOs cross-context | Studies, Traces (annotations) |

### Bloque 5 — Refactor por módulo (paralelizable)
Cada módulo ejecuta su propia Fase 2 según su plan, en cualquier orden:
- Identity Fase 2
- Profiles Fase 2 (corto)
- Studies Fase 2
- Pipelines Fase 2
- Traces Fase 2
- Billing Fase 2 (depende de D1)
- Analysis Fase 2
- Usage Fase 2 (depende de D3)
- Cross-cutting Fase 2 (depende de D2, D4)

**Regla:** ningún PR de Bloque 5 mergea sin tests verdes y revisión por par.

### Bloque 6 — Refactor arquitectónico (Fase 3 por módulo, opcional)
**Solo si la métrica/evidencia justifica.** No se ejecuta por defecto.
- Identity 3.1/3.2/3.3 — solo bajo evidencia.
- Studies 3.1 (StudyEngagement separado) — bajo crecimiento real.
- Traces 3.1 (owned → entidades hijas) — si workarounds EF Core duelen.
- Billing 3.1 (BillingContext unificado) — recomendado tras Fase 2 estable.
- Analysis/Usage 3.x — solo bajo escala.
- Cross-cutting 3.1 (Outbox global) — ya cubierto por Bloque 4.2.

---

## 4. Métricas globales sugeridas

| Métrica | Hoy (estimado) | Objetivo post-Bloque 5 |
|---|---|---|
| Archivos > 400 líneas | 6+ (User, Trace, Study, StudyRepo, AnalysisEventProcessor, TraceEditingEndpoints, PipelineEndpoints, OAuthLoginHandler, RedisEventBusSubscriber) | 0 |
| Archivos con > 1 clase pública | varios (OAuthEndpoints, otros por revisar) | 0 |
| Comentarios no-doc | cientos | 0 |
| Métodos con bare `catch(Exception)` | 5+ | 0 |
| Magic numbers sin nombrar | docenas | < 10 |
| Operaciones críticas sin idempotency-key | 6+ (Stripe, processing) | 0 |
| Tests Application | ~1.096 ✓ | mantener 100% |
| Tests Domain | ~2.243 ✓ | mantener 100% |
| Tests Infrastructure | 12 fallando (Postgres) | 100% (post setup CI) |
| Tests API | 48 fallando (mocks) | 100% (post setup mocks) |
| Tests E2E | 69 fallando (Postgres) | 100% (post setup) |
| Cobertura global | ? | ≥ 80% global, ≥ 90% Domain |
| Mutation score Domain (Stryker) | ? | > 70% |
| Warnings de compilación | ~5 (NU1603, IDE0005) | 0 |
| TODOs en producción | varios (Alignments, etc.) | 0 |
| God Aggregates (>400 líneas) | 3 (User, Trace, Study) | 0 |

---

## 5. Anti-patrones globales prohibidos

Aplican a **todos los módulos**, no se discuten:

1. **NO** introducir AutoMapper.
2. **NO** introducir Repository genérico `IRepository<T>`.
3. **NO** Event Sourcing en ningún módulo.
4. **NO** microservicios separados (este es un monolito modular intencional).
5. **NO** CQRS con BDs separadas.
6. **NO** reemplazar MediatR / Result Pattern / EF Core / PostgreSQL / Redis Streams sin razón documentada de producto.
7. **NO** Reactive Extensions / Akka.NET / paradigmas alternativos.
8. **NO** Feature Flags para activar/desactivar refactor en runtime — se hace por commits.
9. **NO** rewrite de aggregates desde cero.
10. **NO** abstracciones preventivas ("Service Bus interface por si migramos a Kafka").
11. **NO** renames masivos por consistencia estética.
12. **NO** romper contratos REST públicos sin versioning.

---

## 6. Quality gates por PR

Cada PR de refactor debe cumplir, sin excepciones:

- [ ] `dotnet build` con 0 errors, 0 warnings nuevos.
- [ ] `dotnet test` 100% verde (suite completa que aplique al cambio).
- [ ] 0 comentarios no-doc en archivos tocados.
- [ ] 0 archivos con > 1 clase pública en archivos tocados.
- [ ] Cobertura no decrece (vs baseline anterior).
- [ ] Si toca dominio/lógica crítica: tests añadidos antes del refactor.
- [ ] Si toca seguridad / billing: revisión por par obligatoria.
- [ ] Mensaje de commit referencia tarea numerada del plan correspondiente (ej.: `refactor(traces): #2.1 add idempotency-key to processing endpoints`).
- [ ] No se introducen anti-patrones del punto 5.

---

## 7. Roadmap recomendado por módulos (ejecución por sprints lógicos)

### Sprint A — Higiene global + decisiones
- Bloque 0 (decisiones D1-D5).
- Bloque 1 (higiene global) — paralelizable entre devs por módulo.

### Sprint B — Seguridad/atomicidad (urgente)
- Bloque 2 (todas las tareas de seguridad y race conditions).

### Sprint C — Patrones compartidos
- Bloque 4 (Idempotency, Outbox, Locking, ResultFactory, PagedRequestDefaults).

### Sprint D — Descomposición God Classes
- Bloque 3 (todas las descomposiciones).

### Sprint E+ — Refactors por módulo
- Bloque 5 dividido por módulo, cada equipo/dev toma uno.

### Sprint Z (opcional / on-demand)
- Bloque 6 — refactor arquitectónico bajo evidencia explícita.

---

## 8. Cómo medir el éxito

**Inicio del proyecto:**
- Capturar baseline de métricas del punto 4.
- Snapshot de archivos > 400 líneas: `User.cs`, `Trace.cs`, `Study.cs`, `StudyRepository.cs`, `AnalysisEventProcessor.cs`, `TraceEditingEndpoints.cs`, `PipelineEndpoints.cs`, `OAuthLoginCommandHandler.cs`, `RedisEventBusSubscriber.cs`.

**Tras cada sprint:**
- Re-medir métricas.
- Diff de líneas por archivo crítico.
- Cobertura ↑.
- Mutation score ↑.

**Tras Bloque 5 completo:**
- 0 archivos > 400 líneas.
- 0 archivos > 1 clase.
- 0 comentarios no-doc.
- 0 magic numbers críticos sin nombrar.
- 0 operaciones críticas sin idempotency-key.
- 0 TODOs en main.
- Cobertura global ≥ 80%, Domain ≥ 90%.
- Mutation score Domain > 70%.

---

## 9. Riesgos y mitigaciones

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| Refactor de Identity rompe auth de usuarios existentes | Baja | Alto | Tests E2E auth + canary deploy |
| Cambios en Trace processing pierden eventos en flight | Media | Alto | Idempotency-key + DLQ desde Bloque 2/4 |
| Webhooks de Stripe procesados duplicados | Alta sin Bloque 2 | **Crítico (cobros)** | Bloque 2.4/2.8 obligatorios antes de prod |
| Eliminar Alignments rompe algún consumer no descubierto | Baja | Medio | Decisión D2 con búsqueda completa de referencias antes de eliminar |
| Refactor de StudyRepository rompe queries existentes | Media | Medio | Tests de integración con PostgreSQL antes de Specifications |
| Worker Python no acepta cambio de contrato | Media | Medio | Coordinar con equipo Python en D4; aislar parser si no se puede |
| Outbox introduce latencia perceptible | Baja | Bajo | Benchmarks antes/después; outbox solo donde aporta valor |
| Tests no cubren casos críticos descubiertos durante refactor | Media | Medio | Red de seguridad antes de tocar (red-green-refactor invertido) |

---

## 10. Documentos relacionados

- `REFACTORING_PLAN_IDENTITY.md` — módulo Identity.
- `REFACTORING_PLAN_PROFILES.md` — módulo Profiles.
- `REFACTORING_PLAN_STUDIES.md` — módulo Studies.
- `REFACTORING_PLAN_PIPELINES.md` — módulo Pipelines.
- `REFACTORING_PLAN_TRACES.md` — módulo Traces.
- `REFACTORING_PLAN_BILLING.md` — Subscriptions + Plans + PaymentMethods.
- `REFACTORING_PLAN_ANALYSIS_USAGE.md` — Analysis + Usage.
- `REFACTORING_PLAN_CROSS_CUTTING.md` — Events, Jobs, Redis, Storage, DI, Behaviors, Alignments.
- `CLAUDE.md` — convenciones existentes del proyecto.
- `INTEGRATION_PLAN.md`, `MIGRATION_PLAN.md` — contexto histórico (no confundir con este plan).

---

## Próximo paso

1. Revisar y aprobar este plan maestro y los 8 planes por módulo.
2. Resolver Bloque 0 (decisiones D1-D5).
3. Comenzar Bloque 1 (higiene global) como primer sprint paralelo.

**No se ejecutan cambios de código hasta aprobación explícita.**
