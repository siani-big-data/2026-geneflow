# Plan de Refactorización — Módulo Traces

> Reglas obligatorias: **una clase por archivo**, **sin comentarios que no sean XML docs**, **sin sobreingeniería**.

---

## 1. Diagnóstico general

Módulo **central del producto bioinformático** — el más complejo funcionalmente. Aggregate `Trace` (513 líneas, **el más grande del codebase tras Identity**), 17 commands + 15 queries, 5 archivos de endpoints (1.119 líneas totales), 15 eventos de dominio, integración con file storage para archivos SCF/AB1.

**Fortalezas:**
- Modelado DDD sólido: `Trace` agregado raíz con owned entities (`SequenceEdit`, `TraceAnnotation`, `TraceTrim`).
- State machine explícita (`TraceStatus.CanTransitionTo()`).
- Repository con métodos especializados de carga (`GetByIdWithEdits`, `GetByIdWithAnnotations`, `GetByIdWithAll`, `GetByIdWithTrims`) — evita N+1 y cargas innecesarias.
- Value object `QualityMetrics` con helpers (`IsGoodQuality`, `IsHighQuality`).
- Cobertura de tests Domain + Application excelente (30+ archivos).

**Debilidades:**
- **`Trace.cs` 513 líneas** — maneja lifecycle, edits, annotations, trims, archive, name update. God Aggregate.
- **`TraceEditingEndpoints.cs` 402 líneas** mezcla 3 dominios distintos (trims, sequence edits, reverse complement).
- **5 archivos de endpoints** sin documentación clara de quién hace qué — overlap potencial.
- **Workarounds EF Core** (`DeleteAnnotationAsync`, `DeleteTrimAsync` bypass ORM) — síntoma de modelo de owned entities mal ajustado.
- **Workflow de procesamiento opaco**: webhook-driven desde worker Python, pero no documentado, sin protección contra eventos duplicados.
- **Path de storage hardcodeado** en endpoint (`traces/{traceId}/original{extension}`).
- **Magic numbers de calidad** (Q20=20, Q30=30) hardcoded en `QualityMetrics`.

**Veredicto:** Refactor de **alta prioridad** por complejidad y centralidad. Foco en descomponer `Trace`, separar endpoints, blindar el workflow de procesamiento.

---

## 2. Code Smells detectados

| # | Smell | Ubicación | Severidad |
|---|---|---|---|
| 1 | God Aggregate (513 líneas, 4 sub-dominios: edits, annotations, trims, lifecycle) | `Domain/Traces/Trace.cs` | Alta |
| 2 | God endpoint file (402 líneas, 3 dominios mezclados) | `API/Endpoints/Traces/TraceEditingEndpoints.cs` | Alta |
| 3 | Workarounds EF Core para borrar owned entities | `TraceRepository.DeleteAnnotationAsync`, `DeleteTrimAsync` | Alta |
| 4 | Magic numbers (`MaxFileSizeBytes = 10MB`, `Q20`, `Q30`) | `TraceFile.cs`, `QualityMetrics.cs` | Media |
| 5 | Storage path hardcoded en endpoint | `TraceEndpoints.cs:192` (`traces/{traceId}/original{extension}`) | Alta |
| 6 | Checksum SHA256 calculado en endpoint (no en servicio) | `TraceEndpoints.cs UploadTrace` | Media |
| 7 | `TraceRepository.GetByStudyAsync` con 8 parámetros | `TraceRepository.cs` | Media |
| 8 | Workflow de processing sin idempotencia visible (`StartTraceProcessing`, `CompleteTraceProcessing`, `FailTraceProcessing` pueden recibir eventos duplicados) | `Application/Traces/Commands/...` | Alta |
| 9 | Bare/general catch en `TraceAnalysisService` (lee `.analysis.json` y traga errores) | `Infrastructure/Traces/Services/TraceAnalysisService.cs` | Media |
| 10 | Comentarios inline no-doc | varios | Media (regla obligatoria) |
| 11 | DTOs probablemente con info derivada (revisar `TraceDto` para no exponer storage paths internos) | `Application/Traces/DTOs/` | Media (seguridad) |
| 12 | Workflow externo no documentado en CLAUDE.md ni en código | global | Media |

---

## 3. Problemas arquitectónicos

