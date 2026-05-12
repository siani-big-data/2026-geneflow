# Plan de Refactorización — Módulo Pipelines

> Reglas obligatorias: **una clase por archivo**, **sin comentarios que no sean XML docs**, **sin sobreingeniería**.

---

## 1. Diagnóstico general

Módulo **bien diseñado** y razonablemente reciente. Aggregates `Pipeline` (289 líneas) y `PipelineExecution` (165 líneas) bien dimensionados, 19 commands + 6 queries con handlers cortos (max 106 líneas), 8 eventos de dominio correctos, 23 archivos de tests incluyendo E2E (`PipelineLifecycleE2ETests`).

**Fortalezas:**
- Smart Enumeration `StepType` con 7 tipos de paso de bioinformática.
- `StepConfiguration` value object con validación JSON específica por tipo.
- Lifecycle de pipeline claro: Draft → Active → Archived.
- Ejecución modelada como aggregate separado (`PipelineExecution`) — decisión correcta.

**Debilidades:**
- **`StepType` viola Open/Closed**: añadir un nuevo tipo requiere modificar la enum + el `switch` en `GetConfigurationSchema()` + `StepConfiguration.ValidateForStepType()`.
- **Sin transacción atómica DB + job publish**: `ExecutePipelineCommandHandler` persiste la ejecución y luego publica a Redis Streams. Si lo segundo falla → ejecución huérfana en estado Pending.
- **`StepConfiguration` swallow exception** (`StepConfiguration.cs:78-80`): `catch { return new Dictionary(); }`.
- **`PipelineEndpoints.cs` 514 líneas**: 18 endpoints en un solo archivo, magic numbers de paginación inline.

**Veredicto:** Refactor pequeño-mediano. Foco en OCP de StepType, atomicidad de Execute, y limpieza táctica.

---

## 2. Code Smells detectados

| # | Smell | Ubicación | Severidad |
|---|---|---|---|
| 1 | Violación OCP en `StepType` (switch hardcoded) | `Domain/Pipelines/Enumerations/StepType.cs:47` | Alta |
| 2 | Swallow exception en parsing JSON | `Domain/Pipelines/ValueObjects/StepConfiguration.cs:78-80` | Alta |
| 3 | Sin atomicidad DB + Redis publish | `Application/Pipelines/Commands/Execute/ExecutePipelineCommandHandler.cs:90-102` | Alta |
| 4 | Magic numbers de paginación inline | `API/Endpoints/Pipelines/PipelineEndpoints.cs:186, 495` | Baja |
| 5 | `PipelineEndpoints.cs` 514 líneas (18 endpoints) | `PipelineEndpoints.cs` | Media |
| 6 | Validación de configuración duplicada (`StepConfiguration.ValidateForStepType` y handlers) | varios | Baja |
| 7 | Comentarios inline no-doc | varios | Media (regla obligatoria) |
| 8 | `MaxSteps = 10` constante mágica del agregado — no documentado por qué 10 | `Pipeline.cs` | Baja |
| 9 | `ExecutePipelineCommandHandler` 106 líneas — borderline | mismo | Baja |

---

## 3. Problemas arquitectónicos

1. **Acoplamiento bidireccional `StepType` ↔ Python worker keys**: el `AnalysisKey` está en el smart enumeration de dominio. Cambiar el contrato del worker requiere tocar el dominio. Debería estar en una capa de Application/Infrastructure (mapping).
2. **No hay outbox para job publishing**: si Redis cae entre `SaveChanges` y `PublishPipelineJobAsync`, el pipeline queda en estado Pending sin job en cola. Imposible recuperar sin job de barrido.
3. **`StepConfiguration` valida JSON en el value object pero usa `JsonDocument.Parse` en runtime cada vez** — performance pequeña pero acumulable; además, el silent catch mezcla "parse fallido" con "config válida vacía".
4. **Endpoints conocen demasiado**: `PipelineEndpoints` mapea defaults de paginación, lo que pertenece a un `PagedRequest` común.

---

## 4. Plan de refactorización priorizado

