# Plan de Refactorización — Módulos Billing (Subscriptions + Plans + PaymentMethods)

> Reglas obligatorias: **una clase por archivo**, **sin comentarios que no sean XML docs**, **sin sobreingeniería**.
> Plan unificado: los 3 módulos forman un **bounded context billing** y se planifican juntos.

---

## 1. Diagnóstico general

Tres módulos ligeros y razonablemente bien construidos:
- **Plans**: 27 archivos, principalmente lectura (queries simples GET). `Plan.cs` 172 líneas, value objects sólidos (`PlanName`, `PlanPricing`, `PlanLimits`). Seeder en infraestructura.
- **Subscriptions**: 39 archivos. `Subscription.cs` 285 líneas con state machine clara (Trial/Active/Cancelled/Expired). 5 eventos de dominio. Repositorio limpio (117 líneas).
- **PaymentMethods**: 30 archivos. `PaymentMethod.cs` 108 líneas. **Integración con Stripe vía port/adapter** (`IStripeService`/`StripeService` 242 líneas) — bien separado, con stub mode para desarrollo.

**Fortalezas:**
- Port/adapter de Stripe correctamente aplicado.
- Idempotencia básica en `AddPaymentMethod` (busca por `StripeId` antes de crear).
- Stub mode de `StripeService` para desarrollo sin Stripe real.
- `HasActiveSubscriptionAsync` previene duplicados de subscription activa.
- Tests cubren happy paths.

**Debilidades CRÍTICAS:**
- **No existe lógica de cobro/factura**: `Subscription` se crea, pero nadie cobra. ¿Lo hace Stripe automáticamente vía billing portal? ¿Webhook? **No hay handler de webhooks de Stripe** en el código explorado.
- **`ChangePlan` no es idempotente** y no calcula proración con Stripe.
- **No hay validación de `PaymentMethod` antes de crear subscription pagada**.
- **Race condition en `SetAsDefault`**: actualiza N records sin transaction wrap.
- **Inconsistencia UoW**: `SubscriptionRepository` usa `ISubscriptionUnitOfWork`, `PaymentMethodRepository` llama `SaveChangesAsync` directo.

**Veredicto:** Refactor de **alta prioridad por riesgo financiero**. El módulo está construido como esqueleto; necesita completar el flujo de cobro o documentar explícitamente que Stripe lo gestiona externamente.

---

## 2. Code Smells detectados

| # | Smell | Ubicación | Severidad |
|---|---|---|---|
| 1 | **Sin servicio de cobro/factura** (no hay `IInvoiceService`, `IChargeService`, ni handler de webhooks Stripe) | global billing | **Crítica** |
| 2 | `ChangePlan` no idempotente, raise duplicado de `SubscriptionPlanChangedEvent` en retry | `ChangePlanCommandHandler.cs` | **Crítica** |
| 3 | `SetAsDefault` race condition: actualiza N records sin transaction | `SetDefaultPaymentMethodCommandHandler.cs:52-59` | Alta |
| 4 | Inconsistencia patrón UoW (subscriptions usa UoW, paymentMethods llama SaveChanges directo) | `PaymentMethodRepository.cs` | Media |
| 5 | `CreateSetupIntent` con búsqueda por metadata sin lock — potencial creación duplicada de customer en retry | `CreateSetupIntentQueryHandler.cs` | Alta |
| 6 | Falta validación: crear Subscription pagada sin PaymentMethod activo | `CreateSubscriptionCommandHandler.cs` | Alta |
| 7 | `CreateSubscriptionCommandHandler.Handle` 60+ líneas (80-line guideline) | mismo | Baja |
| 8 | `PaymentMethodEndpoints.cs` 219 líneas — borderline | mismo | Baja |
| 9 | `SubscriptionEndpoints.cs` 170 líneas — sin separar admin / user | mismo | Baja |
| 10 | Comentarios inline no-doc | varios | Media (regla obligatoria) |
| 11 | Sin webhook signature validation (si existieran webhooks) | `Infrastructure/PaymentMethods/StripeService.cs` | Media (preventiva) |
| 12 | Stub mode con `cus_dev_` / `seti_dev_` puede llegar a prod si config falla — debería fallar loud | `StripeService.cs:48-52` | Media |
| 13 | Magic number `Subscription.DefaultTrialDays = 14` no documentado | `Subscription.cs:17` | Baja |
| 14 | DTOs/responses con campos opcionales no marcados — riesgo de exponer info de Stripe | revisar `PaymentMethodDto` | Media (seguridad) |
| 15 | `Plan.cs` 172 líneas con regiones — borderline OK | mismo | Baja |

