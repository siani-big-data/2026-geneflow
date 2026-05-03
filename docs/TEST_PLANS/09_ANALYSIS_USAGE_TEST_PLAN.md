# Plan de Tests: Módulos Analysis y Usage

## Cobertura Actual: 0%
## Tests Estimados: ~53 tests

### Objetivos de Cobertura por Capa

| Capa | Archivos | Line | Branch | Method |
|------|----------|------|--------|--------|
| **Application** | Commands/*, Queries/* | **≥90%** | **≥85%** | **≥95%** |
| **Infrastructure** | Services/* | **≥75%** | **≥70%** | **≥85%** |
| **API** | Endpoints/* | **≥85%** | **≥80%** | **≥90%** |

### Objetivos Específicos - Analysis

| Archivo | Line | Branch | Prioridad |
|---------|------|--------|-----------|
| `RunAnalysisCommandHandler` | 90% | 85% | P0 |
| `CancelAnalysisCommandHandler` | 90% | 85% | P1 |
| `GetAnalysisResultQueryHandler` | 85% | 80% | P0 |
| `ListAnalysisResultsQueryHandler` | 85% | 80% | P1 |
| `AnalysisEndpoints.cs` | 85% | 80% | P0 |

### Objetivos Específicos - Usage

| Archivo | Line | Branch | Prioridad |
|---------|------|--------|-----------|
| `GetDashboardStatsQueryHandler` | 85% | 80% | P0 |
| `GetUsageStatsQueryHandler` | 85% | 80% | P1 |
| `GetStorageUsageQueryHandler` | 85% | 80% | P1 |
| `UsageEndpoints.cs` | 85% | 80% | P0 |

---

# MÓDULO ANALYSIS

## 1. ANÁLISIS DEL MÓDULO

### 1.1 Archivos de Aplicación (~200 LOC)

**Commands SIN Tests:**
| Handler | LOC | Prioridad |
|---------|-----|-----------|
| `RunAnalysisCommandHandler` | ~80 | P0 |
| `CancelAnalysisCommandHandler` | ~40 | P1 |

**Queries SIN Tests:**
| Handler | LOC | Prioridad |
|---------|-----|-----------|
| `GetAnalysisResultQueryHandler` | ~50 | P0 |
| `ListAnalysisResultsQueryHandler` | ~50 | P1 |

---

## 2. TESTS DE HANDLERS

### 2.1 Command Handler Tests

#### RunAnalysisCommandHandlerTests.cs (8 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Handle_ValidData_ShouldStartAnalysis` | Iniciar análisis |
| 2 | `Handle_TraceNotFound_ShouldReturnError` | Trace no encontrado |
| 3 | `Handle_TraceNotProcessed_ShouldReturnError` | Trace no procesado |
| 4 | `Handle_InvalidAnalysisType_ShouldReturnError` | Tipo inválido |
| 5 | `Handle_UserNotStudyMember_ShouldReturnError` | Sin permisos |
| 6 | `Handle_ShouldPersistAnalysis` | Persistir análisis |
| 7 | `Handle_ShouldQueueJob` | Encolar trabajo |
| 8 | `Handle_ShouldReturnAnalysisId` | Retornar ID |

#### CancelAnalysisCommandHandlerTests.cs (5 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Handle_RunningAnalysis_ShouldCancel` | Cancelar ejecutando |
| 2 | `Handle_NotFound_ShouldReturnError` | No encontrado |
| 3 | `Handle_AlreadyCompleted_ShouldReturnError` | Ya completado |
| 4 | `Handle_AlreadyFailed_ShouldReturnError` | Ya fallido |
| 5 | `Handle_ShouldUpdateStatus` | Actualizar estado |

### 2.2 Query Handler Tests

#### GetAnalysisResultQueryHandlerTests.cs (5 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Handle_ExistingResult_ShouldReturn` | Retornar resultado |
| 2 | `Handle_NotFound_ShouldReturnError` | No encontrado |
| 3 | `Handle_StillRunning_ShouldReturnPending` | En progreso |
| 4 | `Handle_ShouldIncludeMetadata` | Incluir metadata |
| 5 | `Handle_UserNotStudyMember_ShouldReturnError` | Sin permisos |

#### ListAnalysisResultsQueryHandlerTests.cs (4 tests)

---

## 3. TESTS DE INTEGRACIÓN API

### 3.1 AnalysisEndpointsTests.cs (8 tests)

| # | Test | Endpoint | HTTP | Descripción |
|---|------|----------|------|-------------|
| 1 | `RunAnalysis_ValidData_ShouldReturn202` | POST /traces/{id}/analysis | 202 | Iniciar |
| 2 | `RunAnalysis_InvalidType_ShouldReturn400` | POST /traces/{id}/analysis | 400 | Tipo inválido |
| 3 | `GetAnalysisResult_ShouldReturn200` | GET /analysis/{id} | 200 | Obtener resultado |
| 4 | `GetAnalysisResult_NotFound_ShouldReturn404` | GET /analysis/{id} | 404 | No encontrado |
| 5 | `ListAnalysisResults_ShouldReturn200` | GET /traces/{id}/analysis | 200 | Listar |
| 6 | `CancelAnalysis_Running_ShouldReturn200` | POST /analysis/{id}/cancel | 200 | Cancelar |
| 7 | `CancelAnalysis_Completed_ShouldReturn400` | POST /analysis/{id}/cancel | 400 | Ya completado |
| 8 | `GetAnalysisTypes_ShouldReturn200` | GET /analysis/types | 200 | Tipos disponibles |

---

# MÓDULO USAGE

## 4. ANÁLISIS DEL MÓDULO

### 4.1 Archivos de Aplicación (~200 LOC)

**Queries SIN Tests:**
| Handler | LOC | Prioridad |
|---------|-----|-----------|
| `GetDashboardStatsQueryHandler` | ~80 | P0 |
| `GetUsageStatsQueryHandler` | ~60 | P1 |
| `GetStorageUsageQueryHandler` | ~50 | P1 |

---

## 5. TESTS DE HANDLERS

### 5.1 Query Handler Tests

#### GetDashboardStatsQueryHandlerTests.cs (6 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Handle_AuthenticatedUser_ShouldReturnStats` | Retornar estadísticas |
| 2 | `Handle_ShouldIncludeStudyCount` | Contar estudios |
| 3 | `Handle_ShouldIncludeTraceCount` | Contar traces |
| 4 | `Handle_ShouldIncludeStorageUsed` | Almacenamiento usado |
| 5 | `Handle_ShouldIncludePlanLimits` | Límites del plan |
| 6 | `Handle_ShouldIncludeRecentActivity` | Actividad reciente |

#### GetUsageStatsQueryHandlerTests.cs (5 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Handle_ValidDateRange_ShouldReturnStats` | Estadísticas por rango |
| 2 | `Handle_ShouldGroupByPeriod` | Agrupar por período |
| 3 | `Handle_ShouldIncludeUploadCount` | Contar uploads |
| 4 | `Handle_ShouldIncludeProcessingStats` | Stats de procesamiento |
| 5 | `Handle_InvalidDateRange_ShouldReturnError` | Rango inválido |

#### GetStorageUsageQueryHandlerTests.cs (4 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Handle_ShouldReturnTotalUsed` | Total usado |
| 2 | `Handle_ShouldReturnByStudy` | Por estudio |
| 3 | `Handle_ShouldReturnByFileType` | Por tipo de archivo |
| 4 | `Handle_ShouldReturnPlanLimit` | Límite del plan |

---

## 6. TESTS DE INTEGRACIÓN API

### 6.1 UsageEndpointsTests.cs (8 tests)

| # | Test | Endpoint | HTTP | Descripción |
|---|------|----------|------|-------------|
| 1 | `GetDashboardStats_ShouldReturn200` | GET /usage/dashboard | 200 | Dashboard |
| 2 | `GetUsageStats_ShouldReturn200` | GET /usage/stats | 200 | Estadísticas |
| 3 | `GetUsageStats_WithDateRange_ShouldFilter` | GET /usage/stats?from=...&to=... | 200 | Con rango |
| 4 | `GetStorageUsage_ShouldReturn200` | GET /usage/storage | 200 | Almacenamiento |
| 5 | `GetStorageByStudy_ShouldReturn200` | GET /usage/storage/by-study | 200 | Por estudio |
| 6 | `GetQuotaStatus_ShouldReturn200` | GET /usage/quota | 200 | Estado cuota |
| 7 | `GetActivityLog_ShouldReturn200` | GET /usage/activity | 200 | Log actividad |
| 8 | `ExportUsageReport_ShouldReturnFile` | GET /usage/export | 200 | Exportar reporte |

---

## 7. RESUMEN DE TESTS

### Analysis
| Categoría | Existentes | Nuevos | Total |
|-----------|------------|--------|-------|
| Handlers | 0 | 22 | 22 |
| API | 0 | 8 | 8 |
| **Subtotal** | **0** | **30** | **30** |

### Usage
| Categoría | Existentes | Nuevos | Total |
|-----------|------------|--------|-------|
| Handlers | 0 | 15 | 15 |
| API | 0 | 8 | 8 |
| **Subtotal** | **0** | **23** | **23** |

### **TOTAL COMBINADO: ~53 tests**

---

## 8. ORDEN DE IMPLEMENTACIÓN

### Analysis
1. **Día 1:** Command handlers (RunAnalysis, CancelAnalysis)
2. **Día 2:** Query handlers, API tests

### Usage
3. **Día 3:** Query handlers
4. **Día 4:** API tests

**Tiempo estimado:** 4 días
