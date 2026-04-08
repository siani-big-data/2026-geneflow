# Módulo de Planes y Suscripciones

## Descripción General

El sistema de **Plans & Subscriptions** gestiona los niveles de servicio y el ciclo de vida de las suscripciones de los usuarios de GeneFlow. Son dos bounded contexts relacionados pero separados:

- **Plans**: Define los productos/niveles de servicio disponibles
- **Subscriptions**: Gestiona la relación usuario-plan con su ciclo de vida

---

## Arquitectura

### Relación entre Contexts

```
┌─────────────────┐         ┌─────────────────┐         ┌─────────────────┐
│    Identity     │         │  Subscriptions  │         │     Plans       │
│     (User)      │         │   (Suscripción) │         │   (Producto)    │
├─────────────────┤         ├─────────────────┤         ├─────────────────┤
│ - UserId        │◄───────►│ - UserId        │         │ - PlanId        │
│ - Credentials   │         │ - PlanId        │◄───────►│ - Name          │
│ - OAuth         │         │ - Status        │         │ - Pricing       │
└─────────────────┘         │ - BillingCycle  │         │ - Limits        │
                            │ - Period        │         │ - Features      │
                            └─────────────────┘         └─────────────────┘
```

- Un **User** tiene una **Subscription** activa
- Una **Subscription** referencia un **Plan**
- Los **Plans** son independientes y reutilizables

---

## Modelo de Dominio - Plans

### Aggregate Root: Plan

```csharp
public sealed class Plan : AuditableAggregateRoot<PlanId>
{
    public PlanId Id { get; }
    public PlanName Name { get; }
    public string? Description { get; }
    public PlanPricing Pricing { get; }
    public PlanLimits Limits { get; }
    public bool IsActive { get; }
    public bool IsDefault { get; }
    public int DisplayOrder { get; }
    public IReadOnlyList<PlanFeature> Features { get; }

    // Computed
    public bool IsFree { get; }
}
```

### Value Objects - Plan

| Value Object | Campos | Validaciones |
|--------------|--------|--------------|
| `PlanName` | Value | Requerido, 2-50 caracteres |
| `PlanPricing` | MonthlyPrice, AnnualPrice, Currency | Precios >= 0, Currency: ISO 4217 (3 letras) |
| `PlanLimits` | MaxStudies, MaxTracesPerMonth, MaxMembersPerStudy | Valores > 0 o -1 (ilimitado) |

### Smart Enumeration: PlanFeature

```csharp
public sealed class PlanFeature : Enumeration<PlanFeature>
{
    public static readonly PlanFeature CopilotAccess;        // AI Copilot
    public static readonly PlanFeature PriorityProcessing;   // Procesamiento prioritario
    public static readonly PlanFeature AdvancedAnalytics;    // Analytics avanzados
    public static readonly PlanFeature ApiAccess;            // Acceso API
    public static readonly PlanFeature ExportFeatures;       // Exportación (PDF, CSV)
    public static readonly PlanFeature TeamCollaboration;    // Colaboración en equipo
    public static readonly PlanFeature CustomWorkflows;      // Flujos personalizados
    public static readonly PlanFeature SsoIntegration;       // SSO/SAML
    public static readonly PlanFeature DedicatedSupport;     // Soporte dedicado
}
```

---

## Modelo de Dominio - Subscriptions

### Aggregate Root: Subscription

```csharp
public sealed class Subscription : AggregateRoot<SubscriptionId>
{
    public SubscriptionId Id { get; }
    public UserId UserId { get; }
    public PlanId PlanId { get; }
    public string PlanName { get; }               // Cached para display
    public SubscriptionStatus Status { get; }
    public BillingCycle BillingCycle { get; }
    public SubscriptionPeriod CurrentPeriod { get; }
    public bool AutoRenew { get; }
    public DateTime CreatedAt { get; }
    public DateTime? ModifiedAt { get; }
    public DateTime? CancelledAt { get; }
    public string? CancellationReason { get; }
    public DateTime? TrialEndDate { get; }

    // Computed
    public bool GrantsAccess { get; }
    public bool IsFree { get; }
    public bool IsInTrial { get; }
}
```

### Value Object: SubscriptionPeriod

```csharp
public sealed class SubscriptionPeriod : ValueObject
{
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    // Computed
    public int DaysRemaining { get; }
    public bool HasExpired { get; }
    public bool IsActive { get; }
}
```

### Smart Enumerations

**SubscriptionStatus:**