---

## 3. Problemas arquitectónicos

1. **Bounded context incompleto**: el dominio Billing no cierra el ciclo Subscription → Charge → Invoice → Receipt. Hoy es: crear subscription, registrar payment method, y nada más. **Decisión arquitectónica pendiente y URGENTE**: ¿es Stripe quien cobra y nuestro backend solo refleja estado, o el backend orquesta?
2. **Sin protección contra doble cobro**: si la respuesta es "Stripe orquesta", entonces necesitamos handler de webhooks Stripe (`invoice.paid`, `invoice.payment_failed`, `customer.subscription.updated`) y persistencia idempotente de webhook events.
3. **Cross-aggregate sin transaction**: `Subscription`, `PaymentMethod`, `Plan` viven en DbContexts separados (`SubscriptionContext`, `PlanContext`). Crear una subscription que valida un plan + valida un payment method requiere lecturas cross-context sin garantía de consistencia.
4. **Stub mode en producción** es un footgun: si la configuración de Stripe falla al cargar, el sistema "funciona" con IDs falsos y nadie cobra. Debe ser fail-fast.
5. **Sin observabilidad financiera**: no hay logging estructurado de eventos críticos (subscription creada con qué plan, qué payment method, qué precio efectivo).

---

## 4. Plan de refactorización priorizado

### Fase 0 — Decisión arquitectónica obligatoria (PREVIO a refactor)
**Esto NO es código, es una decisión que debe tomarse y documentarse antes de seguir:**

> **¿Cómo se cobran las subscripciones?**
>
> A) **Stripe gestiona el cobro** vía Stripe Billing (subscription scheduling). Nuestro backend recibe webhooks (`invoice.paid`, `invoice.failed`, `customer.subscription.updated`) y refleja estado.
>
> B) **Nuestro backend orquesta el cobro**: job recurrente que crea PaymentIntent en Stripe usando el PaymentMethod por defecto del usuario.

Toda la Fase 2 depende de esta decisión. **Recomendación: opción A** (Stripe Billing) — menos código, menos responsabilidad financiera, mejor compliance.

### Fase 1 — Cambios seguros
| Tarea | Archivo(s) | Justificación |
|---|---|---|
| 1.1 Eliminar comentarios no-doc | todos los `.cs` de billing | Regla obligatoria |
| 1.2 Documentar `DefaultTrialDays = 14` con XML doc explicando origen de la decisión | `Subscription.cs:17` | Memoria de decisión |
| 1.3 **Fail-fast en Stripe stub mode**: si `StripeSettings.ApiKey` está vacío y `Environment != Development` → lanzar al startup, no fallback silencioso | `StripeService.cs`, `DependencyInjection.cs` | Seguridad financiera |
| 1.4 Particionar `SubscriptionEndpoints.cs` (170 líneas) en `SubscriptionEndpoints` (user) y `SubscriptionAdminEndpoints` (si los hay) | `API/Endpoints/Subscriptions/` | Cohesión |
| 1.5 Constantes para magic strings de Stripe (`stripe_customer_id`, metadata keys) | `Infrastructure/PaymentMethods/StripeMetadataKeys.cs` (NUEVO) | Mantenibilidad |
| 1.6 Logging estructurado con campos: `subscription_id`, `plan_id`, `effective_price`, `payment_method_id` en operaciones de subscription | handlers | Observabilidad financiera |

### Fase 2 — Mejoras de diseño (depende de Fase 0)

