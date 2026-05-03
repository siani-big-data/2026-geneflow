# Identity Module Refactoring Plan

## Executive Summary

El módulo de Identity en GeneFlow.ApiNet2 sigue Clean Architecture y DDD. Sin embargo, hay varias áreas que mejorar en seguridad, mantenibilidad y consistencia.

---

## 1. Estado Actual

### Arquitectura
- **Domain**: User aggregate con value objects (Email, Username, PasswordHash, RefreshToken), entidades (ExternalLogin, TwoFactorCode), eventos de dominio
- **Application**: CQRS con MediatR, DTOs, interfaces, event handlers
- **Infrastructure**: EF Core, JWT, BCrypt, OAuth validators, TOTP
- **API**: Minimal APIs (AuthEndpoints, UserEndpoints, OAuthEndpoints)

### Fortalezas
- Modelo de dominio fuerte con value objects encapsulados
- Result pattern para manejo de errores
- Separación de concerns correcta
- Domain events para cross-cutting concerns
- Soporte para password y OAuth
- 2FA con email y TOTP
- Protección de account lockout
- Soft delete

---

## 2. Issues Encontrados

### Críticos (Seguridad)

| ID | Issue | Ubicación | Riesgo |
|----|-------|-----------|--------|
| S1 | Lockout config hardcodeada | `Domain/Identity/ValueObjects/AccountLockout.cs` | No se puede ajustar sin cambiar código |
| S2 | Sin rate limiting | `API/Endpoints/Identity/AuthEndpoints.cs` | Ataques brute force, DoS |
| S3 | Logging excesivo con datos sensibles | `Infrastructure/Identity/Persistence/Repositories/UserRepository.cs` | Filtración de emails en logs |
| S4 | Sin validación de historial de passwords | `Application/Identity/Commands/ChangePassword/` | Reutilización de passwords |
| S5 | Sin detección de reuso de refresh token | `Application/Identity/Commands/RefreshToken/` | Robo de tokens no detectado |

### Alta Prioridad (Arquitectura)

| ID | Issue | Ubicación | Impacto |
|----|-------|-----------|---------|
| H1 | Falta FluentValidation | Commands en `Application/Identity/` | Validación inconsistente |
| H2 | Duplicación en generación de tokens | `LoginCommandHandler`, `OAuthLoginCommandHandler`, `RefreshTokenCommandHandler` | Mantenimiento difícil |
| H3 | Faltan unit tests | `GeneFlow.ApiNet2.Tests/` | Sin cobertura de código crítico |
| H4 | Queries SQL raw en repository | `UserRepository.cs` | Riesgo SQL injection, bypass de EF Core |
| H5 | Lógica de GitHub en endpoint | `OAuthEndpoints.cs` | Viola separación de concerns |

### Media Prioridad (Calidad)

| ID | Issue |
|----|-------|
| M1 | Manejo inconsistente de UserId (string vs UserId) |
| M2 | Falta convención async suffix en algunos métodos |
| M3 | Null checks redundantes para currentUser.UserId |
| M4 | Falta documentación XML en clases nuevas |
| M5 | Password value object mantiene texto plano en memoria |

### Baja Prioridad (Mejoras)

| ID | Feature faltante |
|----|------------------|
| L1 | Reactivación de cuenta |
| L2 | Cambio de email |
| L3 | Gestión de sesiones |
| L4 | Historial de login/auditoría |
| L5 | Expiración de passwords |

---

## 3. Mejoras Propuestas

### Fase 1: Fixes de Seguridad Críticos (Inmediato)

#### 1.1 Usar configuración para lockout
```csharp
public interface ILockoutPolicy
{
    int MaxFailedAttempts { get; }
    TimeSpan LockoutDuration { get; }
}
```

#### 1.2 Añadir Rate Limiting
- Login: 5 intentos/minuto por IP
- Registro: 3/minuto por IP
- Password reset: 3/minuto por email
- 2FA: 3/minuto por usuario

#### 1.3 Limpiar logging sensible
```csharp
// Antes (malo):
_logger.LogInformation("GetByEmailAsync: Starting for email {Email}", email.Value);

// Después (bien):
_logger.LogDebug("Looking up user by email");
```

#### 1.4 Implementar historial de passwords
- Guardar últimos 5 hashes
- Prevenir reutilización

#### 1.5 Detección de reuso de refresh token
- Cuando se usa token revocado: revocar TODOS los tokens del usuario
- Emitir evento de seguridad
- Notificar al usuario

### Fase 2: Mejoras de Arquitectura (Alta)

#### 2.1 Añadir FluentValidation
```csharp
public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(50);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}
```

