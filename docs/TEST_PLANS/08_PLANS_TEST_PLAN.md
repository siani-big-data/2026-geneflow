# Plan de Tests: Módulo Plans

## Cobertura Actual: ~60%
## Tests Estimados: ~56 tests

### Objetivos de Cobertura por Capa

| Capa | Archivos | Line | Branch | Method |
|------|----------|------|--------|--------|
| **Domain** | Plan.cs, ValueObjects/* | **≥95%** | **≥90%** | **≥98%** |
| **Application** | Queries/* | **≥85%** | **≥80%** | **≥95%** |
| **API** | Endpoints/* | **≥85%** | **≥80%** | **≥90%** |

### Objetivos Específicos por Archivo

| Archivo | Line | Branch | Prioridad |
|---------|------|--------|-----------|
| `Plan.cs` (~100 LOC) | 95% | 92% | P0 |
| `PlanId.cs` (~40 LOC) | 100% | 100% | P1 |
| `PlanName.cs` (~40 LOC) | 100% | 100% | P1 |
| `PlanPricing.cs` (~50 LOC) | 100% | 100% | P0 |
| `PlanLimits.cs` (~50 LOC) | 100% | 100% | P0 |
| Query Handlers | 85% | 80% | P1 |
| `PlanEndpoints.cs` | 85% | 80% | P1 |

---

## 1. ANÁLISIS DEL MÓDULO

### 1.1 Archivos de Dominio (~300 LOC)

| Archivo | LOC | Tests Existentes | Faltantes |
|---------|-----|------------------|-----------|
| `Plan.cs` | ~100 | ~6 | ~6 |
| `PlanId.cs` | ~40 | 0 | 5 |
| `PlanErrors.cs` | ~30 | 0 | 0 |
| `ValueObjects/PlanName.cs` | ~40 | ~5 | 0 |
| `ValueObjects/PlanPricing.cs` | ~50 | ~5 | 3 |
| `ValueObjects/PlanLimits.cs` | ~50 | ~5 | 3 |

### 1.2 Archivos de Aplicación (~100 LOC)

**Queries con Tests:**
- GetAllPlansQueryHandler ✓

**Queries SIN Tests:**
| Handler | LOC | Prioridad |
|---------|-----|-----------|
| `GetPlanByIdQueryHandler` | ~30 | P1 |
| `ComparePlansQueryHandler` | ~40 | P2 |

---

## 2. TESTS EXISTENTES

### Domain Tests
- `PlanTests.cs` - ~6 tests
- `PlanNameTests.cs` - ~5 tests
- `PlanPricingTests.cs` - ~5 tests
- `PlanLimitsTests.cs` - ~5 tests

### Handler Tests
- `GetAllPlansQueryHandlerTests.cs` - ~4 tests

**Total Existentes: ~25 tests**

---

## 3. TESTS UNITARIOS DE DOMINIO FALTANTES

### 3.1 PlanTests.cs - Tests Adicionales (6 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `IsFreePlan_WhenFree_ShouldReturnTrue` | Verificar plan gratis |
| 2 | `IsHigherTier_ShouldCompareCorrectly` | Comparar tiers |
| 3 | `CanUpgradeTo_HigherTier_ShouldReturnTrue` | Puede actualizar |
| 4 | `CanDowngradeTo_LowerTier_ShouldReturnTrue` | Puede degradar |
| 5 | `GetFeatures_ShouldReturnFeatureList` | Lista de features |
| 6 | `GetMonthlyEquivalent_ForYearly_ShouldCalculate` | Equivalente mensual |

### 3.2 PlanIdTests.cs (5 tests)

### 3.3 PlanPricingTests.cs - Tests Adicionales (3 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `GetYearlyDiscount_ShouldCalculate` | Calcular descuento |
| 2 | `GetPricePerMonth_Yearly_ShouldCalculate` | Precio mensual de anual |
| 3 | `IsTrialAvailable_ShouldReturnCorrectly` | Trial disponible |

### 3.4 PlanLimitsTests.cs - Tests Adicionales (3 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `CanCreateStudy_UnderLimit_ShouldReturnTrue` | Puede crear estudio |
| 2 | `CanUploadTrace_UnderLimit_ShouldReturnTrue` | Puede subir trace |
| 3 | `GetRemainingStudies_ShouldCalculate` | Estudios restantes |

---

## 4. TESTS DE HANDLERS FALTANTES

### 4.1 Query Handler Tests

#### GetPlanByIdQueryHandlerTests.cs (4 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Handle_ExistingPlan_ShouldReturnPlan` | Retornar plan |
| 2 | `Handle_NotFound_ShouldReturnError` | No encontrado |
| 3 | `Handle_ShouldIncludePricing` | Incluir precios |
| 4 | `Handle_ShouldIncludeLimits` | Incluir límites |

#### ComparePlansQueryHandlerTests.cs (4 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Handle_TwoPlans_ShouldCompareFeatures` | Comparar features |
| 2 | `Handle_ShouldShowPriceDifference` | Diferencia de precio |
| 3 | `Handle_ShouldShowLimitDifferences` | Diferencias de límites |
| 4 | `Handle_InvalidPlanId_ShouldReturnError` | Plan inválido |

---

## 5. TESTS DE INTEGRACIÓN API

### 5.1 PlanEndpointsTests.cs (6 tests)

| # | Test | Endpoint | HTTP | Descripción |
|---|------|----------|------|-------------|
| 1 | `GetAllPlans_ShouldReturn200` | GET /plans | 200 | Listar planes |
| 2 | `GetPlan_ById_ShouldReturn200` | GET /plans/{id} | 200 | Por ID |
| 3 | `GetPlan_NotFound_ShouldReturn404` | GET /plans/{id} | 404 | No encontrado |
| 4 | `ComparePlans_ShouldReturn200` | GET /plans/compare | 200 | Comparar |
| 5 | `GetPlanFeatures_ShouldReturn200` | GET /plans/{id}/features | 200 | Features |
| 6 | `GetRecommendedPlan_ShouldReturn200` | GET /plans/recommended | 200 | Recomendado |

---

## 6. RESUMEN DE TESTS

| Categoría | Existentes | Nuevos | Total |
|-----------|------------|--------|-------|
| Domain | 21 | 17 | 38 |
| Handlers | 4 | 8 | 12 |
| API | 0 | 6 | 6 |
| **TOTAL** | **~25** | **~31** | **~56** |

---

## 7. ORDEN DE IMPLEMENTACIÓN

1. **Día 1:** Domain tests (Plan, PlanId, VOs)
2. **Día 2:** Query handlers, API tests

**Tiempo estimado:** 2 días