| Estado | ID | Otorga Acceso | Puede Cancelar | Puede Renovar |
|--------|----:|:-------------:|:--------------:|:-------------:|
| Active | 1 | ✓ | ✓ | ✗ |
| Trial | 2 | ✓ | ✓ | ✗ |
| PastDue | 3 | ✓ | ✓ | ✗ |
| Cancelled | 4 | ✓* | ✗ | ✓ |
| Expired | 5 | ✗ | ✗ | ✓ |
| Suspended | 6 | ✗ | ✗ | ✓ |

*Cancelled mantiene acceso hasta fin del período actual

**BillingCycle:**

| Ciclo | ID | Meses |
|-------|---:|------:|
| Monthly | 1 | 1 |
| Annual | 2 | 12 |

---

## Máquina de Estados - Subscription

```
                    ┌─────────────┐
                    │    Trial    │
                    │  (14 días)  │
                    └──────┬──────┘
                           │ activateFromTrial()
                           ▼
┌─────────────┐     ┌─────────────┐
│   Expired   │◄────│   Active    │
└─────────────┘     └──────┬──────┘
       ▲                   │
       │                   │ cancel()
       │                   ▼
       │            ┌─────────────┐
       │            │  Cancelled  │──► (acceso hasta fin período)
       │            └──────┬──────┘
       │                   │ período termina
       └───────────────────┘

┌─────────────┐     ┌─────────────┐
│   PastDue   │◄────│   Active    │ (pago fallido)
└──────┬──────┘     └─────────────┘
       │
       │ suspend()
       ▼
┌─────────────┐
│  Suspended  │
└─────────────┘
```

---

## Base de Datos

### Schema: `plans`

```sql
CREATE TABLE plans.plans (
    id                      UUID PRIMARY KEY,
    name                    VARCHAR(50) NOT NULL UNIQUE,
    description             VARCHAR(500),
    monthly_price           DECIMAL(18,2) NOT NULL,
    annual_price            DECIMAL(18,2) NOT NULL,
    currency                CHAR(3) NOT NULL DEFAULT 'EUR',
    max_studies             INT NOT NULL,
    max_traces_per_month    INT NOT NULL,
    max_members_per_study   INT NOT NULL,
    features                TEXT,  -- JSON array of feature names
    is_active               BOOLEAN NOT NULL DEFAULT TRUE,
    is_default              BOOLEAN NOT NULL DEFAULT FALSE,
    display_order           INT NOT NULL DEFAULT 0,
    created_at              TIMESTAMP WITH TIME ZONE NOT NULL,
    modified_at             TIMESTAMP WITH TIME ZONE
);

CREATE UNIQUE INDEX IX_plans_name ON plans.plans(name);
CREATE INDEX IX_plans_is_active ON plans.plans(is_active);
```

### Schema: `subscriptions`

```sql
CREATE TABLE subscriptions.subscriptions (
    id                   UUID PRIMARY KEY,
    user_id              VARCHAR(10) NOT NULL,
    plan_id              UUID NOT NULL,
    plan_name            VARCHAR(50) NOT NULL,
    status               VARCHAR(20) NOT NULL,
    billing_cycle        VARCHAR(20) NOT NULL,
    period_start_date    TIMESTAMP WITH TIME ZONE NOT NULL,
    period_end_date      TIMESTAMP WITH TIME ZONE NOT NULL,
    auto_renew           BOOLEAN NOT NULL DEFAULT TRUE,
    created_at           TIMESTAMP WITH TIME ZONE NOT NULL,
    modified_at          TIMESTAMP WITH TIME ZONE,
    cancelled_at         TIMESTAMP WITH TIME ZONE,
    cancellation_reason  VARCHAR(500),
    trial_end_date       TIMESTAMP WITH TIME ZONE,

    CONSTRAINT FK_subscriptions_users FOREIGN KEY (user_id)
        REFERENCES identity.users(id),
    CONSTRAINT FK_subscriptions_plans FOREIGN KEY (plan_id)
        REFERENCES plans.plans(id)
);

CREATE INDEX IX_subscriptions_user_id ON subscriptions.subscriptions(user_id);
CREATE INDEX IX_subscriptions_status ON subscriptions.subscriptions(status);
```

---

## API Endpoints

### Plans: `/api/v1/plans`

| Método | Ruta | Auth | Descripción |
|--------|------|:----:|-------------|
| GET | `/` | - | Obtener todos los planes activos |
| GET | `/{planId}` | - | Obtener plan por ID |

### Subscriptions: `/api/v1/subscriptions`

