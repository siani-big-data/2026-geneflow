# Plan de Tests: Módulo Traces

## Cobertura Actual: ~15%
## Tests Estimados: ~403 tests

### Objetivos de Cobertura por Capa

| Capa | Archivos | Line | Branch | Method |
|------|----------|------|--------|--------|
| **Domain** | Trace.cs, Entities/*, ValueObjects/*, Enumerations/* | **≥95%** | **≥90%** | **≥98%** |
| **Application** | Commands/*, Queries/* | **≥90%** | **≥85%** | **≥95%** |
| **Infrastructure** | Repositories/*, Services/*, TraceAnalysisService | **≥75%** | **≥70%** | **≥85%** |
| **API** | Endpoints/* | **≥85%** | **≥80%** | **≥90%** |

### Objetivos Específicos por Archivo

| Archivo | Line | Branch | Prioridad |
|---------|------|--------|-----------|
| `Trace.cs` (513 LOC) | 95% | 92% | P0 |
| `SequenceEdit.cs` (131 LOC) | 95% | 90% | P0 |
| `TraceAnnotation.cs` (219 LOC) | 95% | 90% | P0 |
| `TraceTrim.cs` (141 LOC) | 95% | 90% | P0 |
| `TraceFile.cs` (118 LOC) | 98% | 95% | P0 |
| `QualityMetrics.cs` (117 LOC) | 98% | 95% | P0 |
| `TrimRegion.cs` (144 LOC) | 98% | 95% | P0 |
| `TraceName.cs` (42 LOC) | 100% | 100% | P1 |
| `TraceDescription.cs` (43 LOC) | 100% | 100% | P1 |
| `TraceStatus.cs` (81 LOC) | 100% | 100% | P0 |
| `TraceFormat.cs` (69 LOC) | 100% | 100% | P0 |
| `EditType.cs` (33 LOC) | 100% | 100% | P0 |
| `AnnotationType.cs` (31 LOC) | 100% | 100% | P0 |
| `TrimType.cs` (28 LOC) | 100% | 100% | P0 |
| Command Handlers (Trim/Edit) | 90% | 85% | P0 |
| Command Handlers (Annotations) | 90% | 85% | P0 |
| Command Handlers (Processing) | 90% | 85% | P0 |
| Query Handlers (Sequence) | 85% | 80% | P0 |
| `TraceEndpoints.cs` | 85% | 80% | P0 |
| `TraceEditingEndpoints.cs` | 85% | 80% | P0 |
| `TraceAnnotationEndpoints.cs` | 85% | 80% | P1 |
| `TraceSequenceEndpoints.cs` | 85% | 80% | P1 |

---

## 1. ANÁLISIS DEL MÓDULO

### 1.1 Archivos de Dominio (2,275 LOC)

| Archivo | LOC | Tests Existentes | Faltantes |
|---------|-----|------------------|-----------|
| `Trace.cs` | 513 | ~10 | ~40 |
| `TraceErrors.cs` | 106 | 0 | 0 |
| `TraceId.cs` | 73 | ~5 | 0 |
| `Entities/SequenceEdit.cs` | 131 | 0 | 12 |
| `Entities/TraceAnnotation.cs` | 219 | 0 | 15 |
| `Entities/TraceTrim.cs` | 141 | 0 | 12 |
| `ValueObjects/TraceFile.cs` | 118 | 0 | 10 |
| `ValueObjects/TraceName.cs` | 42 | 0 | 6 |
| `ValueObjects/TraceDescription.cs` | 43 | 0 | 6 |
| `ValueObjects/QualityMetrics.cs` | 117 | 0 | 10 |
| `ValueObjects/TrimRegion.cs` | 144 | 0 | 12 |
| `Enumerations/TraceStatus.cs` | 81 | ~5 | 5 |
| `Enumerations/TraceFormat.cs` | 69 | ~5 | 0 |
| `Enumerations/EditType.cs` | 33 | ~4 | 0 |
| `Enumerations/AnnotationType.cs` | 31 | 0 | 5 |
| `Enumerations/AnnotationStrand.cs` | 44 | 0 | 5 |
| `Enumerations/TrimType.cs` | 28 | 0 | 4 |
| `Enumerations/TrimEnd.cs` | 22 | 0 | 3 |
| Events (14) | ~180 | 0 | 0 |

### 1.2 Archivos de Aplicación (~1,500 LOC)

**Commands con Tests:**
- UploadTraceCommandHandler ✓
- UpdateTraceNameCommandHandler ✓
- ArchiveTraceCommandHandler ✓
- DeleteTraceCommandHandler ✓
- RetryTraceProcessingCommandHandler ✓

**Commands SIN Tests:**
| Handler | LOC | Prioridad |
|---------|-----|-----------|
| `AutoTrimTraceCommandHandler` | ~80 | P0 |
| `ManualTrimTraceCommandHandler` | ~70 | P0 |
| `UndoTrimTraceCommandHandler` | ~50 | P1 |
| `CreateAnnotationCommandHandler` | ~80 | P0 |
| `UpdateAnnotationCommandHandler` | ~60 | P1 |
| `DeleteAnnotationCommandHandler` | ~40 | P1 |
| `CreateSequenceEditCommandHandler` | ~80 | P0 |
| `UndoSequenceEditCommandHandler` | ~50 | P1 |
| `UndoAllSequenceEditsCommandHandler` | ~50 | P1 |
| `StartTraceProcessingCommandHandler` | ~60 | P0 |
| `CompleteTraceProcessingCommandHandler` | ~80 | P0 |
| `FailTraceProcessingCommandHandler` | ~50 | P1 |

**Queries con Tests:**
- GetTraceByIdQueryHandler ✓

**Queries SIN Tests:**
| Handler | LOC | Prioridad |
|---------|-----|-----------|
| `GetStudyTracesQueryHandler` | ~60 | P1 |
| `GetTrimmedSequenceQueryHandler` | ~80 | P0 |
| `GetEditedSequenceQueryHandler` | ~80 | P0 |
| `GetReverseComplementQueryHandler` | ~50 | P1 |
| `GetTraceAnnotationsQueryHandler` | ~50 | P1 |
| `GetStudyAnnotationsQueryHandler` | ~60 | P1 |
| `GetSequenceEditsQueryHandler` | ~50 | P1 |
| `GetTraceCountsByStatusQueryHandler` | ~40 | P2 |
| `PreviewTrimQueryHandler` | ~70 | P1 |
| `GetTraceManifestQueryHandler` | ~60 | P1 |
| `GetSequencePageQueryHandler` | ~70 | P1 |
| `GetTraceTrimsQueryHandler` | ~50 | P2 |

---

## 2. TESTS EXISTENTES

### Domain Tests
- `TraceTests.cs` - ~10 tests
- `TraceIdTests.cs` - ~5 tests
- `TraceStatusTests.cs` - ~5 tests
- `TraceFormatTests.cs` - ~5 tests
- `EditTypeTests.cs` - ~4 tests

### Handler Tests
- `UploadTraceCommandHandlerTests.cs` - ~6 tests
- `UpdateTraceNameCommandHandlerTests.cs` - ~4 tests
- `ArchiveTraceCommandHandlerTests.cs` - ~4 tests
- `DeleteTraceCommandHandlerTests.cs` - ~4 tests
- `RetryTraceProcessingCommandHandlerTests.cs` - ~4 tests
- `GetTraceByIdQueryHandlerTests.cs` - ~4 tests

**Total Existentes: ~55 tests**

---

## 3. TESTS UNITARIOS DE DOMINIO FALTANTES

### 3.1 TraceTests.cs - Tests Adicionales (40 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_WithValidData_ShouldCreate` | Crear trace |
| 2 | `Create_ShouldSetStatusToUploading` | Estado inicial |
| 3 | `MarkUploaded_ShouldSetStatusToUploaded` | Marcar subido |
| 4 | `MarkUploaded_ShouldSetFileSizeAndChecksum` | Metadata |
| 5 | `StartProcessing_FromUploaded_ShouldProcess` | Iniciar procesamiento |
| 6 | `StartProcessing_InvalidStatus_ShouldReturnError` | Estado inválido |
| 7 | `CompleteProcessing_WithMetrics_ShouldComplete` | Completar |
| 8 | `CompleteProcessing_ShouldSetQualityMetrics` | Establecer métricas |
| 9 | `FailProcessing_ShouldSetStatusToFailed` | Fallar procesamiento |
| 10 | `FailProcessing_ShouldSetErrorMessage` | Mensaje de error |
| 11 | `RetryProcessing_FromFailed_ShouldProcess` | Reintentar |
| 12 | `Archive_FromProcessed_ShouldArchive` | Archivar |
| 13 | `Archive_FromFailed_ShouldArchive` | Archivar fallido |
| 14 | `RestoREDACTED` | Restaurar |
| 15 | `Delete_ShouldRaiseDeletedEvent` | Evento de eliminación |
| 16 | `ApplyAutoTrim_ValidParams_ShouldApplyTrim` | Auto-trim |
| 17 | `ApplyAutoTrim_ShouldCreateTrimRegion` | Crear región |
| 18 | `ApplyAutoTrim_ShouldRaiseTrimmedEvent` | Evento trim |
| 19 | `ApplyManualTrim_ValidPositions_ShouldApplyTrim` | Manual trim |
| 20 | `ApplyManualTrim_InvalidPositions_ShouldReturnError` | Posiciones inválidas |
| 21 | `UndoTrim_ExistingTrim_ShouldUndo` | Deshacer trim |
| 22 | `UndoTrim_NoTrim_ShouldReturnError` | Sin trim |
| 23 | `AddAnnotation_ValidData_ShouldAdd` | Agregar anotación |
| 24 | `AddAnnotation_InvalidPositions_ShouldReturnError` | Posiciones inválidas |
| 25 | `AddAnnotation_ShouldRaiseCreatedEvent` | Evento creación |
| 26 | `UpdateAnnotation_ExistingAnnotation_ShouldUpdate` | Actualizar |
| 27 | `UpdateAnnotation_NotFound_ShouldReturnError` | No encontrada |
| 28 | `DeleteAnnotation_ExistingAnnotation_ShouldDelete` | Eliminar |
| 29 | `GetAnnotations_ShouldReturnAllAnnotations` | Obtener todas |
| 30 | `CreateSequenceEdit_ValidData_ShouldCreate` | Crear edición |
| 31 | `CreateSequenceEdit_ShouldIncrementVersion` | Incrementar versión |
| 32 | `CreateSequenceEdit_InvalidPositions_ShouldReturnError` | Posiciones inválidas |
| 33 | `UndoSequenceEdit_LastEdit_ShouldUndo` | Deshacer última |
| 34 | `UndoSequenceEdit_NotLastEdit_ShouldReturnError` | No es última |
| 35 | `UndoAllSequenceEdits_ShouldUndoAll` | Deshacer todas |
| 36 | `GetEditedSequence_ShouldApplyAllEdits` | Secuencia editada |
| 37 | `GetTrimmedSequence_ShouldApplyTrim` | Secuencia trimmed |
| 38 | `GetReverseComplement_ShouldReturnComplement` | Complemento reverso |
| 39 | `UpdateName_ShouldUpdateName` | Actualizar nombre |
| 40 | `CanBeEdited_WhenProcessed_ShouldReturnTrue` | Puede editarse |

### 3.2 SequenceEditTests.cs (12 tests)

```
Tests/Domain/Traces/Entities/SequenceEditTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_Insertion_ShouldCreate` | Crear inserción |
| 2 | `Create_Deletion_ShouldCreate` | Crear deleción |
| 3 | `Create_Substitution_ShouldCreate` | Crear sustitución |
| 4 | `Create_ShouldSetVersion` | Establecer versión |
| 5 | `Create_ShouldSetCreatedAt` | Timestamp |
| 6 | `Apply_Insertion_ShouldInsertBases` | Aplicar inserción |
| 7 | `Apply_Deletion_ShouldDeleteBases` | Aplicar deleción |
| 8 | `Apply_Substitution_ShouldSubstituteBases` | Aplicar sustitución |
| 9 | `Undo_ShouldMarkAsUndone` | Marcar como deshecha |
| 10 | `GetAffectedPositions_ShouldReturnRange` | Posiciones afectadas |
| 11 | `ValidateBases_InvalidBases_ShouldReturnError` | Bases inválidas |
| 12 | `IsActive_WhenNotUndone_ShouldReturnTrue` | Estado activo |

### 3.3 TraceAnnotationTests.cs (15 tests)

```
Tests/Domain/Traces/Entities/TraceAnnotationTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_WithValidData_ShouldCreate` | Crear anotación |
| 2 | `Create_ShouldSetType` | Tipo de anotación |
| 3 | `Create_ShouldSetStrand` | Hebra (forward/reverse) |
| 4 | `Create_ShouldSetPositions` | Posiciones |
| 5 | `Create_InvalidPositions_ShouldReturnError` | Posiciones inválidas |
| 6 | `Create_EndBeforeStart_ShouldReturnError` | End < Start |
| 7 | `Update_ShouldUpdateFields` | Actualizar campos |
| 8 | `Update_ShouldSetModifiedAt` | Timestamp modificación |
| 9 | `SetColor_ValidHex_ShouldSetColor` | Color válido |
| 10 | `SetColor_InvalidHex_ShouldReturnError` | Color inválido |
| 11 | `GetLength_ShouldReturnCorrectLength` | Calcular longitud |
| 12 | `Contains_PositionInRange_ShouldReturnTrue` | Posición contenida |
| 13 | `Overlaps_OverlappingAnnotation_ShouldReturnTrue` | Solapamiento |
| 14 | `IsForwardStrand_WhenForward_ShouldReturnTrue` | Verificar hebra |
| 15 | `GetFeatureKey_ShouldReturnGenbankKey` | Clave GenBank |

### 3.4 TraceTrimTests.cs (12 tests)

```
Tests/Domain/Traces/Entities/TraceTrimTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_AutoTrim_ShouldCreate` | Crear auto-trim |
| 2 | `Create_ManualTrim_ShouldCreate` | Crear manual trim |
| 3 | `Create_ShouldSetTrimType` | Tipo de trim |
| 4 | `Create_ShouldSetTrimEnd` | Extremo (5'/3') |
| 5 | `Create_ShouldSetCutoff` | Valor de corte |
| 6 | `GetTrimmedRange_ShouldReturnRange` | Rango trimmed |
| 7 | `ApplyToSequence_ShouldTrimSequence` | Aplicar a secuencia |
| 8 | `Undo_ShouldMarkAsUndone` | Marcar deshecho |
| 9 | `IsActive_WhenNotUndone_ShouldReturnTrue` | Estado activo |
| 10 | `GetBasesRemoved_5Prime_ShouldReturnCount` | Bases removidas 5' |
| 11 | `GetBasesRemoved_3Prime_ShouldReturnCount` | Bases removidas 3' |
| 12 | `ValidatePositions_ShouldValidate` | Validar posiciones |

### 3.5 Value Objects Tests

#### TraceFileTests.cs (10 tests)
```
Tests/Domain/Traces/ValueObjects/TraceFileTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_WithValidData_ShouldCreate` | Crear archivo |
| 2 | `Create_ShouldSetFileName` | Nombre de archivo |
| 3 | `Create_ShouldSetFormat` | Formato (AB1/SCF) |
| 4 | `Create_InvalidFormat_ShouldReturnError` | Formato inválido |
| 5 | `SetFileSize_ShouldSetSize` | Tamaño de archivo |
| 6 | `SetChecksum_ShouldSetChecksum` | Checksum SHA256 |
| 7 | `GetExtension_ShouldReturnExtension` | Extensión |
| 8 | `IsAb1_WhenAb1Format_ShouldReturnTrue` | Verificar AB1 |
| 9 | `ValidateFileName_InvalidChars_ShouldReturnError` | Caracteres inválidos |
| 10 | `Equality_SameChecksum_ShouldBeEqual` | Igualdad |

#### TraceNameTests.cs (6 tests)
#### TraceDescriptionTests.cs (6 tests)

#### QualityMetricsTests.cs (10 tests)
```
Tests/Domain/Traces/ValueObjects/QualityMetricsTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_WithValidData_ShouldCreate` | Crear métricas |
| 2 | `Create_ShouldSetAverageQuality` | Calidad promedio |
| 3 | `Create_ShouldSetSequenceLength` | Longitud secuencia |
| 4 | `Create_ShouldSetGcContent` | Contenido GC |
| 5 | `Create_InvalidQuality_ShouldReturnError` | Calidad inválida |
| 6 | `GetQualityCategory_HighQuality_ShouldReturnHigh` | Categoría alta |
| 7 | `GetQualityCategory_LowQuality_ShouldReturnLow` | Categoría baja |
| 8 | `GetQ20Percentage_ShouldCalculate` | Porcentaje Q20 |
| 9 | `GetQ30Percentage_ShouldCalculate` | Porcentaje Q30 |
| 10 | `Empty_ShouldHaveDefaultValues` | Valores default |

#### TrimRegionTests.cs (12 tests)
```
Tests/Domain/Traces/ValueObjects/TrimRegionTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_WithValidPositions_ShouldCreate` | Crear región |
| 2 | `Create_EndBeforeStart_ShouldReturnError` | End < Start |
| 3 | `Create_NegativePositions_ShouldReturnError` | Posiciones negativas |
| 4 | `GetLength_ShouldReturnCorrectLength` | Calcular longitud |
| 5 | `Contains_PositionInRange_ShouldReturnTrue` | Posición contenida |
| 6 | `Overlaps_OverlappingRegion_ShouldReturnTrue` | Solapamiento |
| 7 | `Merge_OverlappingRegions_ShouldMerge` | Fusionar regiones |
| 8 | `Subtract_ContainedRegion_ShouldSubtract` | Restar región |
| 9 | `Intersect_OverlappingRegions_ShouldIntersect` | Intersección |
| 10 | `IsEmpty_ZeroLength_ShouldReturnTrue` | Región vacía |
| 11 | `ApplyToSequence_ShouldExtractSubsequence` | Extraer subsecuencia |
| 12 | `Equality_SamePositions_ShouldBeEqual` | Igualdad |

### 3.6 Enumerations Tests

#### AnnotationTypeTests.cs (5 tests)
#### AnnotationStrandTests.cs (5 tests)
#### TrimTypeTests.cs (4 tests)
#### TrimEndTests.cs (3 tests)
#### TraceStatusTests.cs - Tests Adicionales (5 tests)

---

## 4. TESTS DE HANDLERS FALTANTES

### 4.1 Command Handler Tests

#### AutoTrimTraceCommandHandlerTests.cs (8 tests)
```
Tests/Application/Traces/Commands/AutoTrimTraceCommandHandlerTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Handle_ValidData_ShouldApplyAutoTrim` | Auto-trim exitoso |
| 2 | `Handle_TraceNotFound_ShouldReturnNotFoundError` | No encontrado |
| 3 | `Handle_TraceNotProcessed_ShouldReturnError` | No procesado |
| 4 | `Handle_InvalidCutoff_ShouldReturnValidationError` | Cutoff inválido |
| 5 | `Handle_UserNotStudyMember_ShouldReturnPermissionError` | Sin permisos |
| 6 | `Handle_ShouldCalculateTrimPositions` | Calcular posiciones |
| 7 | `Handle_ShouldPersistTrim` | Persistir trim |
| 8 | `Handle_ShouldReturnTrimmedSequenceDto` | Retornar DTO |

#### ManualTrimTraceCommandHandlerTests.cs (8 tests)
#### UndoTrimTraceCommandHandlerTests.cs (6 tests)
#### CreateAnnotationCommandHandlerTests.cs (10 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Handle_ValidData_ShouldCreateAnnotation` | Crear exitosamente |
| 2 | `Handle_TraceNotFound_ShouldReturnNotFoundError` | No encontrado |
| 3 | `Handle_TraceNotProcessed_ShouldReturnError` | No procesado |
| 4 | `Handle_InvalidPositions_ShouldReturnValidationError` | Posiciones inválidas |
| 5 | `Handle_PositionsOutOfRange_ShouldReturnError` | Fuera de rango |
| 6 | `Handle_InvalidAnnotationType_ShouldReturnError` | Tipo inválido |
| 7 | `Handle_UserNotStudyMember_ShouldReturnPermissionError` | Sin permisos |
| 8 | `Handle_ShouldPersistAnnotation` | Persistir |
| 9 | `Handle_ShouldReturnAnnotationDto` | Retornar DTO |
| 10 | `Handle_WithColor_ShouldSetColor` | Establecer color |

#### UpdateAnnotationCommandHandlerTests.cs (7 tests)
#### DeleteAnnotationCommandHandlerTests.cs (5 tests)
#### CreateSequenceEditCommandHandlerTests.cs (10 tests)
#### UndoSequenceEditCommandHandlerTests.cs (6 tests)
#### UndoAllSequenceEditsCommandHandlerTests.cs (5 tests)
#### StartTraceProcessingCommandHandlerTests.cs (6 tests)
#### CompleteTraceProcessingCommandHandlerTests.cs (8 tests)
#### FailTraceProcessingCommandHandlerTests.cs (5 tests)

### 4.2 Query Handler Tests

#### GetStudyTracesQueryHandlerTests.cs (6 tests)
#### GetTrimmedSequenceQueryHandlerTests.cs (8 tests)
#### GetEditedSequenceQueryHandlerTests.cs (8 tests)
#### GetReverseComplementQueryHandlerTests.cs (5 tests)
#### GetTraceAnnotationsQueryHandlerTests.cs (5 tests)
#### GetStudyAnnotationsQueryHandlerTests.cs (5 tests)
#### GetSequenceEditsQueryHandlerTests.cs (5 tests)
#### GetTraceCountsByStatusQueryHandlerTests.cs (4 tests)
#### PreviewTrimQueryHandlerTests.cs (6 tests)
#### GetTraceManifestQueryHandlerTests.cs (5 tests)
#### GetSequencePageQueryHandlerTests.cs (6 tests)
#### GetTraceTrimsQueryHandlerTests.cs (4 tests)

---

## 5. TESTS DE INTEGRACIÓN API

### 5.1 TraceEndpointsTests.cs (16 tests)

| # | Test | Endpoint | HTTP | Descripción |
|---|------|----------|------|-------------|
| 1 | `GetTrace_Authenticated_ShouldReturn200` | GET /traces/{id} | 200 | Obtener trace |
| 2 | `GetTrace_NotFound_ShouldReturn404` | GET /traces/{id} | 404 | No encontrado |
| 3 | `GetStudyTraces_ShouldReturn200` | GET /studies/{id}/traces | 200 | Listar traces |
| 4 | `UploadTrace_ValidFile_ShouldReturn201` | POST /studies/{id}/traces | 201 | Subir trace |
| 5 | `UploadTrace_InvalidFormat_ShouldReturn400` | POST /studies/{id}/traces | 400 | Formato inválido |
| 6 | `UploadTrace_TooLarge_ShouldReturn413` | POST /studies/{id}/traces | 413 | Archivo muy grande |
| 7 | `UpdateTraceName_ShouldReturn200` | PUT /traces/{id}/name | 200 | Actualizar nombre |
| 8 | `ArchiveTrace_ShouldReturn200` | POST /traces/{id}/archive | 200 | Archivar |
| 9 | `RestoreTrace_ShouldReturn200` | POST /traces/{id}/restore | 200 | Restaurar |
| 10 | `DeleteTrace_ShouldReturn204` | DELETE /traces/{id} | 204 | Eliminar |
| 11 | `GetTraceCountsByStatus_ShouldReturn200` | GET /studies/{id}/traces/counts | 200 | Conteos |
| 12 | `RetryProcessing_ShouldReturn200` | POST /traces/{id}/retry | 200 | Reintentar |
| 13 | `GetManifest_ShouldReturn200` | GET /traces/{id}/manifest | 200 | Manifest |
| 14 | `DownloadTrace_ShouldReturnFile` | GET /traces/{id}/download | 200 | Descargar |
| 15 | `BatchUpload_ShouldReturn201` | POST /studies/{id}/traces/batch | 201 | Subir múltiples |
| 16 | `BatchDelete_ShouldReturn204` | DELETE /studies/{id}/traces/batch | 204 | Eliminar múltiples |

### 5.2 TraceEditingEndpointsTests.cs (14 tests)

| # | Test | Endpoint | HTTP | Descripción |
|---|------|----------|------|-------------|
| 1 | `AutoTrim_ValidParams_ShouldReturn200` | POST /traces/{id}/trim/auto | 200 | Auto-trim |
| 2 | `AutoTrim_InvalidCutoff_ShouldReturn400` | POST /traces/{id}/trim/auto | 400 | Cutoff inválido |
| 3 | `ManualTrim_ValidPositions_ShouldReturn200` | POST /traces/{id}/trim/manual | 200 | Manual trim |
| 4 | `ManualTrim_InvalidPositions_ShouldReturn400` | POST /traces/{id}/trim/manual | 400 | Posiciones inválidas |
| 5 | `UndoTrim_ShouldReturn200` | POST /traces/{id}/trim/undo | 200 | Deshacer trim |
| 6 | `PreviewTrim_ShouldReturn200` | GET /traces/{id}/trim/preview | 200 | Preview |
| 7 | `GetTrims_ShouldReturn200` | GET /traces/{id}/trims | 200 | Listar trims |
| 8 | `CreateEdit_ValidData_ShouldReturn201` | POST /traces/{id}/edits | 201 | Crear edición |
| 9 | `CreateEdit_InvalidBases_ShouldReturn400` | POST /traces/{id}/edits | 400 | Bases inválidas |
| 10 | `UndoEdit_LastEdit_ShouldReturn200` | POST /traces/{id}/edits/undo | 200 | Deshacer |
| 11 | `UndoAllEdits_ShouldReturn200` | POST /traces/{id}/edits/undo-all | 200 | Deshacer todas |
| 12 | `GetEdits_ShouldReturn200` | GET /traces/{id}/edits | 200 | Listar ediciones |
| 13 | `GetEditedSequence_ShouldReturn200` | GET /traces/{id}/sequence/edited | 200 | Secuencia editada |
| 14 | `GetTrimmedSequence_ShouldReturn200` | GET /traces/{id}/sequence/trimmed | 200 | Secuencia trimmed |

### 5.3 TraceAnnotationEndpointsTests.cs (10 tests)

| # | Test | Endpoint | HTTP | Descripción |
|---|------|----------|------|-------------|
| 1 | `GetAnnotations_ShouldReturn200` | GET /traces/{id}/annotations | 200 | Listar |
| 2 | `CreateAnnotation_ValidData_ShouldReturn201` | POST /traces/{id}/annotations | 201 | Crear |
| 3 | `CreateAnnotation_InvalidPositions_ShouldReturn400` | POST /traces/{id}/annotations | 400 | Posiciones inválidas |
| 4 | `UpdateAnnotation_ShouldReturn200` | PUT /traces/{id}/annotations/{annId} | 200 | Actualizar |
| 5 | `DeleteAnnotation_ShouldReturn204` | DELETE /traces/{id}/annotations/{annId} | 204 | Eliminar |
| 6 | `GetStudyAnnotations_ShouldReturn200` | GET /studies/{id}/annotations | 200 | Todas del estudio |
| 7 | `ExportAnnotations_GenBank_ShouldReturnFile` | GET /traces/{id}/annotations/export?format=genbank | 200 | Exportar GenBank |
| 8 | `ExportAnnotations_GFF3_ShouldReturnFile` | GET /traces/{id}/annotations/export?format=gff3 | 200 | Exportar GFF3 |
| 9 | `ImportAnnotations_ShouldReturn201` | POST /traces/{id}/annotations/import | 201 | Importar |
| 10 | `BatchCreateAnnotations_ShouldReturn201` | POST /traces/{id}/annotations/batch | 201 | Crear múltiples |

### 5.4 TraceSequenceEndpointsTests.cs (10 tests)

| # | Test | Endpoint | HTTP | Descripción |
|---|------|----------|------|-------------|
| 1 | `GetSequence_ShouldReturn200` | GET /traces/{id}/sequence | 200 | Secuencia completa |
| 2 | `GetSequencePage_ShouldReturn200` | GET /traces/{id}/sequence/page | 200 | Página de secuencia |
| 3 | `GetSequencePage_WithOffset_ShouldReturn200` | GET /traces/{id}/sequence/page?offset=100 | 200 | Con offset |
| 4 | `GetReverseComplement_ShouldReturn200` | GET /traces/{id}/sequence/reverse-complement | 200 | Complemento reverso |
| 5 | `GetTranslation_Frame1_ShouldReturn200` | GET /traces/{id}/sequence/translate?frame=1 | 200 | Traducción frame 1 |
| 6 | `GetTranslation_AllFrames_ShouldReturn200` | GET /traces/{id}/sequence/translate?frames=all | 200 | Todas las frames |
| 7 | `GetQualityScores_ShouldReturn200` | GET /traces/{id}/quality | 200 | Scores de calidad |
| 8 | `GetChromatogram_ShouldReturn200` | GET /traces/{id}/chromatogram | 200 | Datos cromatograma |
| 9 | `SearchMotif_ShouldReturn200` | POST /traces/{id}/sequence/search | 200 | Buscar motivo |
| 10 | `FindRestrictionSites_ShouldReturn200` | GET /traces/{id}/sequence/restriction-sites | 200 | Sitios de restricción |

---

## 6. TESTS E2E

### 6.1 TraceProcessingE2ETests.cs (12 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `UploadTrace_ProcessComplete_ShouldSucceed` | Flujo completo de upload |
| 2 | `UploadTrace_ProcessFail_RetrySucceed` | Fallo y reintento |
| 3 | `BatchUpload_AllProcess_ShouldSucceed` | Upload múltiple |
| 4 | `AutoTrim_ThenManualTrim_ShouldApplyBoth` | Múltiples trims |
| 5 | `CreateAnnotations_ExportGenBank_ShouldExport` | Anotaciones y exportación |
| 6 | `CreateEdits_UndoAll_ShouldRevert` | Ediciones y deshacer |
| 7 | `TrimAndEdit_GetFinalSequence_ShouldApplyAll` | Secuencia final |
| 8 | `ArchiveTrace_PreventEdits_ShouldFail` | Archivar previene ediciones |
| 9 | `DeleteTrace_WithAnnotations_ShouldDeleteAll` | Eliminar con anotaciones |
| 10 | `ProcessMultipleTraces_Concurrently_ShouldSucceed` | Procesamiento concurrente |
| 11 | `LargeTrace_ShouldProcessCorrectly` | Trace grande |
| 12 | `CorruptedFile_ShouldFail_Gracefully` | Archivo corrupto |

---

## 7. RESUMEN DE TESTS

| Categoría | Existentes | Nuevos | Total |
|-----------|------------|--------|-------|
| Domain - Trace | 10 | 40 | 50 |
| Domain - Entities | 0 | 39 | 39 |
| Domain - Value Objects | 0 | 44 | 44 |
| Domain - Enumerations | 14 | 17 | 31 |
| **Subtotal Domain** | **24** | **140** | **164** |
| Handlers - Commands | 22 | 84 | 106 |
| Handlers - Queries | 4 | 67 | 71 |
| **Subtotal Handlers** | **26** | **151** | **177** |
| API - Endpoints | 0 | 50 | 50 |
| **Subtotal API** | **0** | **50** | **50** |
| E2E | 0 | 12 | 12 |
| **Subtotal E2E** | **0** | **12** | **12** |
| **TOTAL** | **~50** | **~353** | **~403** |

---

## 8. ORDEN DE IMPLEMENTACIÓN

1. **Día 1-2:** Value Objects (TraceFile, QualityMetrics, TrimRegion)
2. **Día 3:** Entities (SequenceEdit, TraceTrim)
3. **Día 4:** Entities (TraceAnnotation)
4. **Día 5:** TraceTests adicionales (trim, annotations)
5. **Día 6:** TraceTests adicionales (edits, processing)
6. **Día 7-8:** Command handlers (trim, processing)
7. **Día 9-10:** Command handlers (annotations, edits)
8. **Día 11-12:** Query handlers
9. **Día 13-14:** API endpoint tests
10. **Día 15:** E2E tests

**Tiempo estimado:** 3 semanas
