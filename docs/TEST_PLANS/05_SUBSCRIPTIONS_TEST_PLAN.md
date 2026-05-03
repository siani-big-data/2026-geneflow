# Plan de Tests: Módulo Subscriptions

## Cobertura Actual: ~40%
## Tests Estimados: ~114 tests

### Objetivos de Cobertura por Capa

| Capa | Archivos | Line | Branch | Method |
|------|----------|------|--------|--------|
| **Domain** | Subscription.cs, ValueObjects/*, Enumerations/* | **≥95%** | **≥90%** | **≥98%** |
| **Application** | Commands/*, Queries/* | **≥90%** | **≥85%** | **≥95%** |
| **Infrastructure** | Repositories/*, StripeService | **≥75%** | **≥70%** | **≥85%** |
| **API** | Endpoints/* | **≥85%** | **≥80%** | **≥90%** |

### Objetivos Específicos por Archivo

| Archivo | Line | Branch | Prioridad |
|---------|------|--------|-----------|
| `Subscription.cs` (~200 LOC) | 95% | 92% | P0 |
| `SubscriptionPeriod.cs` (~60 LOC) | 100% | 100% | P0 |
| `SubscriptionStatus.cs` (~30 LOC) | 100% | 100% | P0 |
| `BillingCycle.cs` (~25 LOC) | 100% | 100% | P1 |
| Command Handlers | 90% | 85% | P0 |
| Query Handlers | 85% | 80% | P1 |
| `SubscriptionEndpoints.cs` | 85% | 80% | P0 |

---

## 1. ANÁLISIS DEL MÓDULO

### 1.1 Archivos de Dominio (~400 LOC)

| Archivo | LOC | Tests Existentes | Faltantes |
|---------|-----|------------------|-----------|
| `Subscription.cs` | ~200 | ~8 | ~15 |
| `SubscriptionId.cs` | ~40 | 0 | 5 |
| `SubscriptionErrors.cs` | ~50 | 0 | 0 |
| `ValueObjects/SubscriptionPeriod.cs` | ~60 | ~5 | 3 |
| `Enumerations/SubscriptionStatus.cs` | ~30 | ~4 | 4 |
| `Enumerations/BillingCycle.cs` | ~25 | 0 | 4 |
| Events (5) | ~50 | 0 | 0 |

### 1.2 Archivos de Aplicación (~300 LOC)

**Commands con Tests:**
- CreateSubscriptionCommandHandler ✓
- CancelSubscriptionCommandHandler ✓
- ChangePlanCommandHandler ✓

**Commands SIN Tests:**
| Handler | LOC | Prioridad |
|---------|-----|-----------|
| `RenewSubscriptionCommandHandler` | ~50 | P0 |
| `PauseSubscriptionCommandHandler` | ~40 | P1 |
| `ResumeSubscriptionCommandHandler` | ~40 | P1 |
| `UpdatePaymentMethodCommandHandler` | ~40 | P1 |

**Queries con Tests:**
- GetCurrentSubscriptionQueryHandler ✓

**Queries SIN Tests:**
| Handler | LOC | Prioridad |
|---------|-----|-----------|
| `GetSubscriptionHistoryQueryHandler` | ~40 | P2 |
| `GetUpcomingInvoiceQueryHandler` | ~50 | P1 |

---

## 2. TESTS EXISTENTES

### Domain Tests
- `SubscriptionTests.cs` - ~8 tests
- `SubscriptionPeriodTests.cs` - ~5 tests
- `SubscriptionStatusTests.cs` - ~4 tests

### Handler Tests
- `CreateSubscriptionCommandHandlerTests.cs` - ~5 tests
- `CancelSubscriptionCommandHandlerTests.cs` - ~4 tests
- `ChangePlanCommandHandlerTests.cs` - ~5 tests
- `GetCurrentSubscriptionQueryHandlerTests.cs` - ~4 tests

**Total Existentes: ~35 tests**

---

## 3. TESTS UNITARIOS DE DOMINIO FALTANTES

### 3.1 SubscriptionTests.cs - Tests Adicionales (15 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Renew_Active_ShouldExtendPeriod` | Renovar suscripción |
| 2 | `Renew_Expired_ShouldReactivate` | Reactivar expirada |
| 3 | `Renew_Cancelled_ShouldReturnError` | No renovar cancelada |
| 4 | `Pause_Active_ShouldPause` | Pausar suscripción |
| 5 | `Pause_AlreadyPaused_ShouldReturnError` | Ya pausada |
| 6 | `Resume_Paused_ShouldResume` | Reanudar |
| 7 | `Resume_NotPaused_ShouldReturnError` | No pausada |
| 8 | `ExpiREDACTED` | Expirar |
| 9 | `ExpiREDACTED` | Evento expiración |
| 10 | `IsActive_WhenActive_ShouldReturnTrue` | Verificar activa |
| 11 | `IsExpiringSoon_Within7Days_ShouldReturnTrue` | Próxima a expirar |
| 12 | `GetRemainingDays_ShouldCalculate` | Días restantes |
| 13 | `CanUpgrade_ToPremiumPlan_ShouldReturnTrue` | Puede actualizar |
| 14 | `CanDowngrade_ToFreePlan_ShouldReturnTrue` | Puede degradar |
| 15 | `UpdatePaymentMethod_ShouldUpdate` | Actualizar método pago |

### 3.2 SubscriptionIdTests.cs (5 tests)

### 3.3 SubscriptionPeriodTests.cs - Tests Adicionales (3 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Extend_ShouldExtendEndDate` | Extender período |
| 2 | `GetDaysRemaining_ShouldCalculate` | Calcular días |
| 3 | `HasExpired_AfterEndDate_ShouldReturnTrue` | Verificar expiración |

### 3.4 SubscriptionStatusTests.cs - Tests Adicionales (4 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `CanTransitionTo_ActiveToPaused_ShouldReturnTrue` | Transición válida |
| 2 | `CanTransitionTo_PausedToActive_ShouldReturnTrue` | Transición válida |
| 3 | `CanTransitionTo_CancelledToActive_ShouldReturnFalse` | Transición inválida |
| 4 | `IsTerminal_WhenCancelled_ShouldReturnTrue` | Estado terminal |

### 3.5 BillingCycleTests.cs (4 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `All_ShouldHaveUniqueIds` | IDs únicos |
| 2 | `GetDays_Monthly_ShouldReturn30` | Días mensual |
| 3 | `GetDays_Yearly_ShouldReturn365` | Días anual |
| 4 | `GetDiscountPercentage_Yearly_ShouldReturn20` | Descuento anual |

---

## 4. TESTS DE HANDLERS FALTANTES

### 4.1 Command Handler Tests

#### RenewSubscriptionCommandHandlerTests.cs (6 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Handle_ValidData_ShouldRenewSubscription` | Renovar exitosamente |
| 2 | `Handle_SubscriptionNotFound_ShouldReturnError` | No encontrada |
| 3 | `Handle_AlreadyCancelled_ShouldReturnError` | Cancelada |
| 4 | `Handle_PaymentFailed_ShouldReturnError` | Pago fallido |
| 5 | `Handle_ShouldExtendPeriod` | Extender período |
| 6 | `Handle_ShouldChargePayment` | Cobrar pago |

#### PauseSubscriptionCommandHandlerTests.cs (5 tests)
#### ResumeSubscriptionCommandHandlerTests.cs (5 tests)
#### UpdatePaymentMethodCommandHandlerTests.cs (5 tests)

### 4.2 Query Handler Tests

#### GetSubscriptionHistoryQueryHandlerTests.cs (4 tests)
#### GetUpcomingInvoiceQueryHandlerTests.cs (5 tests)

---

## 5. TESTS DE INTEGRACIÓN API

### 5.1 SubscriptionEndpointsTests.cs (12 tests)

| # | Test | Endpoint | HTTP | Descripción |
|---|------|----------|------|-------------|
| 1 | `GetCurrentSubscription_ShouldReturn200` | GET /subscriptions/current | 200 | Obtener actual |
| 2 | `GetSubscriptionHistory_ShouldReturn200` | GET /subscriptions/history | 200 | Historial |
| 3 | `CreateSubscription_ValidPlan_ShouldReturn201` | POST /subscriptions | 201 | Crear |
| 4 | `CreateSubscription_InvalidPlan_ShouldReturn400` | POST /subscriptions | 400 | Plan inválido |
| 5 | `ChangePlan_UpgradePlan_ShouldReturn200` | PUT /subscriptions/plan | 200 | Actualizar plan |
| 6 | `ChangePlan_DowngradePlan_ShouldReturn200` | PUT /subscriptions/plan | 200 | Degradar plan |
| 7 | `CancelSubscription_ShouldReturn200` | POST /subscriptions/cancel | 200 | Cancelar |
| 8 | `PauseSubscription_ShouldReturn200` | POST /subscriptions/pause | 200 | Pausar |
| 9 | `ResumeSubscription_ShouldReturn200` | POST /subscriptions/resume | 200 | Reanudar |
| 10 | `GetUpcomingInvoice_ShouldReturn200` | GET /subscriptions/invoice/upcoming | 200 | Próxima factura |
| 11 | `UpdatePaymentMethod_ShouldReturn200` | PUT /subscriptions/payment-method | 200 | Actualizar pago |
| 12 | `RenewSubscription_ShouldReturn200` | POST /subscriptions/renew | 200 | Renovar |

---

## 6. TESTS E2E

### 6.1 SubscriptionFlowE2ETests.cs (6 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `CreateSubscription_UseFeatures_ShouldSucceed` | Flujo completo |
| 2 | `UpgradePlan_AccessPremiumFeatures` | Upgrade y features |
| 3 | `CancelSubscription_LoseAccess_AtEndOfPeriod` | Cancelar y perder acceso |
| 4 | `PauseResume_MaintainsSubscription` | Pausar y reanudar |
| 5 | `ExpiredSubscription_AutoDowngrade` | Expiración automática |
| 6 | `PaymentFailuREDACTED` | Fallo y recuperación |

---

## 7. RESUMEN DE TESTS

| Categoría | Existentes | Nuevos | Total |
|-----------|------------|--------|-------|
| Domain | 17 | 31 | 48 |
| Handlers | 18 | 30 | 48 |
| API | 0 | 12 | 12 |
| E2E | 0 | 6 | 6 |
| **TOTAL** | **~35** | **~79** | **~114** |

---

## 8. ORDEN DE IMPLEMENTACIÓN

1. **Día 1:** Domain tests adicionales
2. **Día 2:** Command handlers
3. **Día 3:** Query handlers, API tests
4. **Día 4:** E2E tests

**Tiempo estimado:** 4 días