1. **`Trace` aggregate sobrecargado**: maneja 4 sub-dominios cohesivos pero distintos (lifecycle/processing, sequence editing, annotations, trims). Cada uno podría ser un VO/sub-aggregate enriquecido.
2. **Workflow de procesamiento webhook-based sin protección**: el worker Python llama endpoints `/traces/{id}/processing/start|complete|fail`. Si la red reintenta, se llama dos veces. No hay idempotency-key. Riesgo de transiciones de estado inválidas o duplicación de `QualityMetrics`.
3. **API layer conoce convenciones de storage**: `traces/{traceId}/original{extension}` está en el endpoint. Cualquier cambio de convención requiere cambio en API. Debería estar en `IFileStorageService` o un `ITraceFileLocator`.
4. **Cálculo de checksum en endpoint**: lectura completa del stream + SHA256 inline en el endpoint. Pertenece a un servicio (`IFileChecksumCalculator`) y idealmente pipelineado con el upload.
5. **Repository con queries muy parametrizadas**: `GetByStudyAsync(studyId, search, status, format, sortBy, sortDir, page, size)` — Specification clamando por existir.
6. **`TraceAnalysisService` come errores**: leer `.analysis.json` y devolver `null` en error → el caller no sabe si es "no procesado aún" vs "corrupto" vs "Redis caído".
7. **Owned entities con bypass de ORM** (`DeleteAnnotationAsync`, `DeleteTrimAsync`): si EF Core no puede borrar correctamente entidades owned, posiblemente deberían ser **entidades hijas con FK explícita** (no owned), permitiendo CRUD normal.

---

## 4. Plan de refactorización priorizado

### Fase 1 — Cambios seguros
| Tarea | Archivo(s) | Justificación |
|---|---|---|
| 1.1 Eliminar comentarios no-doc del módulo Traces | todos | Regla obligatoria |
| 1.2 Constantes nombradas para magic numbers de calidad y file size: `QualityThresholds.Q20Score = 20`, `Q30Score = 30`, `TraceFileLimits.MaxSizeBytes` | `Domain/Traces/Constants/` (NUEVO) | Configurabilidad futura |
| 1.3 **Split `TraceEditingEndpoints.cs` (402 líneas) en 3 archivos**: `TraceTrimEndpoints.cs`, `TraceSequenceEditEndpoints.cs`, `TraceComplementEndpoints.cs` | `API/Endpoints/Traces/` | Cohesión, regla de archivos manejables |
| 1.4 Documentar el workflow externo (Python worker) en CLAUDE.md o en `docs/` (sequence diagram simple) | `docs/trace-processing-workflow.md` (NUEVO) | Memoria del sistema |
| 1.5 Particionar `TraceErrors.cs` (107 líneas) en `TraceLifecycleErrors`, `TraceEditErrors`, `TraceAnnotationErrors`, `TraceTrimErrors` | `Domain/Traces/Errors/` | Cohesión |
| 1.6 Catch tipado en `TraceAnalysisService` (`FileNotFoundException`, `JsonException`) → devolver `Result<TraceAnalysis, Error>` en lugar de `null` | `Infrastructure/Traces/Services/TraceAnalysisService.cs` | Caller distingue casos |

### Fase 2 — Mejoras de diseño
| Tarea | Archivo(s) | Justificación |
|---|---|---|
| 2.1 **Idempotency-key en endpoints de processing**: `StartTraceProcessing`, `CompleteTraceProcessing`, `FailTraceProcessing` aceptan header `X-Idempotency-Key`. Persistir clave en tabla `processing_idempotency` con TTL. Si recibe misma clave → retornar resultado anterior | `API/Endpoints/Traces/TraceProcessingEndpoints.cs`, `Infrastructure/Idempotency/` (NUEVO compartido) | **Seguridad de workflow** — críticamente importante |
| 2.2 Validar transiciones de estado en cada command de processing: `StartTraceProcessing` solo válido si `Status == Uploaded`. Si recibe `Status == Processing`, retorna OK idempotente, no error | `Application/Traces/Commands/.../StartTraceProcessingCommandHandler.cs` etc | Idempotencia semántica |
| 2.3 `ITraceFileLocator` que centraliza convenciones (`GetOriginalPath(traceId, extension)`, `GetAnalysisJsonPath(traceId)`) | `Application/Traces/Abstractions/ITraceFileLocator.cs` (NUEVO), `Infrastructure/Traces/Services/TraceFileLocator.cs` (NUEVO) | API no conoce paths |
| 2.4 `IFileChecksumCalculator` extraído del endpoint | `Application/Traces/Abstractions/`, `Infrastructure/Traces/Services/` | Testabilidad |
| 2.5 Specification para `GetByStudyAsync`: `TracesByStudySpec(studyId, search, filters, sort, paging)` | `Infrastructure/Traces/Specifications/` | Reduce parámetros largos |
| 2.6 **Descomponer `Trace.cs`**: extraer comportamiento a sub-VOs:<br>- `TraceEditCollection` (gestiona `_edits`, `AddEdit`, `UndoEdit`, `UndoAllEdits`)<br>- `TraceAnnotationCollection` (gestiona `_annotations`)<br>- `TraceTrimCollection` (gestiona `_trims`)<br>El aggregate `Trace` queda como facade orquestando colecciones + lifecycle | `Domain/Traces/ValueObjects/` | God Aggregate → ~300 líneas |

