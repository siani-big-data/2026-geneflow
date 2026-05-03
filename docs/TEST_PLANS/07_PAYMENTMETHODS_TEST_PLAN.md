# Plan de Tests: Módulo PaymentMethods

## Cobertura Actual: 0%
## Tests Estimados: ~57 tests

### Objetivos de Cobertura por Capa

| Capa | Archivos | Line | Branch | Method |
|------|----------|------|--------|--------|
| **Domain** | PaymentMethod.cs, PaymentMethodId.cs | **≥95%** | **≥90%** | **≥98%** |
| **Application** | Commands/*, Queries/* | **≥90%** | **≥85%** | **≥95%** |
| **Infrastructure** | Repositories/*, StripeService | **≥75%** | **≥70%** | **≥85%** |
| **API** | Endpoints/* | **≥85%** | **≥80%** | **≥90%** |

### Objetivos Específicos por Archivo

| Archivo | Line | Branch | Prioridad |
|---------|------|--------|-----------|
| `PaymentMethod.cs` (~100 LOC) | 95% | 92% | P0 |
| `PaymentMethodId.cs` (~40 LOC) | 100% | 100% | P1 |
| Command Handlers | 90% | 85% | P0 |
| Query Handlers | 85% | 80% | P1 |
| `PaymentMethodEndpoints.cs` | 85% | 80% | P0 |

---

## 1. ANÁLISIS DEL MÓDULO

### 1.1 Archivos de Dominio (~200 LOC)

| Archivo | LOC | Tests Existentes | Faltantes |
|---------|-----|------------------|-----------|
| `PaymentMethod.cs` | ~100 | 0 | 12 |
| `PaymentMethodId.cs` | ~40 | 0 | 5 |
| `PaymentMethodErrors.cs` | ~30 | 0 | 0 |
| `IPaymentMethodRepository.cs` | ~30 | 0 | 0 |

### 1.2 Archivos de Aplicación (~200 LOC)

**Commands SIN Tests:**
| Handler | LOC | Prioridad |
|---------|-----|-----------|
| `AddPaymentMethodCommandHandler` | ~60 | P0 |
| `RemovePaymentMethodCommandHandler` | ~40 | P0 |
| `SetDefaultPaymentMethodCommandHandler` | ~50 | P0 |
| `UpdatePaymentMethodCommandHandler` | ~40 | P1 |

**Queries SIN Tests:**
| Handler | LOC | Prioridad |
|---------|-----|-----------|
| `GetUserPaymentMethodsQueryHandler` | ~40 | P0 |
| `GetDefaultPaymentMethodQueryHandler` | ~30 | P1 |

---

## 2. TESTS UNITARIOS DE DOMINIO

### 2.1 PaymentMethodTests.cs (12 tests)

```
Tests/Domain/PaymentMethods/PaymentMethodTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_WithValidData_ShouldCreate` | Crear método de pago |
| 2 | `Create_ShouldSetUserId` | Usuario propietario |
| 3 | `Create_Card_ShouldMaskCardNumber` | Enmascarar número |
| 4 | `Create_Card_ShouldSetLast4Digits` | Últimos 4 dígitos |
| 5 | `Create_Card_ShouldSetExpirationDate` | Fecha expiración |
| 6 | `Create_ShouldSetIsDefaultFalse` | No default por defecto |
| 7 | `SetAsDefault_ShouldSetIsDefaultTrue` | Establecer como default |
| 8 | `RemoveDefault_ShouldSetIsDefaultFalse` | Quitar default |
| 9 | `IsExpired_ExpiredCard_ShouldReturnTrue` | Tarjeta expirada |
| 10 | `IsExpired_ValidCard_ShouldReturnFalse` | Tarjeta válida |
| 11 | `GetDisplayName_ShouldFormat` | Nombre display |
| 12 | `Deactivate_ShouldSetIsActiveFalse` | Desactivar |

### 2.2 PaymentMethodIdTests.cs (5 tests)

```
Tests/Domain/PaymentMethods/PaymentMethodIdTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_WithValidValue_ShouldCreate` | Crear ID |
| 2 | `Equality_SameValue_ShouldBeEqual` | Igualdad |
| 3 | `Equality_DifferentValue_ShouldNotBeEqual` | Desigualdad |
| 4 | `GetHashCode_SameValue_ShouldBeSame` | Hash code |
| 5 | `ToString_ShouldReturnPrefixedId` | Formato string |

---

## 3. TESTS DE HANDLERS

### 3.1 Command Handler Tests

#### AddPaymentMethodCommandHandlerTests.cs (8 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Handle_ValidCard_ShouldAddPaymentMethod` | Agregar tarjeta |
| 2 | `Handle_InvalidCardNumber_ShouldReturnError` | Número inválido |
| 3 | `Handle_ExpiredCard_ShouldReturnError` | Tarjeta expirada |
| 4 | `Handle_FirstMethod_ShouldSetAsDefault` | Primera es default |
| 5 | `Handle_ShouldValidateWithStripe` | Validar con Stripe |
| 6 | `Handle_ShouldCreateStripePaymentMethod` | Crear en Stripe |
| 7 | `Handle_ShouldPersistMethod` | Persistir |
| 8 | `Handle_MaxMethodsReached_ShouldReturnError` | Máximo alcanzado |

#### RemovePaymentMethodCommandHandlerTests.cs (6 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Handle_ExistingMethod_ShouldRemove` | Remover existente |
| 2 | `Handle_NotFound_ShouldReturnError` | No encontrado |
| 3 | `Handle_DefaultMethod_ShouldSelectNewDefault` | Seleccionar nuevo default |
| 4 | `Handle_OnlyMethod_WithActiveSubscription_ShouldReturnError` | Único método con sub activa |
| 5 | `Handle_ShouldRemoveFromStripe` | Remover de Stripe |
| 6 | `Handle_NotOwner_ShouldReturnError` | No es propietario |

#### SetDefaultPaymentMethodCommandHandlerTests.cs (5 tests)
#### UpdatePaymentMethodCommandHandlerTests.cs (4 tests)

### 3.2 Query Handler Tests

#### GetUserPaymentMethodsQueryHandlerTests.cs (4 tests)
#### GetDefaultPaymentMethodQueryHandlerTests.cs (3 tests)

---

## 4. TESTS DE INTEGRACIÓN API

### 4.1 PaymentMethodEndpointsTests.cs (10 tests)

| # | Test | Endpoint | HTTP | Descripción |
|---|------|----------|------|-------------|
| 1 | `GetPaymentMethods_ShouldReturn200` | GET /payment-methods | 200 | Listar |
| 2 | `GetDefaultPaymentMethod_ShouldReturn200` | GET /payment-methods/default | 200 | Obtener default |
| 3 | `AddPaymentMethod_ValidCard_ShouldReturn201` | POST /payment-methods | 201 | Agregar |
| 4 | `AddPaymentMethod_InvalidCard_ShouldReturn400` | POST /payment-methods | 400 | Inválido |
| 5 | `RemovePaymentMethod_ShouldReturn204` | DELETE /payment-methods/{id} | 204 | Remover |
| 6 | `RemovePaymentMethod_OnlyWithSub_ShouldReturn400` | DELETE /payment-methods/{id} | 400 | Único con sub |
| 7 | `SetDefaultPaymentMethod_ShouldReturn200` | POST /payment-methods/{id}/default | 200 | Establecer default |
| 8 | `UpdatePaymentMethod_ShouldReturn200` | PUT /payment-methods/{id} | 200 | Actualizar |
| 9 | `GetPaymentMethod_ById_ShouldReturn200` | GET /payment-methods/{id} | 200 | Por ID |
| 10 | `GetPaymentMethod_NotOwner_ShouldReturn403` | GET /payment-methods/{id} | 403 | No propietario |

---

## 5. RESUMEN DE TESTS

| Categoría | Existentes | Nuevos | Total |
|-----------|------------|--------|-------|
| Domain | 0 | 17 | 17 |
| Handlers | 0 | 30 | 30 |
| API | 0 | 10 | 10 |
| **TOTAL** | **0** | **~57** | **~57** |

---

## 6. ORDEN DE IMPLEMENTACIÓN

1. **Día 1:** Domain tests (PaymentMethod, PaymentMethodId)
2. **Día 2:** Command handlers
3. **Día 3:** Query handlers, API tests

**Tiempo estimado:** 3 días