| Método | Ruta | Auth | Descripción |
|--------|------|:----:|-------------|
| GET | `/current` | ✓ | Obtener suscripción actual del usuario |
| GET | `/history` | ✓ | Obtener historial de suscripciones |
| POST | `/` | ✓ | Crear nueva suscripción |
| POST | `/cancel` | ✓ | Cancelar suscripción actual |
| POST | `/change-plan` | ✓ | Cambiar a otro plan |

---

## Ejemplos de Request/Response

### GET /api/v1/plans

**Response 200:**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440001",
    "name": "Free",
    "description": "Plan gratuito para empezar",
    "pricing": {
      "monthlyPrice": 0,
      "annualPrice": 0,
      "currency": "EUR"
    },
    "limits": {
      "maxStudies": 2,
      "maxTracesPerMonth": 50,
      "maxMembersPerStudy": 3
    },
    "features": [],
    "isActive": true,
    "isDefault": true,
    "isFree": true,
    "displayOrder": 0
  },
  {
    "id": "550e8400-e29b-41d4-a716-446655440002",
    "name": "Pro",
    "description": "Para investigadores individuales",
    "pricing": {
      "monthlyPrice": 29.00,
      "annualPrice": 290.00,
      "currency": "EUR"
    },
    "limits": {
      "maxStudies": 10,
      "maxTracesPerMonth": 500,
      "maxMembersPerStudy": 10
    },
    "features": [
      "CopilotAccess",
      "PriorityProcessing",
      "ExportFeatures"
    ],
    "isActive": true,
    "isDefault": false,
    "isFree": false,
    "displayOrder": 1
  },
  {
    "id": "550e8400-e29b-41d4-a716-446655440003",
    "name": "Enterprise",
    "description": "Para equipos y organizaciones",
    "pricing": {
      "monthlyPrice": 99.00,
      "annualPrice": 990.00,
      "currency": "EUR"
    },
    "limits": {
      "maxStudies": -1,
      "maxTracesPerMonth": -1,
      "maxMembersPerStudy": -1
    },
    "features": [
      "CopilotAccess",
      "PriorityProcessing",
      "AdvancedAnalytics",
      "ApiAccess",
      "ExportFeatures",
      "TeamCollaboration",
      "CustomWorkflows",
      "SsoIntegration",
      "DedicatedSupport"
    ],
    "isActive": true,
    "isDefault": false,
    "isFree": false,
    "displayOrder": 2
  }
]
```

### GET /api/v1/subscriptions/current

**Response 200:**
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "userId": "U00000001",
  "planId": "550e8400-e29b-41d4-a716-446655440002",
  "planName": "Pro",
  "status": "Active",
  "billingCycle": "Annual",
  "currentPeriod": {
    "startDate": "2024-01-15T00:00:00Z",
    "endDate": "2025-01-15T00:00:00Z",
    "daysRemaining": 280
  },
  "autoRenew": true,
  "grantsAccess": true,
  "isFree": false,
  "isInTrial": false,
  "trialEndDate": null,
  "cancelledAt": null,
  "cancellationReason": null,
  "createdAt": "2024-01-15T10:00:00Z",
  "modifiedAt": null
}
```

### POST /api/v1/subscriptions

**Request:**
```json
{
  "planId": "550e8400-e29b-41d4-a716-446655440002",
  "billingCycleId": 2,
  "startWithTrial": true
}
```

**Response 201:**
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174001",
  "userId": "U00000001",
  "planId": "550e8400-e29b-41d4-a716-446655440002",
  "planName": "Pro",
  "status": "Trial",
  "billingCycle": "Annual",
  "currentPeriod": {
    "startDate": "2024-03-20T14:30:00Z",
    "endDate": "2025-03-20T14:30:00Z",
    "daysRemaining": 365
  },
  "autoRenew": true,
  "grantsAccess": true,
  "isFree": false,
  "isInTrial": true,
  "trialEndDate": "2024-04-03T14:30:00Z",
  "cancelledAt": null,
  "cancellationReason": null,
  "createdAt": "2024-03-20T14:30:00Z",
  "modifiedAt": null
}
```

### POST /api/v1/subscriptions/cancel

**Request:**
```json
{
  "reason": "Switching to a competitor product"
}
```

**Response 204:** No Content

### POST /api/v1/subscriptions/change-plan

**Request:**
```json
{
  "newPlanId": "550e8400-e29b-41d4-a716-446655440003",
  "billingCycleId": 2
}
```

**Response 200:**
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "planId": "550e8400-e29b-41d4-a716-446655440003",
  "planName": "Enterprise",
  "status": "Active",
  "billingCycle": "Annual",
  "currentPeriod": {
    "startDate": "2024-03-20T14:30:00Z",
    "endDate": "2025-03-20T14:30:00Z",
    "daysRemaining": 365
  },
  "...": "..."
}
```