#### Si Fase 0 = Opción A (Stripe Billing gestiona cobro):
| Tarea | Archivo(s) | Justificación |
|---|---|---|
| 2.A.1 **Implementar handler de webhooks Stripe**: `POST /api/webhooks/stripe` con verificación de signature, idempotency-key (event.id), persistencia en tabla `stripe_webhook_events` | `API/Endpoints/Webhooks/StripeWebhookEndpoints.cs` (NUEVO), `Infrastructure/PaymentMethods/StripeWebhookHandler.cs` (NUEVO) | **Crítico para opción A** |
| 2.A.2 Mapear eventos Stripe → commands de dominio:<br>- `invoice.paid` → `RecordSubscriptionPaymentCommand`<br>- `invoice.payment_failed` → `MarkSubscriptionPaymentFailedCommand`<br>- `customer.subscription.deleted` → `CancelSubscriptionCommand` (auto)<br>- `customer.subscription.updated` → `SyncSubscriptionStateCommand` | `Application/Subscriptions/Commands/` (NUEVOS) | Refleja realidad de Stripe en dominio |
| 2.A.3 `CreateSubscriptionCommandHandler` orquesta: crear `Subscription` en BD + crear `Subscription` en Stripe (vía `IStripeBillingService.CreateSubscription(planStripeId, customerId, paymentMethodId)`) en una transacción local + outbox | `CreateSubscriptionCommandHandler.cs`, `IStripeBillingService` (NUEVO) | Atomicidad |

#### Si Fase 0 = Opción B (backend orquesta):
| Tarea | Archivo(s) | Justificación |
|---|---|---|
| 2.B.1 Job recurrente `BillSubscriptionsJob` que escanea subscripciones próximas a renovar y dispara `ChargeSubscriptionCommand` | `Infrastructure/Jobs/BillSubscriptionsJob.cs` (NUEVO) | Cobro automático |
| 2.B.2 `ChargeSubscriptionCommand` con idempotency-key (`subscription_id` + `period`) | `Application/Subscriptions/Commands/Charge/` | Sin doble cobro |
| 2.B.3 `IPaymentProcessor` (Application) + `StripePaymentProcessor` (Infrastructure) que crea PaymentIntent off-session | nuevos | Port/adapter limpio |

#### Comunes a ambas opciones:
| Tarea | Archivo(s) | Justificación |
|---|---|---|
| 2.C.1 **Idempotency-key obligatoria** en `ChangePlanCommand` y `CancelSubscriptionCommand` (header `X-Idempotency-Key`) | endpoints + handlers | Prevenir doble ejecución |
| 2.C.2 Validar `PaymentMethod` activo antes de crear subscription pagada (free plans bypass) | `CreateSubscriptionCommandHandler.cs` | Cierra hueco lógico |
| 2.C.3 **Transaction wrap en `SetAsDefault`** (postgres advisory lock por `user_id` o `SERIALIZABLE` isolation) | `PaymentMethodRepository.cs`, handler | Race condition |
| 2.C.4 Unificar UoW: `IPaymentMethodUnitOfWork` con `SaveChangesAsync` — eliminar `SaveChangesAsync` directo en repo | `Infrastructure/PaymentMethods/...` | Coherencia |
| 2.C.5 `CreateSetupIntentQueryHandler` con lock distribuido (Redis lock por `user_id`) para evitar customer duplicado | `CreateSetupIntentQueryHandler.cs` | Idempotencia |
| 2.C.6 Verificación de signature de webhook de Stripe (`Stripe.EventUtility.ConstructEvent` con webhook secret) | si Fase 2.A.1 | Seguridad anti-spoofing |

### Fase 3 — Refactor arquitectónico
| Tarea | Recomendación |
|---|---|
| 3.1 Unificar `SubscriptionContext`, `PlanContext`, `PaymentMethodContext` en un único `BillingContext` | **SÍ** — son un bounded context, los DbContexts separados son fricción innecesaria |
| 3.2 Outbox compartido para eventos de billing (mismo del módulo Pipelines/Traces) | **SÍ** — eventos de billing son críticos |
| 3.3 Read model materializado de `BillingDashboard` (subscription + plan + payment method en una vista) | **Solo si UI lo demanda** |
| 3.4 Migrar a Stripe Customer Portal completo (delegar UI de cobro a Stripe) | **Decisión de producto, no técnica** |