#### 2.2 Extraer servicio de tokens
```csharp
public interface IAuthTokenService
{
    Task<AuthTokensDto> GenerateTokensAsync(User user, CancellationToken ct);
    Task<Result<AuthTokensDto>> RefreshTokensAsync(string refreshToken, CancellationToken ct);
}
```

#### 2.3 Unit tests comprehensivos
- Command handlers
- Comportamiento de entidades
- Validación de value objects
- Generación de tokens
- Password hashing
- Validación 2FA

#### 2.4 Reemplazar SQL raw con EF Core
```csharp
public async Task<User?> GetByEmailAsync(Email email, CancellationToken ct)
{
    return await _context.Users
        .FirstOrDefaultAsync(u => u.Email == email, ct);
}
```

#### 2.5 Extraer GitHub exchange a Command
Crear `ExchangeGitHubCodeCommand` con handler propio.

### Fase 3: Calidad de Código (Media)

#### 3.1 Estandarizar manejo de UserId
- Todos los commands aceptan `string UserId`
- Extension method para parsing consistente
- Validación en capa API

#### 3.2 Crear filtro de usuario autenticado
```csharp
public class RequireAuthenticatedUserFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var currentUser = context.HttpContext.RequestServices
            .GetRequiredService<ICurrentUserService>();
        if (currentUser.UserId is null)
            return Results.Unauthorized();
        return await next(context);
    }
}
```

### Fase 4: Features (Baja)

- Reactivación de cuenta
- Cambio de email con verificación
- Gestión de sesiones (ver/revocar dispositivos)
- Historial de login
- "Sign out everywhere"

---

## 4. Breaking Changes

### API
1. Rate limiting puede afectar integraciones existentes
2. Validación de historial de passwords rechazará passwords usados
3. Detección de reuso de refresh token invalidará sesiones

### Base de Datos
1. Nueva tabla `password_history`
2. Nueva tabla o columnas para `login_history`
3. Nuevos índices

### Estrategia de Migración
1. Desplegar migraciones de DB primero
2. Desplegar API con feature flags
3. Habilitar features gradualmente
4. Monitorear antes de rollout completo

---

## 5. Matriz de Prioridad

| Issue | Severidad | Esfuerzo | Prioridad |
|-------|-----------|----------|-----------|
| S3 - Logging excesivo | Crítico | Bajo | P1 |
| S2 - Sin rate limiting | Crítico | Medio | P1 |
| S1 - Lockout hardcodeado | Crítico | Bajo | P1 |
| H1 - Sin FluentValidation | Alto | Medio | P2 |
| H2 - Duplicación tokens | Alto | Bajo | P2 |
| S4 - Password history | Crítico | Medio | P2 |
| S5 - Refresh token reuse | Crítico | Medio | P2 |
| H3 - Sin unit tests | Alto | Alto | P2 |
| H4 - SQL raw | Alto | Medio | P3 |
| H5 - GitHub en endpoint | Alto | Bajo | P3 |
| M1-M5 | Medio | Bajo | P4 |
| L1-L5 | Bajo | Medio-Alto | P5 |

---

## 6. Plan de Implementación

### Sprint 1 (2 semanas)
- [ ] Limpiar logging excesivo (S3)
- [ ] Implementar rate limiting (S2)
- [ ] Usar configuración de lockout (S1)
- [ ] Añadir FluentValidation pipeline (H1)

### Sprint 2 (2 semanas)
- [ ] Extraer token service (H2)
- [ ] Añadir password history (S4)
- [ ] Detección de refresh token reuse (S5)
- [ ] Comenzar unit tests (H3)

### Sprint 3 (2 semanas)
- [ ] Reemplazar SQL raw (H4)
- [ ] Extraer GitHub exchange a command (H5)
- [ ] Continuar unit tests
- [ ] Issues de media prioridad (M1-M5)

### Sprint 4+ (ongoing)
- [ ] Feature enhancements (L1-L5)
- [ ] Optimización de performance
- [ ] Providers OAuth adicionales

---

## 7. Archivos Críticos

1. `Domain/Identity/ValueObjects/AccountLockout.cs` - Refactorizar para usar config inyectada
2. `Application/Identity/Commands/Login/LoginCommandHandler.cs` - Extraer token service
3. `Infrastructure/Identity/Persistence/Repositories/UserRepository.cs` - Eliminar SQL raw, limpiar logging
4. `API/Endpoints/Identity/AuthEndpoints.cs` - Rate limiting, validation pipeline
5. `Application/Identity/Commands/RefreshToken/RefreshTokenCommandHandler.cs` - Detección de reuso