---

## Eventos de Dominio

### Plan Events

| Evento | Trigger | Payload |
|--------|---------|---------|
| `PlanCreatedEvent` | Crear plan | PlanId |

### Subscription Events

| Evento | Trigger | Payload |
|--------|---------|---------|
| `SubscriptionCreatedEvent` | Crear suscripción | SubscriptionId, UserId, PlanId |
| `SubscriptionCancelledEvent` | Cancelar | SubscriptionId, UserId, Reason |
| `SubscriptionRenewedEvent` | Renovar | SubscriptionId, UserId, PlanId |
| `SubscriptionPlanChangedEvent` | Cambiar plan | SubscriptionId, UserId, OldPlanId, NewPlanId, OldPlanName, NewPlanName, IsUpgrade |
| `SubscriptionExpiredEvent` | Expirar | SubscriptionId, UserId |

---

## Comandos y Queries

### Plans - Commands

| Command | Descripción | Retorna |
|---------|-------------|---------|
| (Admin only) | Crear/Actualizar planes | - |

### Plans - Queries

| Query | Descripción | Retorna |
|-------|-------------|---------|
| `GetAllPlansQuery` | Todos los planes activos | `Result<IReadOnlyList<PlanDto>>` |
| `GetPlanByIdQuery` | Plan por ID | `Result<PlanDto>` |

### Subscriptions - Commands

| Command | Descripción | Retorna |
|---------|-------------|---------|
| `CreateSubscriptionCommand` | Crear suscripción (con trial opcional) | `Result<SubscriptionDto>` |
| `CancelSubscriptionCommand` | Cancelar suscripción | `Result` |
| `ChangePlanCommand` | Cambiar a otro plan | `Result<SubscriptionDto>` |

### Subscriptions - Queries

| Query | Descripción | Retorna |
|-------|-------------|---------|
| `GetCurrentSubscriptionQuery` | Suscripción activa del usuario | `Result<SubscriptionDto>` |
| `GetSubscriptionHistoryQuery` | Historial de suscripciones | `Result<IReadOnlyList<SubscriptionSummaryDto>>` |

---

## DTOs

### PlanDto
```csharp
public sealed record PlanDto(
    Guid PlanId,
    string Name,
    string? Description,
    decimal MonthlyPrice,
    decimal AnnualPrice,
    string Currency,
    int MaxStudies,
    int MaxTracesPerMonth,
    int MaxMembersPerStudy,
    IReadOnlyList<string> Features,
    bool IsActive,
    bool IsDefault,
    bool IsFree,
    int DisplayOrder);
```

### SubscriptionDto
```csharp
public sealed record SubscriptionDto(
    Guid SubscriptionId,
    string UserId,
    Guid PlanId,
    string PlanName,
    string Status,
    string BillingCycle,
    SubscriptionPeriodDto CurrentPeriod,
    bool AutoRenew,
    bool GrantsAccess,
    bool IsFree,
    bool IsInTrial,
    DateTime? TrialEndDate,
    DateTime? CancelledAt,
    string? CancellationReason,
    DateTime CreatedAt,
    DateTime? ModifiedAt);
```

### SubscriptionSummaryDto
```csharp
public sealed record SubscriptionSummaryDto(
    Guid SubscriptionId,
    string PlanName,
    string Status,
    DateTime StartDate,
    DateTime EndDate,
    bool GrantsAccess);
```

---

## Errores de Dominio

### Plan Errors

| Código | Mensaje |
|--------|---------|
| `Plan.NotFound` | The specified plan was not found |
| `Plan.NameRequired` | Plan name is required |
| `Plan.NameTooShort` | Plan name must be at least {min} characters |
| `Plan.NameTooLong` | Plan name cannot exceed {max} characters |
| `Plan.NameAlreadyExists` | A plan with this name already exists |
| `Plan.InvalidMonthlyPrice` | Monthly price cannot be negative |
| `Plan.InvalidAnnualPrice` | Annual price cannot be negative |
| `Plan.InvalidCurrency` | Currency must be a valid 3-letter ISO 4217 code |
| `Plan.InvalidMaxStudies` | Max studies must be -1 (unlimited) or positive |
| `Plan.InvalidMaxTraces` | Max traces per month must be -1 (unlimited) or positive |
| `Plan.InvalidMaxMembers` | Max members per study must be -1 (unlimited) or positive |
| `Plan.CannotDeactivateDefaultPlan` | Cannot deactivate the default plan |
| `Plan.FeatureAlreadyExists` | This feature is already included in the plan |
| `Plan.FeatureNotFound` | This feature is not included in the plan |