---

## 5. Patrones aplicables

| Patrón | Dónde | Por qué SÍ |
|---|---|---|
| **Webhook Idempotent Consumer** | `StripeWebhookEndpoints` | Stripe garantiza al menos una entrega |
| **Port/Adapter** (ya existe) | `IStripeService` / `IStripeBillingService` | Mantener |
| **Outbox** | Eventos de billing | Crítico para reflejar Stripe ↔ BD |
| **Idempotency Key** | ChangePlan, Cancel, Charge | Operaciones financieras irreversibles |
| **Distributed Lock** | CreateSetupIntent, SetAsDefault | Race conditions reales |
| **Unit of Work** | Unificar entre repos | Coherencia |

**NO aplicar:**
- Event Sourcing — Stripe es la fuente de verdad financiera, no nuestra BD.
- Saga compleja — flujo de billing es relativamente lineal con Stripe.
- CQRS read DB separada.
- Microservicio Billing — no hay justificación de escala.
- Reemplazar Stripe por procesador propio — fuera de scope.

---

## 6. Estrategia de testing

**Antes:**
- Tests existentes cubren happy paths de Subscription, PaymentMethod, Plan.
- Faltan tests para:
  - `ChangePlan` con retry (debe ser idempotente).
  - `SetAsDefault` concurrente (2 threads).
  - `CreateSetupIntent` con error de Stripe a mitad.
  - `AddPaymentMethod` con `StripeException`.
  - Validación de subscription sin PaymentMethod (rechazo).

**Durante Fase 2.A.1 (webhooks):**
- Tests de webhook con eventos sintéticos de Stripe (Stripe CLI / fixtures).
- Test de idempotencia: mismo event.id 3 veces → 1 procesamiento.
- Test de signature inválida → rechazo 400.

**Durante Fase 2.C.2 (validación payment method):**
- Test: crear subscription paid sin payment → error tipado.
- Test: crear subscription free sin payment → OK.

**Después:**
- Mutation testing en `Domain/Subscriptions` y commands de billing.
- Test de carga: 100 webhooks paralelos del mismo evento → 1 procesamiento.
- Test E2E con Stripe CLI en CI: create subscription → trigger invoice.paid → verify state.

---

## 7. Orden de ejecución

```
Fase 0:
  Decisión arquitectónica (Stripe Billing vs orquestación propia) ← BLOQUEANTE

Fase 1 (independiente de Fase 0):
  1.1 (comentarios)
  1.3 (fail-fast Stripe stub) ← URGENTE seguridad financiera
  1.5 (constantes Stripe metadata)
  1.6 (logging estructurado)
  1.2 (XML doc trial days)
  1.4 (split SubscriptionEndpoints)

Fase 2 (orden por riesgo):
  2.C.3 (transaction SetAsDefault) ← race condition real
  2.C.5 (lock CreateSetupIntent) ← idempotencia customer
  2.C.4 (unificar UoW)
  2.C.1 (idempotency keys ChangePlan/Cancel)
  2.C.2 (validar PaymentMethod en CreateSubscription)
  2.A.1, 2.A.2, 2.A.3 (si Opción A) ← cierra el ciclo
  2.A.6 (verificación signature webhook)

Fase 3:
  3.1 (BillingContext unificado) ← tras Fase 2 estable
  3.2 (Outbox)
```

---

## 8. Cambios NO recomendados

- Implementar procesador de pagos propio.
- Almacenar números de tarjeta o CVV (PCI scope).
- Mover `Plan` a archivo de configuración JSON (es entidad de dominio con validación).
- Multi-tenancy de planes — no hay requisito.
- Suscripciones grupales / múltiples por usuario — fuera de scope sin requisito.
- Cambiar de Stripe a otro proveedor sin razón de producto.
- Microservicio Billing.
- Event Sourcing.
- AutoMapper para DTOs de billing.
- Currency handling complejo si solo se trabaja una moneda — añadir `Money` VO solo cuando exista soporte multi-currency.