### Fase 1 — Cambios seguros
| Tarea | Archivo(s) | Justificación |
|---|---|---|
| 1.1 Eliminar comentarios no-doc | todos los `.cs` | Regla obligatoria |
| 1.2 Reemplazar `catch { return new Dictionary(); }` por catch tipado de `JsonException` que retorne `Result<Dictionary, Error>` o lanzar — decisión: **lanzar en `ToDictionary()` porque el JSON ya fue validado en `Create`, así que un fallo aquí indica corrupción** | `StepConfiguration.cs:78-80` | Manejo explícito |
| 1.3 Magic numbers de paginación → `PagedRequestDefaults.PageNumber/PageSize` constantes en `SharedKernel/Application` | `PipelineEndpoints.cs`, todos los endpoints paginados del proyecto | Reutilizable |
| 1.4 XML doc `MaxSteps = 10` explicando la decisión (límite por pipeline para evitar runaway) | `Pipeline.cs` | Memoria de decisión |
| 1.5 Split `PipelineEndpoints.cs` (18 endpoints, 514 líneas) en 2-3 archivos: `PipelineCrudEndpoints`, `PipelineStepEndpoints`, `PipelineActivationEndpoints` | `API/Endpoints/Pipelines/` | Cohesión, archivo manejable |

### Fase 2 — Mejoras de diseño
| Tarea | Archivo(s) | Justificación |
|---|---|---|
| 2.1 **Resolver OCP de `StepType`**: extraer `IStepTypeRegistry` (Application) que devuelve `StepTypeDefinition` (Name, DisplayName, AnalysisKey, Schema, Validator). `StepType` smart enum se queda solo como ID + Name. Validation y schema dejan de vivir en el enum | `Application/Pipelines/Abstractions/IStepTypeRegistry.cs` (NUEVO), `Infrastructure/Pipelines/Services/StepTypeRegistry.cs` (NUEVO) | OCP — añadir tipo nuevo no requiere modificar dominio |
| 2.2 **Atomicidad Execute**: implementar **Outbox local** para `PipelineJob`. La operación es: persistir Execution + insertar OutboxMessage en una transacción → un `OutboxRelay` lee y publica a Redis con confirmación. | `Infrastructure/Outbox/`, `Application/Pipelines/Commands/Execute/ExecutePipelineCommandHandler.cs` | Sin pérdida de jobs |
| 2.3 Mover el mapping `StepType → AnalysisKey` (Python worker contract) de Domain a Infrastructure (`StepTypeRegistry`) | `Domain/Pipelines/Enumerations/StepType.cs`, nuevo `StepTypeRegistry` | Domain no debe conocer infraestructura externa |
| 2.4 Extraer validación de unicidad de nombre de pipeline en estudio a `IPipelineUniquenessChecker` (separar de repo) | `Application/Pipelines/Abstractions/` | SRP en repo |

### Fase 3 — Refactor arquitectónico
| Tarea | Recomendación |
|---|---|
| 3.1 Saga / Process Manager para orquestar `PipelineExecution` step-by-step desde .NET (en lugar de delegar todo al Python worker) | **NO recomendado** salvo que se quiera control fino desde .NET. La arquitectura actual (worker autónomo + eventos de completion) es más simple |
| 3.2 Reemplazar smart enum `StepType` por entidad persistida en DB | **NO** — los step types son estables y pocos. El registry de Fase 2.1 es suficiente |
| 3.3 Versionado de Pipelines (immutable + new version on edit) | **Solo si producción demanda audit/rollback** — añade complejidad seria |

---

## 5. Patrones aplicables

| Patrón | Dónde | Por qué SÍ |
|---|---|---|
| **Registry / Plugin** | `IStepTypeRegistry` para tipos de paso | Resuelve OCP sin BD |
| **Outbox** | Execute pipeline + job publish | Resuelve atomicidad real, no preventiva |
| **Strategy** | Validator por step type (dentro del registry) | Cada tipo valida su propia configuración |

**NO aplicar:**
- Saga full-blown — el flujo no lo justifica.
- Event Sourcing para Pipeline.
- CQRS read DB separada.
- Workflow engine externo (Camunda, Elsa, etc.) — Python worker basta.

---

## 6. Estrategia de testing

**Antes:**
- Tests Domain de `Pipeline`, `PipelineExecution`, `PipelineStep`, `StepConfiguration` ya existen.
- E2E `PipelineLifecycleE2ETests` cubre Create → AddStep → Activate → Execute → Cancel.
- Verificar tests para edge cases:
  - Pipeline con MaxSteps + 1 (rechazo).
  - Activate sin steps válidos.
  - Execute concurrente (¿`HasRunningExecutionForTraceAsync` es race-free?).

**Durante:**
- Fase 2.1 (StepType registry): tests deben demostrar que añadir un step type ficticio no requiere modificar `Pipeline` ni `StepConfiguration`.
- Fase 2.2 (Outbox): tests de integración con falla simulada de Redis post-commit.

**Después:**
- Mutation testing en `Domain/Pipelines` y `Application/Pipelines/Commands/Execute`.
- Stress test de Execute concurrente (10 ejecuciones simultáneas para mismo trace) — debe rechazar 9.