### Subscription Errors

| Código | Mensaje |
|--------|---------|
| `Subscription.NotFound` | The specified subscription was not found |
| `Subscription.InvalidPeriod` | End date must be after start date |
| `Subscription.UserAlreadyHasActiveSubscription` | User already has an active subscription |
| `Subscription.PlanNotFound` | The specified plan was not found |
| `Subscription.PlanNotActive` | The specified plan is not active |
| `Subscription.CannotCancelExpired` | Cannot cancel an expired subscription |
| `Subscription.AlreadyCancelled` | Subscription is already cancelled |
| `Subscription.CannotRenewActive` | Cannot renew an active subscription |
| `Subscription.CannotChangePlanOnExpired` | Cannot change plan on an expired subscription |
| `Subscription.CannotDowngradeToFreeDuringPaidPeriod` | Cannot downgrade to free plan during active paid period |
| `Subscription.CannotActivateFromNonTrial` | Can only activate a subscription in trial status |
| `Subscription.TrialExpired` | The trial period has expired |
| `Subscription.CannotSuspendNonActive` | Can only suspend an active subscription |
| `Subscription.CannotReactivateNonSuspended` | Can only reactivate a suspended subscription |
| `Subscription.InvalidBillingCycle` | Invalid billing cycle specified |

---

## Planes Predefinidos

| Plan | Precio/mes | Precio/año | Studies | Traces/mes | Members | Features |
|------|----------:|----------:|--------:|----------:|--------:|----------|
| **Free** | 0€ | 0€ | 2 | 50 | 3 | - |
| **Pro** | 29€ | 290€ | 10 | 500 | 10 | Copilot, Priority, Export |
| **Enterprise** | 99€ | 990€ | ∞ | ∞ | ∞ | Todos |

---

## Trial Period

- Duración por defecto: **14 días**
- Solo disponible para planes de pago
- Durante el trial:
  - `Status = Trial`
  - `GrantsAccess = true`
  - Se muestra `TrialEndDate`
- Al finalizar trial sin pago → `Status = Expired`
- Se puede activar manualmente con `ActivateFromTrial()`

---

## Flujo de Suscripción

### Nuevo Usuario

```
1. Usuario se registra
   └── Se crea Subscription con Plan "Free" (100 años)

2. Usuario upgrade a Pro
   └── CreateSubscription(planId: Pro, startWithTrial: true)
   └── Status = Trial, TrialEndDate = +14 días

3. Usuario paga antes de fin de trial
   └── ActivateFromTrial()
   └── Status = Active

4. Usuario cancela
   └── Cancel(reason)
   └── Status = Cancelled (acceso hasta fin período)
   └── Al terminar período → Status = Expired
```

### Cambio de Plan

```
Upgrade (Free → Pro):
└── ChangePlan() con isUpgrade = true
└── Nuevo período desde hoy

Downgrade (Pro → Free):
└── Solo permitido si período terminado
└── Error si intenta durante período activo
```

---

## Dependencias

```
GeneFlow.ApiNet2.Domain.Plans
    └── GeneFlow.ApiNet2.SharedKernel

GeneFlow.ApiNet2.Domain.Subscriptions
    ├── GeneFlow.ApiNet2.Domain.Plans
    ├── GeneFlow.ApiNet2.Domain.Identity
    └── GeneFlow.ApiNet2.SharedKernel

GeneFlow.ApiNet2.Infrastructure.Plans
    ├── GeneFlow.ApiNet2.Domain.Plans
    └── Microsoft.EntityFrameworkCore

GeneFlow.ApiNet2.Infrastructure.Subscriptions
    ├── GeneFlow.ApiNet2.Domain.Subscriptions
    └── Microsoft.EntityFrameworkCore

GeneFlow.ApiNet2.API.Plans
    ├── GeneFlow.ApiNet2.Application.Plans
    └── MediatR

GeneFlow.ApiNet2.API.Subscriptions
    ├── GeneFlow.ApiNet2.Application.Subscriptions
    └── MediatR
```

---

## Tests

El módulo incluye tests unitarios para:

- **Domain**: Plan, Subscription, Value Objects, Enumerations
- **Application**: Command y Query handlers

```bash
# Ejecutar tests de Plans
dotnet test --filter "FullyQualifiedName~Plans"

# Ejecutar tests de Subscriptions
dotnet test --filter "FullyQualifiedName~Subscriptions"
```