---

## 9. Métricas de calidad sugeridas

| Métrica | Hoy | Objetivo |
|---|---|---|
| Líneas por archivo (max) | `StripeService` 242 | < 300 ✓ |
| Líneas por método (max) | ~60 | < 50 |
| Cobertura Domain billing | ? | ≥ 90% |
| Cobertura Application billing | ? | ≥ 85% |
| Comentarios no-doc | varios | 0 |
| Operaciones financieras con idempotency-key | 0/3 (Create/Change/Cancel) | 3/3 |
| Race conditions detectadas en SetAsDefault | 1 | 0 |
| Webhooks Stripe procesados | 0 | n (post Fase 2.A.1) |
| Eventos webhook duplicados procesados | n/a | 0 |
| Stripe stub activo en producción | posible | imposible (post Fase 1.3) |
| Subscriptions paid creadas sin PaymentMethod | posible | 0 (post Fase 2.C.2) |

---

## 10. Resultado final esperado

```
Domain/Subscriptions/                  (sin cambios estructurales)
├── Subscription.cs                    (~285 líneas, sin cambios)
├── ValueObjects/
└── Events/

Domain/Plans/                          (sin cambios)

Application/Subscriptions/
├── Commands/
│   ├── Create/
│   ├── ChangePlan/                    (con idempotency)
│   ├── Cancel/                        (con idempotency)
│   └── (Opción A) Webhooks/
│       ├── RecordSubscriptionPaymentCommand.cs
│       ├── MarkSubscriptionPaymentFailedCommand.cs
│       └── SyncSubscriptionStateCommand.cs
└── Abstractions/
    ├── IStripeBillingService.cs       (NUEVO Opción A)
    └── IPaymentProcessor.cs           (NUEVO Opción B)

Application/PaymentMethods/            (sin cambios estructurales)

Infrastructure/PaymentMethods/
├── StripeService.cs                   (sin stub silencioso, fail-fast)
├── StripeBillingService.cs            (NUEVO Opción A)
├── StripeMetadataKeys.cs              (NUEVO constantes)
└── Webhooks/                          (NUEVO Opción A)
    ├── StripeWebhookHandler.cs
    └── StripeEventMapper.cs

Infrastructure/Subscriptions/
└── BillingContext.cs                  (Fase 3.1 — unifica Subscription + Plan + PaymentMethod)

Infrastructure/Idempotency/            (compartido)
└── PostgresIdempotencyStore.cs

Infrastructure/Locking/                (NUEVO compartido)
└── RedisDistributedLock.cs

API/Endpoints/Subscriptions/
├── SubscriptionEndpoints.cs           (~120 líneas — user)
└── SubscriptionAdminEndpoints.cs      (NUEVO si hay admin)

API/Endpoints/Webhooks/                (NUEVO Opción A)
└── StripeWebhookEndpoints.cs

docs/
└── billing-flow.md                    (NUEVO — documenta decisión Fase 0)
```

**Beneficios concretos:**
- Cierre del ciclo de cobro (sea opción A o B).
- 0 race conditions en SetAsDefault y CreateSetupIntent.
- 0 stub silencioso de Stripe en producción.
- 100% operaciones financieras con idempotency-key.
- Webhooks idempotentes con signature verification.
- Logging financiero estructurado.
- Bounded context Billing unificado en un DbContext.

**Lo que NO cambia:**
- Modelo de dominio `Subscription`, `Plan`, `PaymentMethod` (estructura).
- Stripe como procesador.
- Patrón port/adapter `IStripeService` (se enriquece, no se reemplaza).
- Stub mode en development (con fail-fast en otros entornos).
- Smart enum `SubscriptionStatus`.
- Contratos REST públicos.

**Riesgos a mitigar antes de tocar código:**
1. Confirmar Fase 0 con producto/legal — define la mitad del refactor.
2. Inventariar webhooks que Stripe ya envía a este backend (si los hay) — pueden estar en otro endpoint sin descubrir.
3. Verificar `StripeSettings.WebhookSecret` configurado en todos los entornos antes de Fase 2.A.6.