### Fase 3 — Refactor arquitectónico
| Tarea | Recomendación |
|---|---|
| 3.1 Convertir `TraceAnnotation`, `TraceTrim`, `SequenceEdit` de **owned entities a entidades hijas con FK explícita** | **Recomendado** — los workarounds en repo son la prueba. EF Core es más predecible con entidades regulares para colecciones grandes |
| 3.2 Outbox para eventos de procesamiento (notificar UI vía SignalR/SSE) | **Solo si hay UI real-time activa** que dependa de los eventos |
| 3.3 Separar `TraceProcessingState` del `Trace` aggregate (saga pattern para tracking del worker externo) | **NO recomendado** — complejidad alta sin beneficio claro |
| 3.4 Procesamiento síncrono (eliminar dependencia del worker Python) | **NO** — el procesamiento bioinformático es cómputo intensivo, requiere worker dedicado |

---

## 5. Patrones aplicables

| Patrón | Dónde | Por qué SÍ |
|---|---|---|
| **Idempotency Key** | Endpoints de processing | Crítico para workflow externo confiable |
| **Specification** | `GetByStudyAsync` | 8 parámetros piden specification |
| **Value Object Collection** | Edits/Annotations/Trims dentro de `Trace` | Reduce God Aggregate sin perder cohesión transaccional |
| **Locator / Path Builder** | `ITraceFileLocator` | Aísla convenciones de storage |

**NO aplicar:**
- Saga pattern full — workflow es lineal.
- Event Sourcing para `Trace` — overkill, ya hay 15 eventos de dominio para notificar, no para reconstruir estado.
- CQRS read DB separada.
- Microservicio Traces — el módulo es central pero el split físico no soluciona nada.
- Repository genérico.

---

## 6. Estrategia de testing

**Antes:**
- Tests Domain de `Trace`, `SequenceEdit`, `TraceAnnotation`, `TraceTrim`, `QualityMetrics` ya existen.
- Tests Application para 17 commands y 15 queries — verificar cobertura de:
  - Transiciones inválidas de estado (lifecycle).
  - Edición tras archive (debe rechazar).
  - Trim concurrente.
  - Undo de annotation ya borrada.

**Durante Fase 2.1 (Idempotency):**
- Test: enviar `StartTraceProcessing` 3 veces con misma key → 1 ejecución, 3 respuestas idénticas.
- Test: enviar `CompleteTraceProcessing` con QualityMetrics distintas usando misma key → segunda llamada NO debe sobreescribir.

**Durante Fase 3.1 (owned → entidades hijas):**
- Tests de integración con PostgreSQL real validando paridad de comportamiento.
- Tests de cascada: borrar Trace borra annotations/trims/edits.

**Después:**
- Mutation testing en `Domain/Traces` y commands de processing.
- E2E test: upload trace → start processing → complete → verify QualityMetrics.

---

## 7. Orden de ejecución

```
Fase 1:
  1.1 (comentarios)
  1.5 (split TraceErrors)
  1.2 (constantes calidad/size)
  1.4 (doc workflow)
  1.6 (catch tipado TraceAnalysisService)
  1.3 (split TraceEditingEndpoints)

Fase 2:
  2.4 (IFileChecksumCalculator) ← independiente
  2.3 (ITraceFileLocator) ← preparación
  2.1 + 2.2 (Idempotency + state validation) ← CRÍTICO
  2.5 (Specifications)
  2.6 (descomponer Trace en VO collections)

Fase 3:
  3.1 (owned → entidades hijas) ← solo tras Fase 2.6 estable
```

---

## 8. Cambios NO recomendados

- Procesamiento bioinformático en .NET — Python tiene el ecosistema (BioPython, etc.).
- Reescribir el modelo de annotations/trims sin antes intentar 3.1 (entidades hijas).
- Microservicio Traces.
- Workflow engine externo.
- Reemplazar EF Core por Dapper completo en este módulo — la complejidad de las queries está mejor servida por LINQ + Specifications.
- Cambiar la state machine de `TraceStatus` por máquina explícita externa — la forma actual con `CanTransitionTo` es suficiente.
- Event Sourcing para `Trace`.
- Mover `SequenceEdit` / `TraceAnnotation` / `TraceTrim` a aggregates separados — perderían consistencia transaccional con `Trace.Status`.

