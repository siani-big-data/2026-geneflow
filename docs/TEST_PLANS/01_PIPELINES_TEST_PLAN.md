# Plan de Tests: Módulo Pipelines

## Cobertura Actual: 0%
## Tests Estimados: ~305 tests

### Objetivos de Cobertura por Capa

| Capa | Archivos | Line | Branch | Method |
|------|----------|------|--------|--------|
| **Domain** | Pipeline.cs, Entities/*, ValueObjects/*, Enumerations/* | **≥95%** | **≥90%** | **≥98%** |
| **Application** | Commands/*, Queries/* | **≥90%** | **≥85%** | **≥95%** |
| **Infrastructure** | Repositories/*, Services/* | **≥75%** | **≥70%** | **≥85%** |
| **API** | Endpoints/* | **≥85%** | **≥80%** | **≥90%** |

### Objetivos Específicos por Archivo

| Archivo | Line | Branch | Prioridad |
|---------|------|--------|-----------|
| `Pipeline.cs` (288 LOC) | 95% | 92% | P0 |
| `PipelineExecution.cs` (164 LOC) | 95% | 90% | P0 |
| `PipelineStep.cs` (84 LOC) | 98% | 95% | P0 |
| `StepExecution.cs` (104 LOC) | 95% | 90% | P0 |
| `StepConfiguration.cs` (149 LOC) | 98% | 95% | P0 |
| `PipelineName.cs` (42 LOC) | 100% | 100% | P1 |
| `PipelineDescription.cs` (43 LOC) | 100% | 100% | P1 |
| `PipelineStatus.cs` (61 LOC) | 100% | 100% | P1 |
| `ExecutionStatus.cs` (64 LOC) | 100% | 100% | P1 |
| `StepType.cs` (107 LOC) | 100% | 100% | P1 |
| Command Handlers (14 archivos) | 90% | 85% | P0 |
| Query Handlers (6 archivos) | 85% | 80% | P1 |
| `PipelineEndpoints.cs` | 85% | 80% | P1 |
| `PipelineExecutionEndpoints.cs` | 85% | 80% | P1 |

---

## 1. ANÁLISIS DEL MÓDULO

### 1.1 Archivos de Dominio (1,554 LOC)

| Archivo | LOC | Complejidad | Prioridad |
|---------|-----|-------------|-----------|
| `Pipeline.cs` | 288 | Alta | P0 |
| `Entities/PipelineExecution.cs` | 164 | Alta | P0 |
| `Entities/PipelineStep.cs` | 84 | Media | P0 |
| `Entities/StepExecution.cs` | 104 | Media | P0 |
| `ValueObjects/StepConfiguration.cs` | 149 | Alta | P0 |
| `ValueObjects/PipelineName.cs` | 42 | Baja | P1 |
| `ValueObjects/PipelineDescription.cs` | 43 | Baja | P1 |
| `Enumerations/PipelineStatus.cs` | 61 | Media | P1 |
| `Enumerations/ExecutionStatus.cs` | 64 | Media | P1 |
| `Enumerations/StepExecutionStatus.cs` | 63 | Media | P1 |
| `Enumerations/StepType.cs` | 107 | Media | P1 |
| `PipelineId.cs` | 40 | Baja | P2 |
| `PipelineExecutionId.cs` | 40 | Baja | P2 |
| `PipelineErrors.cs` | 67 | Baja | P2 |
| Eventos (9 archivos) | ~120 | Baja | P2 |

### 1.2 Archivos de Aplicación (~1,200 LOC)

**Commands (14):**
| Handler | LOC | Complejidad |
|---------|-----|-------------|
| `CreatePipelineCommandHandler` | 82 | Alta |
| `UpdatePipelineCommandHandler` | 64 | Media |
| `DeletePipelineCommandHandler` | 51 | Media |
| `ActivatePipelineCommandHandler` | 54 | Media |
| `DeactivatePipelineCommandHandler` | 54 | Media |
| `ArchivePipelineCommandHandler` | 54 | Media |
| `AddPipelineStepCommandHandler` | 71 | Alta |
| `UpdatePipelineStepCommandHandler` | 76 | Alta |
| `RemovePipelineStepCommandHandler` | 55 | Media |
| `ReorderPipelineStepsCommandHandler` | 63 | Alta |
| `ExecutePipelineCommandHandler` | 106 | Muy Alta |
| `CancelExecutionCommandHandler` | 61 | Media |
| `CompleteStepExecutionCommandHandler` | 74 | Alta |
| `FailStepExecutionCommandHandler` | 85 | Alta |

**Queries (6):**
| Handler | LOC | Complejidad |
|---------|-----|-------------|
| `GetPipelineByIdQueryHandler` | ~40 | Baja |
| `GetStudyPipelinesQueryHandler` | ~50 | Media |
| `GetExecutionByIdQueryHandler` | ~40 | Baja |
| `GetPipelineExecutionsQueryHandler` | ~50 | Media |
| `GetTraceExecutionsQueryHandler` | ~50 | Media |
| `GetStepTypesQueryHandler` | ~30 | Baja |

### 1.3 Archivos de API

| Endpoint | Métodos |
|----------|---------|
| `PipelineEndpoints.cs` | GET, POST, PUT, DELETE, POST /activate, POST /deactivate, POST /archive |
| `PipelineExecutionEndpoints.cs` | GET, POST /execute, POST /cancel, POST /complete-step, POST /fail-step |

---

## 2. TESTS EXISTENTES

**Ninguno** - Este módulo tiene 0% de cobertura.

---

## 3. TESTS UNITARIOS DE DOMINIO

### 3.1 PipelineTests.cs (35 tests)

```
Tests/Domain/Pipelines/PipelineTests.cs
```

#### Factory Method Tests
| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_WithValidData_ShouldReturnSuccess` | Crear pipeline con datos válidos |
| 2 | `Create_ShouldSetStatusToDraft` | Estado inicial debe ser Draft |
| 3 | `Create_ShouldSetOwnerId` | Owner debe ser el creador |
| 4 | `Create_ShouldInitializeEmptyStepsList` | Lista de steps vacía |
| 5 | `Create_ShouldRaisePipelineCreatedEvent` | Evento de creación |
| 6 | `Create_ShouldSetCreatedAt` | Timestamp de creación |

#### Update Tests
| # | Test | Descripción |
|---|------|-------------|
| 7 | `Update_WhenDraft_ShouldUpdateFields` | Actualizar en estado Draft |
| 8 | `Update_WhenActive_ShouldReturnNotEditableError` | No editable en Active |
| 9 | `Update_WhenArchived_ShouldReturnNotEditableError` | No editable en Archived |
| 10 | `Update_ShouldSetModifiedAt` | Timestamp de modificación |

#### Step Management Tests
| # | Test | Descripción |
|---|------|-------------|
| 11 | `AddStep_WhenDraft_ShouldAddStep` | Agregar step en Draft |
| 12 | `AddStep_ShouldAssignCorrectOrder` | Orden correcto (1, 2, 3...) |
| 13 | `AddStep_WhenMaxStepsReached_ShouldReturnError` | Máximo 10 steps |
| 14 | `AddStep_WhenActive_ShouldReturnNotEditableError` | No editable |
| 15 | `AddStep_WithLongLabel_ShouldReturnError` | Label > 100 chars |
| 16 | `AddStep_ShouldRaisePipelineStepAddedEvent` | Evento de step agregado |
| 17 | `UpdateStep_WithValidData_ShouldUpdateStep` | Actualizar step |
| 18 | `UpdateStep_NonExistentStep_ShouldReturnStepNotFoundError` | Step no existe |
| 19 | `UpdateStep_ShouldEnableOrDisableStep` | Enable/Disable step |
| 20 | `RemoveStep_ShouldRemoveAndReorderSteps` | Remover y reordenar |
| 21 | `RemoveStep_NonExistentStep_ShouldReturnStepNotFoundError` | Step no existe |
| 22 | `RemoveStep_ShouldRaisePipelineStepRemovedEvent` | Evento de step removido |
| 23 | `ReorderSteps_WithValidOrder_ShouldReorderSteps` | Reordenar steps |
| 24 | `ReorderSteps_WithInvalidStepCount_ShouldReturnError` | Cantidad incorrecta |
| 25 | `ReorderSteps_WithDuplicateIds_ShouldReturnError` | IDs duplicados |
| 26 | `ReorderSteps_WithUnknownStepId_ShouldReturnError` | ID desconocido |

#### Status Management Tests
| # | Test | Descripción |
|---|------|-------------|
| 27 | `Activate_FromDraft_WithSteps_ShouldActivate` | Draft -> Active |
| 28 | `Activate_WithNoSteps_ShouldReturnNoStepsError` | Sin steps configurados |
| 29 | `Activate_FromActive_ShouldReturnInvalidTransitionError` | Transición inválida |
| 30 | `Activate_ShouldRaisePipelineActivatedEvent` | Evento de activación |
| 31 | `Deactivate_FromActive_ShouldDeactivate` | Active -> Draft |
| 32 | `Archive_FromDraft_ShouldArchive` | Draft -> Archived |
| 33 | `Archive_FromActive_ShouldArchive` | Active -> Archived |
| 34 | `Archive_ShouldRaisePipelineArchivedEvent` | Evento de archivo |
| 35 | `RestoREDACTED` | Archived -> Draft |

#### Execution Tests
| # | Test | Descripción |
|---|------|-------------|
| 36 | `StartExecution_WhenActive_ShouldStartExecution` | Iniciar ejecución |
| 37 | `StartExecution_WhenNotActive_ShouldReturnNotExecutableError` | No ejecutable |
| 38 | `StartExecution_WithNoSteps_ShouldReturnNoStepsError` | Sin steps |
| 39 | `StartExecution_ShouldRaisePipelineExecutionStartedEvent` | Evento de inicio |

#### Computed Properties Tests
| # | Test | Descripción |
|---|------|-------------|
| 40 | `CanBeEdited_WhenDraft_ShouldReturnTrue` | Editable en Draft |
| 41 | `CanBeEdited_WhenActive_ShouldReturnFalse` | No editable en Active |
| 42 | `CanBeExecuted_WhenActive_ShouldReturnTrue` | Ejecutable en Active |
| 43 | `CanBeDeleted_WhenNotArchived_ShouldReturnTrue` | Eliminable |
| 44 | `EnabledStepCount_ShouldReturnCorrectCount` | Contar steps habilitados |

### 3.2 PipelineExecutionTests.cs (20 tests)

```
Tests/Domain/Pipelines/Entities/PipelineExecutionTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_WithValidData_ShouldCreateExecution` | Crear ejecución |
| 2 | `Create_ShouldSetStatusToPending` | Estado inicial Pending |
| 3 | `Create_ShouldInitializeStepExecutions` | Steps de ejecución |
| 4 | `Start_ShouldSetStatusToRunning` | Pending -> Running |
| 5 | `Start_ShouldSetStartedAt` | Timestamp de inicio |
| 6 | `CompleteStep_ShouldMarkStepCompleted` | Completar step |
| 7 | `CompleteStep_WhenAllComplete_ShouldCompleteExecution` | Completar ejecución |
| 8 | `CompleteStep_ShouldRaisePipelineStepCompletedEvent` | Evento de step |
| 9 | `FailStep_ShouldMarkStepFailed` | Fallar step |
| 10 | `FailStep_ShouldFailEntireExecution` | Fallar ejecución |
| 11 | `FailStep_ShouldRaisePipelineExecutionFailedEvent` | Evento de fallo |
| 12 | `Cancel_WhenRunning_ShouldCancel` | Cancelar ejecución |
| 13 | `Cancel_WhenPending_ShouldCancel` | Cancelar pendiente |
| 14 | `Cancel_WhenCompleted_ShouldReturnError` | No cancelable |
| 15 | `Cancel_WhenFailed_ShouldReturnError` | No cancelable |
| 16 | `GetNextStep_ShouldReturnNextPendingStep` | Siguiente step |
| 17 | `GetStepExecution_ById_ShouldReturnStep` | Obtener step por ID |
| 18 | `Duration_WhenCompleted_ShouldCalculateDuration` | Calcular duración |
| 19 | `Progress_ShouldReturnPercentageCompleted` | Calcular progreso |
| 20 | `IsTerminal_WhenCompletedOrFailed_ShouldReturnTrue` | Estado terminal |

### 3.3 PipelineStepTests.cs (12 tests)

```
Tests/Domain/Pipelines/Entities/PipelineStepTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_WithValidData_ShouldCreateStep` | Crear step |
| 2 | `Create_ShouldGenerateUniqueId` | ID único |
| 3 | `UpdateConfiguration_ShouldUpdateConfiguration` | Actualizar config |
| 4 | `UpdateLabel_ShouldUpdateLabel` | Actualizar label |
| 5 | `Enable_ShouldSetIsEnabledTrue` | Habilitar step |
| 6 | `Disable_ShouldSetIsEnabledFalse` | Deshabilitar step |
| 7 | `SetOrder_ShouldUpdateOrder` | Cambiar orden |
| 8 | `StepType_ShouldBeImmutable` | Tipo inmutable |
| 9 | `Id_ShouldBeImmutable` | ID inmutable |
| 10 | `MaxLabelLength_ShouldBe100` | Longitud máxima |
| 11 | `Label_WhenNull_ShouldAllowNull` | Label nullable |
| 12 | `IsEnabled_Default_ShouldBeTrue` | Habilitado por defecto |

### 3.4 StepExecutionTests.cs (10 tests)

```
Tests/Domain/Pipelines/Entities/StepExecutionTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_ShouldSetStatusToPending` | Estado inicial |
| 2 | `Start_ShouldSetStatusToRunning` | Iniciar step |
| 3 | `Start_ShouldSetStartedAt` | Timestamp inicio |
| 4 | `Complete_WithResult_ShouldComplete` | Completar con resultado |
| 5 | `Complete_ShouldSetCompletedAt` | Timestamp fin |
| 6 | `Fail_WithError_ShouldFail` | Fallar con error |
| 7 | `Skip_ShouldSetStatusToSkipped` | Saltar step |
| 8 | `Duration_ShouldCalculateCorrectly` | Calcular duración |
| 9 | `CanTransitionTo_ValidTransitions` | Transiciones válidas |
| 10 | `CanTransitionTo_InvalidTransitions` | Transiciones inválidas |

### 3.5 Value Objects Tests

#### PipelineNameTests.cs (8 tests)
```
Tests/Domain/Pipelines/ValueObjects/PipelineNameTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_WithValidName_ShouldReturnSuccess` | Nombre válido |
| 2 | `Create_WithNull_ShouldReturnNameRequiredError` | Null |
| 3 | `Create_WithEmpty_ShouldReturnNameRequiredError` | Vacío |
| 4 | `Create_WithWhitespace_ShouldReturnNameRequiredError` | Solo espacios |
| 5 | `Create_TooShort_ShouldReturnNameTooShortError` | < 3 chars |
| 6 | `Create_TooLong_ShouldReturnNameTooLongError` | > 100 chars |
| 7 | `Create_ShouldTrimWhitespace` | Trimear espacios |
| 8 | `Equality_SameValue_ShouldBeEqual` | Igualdad |

#### PipelineDescriptionTests.cs (6 tests)
```
Tests/Domain/Pipelines/ValueObjects/PipelineDescriptionTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_WithValidDescription_ShouldReturnSuccess` | Descripción válida |
| 2 | `Create_WithNull_ShouldReturnEmptyDescription` | Null -> Empty |
| 3 | `Create_WithEmpty_ShouldReturnEmptyDescription` | Vacío -> Empty |
| 4 | `Create_TooLong_ShouldReturnDescriptionTooLongError` | > 1000 chars |
| 5 | `Create_ShouldTrimWhitespace` | Trimear espacios |
| 6 | `Empty_ShouldReturnNullValue` | Empty.Value = null |

#### StepConfigurationTests.cs (15 tests)
```
Tests/Domain/Pipelines/ValueObjects/StepConfigurationTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_WithValidJson_ShouldReturnSuccess` | JSON válido |
| 2 | `Create_WithNull_ShouldReturnEmptyConfiguration` | Null -> Empty |
| 3 | `Create_WithInvalidJson_ShouldReturnInvalidConfigError` | JSON inválido |
| 4 | `Create_TooLong_ShouldReturnConfigTooLongError` | > 10000 chars |
| 5 | `ValidateForStepType_QualityTrimming_ValidCutoff` | Cutoff válido |
| 6 | `ValidateForStepType_QualityTrimming_InvalidCutoff` | Cutoff inválido |
| 7 | `ValidateForStepType_MotifSearch_WithPattern` | Pattern presente |
| 8 | `ValidateForStepType_MotifSearch_WithoutPattern` | Pattern faltante |
| 9 | `ValidateForStepType_RestrictionEnzyme_WithEnzymes` | Enzimas presentes |
| 10 | `ValidateForStepType_RestrictionEnzyme_WithoutEnzymes` | Enzimas faltantes |
| 11 | `GetValue_ExistingKey_ShouldReturnValue` | Obtener valor |
| 12 | `GetValue_NonExistingKey_ShouldReturnDefault` | Valor default |
| 13 | `GetValue_TypeMismatch_ShouldReturnDefault` | Tipo incorrecto |
| 14 | `Empty_ShouldHaveNullJson` | Empty.Json = null |
| 15 | `Equality_SameJson_ShouldBeEqual` | Igualdad |

### 3.6 Enumerations Tests

#### PipelineStatusTests.cs (10 tests)
```
Tests/Domain/Pipelines/Enumerations/PipelineStatusTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `All_ShouldHaveUniqueIds` | IDs únicos |
| 2 | `All_ShouldHaveUniqueNames` | Nombres únicos |
| 3 | `FromId_ValidId_ShouldReturnStatus` | FromId válido |
| 4 | `FromId_InvalidId_ShouldThrow` | FromId inválido |
| 5 | `FromName_ValidName_ShouldReturnStatus` | FromName válido |
| 6 | `CanTransitionTo_DraftToActive_ShouldReturnTrue` | Transición válida |
| 7 | `CanTransitionTo_DraftToArchived_ShouldReturnTrue` | Transición válida |
| 8 | `CanTransitionTo_ActiveToDraft_ShouldReturnTrue` | Transición válida |
| 9 | `CanTransitionTo_ArchivedToActive_ShouldReturnFalse` | Transición inválida |
| 10 | `CanEdit_OnlyDraft_ShouldReturnTrue` | Solo Draft editable |

#### ExecutionStatusTests.cs (8 tests)
```
Tests/Domain/Pipelines/Enumerations/ExecutionStatusTests.cs
```

#### StepExecutionStatusTests.cs (8 tests)
```
Tests/Domain/Pipelines/Enumerations/StepExecutionStatusTests.cs
```

#### StepTypeTests.cs (10 tests)
```
Tests/Domain/Pipelines/Enumerations/StepTypeTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `All_ShouldHaveUniqueIds` | IDs únicos |
| 2 | `All_ShouldHaveDisplayNames` | Display names |
| 3 | `All_ShouldHaveCategories` | Categorías |
| 4 | `QualityTrimming_RequiresConfiguration` | Requiere config |
| 5 | `MotifSearch_RequiresConfiguration` | Requiere config |
| 6 | `RestrictionEnzyme_RequiresConfiguration` | Requiere config |
| 7 | `FromId_AllValidIds_ShouldWork` | Todos los IDs |
| 8 | `GetByCategory_ShouldFilterCorrectly` | Filtrar por categoría |
| 9 | `IsAnalysisStep_ShouldReturnCorrectly` | Es step de análisis |
| 10 | `RequiresExternalService_ShouldReturnCorrectly` | Requiere servicio |

### 3.7 ID Tests

#### PipelineIdTests.cs (5 tests)
```
Tests/Domain/Pipelines/PipelineIdTests.cs
```

#### PipelineExecutionIdTests.cs (5 tests)
```
Tests/Domain/Pipelines/PipelineExecutionIdTests.cs
```

---

## 4. TESTS DE HANDLERS

### 4.1 Command Handler Tests

#### CreatePipelineCommandHandlerTests.cs (8 tests)
```
Tests/Application/Pipelines/Commands/CreatePipelineCommandHandlerTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Handle_WithValidData_ShouldCreatePipeline` | Crear exitosamente |
| 2 | `Handle_WithInvalidName_ShouldReturnValidationError` | Nombre inválido |
| 3 | `Handle_WithInvalidStudyId_ShouldReturnStudyNotFoundError` | Study no existe |
| 4 | `Handle_UserNotStudyMember_ShouldReturnPermissionError` | Sin permisos |
| 5 | `Handle_UserIsViewer_ShouldReturnPermissionError` | Viewer no puede crear |
| 6 | `Handle_ShouldGenerateNewPipelineId` | Generar ID |
| 7 | `Handle_ShouldPersistPipeline` | Persistir |
| 8 | `Handle_ShouldReturnPipelineDto` | Retornar DTO |

#### UpdatePipelineCommandHandlerTests.cs (7 tests)
```
Tests/Application/Pipelines/Commands/UpdatePipelineCommandHandlerTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Handle_WithValidData_ShouldUpdatePipeline` | Actualizar exitosamente |
| 2 | `Handle_PipelineNotFound_ShouldReturnNotFoundError` | No encontrado |
| 3 | `Handle_PipelineNotEditable_ShouldReturnError` | No editable |
| 4 | `Handle_UserNotOwner_ShouldReturnPermissionError` | Sin permisos |
| 5 | `Handle_WithInvalidName_ShouldReturnValidationError` | Nombre inválido |
| 6 | `Handle_ShouldPersistChanges` | Persistir cambios |
| 7 | `Handle_ShouldReturnUpdatedDto` | Retornar DTO |

#### DeletePipelineCommandHandlerTests.cs (6 tests)
```
Tests/Application/Pipelines/Commands/DeletePipelineCommandHandlerTests.cs
```

#### ActivatePipelineCommandHandlerTests.cs (6 tests)
```
Tests/Application/Pipelines/Commands/ActivatePipelineCommandHandlerTests.cs
```

#### DeactivatePipelineCommandHandlerTests.cs (5 tests)
```
Tests/Application/Pipelines/Commands/DeactivatePipelineCommandHandlerTests.cs
```

#### ArchivePipelineCommandHandlerTests.cs (5 tests)
```
Tests/Application/Pipelines/Commands/ArchivePipelineCommandHandlerTests.cs
```

#### AddPipelineStepCommandHandlerTests.cs (8 tests)
```
Tests/Application/Pipelines/Commands/AddPipelineStepCommandHandlerTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Handle_WithValidData_ShouldAddStep` | Agregar exitosamente |
| 2 | `Handle_PipelineNotFound_ShouldReturnNotFoundError` | No encontrado |
| 3 | `Handle_PipelineNotEditable_ShouldReturnError` | No editable |
| 4 | `Handle_InvalidStepType_ShouldReturnError` | Tipo inválido |
| 5 | `Handle_InvalidConfiguration_ShouldReturnError` | Config inválida |
| 6 | `Handle_MaxStepsReached_ShouldReturnError` | Máximo alcanzado |
| 7 | `Handle_ShouldPersistStep` | Persistir step |
| 8 | `Handle_ShouldReturnStepDto` | Retornar DTO |

#### UpdatePipelineStepCommandHandlerTests.cs (7 tests)
#### RemovePipelineStepCommandHandlerTests.cs (6 tests)
#### ReorderPipelineStepsCommandHandlerTests.cs (7 tests)

#### ExecutePipelineCommandHandlerTests.cs (10 tests)
```
Tests/Application/Pipelines/Commands/ExecutePipelineCommandHandlerTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Handle_WithValidData_ShouldStartExecution` | Ejecutar exitosamente |
| 2 | `Handle_PipelineNotFound_ShouldReturnNotFoundError` | Pipeline no existe |
| 3 | `Handle_TraceNotFound_ShouldReturnNotFoundError` | Trace no existe |
| 4 | `Handle_PipelineNotActive_ShouldReturnError` | No activo |
| 5 | `Handle_TraceNotProcessed_ShouldReturnError` | Trace no procesado |
| 6 | `Handle_TraceAlreadyRunning_ShouldReturnConflictError` | Ya ejecutándose |
| 7 | `Handle_UserNotStudyMember_ShouldReturnPermissionError` | Sin permisos |
| 8 | `Handle_ShouldGenerateExecutionId` | Generar ID ejecución |
| 9 | `Handle_ShouldPersistExecution` | Persistir ejecución |
| 10 | `Handle_ShouldReturnExecutionDto` | Retornar DTO |

#### CancelExecutionCommandHandlerTests.cs (6 tests)
#### CompleteStepExecutionCommandHandlerTests.cs (8 tests)
#### FailStepExecutionCommandHandlerTests.cs (7 tests)

### 4.2 Query Handler Tests

#### GetPipelineByIdQueryHandlerTests.cs (5 tests)
```
Tests/Application/Pipelines/Queries/GetPipelineByIdQueryHandlerTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Handle_ExistingPipeline_ShouldReturnPipelineDto` | Retornar DTO |
| 2 | `Handle_NonExistingPipeline_ShouldReturnNotFoundError` | No encontrado |
| 3 | `Handle_UserNotStudyMember_ShouldReturnPermissionError` | Sin permisos |
| 4 | `Handle_ShouldIncludeSteps` | Incluir steps |
| 5 | `Handle_ShouldMapAllFields` | Mapear todos los campos |

#### GetStudyPipelinesQueryHandlerTests.cs (6 tests)
#### GetExecutionByIdQueryHandlerTests.cs (5 tests)
#### GetPipelineExecutionsQueryHandlerTests.cs (6 tests)
#### GetTraceExecutionsQueryHandlerTests.cs (5 tests)
#### GetStepTypesQueryHandlerTests.cs (3 tests)

---

## 5. TESTS DE INTEGRACIÓN API

### 5.1 PipelineEndpointsTests.cs (18 tests)

```
Tests/API/Pipelines/PipelineEndpointsTests.cs
```

| # | Test | Endpoint | HTTP | Descripción |
|---|------|----------|------|-------------|
| 1 | `GetPipeline_Authenticated_ShouldReturn200` | GET /pipelines/{id} | 200 | Obtener pipeline |
| 2 | `GetPipeline_Unauthenticated_ShouldReturn401` | GET /pipelines/{id} | 401 | Sin auth |
| 3 | `GetPipeline_NotFound_ShouldReturn404` | GET /pipelines/{id} | 404 | No encontrado |
| 4 | `GetStudyPipelines_ShouldReturn200` | GET /studies/{id}/pipelines | 200 | Listar |
| 5 | `CreatePipeline_ValidData_ShouldReturn201` | POST /pipelines | 201 | Crear |
| 6 | `CreatePipeline_InvalidData_ShouldReturn400` | POST /pipelines | 400 | Datos inválidos |
| 7 | `CreatePipeline_Unauthenticated_ShouldReturn401` | POST /pipelines | 401 | Sin auth |
| 8 | `UpdatePipeline_ValidData_ShouldReturn200` | PUT /pipelines/{id} | 200 | Actualizar |
| 9 | `UpdatePipeline_NotEditable_ShouldReturn400` | PUT /pipelines/{id} | 400 | No editable |
| 10 | `DeletePipeline_ShouldReturn204` | DELETE /pipelines/{id} | 204 | Eliminar |
| 11 | `DeletePipeline_NotDeletable_ShouldReturn400` | DELETE /pipelines/{id} | 400 | No eliminable |
| 12 | `ActivatePipeline_ShouldReturn200` | POST /pipelines/{id}/activate | 200 | Activar |
| 13 | `ActivatePipeline_NoSteps_ShouldReturn400` | POST /pipelines/{id}/activate | 400 | Sin steps |
| 14 | `DeactivatePipeline_ShouldReturn200` | POST /pipelines/{id}/deactivate | 200 | Desactivar |
| 15 | `ArchivePipeline_ShouldReturn200` | POST /pipelines/{id}/archive | 200 | Archivar |
| 16 | `AddStep_ValidData_ShouldReturn201` | POST /pipelines/{id}/steps | 201 | Agregar step |
| 17 | `UpdateStep_ValidData_ShouldReturn200` | PUT /pipelines/{id}/steps/{stepId} | 200 | Actualizar step |
| 18 | `RemoveStep_ShouldReturn204` | DELETE /pipelines/{id}/steps/{stepId} | 204 | Remover step |

### 5.2 PipelineExecutionEndpointsTests.cs (12 tests)

```
Tests/API/Pipelines/PipelineExecutionEndpointsTests.cs
```

| # | Test | Endpoint | HTTP | Descripción |
|---|------|----------|------|-------------|
| 1 | `GetExecution_ShouldReturn200` | GET /executions/{id} | 200 | Obtener ejecución |
| 2 | `GetExecution_NotFound_ShouldReturn404` | GET /executions/{id} | 404 | No encontrado |
| 3 | `GetPipelineExecutions_ShouldReturn200` | GET /pipelines/{id}/executions | 200 | Listar ejecuciones |
| 4 | `GetTraceExecutions_ShouldReturn200` | GET /traces/{id}/executions | 200 | Ejecuciones de trace |
| 5 | `ExecutePipeline_ValidData_ShouldReturn201` | POST /pipelines/{id}/execute | 201 | Ejecutar |
| 6 | `ExecutePipeline_NotActive_ShouldReturn400` | POST /pipelines/{id}/execute | 400 | No activo |
| 7 | `ExecutePipeline_TraceNotProcessed_ShouldReturn400` | POST /pipelines/{id}/execute | 400 | Trace no procesado |
| 8 | `CancelExecution_ShouldReturn200` | POST /executions/{id}/cancel | 200 | Cancelar |
| 9 | `CancelExecution_NotCancellable_ShouldReturn400` | POST /executions/{id}/cancel | 400 | No cancelable |
| 10 | `CompleteStep_ValidData_ShouldReturn200` | POST /executions/{id}/steps/{stepId}/complete | 200 | Completar step |
| 11 | `FailStep_ValidData_ShouldReturn200` | POST /executions/{id}/steps/{stepId}/fail | 200 | Fallar step |
| 12 | `GetStepTypes_ShouldReturn200` | GET /pipelines/step-types | 200 | Listar tipos |

---

## 6. TESTS E2E

### 6.1 PipelineLifecycleE2ETests.cs (8 tests)

```
Tests/E2E/PipelineLifecycleE2ETests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `CreatePipeline_ConfigureSteps_Activate_Execute` | Flujo completo de creación y ejecución |
| 2 | `CreatePipeline_WithMultipleSteps_ReorderSteps` | Reordenamiento de steps |
| 3 | `ExecutePipeline_AllStepsComplete_ShouldCompleteExecution` | Ejecución exitosa completa |
| 4 | `ExecutePipeline_StepFails_ShouldFailExecution` | Ejecución fallida |
| 5 | `ExecutePipeline_Cancel_ShouldCancelAllPendingSteps` | Cancelación |
| 6 | `ArchivePipeline_ShouldPreventExecution` | Archivo previene ejecución |
| 7 | `RestorePipeline_ShouldAllowEditing` | Restaurar permite edición |
| 8 | `ConcurrentExecutions_SameTrace_ShouldPreventDuplicate` | Prevenir ejecuciones duplicadas |

---

## 7. RESUMEN DE TESTS

| Categoría | Tests |
|-----------|-------|
| Domain - Pipeline | 44 |
| Domain - PipelineExecution | 20 |
| Domain - PipelineStep | 12 |
| Domain - StepExecution | 10 |
| Domain - Value Objects | 29 |
| Domain - Enumerations | 36 |
| Domain - IDs | 10 |
| **Subtotal Domain** | **161** |
| Handlers - Commands | 76 |
| Handlers - Queries | 30 |
| **Subtotal Handlers** | **106** |
| API - Endpoints | 30 |
| **Subtotal API** | **30** |
| E2E | 8 |
| **Subtotal E2E** | **8** |
| **TOTAL** | **305** |

---

## 8. ARCHIVOS A CREAR

```
GeneFlow.ApiNet2.Tests/
├── Domain/
│   └── Pipelines/
│       ├── PipelineTests.cs
│       ├── PipelineIdTests.cs
│       ├── PipelineExecutionIdTests.cs
│       ├── Entities/
│       │   ├── PipelineExecutionTests.cs
│       │   ├── PipelineStepTests.cs
│       │   └── StepExecutionTests.cs
│       ├── ValueObjects/
│       │   ├── PipelineNameTests.cs
│       │   ├── PipelineDescriptionTests.cs
│       │   └── StepConfigurationTests.cs
│       └── Enumerations/
│           ├── PipelineStatusTests.cs
│           ├── ExecutionStatusTests.cs
│           ├── StepExecutionStatusTests.cs
│           └── StepTypeTests.cs
├── Application/
│   └── Pipelines/
│       ├── Commands/
│       │   ├── CreatePipelineCommandHandlerTests.cs
│       │   ├── UpdatePipelineCommandHandlerTests.cs
│       │   ├── DeletePipelineCommandHandlerTests.cs
│       │   ├── ActivatePipelineCommandHandlerTests.cs
│       │   ├── DeactivatePipelineCommandHandlerTests.cs
│       │   ├── ArchivePipelineCommandHandlerTests.cs
│       │   ├── AddPipelineStepCommandHandlerTests.cs
│       │   ├── UpdatePipelineStepCommandHandlerTests.cs
│       │   ├── RemovePipelineStepCommandHandlerTests.cs
│       │   ├── ReorderPipelineStepsCommandHandlerTests.cs
│       │   ├── ExecutePipelineCommandHandlerTests.cs
│       │   ├── CancelExecutionCommandHandlerTests.cs
│       │   ├── CompleteStepExecutionCommandHandlerTests.cs
│       │   └── FailStepExecutionCommandHandlerTests.cs
│       └── Queries/
│           ├── GetPipelineByIdQueryHandlerTests.cs
│           ├── GetStudyPipelinesQueryHandlerTests.cs
│           ├── GetExecutionByIdQueryHandlerTests.cs
│           ├── GetPipelineExecutionsQueryHandlerTests.cs
│           ├── GetTraceExecutionsQueryHandlerTests.cs
│           └── GetStepTypesQueryHandlerTests.cs
├── API/
│   └── Pipelines/
│       ├── PipelineEndpointsTests.cs
│       └── PipelineExecutionEndpointsTests.cs
└── E2E/
    └── PipelineLifecycleE2ETests.cs
```

---

## 9. DEPENDENCIAS Y BUILDERS

### PipelineBuilder.cs
```csharp
public class PipelineBuilder
{
    public PipelineBuilder WithId(long id);
    public PipelineBuilder WithStudyId(long studyId);
    public PipelineBuilder WithOwnerId(long ownerId);
    public PipelineBuilder WithName(string name);
    public PipelineBuilder WithDescription(string description);
    public PipelineBuilder WithStatus(PipelineStatus status);
    public PipelineBuilder WithStep(StepType type, string config);
    public PipelineBuilder WithSteps(int count);
    public Pipeline Build();
}
```

### PipelineFaker.cs
```csharp
public class PipelineFaker : Faker<Pipeline>
{
    public PipelineFaker()
    {
        RuleFor(p => p.Name, f => f.Lorem.Sentence(3));
        RuleFor(p => p.Description, f => f.Lorem.Paragraph());
        // ...
    }
}
```

---

## 10. ORDEN DE IMPLEMENTACIÓN

1. **Día 1:** PipelineNameTests, PipelineDescriptionTests, PipelineIdTests
2. **Día 2:** StepConfigurationTests, PipelineStatusTests
3. **Día 3:** ExecutionStatusTests, StepExecutionStatusTests, StepTypeTests
4. **Día 4:** PipelineStepTests, StepExecutionTests
5. **Día 5:** PipelineExecutionTests
6. **Día 6-7:** PipelineTests (completo)
7. **Día 8-9:** Command handlers (Create, Update, Delete, Activate)
8. **Día 10-11:** Command handlers (Steps, Execute, Cancel)
9. **Día 12:** Query handlers
10. **Día 13-14:** API endpoint tests
11. **Día 15:** E2E tests

**Tiempo estimado:** 3 semanas