---

## 7. Orden de ejecución

```
Fase 1:
  1.1 (comentarios)
  1.2 (catch tipado StepConfiguration)
  1.4 (XML doc MaxSteps)
  1.3 (PagedRequestDefaults)
  1.5 (split PipelineEndpoints)

Fase 2:
  2.4 (uniqueness checker — preparación)
  2.3 (mover AnalysisKey a Infrastructure) ← preparación de 2.1
  2.1 (StepTypeRegistry) ← mayor impacto OCP
  2.2 (Outbox) ← independiente, alto valor

Fase 3:
  No ejecutar salvo evidencia.
```

---

## 8. Cambios NO recomendados

- Reescribir `Pipeline` o `PipelineExecution` aggregates — están bien dimensionados.
- Workflow engine (Elsa, Workflow Core, Camunda) — Python worker hace el trabajo.
- Mover step types a tabla en DB — innecesaria mutabilidad.
- Mediator entre `Pipeline` y `PipelineExecution` — la dependencia es directa y clara.
- Versionado de Pipelines — solo bajo evidencia de necesidad.
- Saga pattern para coordinación — el patrón actual (publish job → consume completion event) es más simple y suficiente.

---

## 9. Métricas de calidad sugeridas

| Métrica | Hoy | Objetivo |
|---|---|---|
| `Pipeline.cs` líneas | 289 | < 300 ✓ |
| `PipelineEndpoints.cs` líneas | 514 | < 200 por archivo (post split) |
| Líneas por método (max) | 106 | < 60 |
| Tests E2E pipeline lifecycle | 1 | ≥ 1 ✓ |
| Cobertura Domain | ? | ≥ 90% |
| Cobertura Application | ? | ≥ 85% |
| Comentarios no-doc | varios | 0 |
| Archivos > 1 clase pública | 0 | 0 ✓ |
| Outbox con jobs perdidos por falla Redis | depende | 0 (post Fase 2.2) |
| Pasos requeridos para añadir nuevo `StepType` | 3 (enum + switch + validator) | 1 (registrar en Registry) |

---

## 10. Resultado final esperado

```
Domain/Pipelines/
├── Pipeline.cs                       (~280 líneas)
├── PipelineExecution.cs              (~165 líneas)
├── Entities/
│   ├── PipelineStep.cs
│   └── StepExecution.cs
├── ValueObjects/
│   ├── PipelineName.cs
│   ├── PipelineDescription.cs
│   └── StepConfiguration.cs          (sin silent catch)
├── Enumerations/
│   ├── StepType.cs                   (solo Id + Name, sin Schema/AnalysisKey/Validator)
│   ├── PipelineStatus.cs
│   ├── ExecutionStatus.cs
│   └── StepExecutionStatus.cs
└── Events/                           (sin cambios)

Application/Pipelines/
├── Abstractions/
│   ├── IStepTypeRegistry.cs          (NUEVO)
│   ├── IPipelineUniquenessChecker.cs (NUEVO)
│   └── IJobPublisher.cs              (sin cambios)
└── ...

Infrastructure/Pipelines/
├── Services/
│   ├── StepTypeRegistry.cs           (NUEVO — registra los 7 tipos + extensible)
│   └── StepTypeDefinition.cs         (NUEVO — schema, analysisKey, validator delegate)

Infrastructure/Outbox/                (NUEVO, compartido)
├── OutboxMessage.cs
├── OutboxRelay.cs
└── IOutboxStore.cs

API/Endpoints/Pipelines/
├── PipelineCrudEndpoints.cs          (~150 líneas)
├── PipelineStepEndpoints.cs          (~150 líneas)
├── PipelineActivationEndpoints.cs    (~100 líneas)
└── PipelineExecutionEndpoints.cs     (sin cambios — ya separado)
```

**Beneficios concretos:**
- OCP cumplido: añadir `StepType` nuevo = registrar en `StepTypeRegistry`, sin tocar dominio.
- Atomicidad real: Execute persiste + publica vía outbox, sin huérfanos.
- `PipelineEndpoints.cs` 514 → 3 archivos < 200 líneas cada uno.
- Sin silent catches.
- Defaults de paginación reutilizables (también beneficia otros módulos).

**Lo que NO cambia:**
- Aggregates `Pipeline` / `PipelineExecution`.
- Eventos de dominio.
- Modelo de ejecución delegado a Python worker.
- Smart enum `PipelineStatus` (este sí justificado, no cambia).
- Contratos REST públicos.