---

## 9. Métricas de calidad sugeridas

| Métrica | Hoy | Objetivo |
|---|---|---|
| `Trace.cs` líneas | 513 | < 350 |
| `TraceEditingEndpoints.cs` líneas | 402 | n/a (eliminado) |
| Líneas por archivo de endpoint (max) | 402 | < 200 |
| Parámetros por método (max) | 8 | < 5 (con Spec) |
| Cobertura Domain/Traces | ? | ≥ 90% |
| Cobertura Application/Traces | ? | ≥ 85% |
| Comentarios no-doc | varios | 0 |
| Archivos > 1 clase pública | 0 | 0 |
| Workarounds ORM | 2 (`DeleteAnnotationAsync`, `DeleteTrimAsync`) | 0 (post Fase 3.1) |
| Endpoints de processing con idempotency | 0/3 | 3/3 |
| Magic numbers de calidad | 4+ | 0 (post Fase 1.2) |
| Hardcoded storage paths en API | 1+ | 0 (post Fase 2.3) |

---

## 10. Resultado final esperado

```
Domain/Traces/
├── Trace.cs                          (~330 líneas, facade)
├── Constants/
│   ├── QualityThresholds.cs          (Q20, Q30)
│   └── TraceFileLimits.cs            (MaxSizeBytes)
├── ValueObjects/
│   ├── TraceFile.cs
│   ├── QualityMetrics.cs
│   ├── TraceName.cs
│   ├── TraceDescription.cs
│   ├── TrimRegion.cs
│   ├── TraceEditCollection.cs        (NUEVO — gestiona edits)
│   ├── TraceAnnotationCollection.cs  (NUEVO — gestiona annotations)
│   └── TraceTrimCollection.cs        (NUEVO — gestiona trims)
├── Entities/                         (Fase 3 → entidades regulares con FK)
│   ├── SequenceEdit.cs
│   ├── TraceAnnotation.cs
│   └── TraceTrim.cs
├── Errors/
│   ├── TraceLifecycleErrors.cs
│   ├── TraceEditErrors.cs
│   ├── TraceAnnotationErrors.cs
│   └── TraceTrimErrors.cs
├── Enumerations/                     (sin cambios)
└── Events/                           (sin cambios)

Application/Traces/
├── Abstractions/
│   ├── ITraceFileLocator.cs          (NUEVO)
│   └── IFileChecksumCalculator.cs    (NUEVO)
└── ... (handlers sin cambios estructurales)

Infrastructure/Traces/
├── Services/
│   ├── TraceFileLocator.cs           (NUEVO)
│   ├── TraceAnalysisService.cs       (con Result, sin null)
│   └── Sha256ChecksumCalculator.cs   (NUEVO)
├── Specifications/
│   └── TracesByStudySpec.cs          (NUEVO)
└── Persistence/
    └── Repositories/
        └── TraceRepository.cs        (sin DeleteAnnotationAsync/DeleteTrimAsync workarounds)

Infrastructure/Idempotency/           (NUEVO compartido)
├── IIdempotencyStore.cs
└── PostgresIdempotencyStore.cs

API/Endpoints/Traces/
├── TraceEndpoints.cs                 (sin cálculo checksum, sin paths hardcoded)
├── TraceProcessingEndpoints.cs       (con idempotency-key)
├── TraceAnnotationEndpoints.cs       (sin cambios)
├── TraceSequenceEndpoints.cs         (sin cambios)
├── TraceTrimEndpoints.cs             (NUEVO — split)
├── TraceSequenceEditEndpoints.cs     (NUEVO — split)
└── TraceComplementEndpoints.cs       (NUEVO — split)

docs/
└── trace-processing-workflow.md      (NUEVO)
```

**Beneficios concretos:**
- Workflow de processing protegido contra duplicados (calidad de servicio crítica).
- `Trace.cs` 513 → ~330 líneas, métodos < 40 líneas.
- `TraceEditingEndpoints` desaparece, reemplazado por 3 archivos cohesivos.
- Sin workarounds EF Core (post Fase 3.1).
- Storage paths centralizados.
- `TraceAnalysisService` retorna errores tipados.
- Workflow externo documentado.

**Lo que NO cambia:**
- Decisión de delegar procesamiento a Python worker.
- 17 commands, 15 queries (mismos contratos).
- 15 eventos de dominio.
- State machine `TraceStatus`.
- Modelo de SCF/AB1 + checksum SHA256 + analysis.json en datalake.
- Contratos REST públicos.
