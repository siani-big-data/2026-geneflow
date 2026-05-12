# Plan de Refactorización — Módulo Identity

> Análisis exhaustivo y plan ejecutable para `GeneFlow.ApiNet2.{Domain,Application,Infrastructure,API}/Identity` y sus tests.
>
> **Reglas obligatorias del proyecto** (aplicadas en todo el plan):
> 1. Una clase pública por archivo. Records/DTOs anidados están prohibidos.
> 2. Solo se permiten comentarios XML doc (`/// <summary>`). Cualquier otro comentario (`//`, `/* */`) se considera smell y debe eliminarse.
> 3. Sin sobreingeniería. Cada cambio justifica el problema concreto que resuelve.

---

## 1. Diagnóstico general del backend (módulo Identity)

### 1.1 Estado actual

Identity es el módulo **más maduro** del backend. Aplica Clean Architecture (Domain/Application/Infrastructure/API), CQRS con MediatR, Result Pattern, DDD (User como Aggregate Root con value objects, eventos de dominio y auditoría), Smart Enumerations y validators dedicados. Compila sin errores ni warnings (verificado 2026-04-16). 40 archivos de tests. Cobertura estimada: Domain ~85%, Application ~70%, Infrastructure ~30%, API ~50%.

### 1.2 Fortalezas reales

- Separación de capas correcta: no hay fugas de Infrastructure hacia Domain.
- Result Pattern aplicado consistentemente: no hay `throw` para flujo de control.
- Value Objects bien modelados: `Email`, `PasswordHash`, `RefreshToken`, `TwoFactorAuth`, `AccountLockout`, `EmailVerification`, `PasswordReset`.
- Smart Enumerations (`Role`, `ExternalProvider`) usadas como `Enumeration<T>` con `FromId` / `Id`.
- Domain Events emitidos desde el agregado (16 eventos) con namespace correcto `SharedKernel.Application.EventNotifications`.
- Servicios de Infrastructure limpios: `JwtTokenGenerator`, `TwoFactorAuthenticator`, `GoogleTokenValidator`, `GitHubTokenValidator`.
- Configuración inyectada vía `IOptions<T>` (`JwtSettings`, `OAuthSettings`, `TwoFactorSettings`, `LockoutSettings`, `EmailSettings`).
- Strategy pattern aplicado correctamente en `OAuthTokenValidator` para selección de proveedor.

### 1.3 Problemas principales

- **God Aggregate**: `User.cs` (554 líneas) maneja 8 responsabilidades distintas: autenticación, password, email verification, 2FA, refresh tokens, OAuth links, lockout y roles.
- **God Method**: `OAuthLoginCommandHandler` (256 líneas) con un `CreateNewOAuthUser` interno de 76 líneas y un `Handle` de ~100 líneas.
- **Mezcla ADO.NET + EF Core** en `UserRepository.cs` (274 líneas) sin documentar plenamente la justificación; añade dos modelos de acceso a datos en una misma clase.
- **Múltiples clases por archivo** en `OAuthEndpoints.cs` (3 records anidados): `GitHubCodeExchangeRequest`, `GitHubTokenResponse`, `GitHubOAuthResponse`. **Violación de regla obligatoria.**
- **Comentarios `//` no-doc** dispersos por handlers, repositorio y endpoints. **Violación de regla obligatoria.**
- **Logging excesivo**: 18+ statements en `OAuthLoginCommandHandler`, 11+ en `UserRepository`. Mezcla observabilidad con lógica.
- **`IConfiguration` leído directamente en endpoint** (`OAuthEndpoints.ExchangeGitHubCode`), saltándose Application.
- **Email enumeration potencial** en `LoginCommandHandler` vía logging diferenciado para usuario inexistente vs password inválido.
- **`UserErrors.cs`** (197 líneas) es una clase estática gigante con todos los errores del módulo.

### 1.4 Riesgos técnicos

| Riesgo | Probabilidad | Impacto |
|---|---|---|
| Bug latente al modificar `User.cs` por su tamaño y entrelazamiento de responsabilidades | Media | Alto |
| Inconsistencia transaccional en `OAuthLogin` / `Register` por queries ADO.NET fuera del UoW EF | Baja | Medio |
| Pérdida silenciosa de side-effects (email verificación, password reset) sin Outbox | Baja | Alto |
| Refactor de `UserRepository` rompe queries que dependen de SQL crudo por limitaciones EF con owned types | Media | Medio |
| Bucle de generación de username único en OAuth (max 100 intentos) puede degradar bajo colisión adversarial | Baja | Bajo |
| Cambio en estructura de claims JWT rompe clientes ya autenticados | Media | Alto |

---

## 2. Code smells detectados

### 2.1 Smells críticos (severidad alta)

#### S-01 · God Aggregate: `User.cs`
- **Ubicación**: `Domain/Identity/User.cs` (1–554).
- **Problema**: 35 métodos públicos cubriendo 8 responsabilidades; 22 propiedades; mezcla orquestación de RefreshTokens, ExternalLogins, TwoFactorAuth, EmailVerification, PasswordReset, AccountLockout y Roles.
- **Por qué es un problema**: difícil de razonar, tests acoplados, cualquier cambio toca un archivo de 554 líneas, alto riesgo de regresión.
- **Principios afectados**: SRP (a nivel de agregado), Cohesión, Clean Code (tamaño de clase).
- **Recomendación**: extraer lógica de login attempts, 2FA y refresh tokens a métodos del VO correspondiente; el agregado solo orquesta. No partir en agregados separados todavía (ver §3.5).

#### S-02 · God Method: `OAuthLoginCommandHandler.Handle` y `CreateNewOAuthUser`
- **Ubicación**: `Application/Identity/Commands/OAuthLogin/OAuthLoginCommandHandler.cs` (44–100 y 137–212).
- **Problema**: Handle de ~100 líneas; CreateNewOAuthUser de 76 líneas mezcla validación de proveedor, búsqueda de usuario, vinculación de external login, creación con username único y emisión de tokens.
- **Principios**: SRP, Clean Code (longitud de método < 40), Complejidad ciclomática.
- **Recomendación**: descomponer en pipeline: `ExternalIdentityResolver` → `UserLookupOrProvisioner` → `SessionIssuer`. Cada paso < 60 líneas y testeable aislado.

#### S-03 · Múltiples clases en `OAuthEndpoints.cs` (regla obligatoria)
- **Ubicación**: `API/Endpoints/Identity/OAuthEndpoints.cs:126,131,136`.
- **Problema**: 3 records (`GitHubCodeExchangeRequest`, `GitHubTokenResponse`, `GitHubOAuthResponse`) en el mismo archivo del endpoint.
- **Principio**: regla obligatoria del proyecto.
- **Recomendación**: mover a `API/Contracts/Identity/OAuth/` un archivo por record.

#### S-04 · Mezcla ADO.NET + EF Core en `UserRepository`
- **Ubicación**: `Infrastructure/Identity/Persistence/Repositories/UserRepository.cs` (104–174 ADO.NET; resto EF).
- **Problema**: cinco métodos (`GetUserIdByEmailAsync`, `GetUserIdByUsernameAsync`, `GetUserIdByEmailOrUsernameAsync`, `GetByRefreshTokenAsync`, `GetByExternalLoginAsync`) ejecutan SQL crudo accediendo a `DbConnection`. El resto usa EF.
- **Por qué es problema**: dos modelos de acceso a datos coexistiendo, parsing manual, queries fuera del contexto transaccional de EF, mantenibilidad baja.
- **Principios**: SoC, KISS, Persistence Ignorance.
- **Recomendación**: unificar en EF Core puro. Si EF no soporta queries sobre owned collections (refresh tokens, external logins), abrir issue en Domain para mover esas colecciones a entidades hijas dependientes en lugar de owned types — no introducir Dapper como solución.

#### S-05 · Email enumeration leak en `LoginCommandHandler`
- **Ubicación**: `Application/Identity/Commands/Login/LoginCommandHandler.cs:~52`.
- **Problema**: aunque el error retornado al cliente es genérico `InvalidCredentials`, el logging diferencia entre "usuario no existe" y "password inválida". Si el log es accesible (incluso a personal con permisos amplios) un atacante podría enumerar cuentas.
- **Principio**: seguridad por defecto, OWASP A07:2021 — Identification and Authentication Failures.
- **Severidad**: **crítica** (única severidad crítica en el plan).
- **Recomendación**: loggear como mismo `LogWarning` con motivo neutralizado a nivel `Information`; mantener detalle solo a `Debug` en entornos no-prod.

### 2.2 Smells medios (severidad media)

#### S-06 · `catch (Exception)` sin tipo específico
- **Ubicación 1**: `Infrastructure/Identity/Services/TwoFactorAuthenticator.cs:54`.
- **Ubicación 2**: `API/Endpoints/Identity/OAuthEndpoints.cs:116`.
- **Problema**: oculta errores no contemplados; difícil diagnosticar fallos reales.
- **Principio**: Clean Code (manejo de errores explícito).
- **Recomendación**: cambiar a `catch (FormatException)` / `catch (CryptographicException)` y dejar propagar el resto.

#### S-07 · Logging excesivo en handlers
- **Ubicación 1**: `OAuthLoginCommandHandler.cs` (18+ statements en 256 líneas).
- **Ubicación 2**: `UserRepository.cs` (11+ statements `LogInformation`).
- **Problema**: ratio señal/ruido bajo; logs entremezclados con flujo dificultan leer la lógica.
- **Principio**: SoC (observabilidad ≠ lógica).
- **Recomendación**: dejar 3–4 puntos clave por handler (entrada con identificadores no sensibles, error de proveedor externo, éxito, fallo de invariante). Resto a nivel `Debug`.

#### S-08 · `IConfiguration` leído en endpoint
- **Ubicación**: `OAuthEndpoints.cs:63` (parámetro `IConfiguration configuration` en `ExchangeGitHubCode`).
- **Problema**: la capa de presentación lee configuración GitHub directamente; rompe el patrón de `IOptions<OAuthSettings>` ya usado en el resto del módulo.
- **Principio**: SoC, consistencia.
- **Recomendación**: mover el exchange a un handler de Application (`ExchangeGitHubCodeCommandHandler`) que reciba `IOptions<OAuthSettings>`.

#### S-09 · Comentarios inline no-doc (regla obligatoria)
- **Ubicación**: dispersos en `OAuthLoginCommandHandler`, `UserRepository`, `OAuthEndpoints`, varios handlers.
- **Problema**: regla obligatoria del proyecto: solo XML docs.
- **Recomendación**: eliminar todos. Si un comentario explica "por qué", convertir en `/// <remarks>` del método o en commit message; si explica "qué", el código debe ser autoexplicativo.

#### S-10 · Validators duplicados Domain vs FluentValidation
- **Ubicación**: `Domain/Identity/Validators/{Email,Username,Password,...}Validator.cs` y validators FluentValidation en Application.
- **Problema**: validación de invariantes en dos sitios; divergen con el tiempo.
- **Principio**: DRY.
- **Recomendación**: FluentValidation solo valida shape de request (no nulo, longitud, formato superficial); Domain validators imponen invariantes de negocio (regex de email RFC-compliant, política de password). Eliminar lo redundante.

#### S-11 · Magic numbers
- **Ubicación 1**: `Domain/Identity/ValueObjects/AccountLockout.cs:11` (`MaxFailedAttempts = 5`).
- **Ubicación 2**: `Domain/Identity/ValueObjects/AccountLockout.cs:14` (`LockoutDuration = 15 min`).
- **Ubicación 3**: `Domain/Identity/ValueObjects/TwoFactorAuth.cs:~112` (`maxCodesToKeep = 10`).
- **Ubicación 4**: `OAuthLoginCommandHandler.cs:252` (`MaxUsernameAttempts = 100`).
- **Ubicación 5**: `OAuthLoginCommandHandler.cs:223` (truncado de username a 20).
- **Recomendación**: convertir a `const` o `static readonly` con nombre descriptivo. Si dependen de configuración (lockout, attempts), exponerlas vía `IOptions<LockoutSettings>` y `IOptions<TwoFactorSettings>`.

#### S-12 · `UserErrors.cs` gigante (197 líneas)
- **Ubicación**: `Domain/Identity/UserErrors.cs`.
- **Problema**: una sola clase estática con todos los errores del módulo; difícil navegar.
- **Principio**: cohesión, tamaño de clase.
- **Recomendación**: particionar por subdominio en `Domain/Identity/Errors/` (ver §4 Fase 1.7).

### 2.3 Smells bajos (severidad baja)

#### S-13 · Bucle potencialmente lento de generación de username único
- **Ubicación**: `OAuthLoginCommandHandler.GenerateUniqueUsernameAsync` (226–254).
- **Problema**: hasta 100 queries secuenciales a BD. Aceptable hoy, frágil bajo presión.
- **Recomendación**: bajar el límite a 20 y, en caso de no encontrar, anexar un sufijo aleatorio basado en `Guid.NewGuid().ToString("N")[..6]`. Una query, fin.

#### S-14 · `RegisterUserCommandHandler` orquesta hash, token, evento sin atomicidad
- **Ubicación**: `Application/Identity/Commands/Register/RegisterUserCommandHandler.cs`.
- **Problema**: si falla el envío de email tras commit, el usuario queda creado sin email enviado.
- **Recomendación**: el envío de email ya está en `EventHandler` del evento `UserRegisteredEvent`. Si se quiere garantía, ver §3.3 Fase 3.1 (Outbox).

#### S-15 · `LoginResultDto` semánticamente confuso
- **Ubicación**: `Application/Identity/DTOs/LoginResultDto.cs` (16 líneas).
- **Problema**: representa tres estados (éxito con tokens, requiere 2FA, requiere recovery) en un único DTO con campos opcionales. Cliente debe inspeccionar campos para entender estado.
- **Recomendación**: convertir a `OneOf`/discriminated union solo si crece. Por ahora documentar con XML doc los estados válidos.

#### S-16 · Dependencias del handler de OAuth
- **Ubicación**: `OAuthLoginCommandHandler` inyecta 6 dependencias.
- **Problema**: número de dependencias es señal de exceso de responsabilidad.
- **Recomendación**: tras 2.1 (descomposición), cada subservicio tendrá 2–3 dependencias.

---

## 3. Problemas arquitectónicos

### 3.1 Acoplamiento Application → Infrastructure por DTOs específicos de proveedor

`IOAuthTokenValidator` devuelve un `OAuthUserInfo` con campos cuya semántica varía por proveedor (`EmailVerified` puede no venir en GitHub si el email es privado). Los handlers tienen lógica condicional sobre estos campos. Falta una abstracción `IExternalIdentityProvider` neutra que devuelva un `ExternalIdentity` (provider, subjectId, email, emailVerified, displayName, avatarUrl).

### 3.2 Unit of Work parcial

`IUserUnitOfWork` existe, pero las queries ADO.NET de `UserRepository` se ejecutan fuera del contexto transaccional de EF. En `OAuthLogin` y `Register`, la secuencia "buscar por external login → buscar por email → crear → commit" no es atómica frente a inserciones concurrentes (race condition en alta concurrencia, baja probabilidad).

### 3.3 Domain Events sin garantía de entrega

`UserRegisteredEvent`, `PasswordResetRequestedEvent`, `TwoFactorCodeGeneratedEvent` se publican mediante el bus interno tras commit. Si el handler que envía email falla, no hay reintento. Bajo volumen actual no es crítico, pero es deuda conocida.

### 3.4 JWT: política mezclada con firma

`JwtTokenGenerator.GenerateAccessToken(User)` decide qué claims incluir (decisión de negocio: incluir `email_verified`, todos los roles, `jti`) en una clase de Infrastructure. Cambiar la política de claims requiere modificar Infrastructure.

### 3.5 `User` aggregate con 8 responsabilidades

Visión a futuro: separar en agregados `User` (identidad core), `UserSecurity` (2FA, lockout, refresh tokens), `UserExternalLogins`. **No recomendado en este momento**: las invariantes cruzadas (e.g., desactivar 2FA al cambiar password) requerirían coordinación inter-agregados que añade más complejidad de la que quita. Reevaluar tras Fase 2.5/2.6 con métricas en mano.

### 3.6 Endpoints OAuth con lógica de protocolo

`OAuthEndpoints.ExchangeGitHubCode` hace una llamada HTTP a `https://github.com/login/oauth/access_token` directamente desde el endpoint. Esto es lógica de aplicación, no de presentación.

### 3.7 Repositorio expuesto con 15+ métodos `GetByXxx`

`IUserRepository` tiene 15 métodos de consulta. Aunque cohesivos al agregado, el número sugiere que parte de la lógica podría vivir en queries CQRS específicas (`Application/Identity/Queries/`) en lugar de en el repositorio. No urgente.

### 3.8 No hay refresh token rotation ni family chaining

Cada refresh emite tokens nuevos pero no invalida la familia anterior. Robo de refresh token no se detecta hasta el siguiente cambio de password. **Riesgo de seguridad medio**, no incluido en plan inicial — anotarlo como deuda conocida y revisar en hoja de ruta de seguridad.

---

## 4. Plan de refactorización priorizado

### Fase 1 — Cambios seguros y de bajo riesgo

Sin cambios de comportamiento, sin breaking changes en API pública. Cada tarea es un commit atómico con `dotnet test` verde antes y después.

#### Tarea 1.1 · Extraer DTOs de `OAuthEndpoints.cs` a archivos individuales
- **Descripción**: mover `GitHubCodeExchangeRequest`, `GitHubTokenResponse`, `GitHubOAuthResponse` a `API/Contracts/Identity/OAuth/`, un archivo por record.
- **Archivos afectados**: `API/Endpoints/Identity/OAuthEndpoints.cs`; nuevos `GitHubCodeExchangeRequest.cs`, `GitHubTokenResponse.cs`, `GitHubOAuthResponse.cs`.
- **Beneficio**: cumple regla obligatoria.
- **Riesgo**: nulo (mecánico).
- **Dependencias**: ninguna.
- **Criterio de finalización**: `OAuthEndpoints.cs` no contiene declaraciones `record`/`class` anidadas; `dotnet build` y `dotnet test` verdes.
- **Tests**: los existentes deben seguir pasando sin modificación.

#### Tarea 1.2 · Eliminar todos los comentarios no-doc en módulo Identity
- **Descripción**: regex `^\s*//(?!/)` en todos los `.cs` bajo `*/Identity/` (Domain, Application, Infrastructure, API). Eliminar o convertir a XML doc relevante.
- **Archivos afectados**: estimado 25–30 archivos.
- **Beneficio**: cumple regla obligatoria.
- **Riesgo**: nulo si no se altera código.
- **Dependencias**: ninguna.
- **Criterio**: `grep "// " --include="*.cs" -R Identity/` devuelve 0 resultados (excluyendo `///`).
- **Tests**: los existentes intactos.

#### Tarea 1.3 · Cerrar leak de email enumeration en `LoginCommandHandler`
- **Descripción**: unificar logging de "usuario no encontrado" y "password inválido" a un único `LogWarning("Failed login attempt for identifier {Identifier}", identifier)` sin distinguir motivo a nivel `Information`. Mantener detalle a `LogDebug`.
- **Archivos afectados**: `LoginCommandHandler.cs`.
- **Beneficio**: cierra vector OWASP A07.
- **Riesgo**: bajo. Verificar que no haya alertas de monitorización que dependan del log diferenciado.
- **Dependencias**: ninguna.
- **Criterio**: tests `LoginCommandHandlerTests` (caso usuario inexistente, caso password inválida) no observan diferencia en log message a nivel `Information`.
- **Tests**: añadir test que verifique uniformidad del log.

#### Tarea 1.4 · Particionar `UserErrors.cs` en errores por subdominio
- **Descripción**: dividir en `Domain/Identity/Errors/` con clases estáticas:
  - `UserAuthErrors.cs` (InvalidCredentials, AccountLockedOut, EmailNotVerified, InactiveAccount).
  - `UserPasswordErrors.cs` (InvalidPassword, PasswordResetTokenInvalid, PasswordResetTokenExpired).
  - `UserEmailErrors.cs` (EmailAlreadyExists, EmailVerificationTokenInvalid, EmailVerificationTokenExpired).
  - `UserTwoFactorErrors.cs` (TwoFactorAlreadyEnabled, TwoFactorNotEnabled, InvalidTwoFactorCode, TotpSetupFailed).
  - `UserOAuthErrors.cs` (referenciar/mover los de `OAuthErrors.cs` aquí también o mantener separación).
  - `UserAccountErrors.cs` (UserNotFound, UsernameAlreadyExists, AccountAlreadyDeactivated).
- **Archivos afectados**: `UserErrors.cs` (a eliminar tras migración), nuevos archivos en `Errors/`. Buscar y reemplazar referencias `UserErrors.X` por la clase específica.
- **Beneficio**: legibilidad, navegación, archivos < 60 líneas.
- **Riesgo**: medio (alcance de cambio amplio en Application por referencias).
- **Dependencias**: ninguna.
- **Criterio**: `UserErrors.cs` eliminado; `dotnet build` verde; tests verdes.
- **Tests**: ninguno nuevo, los existentes validan los Errors por equality.

#### Tarea 1.5 · Magic numbers a constantes nombradas
- **Descripción**: introducir `const` o `static readonly` con nombres descriptivos:
  - `AccountLockout`: `MaxFailedAttempts`, `LockoutDuration` ya son constantes pero documentarlas con XML doc; mover a `LockoutSettings` si requieren ser configurables.
  - `TwoFactorAuth.cs`: `MaxRetainedCodes = 10` (sustituir literal en cleanup).
  - `OAuthLoginCommandHandler`: `MaxUsernameAttempts = 100` y `MaxUsernameLength = 20`.
- **Archivos afectados**: `AccountLockout.cs`, `TwoFactorAuth.cs`, `OAuthLoginCommandHandler.cs`, opcionalmente `LockoutSettings.cs` y `TwoFactorSettings.cs`.
- **Beneficio**: legibilidad, eliminación de magic numbers.
- **Riesgo**: nulo.
- **Dependencias**: ninguna.
- **Criterio**: ningún literal numérico sin nombre fuera de tests.
- **Tests**: existentes intactos.

#### Tarea 1.6 · Reemplazar `catch (Exception)` por catch tipados
- **Descripción**:
  - `TwoFactorAuthenticator.cs:54` (decrypt): `catch (FormatException)` y `catch (CryptographicException)` → retornar `false`. Resto se propaga.
  - `OAuthEndpoints.cs:116` (GitHub exchange): `catch (HttpRequestException)` → retornar `BadRequest`; `catch (JsonException)` → retornar `BadRequest`. Resto se propaga.
- **Beneficio**: errores no esperados ya no se silencian.
- **Riesgo**: bajo.
- **Dependencias**: ninguna.
- **Criterio**: ningún `catch (Exception)` en el módulo Identity.
- **Tests**: añadir test de `TwoFactorAuthenticator.DecryptSecret` con base64 inválida y con clave errónea — debe retornar `false` sin lanzar.

#### Tarea 1.7 · Reducir logging en handlers a 3–4 puntos clave
- **Descripción**: en `OAuthLoginCommandHandler` y `UserRepository`, dejar:
  - 1 `LogInformation` de entrada con identificadores no sensibles (no email, sí provider).
  - 1 `LogWarning` por error de proveedor externo.
  - 1 `LogInformation` de éxito.
  - 1 `LogWarning` de fallo de invariante.
  - Resto a `LogDebug`.
- **Beneficio**: ratio señal/ruido alto en logs de producción.
- **Riesgo**: bajo (solo si hay alertas que dependen de log statements concretos — verificar).
- **Dependencias**: ninguna.
- **Criterio**: < 5 statements `LogInformation` o superior en `OAuthLoginCommandHandler`.
- **Tests**: existentes intactos.

#### Tarea 1.8 · Eliminar `static class JwtSettings` (o equivalentes) inline si existieran en archivos compartidos
- **Descripción**: verificar que `JwtSettings`, `OAuthSettings`, `LockoutSettings`, `EmailSettings`, `TwoFactorSettings` están cada uno en su propio archivo. Confirmado por exploración pero re-validar tras cambios.
- **Beneficio**: cumple regla obligatoria.
- **Riesgo**: nulo.

---

### Fase 2 — Mejoras de diseño

Cambios internos sin afectar contratos públicos. Requieren suite de tests verde antes y después.

#### Tarea 2.1 · Consolidar validators (Domain vs FluentValidation)
- **Descripción**: definir contrato:
  - FluentValidation (Application): valida shape del request (no nulo, longitud, formato superficial regex `@`).
  - Domain Validators (`Domain/Identity/Validators/`): imponen invariantes de negocio (RFC 5322 strict para email, política de password con N reglas).
- **Archivos afectados**: validators FluentValidation de `Login`, `Register`, etc.; Domain validators existentes.
- **Beneficio**: única fuente de verdad por nivel.
- **Riesgo**: medio — eliminar duplicación puede dejar gaps si no se mapea cuidadosamente.
- **Dependencias**: ninguna.
- **Criterio**: matriz documentada de qué se valida dónde; tests cubren ambos niveles.
- **Tests**: añadir tests de Application (FluentValidation) y de Domain por separado.

#### Tarea 2.2 · Extraer `LoginAttemptTracker` VO
- **Descripción**: nuevo VO que encapsula `RecordSuccessfulLogin`, `RecordFailedLogin` y la decisión de lockout. `User` solo expone `RecordSuccessfulLogin()` y `RecordFailedLogin()` que delegan al VO.
- **Archivos afectados**: `Domain/Identity/ValueObjects/LoginAttemptTracker.cs` (nuevo); `User.cs` reduce de 554 a ~480 líneas; configuración EF (`UserConfiguration.cs`) mapea como owned type o conversión.
- **Beneficio**: reduce User; agrupa invariantes de bloqueo en un único punto testeable.
- **Riesgo**: medio — mapeo EF de owned types con backing fields puede requerir migración de BD si la columna se renombra.
- **Dependencias**: ninguna técnica; deseable después de 1.4 (errores particionados).
- **Criterio**: User.cs < 500 líneas; `LoginAttemptTrackerTests` cubre boundary (n-1, n, n+1 intentos).
- **Tests**: nuevos tests del VO.

#### Tarea 2.3 · Mover lógica 2FA al VO `TwoFactorAuth`
- **Descripción**: extraer setup TOTP, verify code, disable, recovery code consumption desde `User` al VO `TwoFactorAuth.cs` (que ya tiene parte de la lógica). `User` solo orquesta y emite eventos.
- **Archivos afectados**: `User.cs`, `TwoFactorAuth.cs`, posiblemente `TwoFactorSecret.cs`.
- **Beneficio**: cohesión; User baja a ~400 líneas.
- **Riesgo**: medio — eventos de dominio deben seguir disparándose desde el agregado.
- **Dependencias**: ninguna técnica.
- **Criterio**: métodos 2FA del agregado son one-liners delegando al VO + emisión de evento.
- **Tests**: ampliar `TwoFactorAuthTests` con todos los caminos.

#### Tarea 2.4 · Introducir `IClaimsFactory` (Application)
- **Descripción**: nueva abstracción `IClaimsFactory.BuildClaims(User user) : IReadOnlyList<Claim>` en Application. `JwtTokenGenerator` (Infrastructure) recibe el factory y solo firma.
- **Archivos afectados**: `Application/Identity/Abstractions/IClaimsFactory.cs` (nuevo); `Application/Identity/Services/UserClaimsFactory.cs` (nuevo); `JwtTokenGenerator.cs` se simplifica.
- **Beneficio**: política de claims (negocio) en Application; firma (cripto) en Infrastructure.
- **Riesgo**: bajo — el contrato de claims emitidos no cambia.
- **Dependencias**: ninguna.
- **Criterio**: `JwtTokenGenerator.GenerateAccessToken` no construye claims literales.
- **Tests**: snapshot tests de los claims emitidos por `UserClaimsFactory` para detectar cambios accidentales en payload.

#### Tarea 2.5 · Introducir `IExternalIdentityProvider` neutro
- **Descripción**: nueva abstracción en Application. `ExternalIdentity` DTO neutro: `{ Provider, SubjectId, Email, EmailVerified, DisplayName, AvatarUrl }`. Adaptar `GoogleTokenValidator` y `GitHubTokenValidator` para implementarla y renombrar a `GoogleExternalIdentityProvider`, `GitHubExternalIdentityProvider`.
- **Archivos afectados**: `Application/Identity/Abstractions/IExternalIdentityProvider.cs` (nuevo); `Application/Identity/DTOs/ExternalIdentity.cs` (nuevo); `GoogleTokenValidator.cs`, `GitHubTokenValidator.cs` (renombrar y adaptar); `OAuthTokenValidator.cs` (rebautizar a `ExternalIdentityProviderResolver` o mantener).
- **Beneficio**: añadir nuevos proveedores (Microsoft, Apple) sin tocar handlers.
- **Riesgo**: medio — cambia DI registrations.
- **Dependencias**: facilita 2.6.
- **Criterio**: tests de OAuth handlers mockean `IExternalIdentityProvider`, no proveedores específicos.
- **Tests**: existentes adaptados; nuevo test de selección de strategy por provider.

#### Tarea 2.6 · Descomponer `OAuthLoginCommandHandler` en pipeline
- **Descripción**: nuevos servicios en `Application/Identity/Services/OAuth/`:
  - `ExternalIdentityResolver` — usa `IExternalIdentityProvider` para validar token y devolver `ExternalIdentity`.
  - `UserOAuthProvisioner` — busca por external login; si no, busca por email; si no, crea nuevo (delegando username único a `IUsernameSuggester`).
  - `SessionIssuer` — emite access + refresh + persiste refresh token.
  - `IUsernameSuggester` — sanitiza email/displayName y resuelve colisiones (con sufijo Guid si hay colisión, no bucle de 100 queries).
- **Archivos afectados**: `OAuthLoginCommandHandler.cs` (256 → ~80 líneas, solo orquesta); 4 servicios nuevos.
- **Beneficio**: testabilidad y aislamiento de cada paso.
- **Riesgo**: alto — refactor de método grande con muchas branches.
- **Dependencias**: 2.5 (`IExternalIdentityProvider`).
- **Criterio**: cada nuevo servicio < 80 líneas; cada método < 40; complejidad ciclomática < 10.
- **Tests**: tests existentes del handler como caja negra deben seguir pasando; nuevos tests por servicio.

#### Tarea 2.7 · Unificar ORM en `UserRepository` (EF Core puro)
- **Descripción**: eliminar las cinco queries ADO.NET. Si EF no soporta queries sobre owned collections (refresh tokens, external logins), promover esas colecciones de owned types a entidades hijas dependientes (`ExternalLogin` ya es entidad; `RefreshToken` puede ser entidad si es necesario, manteniendo invariante de que su ciclo de vida pertenece a User).
- **Archivos afectados**: `UserRepository.cs` (274 → ~150 líneas); `UserConfiguration.cs` (mapeo EF); migraciones EF.
- **Beneficio**: un solo modelo de acceso a datos; mantenibilidad.
- **Riesgo**: alto — migraciones de BD si cambia mapeo de RefreshToken.
- **Dependencias**: ninguna técnica (ejecutable en paralelo a 2.6).
- **Criterio**: 0 referencias a `DbConnection` en `UserRepository`.
- **Tests**: tests de integración contra PostgreSQL real validan paridad.

#### Tarea 2.8 · Mover GitHub code exchange a Application
- **Descripción**: nuevo `ExchangeGitHubCodeCommand` + handler. Endpoint solo dispatch. `OAuthSettings` inyectado vía `IOptions`.
- **Archivos afectados**: `OAuthEndpoints.cs` (eliminar lógica HTTP), nuevo `Application/Identity/Commands/ExchangeGitHubCode/{Command,Handler}.cs`, `IGitHubCodeExchanger` en Application + implementación con `HttpClient` en Infrastructure.
- **Beneficio**: presentación libre de lógica; testeable.
- **Riesgo**: bajo.
- **Dependencias**: ninguna.
- **Criterio**: `OAuthEndpoints.cs` no usa `IConfiguration` ni `HttpClient` directo.
- **Tests**: tests de handler con `IGitHubCodeExchanger` mockeado.

---

### Fase 3 — Refactorización arquitectónica

Cambios estructurales. **Solo abordar si Fase 1 y 2 están consolidadas** y métricas en producción justifican.

#### Tarea 3.1 · Outbox Pattern para Domain Events de Identity
- **Descripción**: tabla `outbox_messages`; los handlers de eventos críticos (verificación email, password reset, 2FA setup, OAuth linking) se convierten en consumers de outbox. Persistencia atómica con commit del agregado.
- **Beneficio**: garantía de entrega de side-effects.
- **Riesgo**: alto. Coste operacional alto (worker, idempotencia).
- **Criterio de inicio**: evidencia en producción de eventos perdidos.

#### Tarea 3.2 · Refresh token rotation + family chaining
- **Descripción**: cada refresh emite token nuevo y revoca el anterior. Si un token revocado se usa, se invalida toda la familia (sospecha de robo).
- **Beneficio**: mitigación de robo de refresh tokens.
- **Riesgo**: medio.
- **Criterio de inicio**: revisión de seguridad lo prioriza.

#### Tarea 3.3 · Separar agregados (`User`, `UserSecurity`, `UserExternalLogins`)
- **Descripción**: agregados independientes con coordinación vía eventos.
- **Riesgo**: muy alto. Las invariantes cruzadas (e.g., desactivar 2FA al soft-delete) requieren coordinación que añade complejidad.
- **Recomendación**: **NO ejecutar** salvo evidencia post-Fase 2 de que `User` sigue creciendo descontroladamente.

---

## 5. Patrones de diseño recomendados

| Patrón | Problema que resuelve | Dónde aplicarlo | Alternativas más simples | Riesgo de sobreingeniería | Ejemplo breve |
|---|---|---|---|---|---|
| **Strategy** | Variabilidad real entre proveedores OAuth | `IExternalIdentityProvider` con implementaciones Google/GitHub (Tarea 2.5) | Switch sobre enum (lo que hay hoy) — funciona pero filtra detalles | Bajo: ya existe la variabilidad | `provider.Name switch { Google => _google, GitHub => _github }` reemplazado por DI keyed services |
| **Pipeline** | God Method en OAuthLogin | Descomposición Resolver→Provisioner→Issuer (Tarea 2.6) | Dejar como está (no recomendado) | Bajo: descomposición natural | Cada paso devuelve `Result<T>` y el siguiente recibe; encadenado con `Bind` o continuation |
| **Factory** | Política de claims en Infrastructure | `IClaimsFactory` (Tarea 2.4) | Hardcodear claims en JwtTokenGenerator (lo que hay) | Bajo | `IClaimsFactory.BuildClaims(user)` retorna `IReadOnlyList<Claim>` consumido por el generator |
| **Value Object enrichment** | Lógica dispersa en User | `LoginAttemptTracker`, `TwoFactorAuth` enriquecidos (Tareas 2.2, 2.3) | Métodos en User (lo actual) | Bajo: VO ya existen, solo migrar lógica | `_lockout = _lockout.RecordFailedAttempt()` retorna nuevo VO inmutable |

**Patrones rechazados explícitamente:**

| Patrón | Por qué NO |
|---|---|
| **Specification** | No hay queries complejas combinables. Sería overengineering. |
| **Repository genérico `IRepository<T>`** | Los repositorios actuales son cohesivos por agregado, así debe ser en DDD. |
| **Event Sourcing** | Resuelve problema que no existe. Coste operacional altísimo. |
| **CQRS read/write split físico** | No hay problema de escala que lo justifique. |
| **Mediator dentro del agregado** | Ya existe MediatR en Application. Duplicar es ruido. |
| **AbstractFactory para `RefreshToken`** | Constructor + factory método estático bastan. |
| **Decorator para logging** | Inyectar `ILogger` y loggear con discreción es más simple. |
| **AutoMapper** | Mappings manuales son más legibles y debuggeables. |

---

## 6. Estrategia de testing antes y después

### 6.1 Tests que deben existir antes de refactorizar

#### Tests de regresión obligatorios (red de seguridad)
- **`LoginCommandHandlerTests`**: cubrir
  - usuario inexistente → `InvalidCredentials` y log uniforme.
  - password inválida → `InvalidCredentials` y log uniforme.
  - cuenta inactiva → error correspondiente.
  - cuenta bloqueada → error correspondiente.
  - email no verificado → error correspondiente.
  - 2FA habilitado sin código → `RequiresTwoFactor=true`.
  - 2FA habilitado con código TOTP válido → éxito.
  - 2FA habilitado con código TOTP inválido → error.
  - 2FA habilitado con código email válido → éxito.
- **`OAuthLoginCommandHandlerTests`**: cubrir
  - usuario nuevo (no email previo) → crea + emite tokens.
  - usuario existente con external login → logea.
  - usuario existente sin external login → vincula + logea.
  - email no verificado por proveedor → comportamiento definido.
  - colisión de username (>3 colisiones) → resuelve con sufijo.
  - validación de token externo falla → error.
- **`UserTests` (boundary)**:
  - `RecordFailedLogin` 4 veces → no bloqueado.
  - `RecordFailedLogin` 5 veces → bloqueado.
  - intento de login bloqueado → error sin incrementar contador.
  - tras `LockoutDuration` → puede intentar de nuevo.
- **`TwoFactorAuthenticatorTests`**:
  - encrypt → decrypt roundtrip ok.
  - decrypt con base64 inválida → `false`.
  - decrypt con clave errónea → `false`.
  - validate TOTP en ventana → `true`.
  - validate TOTP fuera de ventana → `false`.
- **`UserRepositoryTests` (integration)**:
  - `GetByEmailAsync`, `GetByUsernameAsync`, `GetByRefreshTokenAsync`, `GetByExternalLoginAsync` con datos de prueba reales.

#### Tests unitarios a añadir
- `LoginAttemptTrackerTests` (cuando se cree el VO en 2.2).
- `UserClaimsFactoryTests` con snapshot de claims (cuando se cree en 2.4).
- `ExternalIdentityResolverTests`, `UserOAuthProvisionerTests`, `SessionIssuerTests` (cuando se creen en 2.6).
- `UsernameSuggesterTests` con caso de colisión.

#### Tests de integración a añadir
- `OAuthLoginIntegrationTests` end-to-end con `WebApplicationFactory` (existe `GeneFlowWebApplicationFactory.cs`).
- `RegisterUserIntegrationTests` validando que `UserRegisteredEvent` triggerea el handler de email.

### 6.2 Mocks o fakes necesarios

- `FakeExternalIdentityProvider` (después de 2.5) para tests de OAuth sin red.
- `FakeEmailService` (probablemente ya existe) capturando emails enviados.
- `FakeJwtTokenGenerator` o builder de tokens deterministas para tests.
- `FakeClock` / `IDateTimeProvider` si no existe — para tests de expiración.

### 6.3 Cómo asegurar que el comportamiento no cambia

- Cada PR de Fase 1 y Fase 2 mantiene 100% verde.
- **Snapshot tests** de claims JWT antes de Tarea 2.4.
- **Mutation testing** con Stryker (`stryker-config.json` ya existe) sobre `Domain/Identity` objetivo > 70% mutation score.
- **Tests de contract** en API pública: forma de respuesta de `/auth/login`, `/auth/oauth/{provider}`, `/auth/refresh` no cambian (snapshot JSON).

---

## 7. Orden recomendado de ejecución

```
Fase 1 (orden estricto, commits atómicos):
  1.3  Cerrar email enumeration leak           ← URGENTE (seguridad)
  1.2  Eliminar comentarios no-doc             ← regla obligatoria
  1.1  Split DTOs OAuthEndpoints               ← regla obligatoria
  1.4  Particionar UserErrors.cs
  1.5  Magic numbers a constantes
  1.6  Catch tipados
  1.7  Reducir logging
  1.8  Verificar 1 archivo = 1 clase global

Fase 2 (en este orden por dependencias):
  2.1  Consolidar validators                   ← preparación
  2.2  Extraer LoginAttemptTracker             ← reduce User.cs
  2.3  Mover lógica 2FA al VO TwoFactorAuth    ← reduce User.cs más
  2.4  IClaimsFactory                          ← desacoplar JWT
  2.5  IExternalIdentityProvider               ← prepara 2.6
  2.6  Descomponer OAuthLoginCommandHandler    ← depende de 2.5
  2.8  GitHub code exchange a Application      ← depende de 2.5
  2.7  Unificar ORM UserRepository             ← independiente, paralelizable

Fase 3:
  Reevaluar después de Fase 2 con métricas en mano.
  No ejecutar por estética.
```

Cada paso debe: tener un único responsable, ser revisable en < 200 líneas de diff (excepto 2.6 y 2.7), pasar `dotnet test` y `dotnet build` con 0 warnings, ser reversible vía `git revert` sin romper otros pasos.

---

## 8. Cambios que NO recomiendo hacer

| Anti-propuesta | Por qué NO |
|---|---|
| Reescribir `User.cs` desde cero | Funciona, tiene tests, contiene invariantes ganadas con esfuerzo. Refactor incremental > rewrite. |
| Separar `User` en agregados antes de Fase 2 | Las invariantes cruzadas justifican mantener un único agregado hasta tener métricas. |
| Introducir Event Sourcing | Resuelve problema inexistente; coste operacional altísimo. |
| Microservicio Identity separado | El monolito modular es la decisión correcta para el tamaño actual. |
| Reemplazar MediatR por handlers manuales | MediatR no es cuello de botella; cambio puramente ideológico. |
| Migrar Result Pattern a `OneOf<TSuccess, TError>` | El Result actual funciona, está integrado, tiene `ToHttpResult()`. Cambiar es churn sin valor. |
| AutoMapper para `UserMappings` | Mappings explícitos son más fáciles de leer y debuggear. |
| Repository genérico `IRepository<TEntity, TId>` | Los repos actuales son cohesivos por agregado, así debe ser. |
| AbstractFactory para `RefreshToken` o `User` | Constructor + factory estático bastan. |
| IDataLoader / batching | No hay N+1 evidenciado en Identity. |
| Renombrar masivamente para "consistencia" estética | Introduce ruido en git blame y bloquea PRs. |
| Migrar BCrypt a Argon2 sin métrica de seguridad | Cambio costoso (rotación de hashes en login) sin beneficio cuantificado en este proyecto. |
| Implementar IdentityServer / OpenIddict | Para el flujo actual basta JWT propio. Migrar duplica complejidad. |

---

## 9. Métricas de calidad sugeridas

**Baseline a medir HOY (antes de empezar Fase 1):**

| Métrica | Herramienta | Baseline hoy | Objetivo post-refactor |
|---|---|---|---|
| Líneas por archivo (max en Identity) | `cloc` / manual | 554 (`User.cs`) | < 350 |
| Líneas por método (max) | SonarLint / Roslyn | ~100 (`OAuthLoginCommandHandler.Handle`) | < 40 |
| Complejidad ciclomática (max) | SonarLint | estimado > 15 | < 10 |
| Métodos públicos en `User` aggregate | manual | ~35 | ~22 |
| Cobertura tests Domain/Identity | `coverlet` + `coverage.runsettings` | ~85% | ≥ 90% |
| Cobertura tests Application/Identity | `coverlet` | ~70% | ≥ 85% |
| Cobertura tests Infrastructure/Identity | `coverlet` | ~30% | ≥ 60% |
| Mutation score (Stryker) Domain/Identity | Stryker.NET (`stryker-config.json`) | sin medir | > 70% |
| Comentarios no-doc en módulo | regex `// ` excluyendo `///` | varios | 0 (regla obligatoria) |
| Archivos con > 1 clase pública | analizador custom / regex | ≥ 1 (`OAuthEndpoints.cs`) | 0 (regla obligatoria) |
| Warnings de compilación módulo | `dotnet build` | 0 | 0 (mantener) |
| Statements de logging por handler | manual | 18+ (`OAuthLoginCommandHandler`) | ≤ 5 |
| Tiempo de ejecución de tests Identity | `dotnet test` | a medir | sin regresión > 10% |
| Acoplamiento (Ce + Ca) en User | NDepend / manual | a medir | reducir 20% |

Configurar el CI para fallar PR si:
- aparece `// ` no `///` en archivos del módulo Identity.
- aparece más de un `class` o `record` público por archivo.
- cobertura baja respecto al baseline.
- nuevos warnings de compilación.

---

## 10. Resultado final esperado

Tras Fase 1 y 2 ejecutadas:

```
Domain/Identity/
├── User.cs                                  (~350 líneas, antes 554)
├── UserId.cs
├── IUserRepository.cs
├── IUserUnitOfWork.cs
├── Errors/                                  (NUEVO — split de UserErrors.cs)
│   ├── UserAuthErrors.cs
│   ├── UserPasswordErrors.cs
│   ├── UserEmailErrors.cs
│   ├── UserTwoFactorErrors.cs
│   ├── UserAccountErrors.cs
│   └── UserOAuthErrors.cs
├── Enumerations/
│   ├── Role.cs
│   └── ExternalProvider.cs
├── ValueObjects/
│   ├── Email.cs
│   ├── Username.cs
│   ├── Password.cs
│   ├── PasswordHash.cs
│   ├── RefreshToken.cs
│   ├── EmailVerification.cs
│   ├── PasswordReset.cs
│   ├── TwoFactorAuth.cs                     (con lógica de setup/verify/recovery)
│   ├── TwoFactorSecret.cs
│   ├── AccountLockout.cs
│   └── LoginAttemptTracker.cs               (NUEVO — extraído de User)
├── Entities/
│   ├── ExternalLogin.cs
│   └── TwoFactorCode.cs
├── Events/                                  (16 archivos, sin cambios estructurales)
└── Validators/                              (consolidados, una responsabilidad por archivo)

Application/Identity/
├── Abstractions/                            (NUEVO)
│   ├── IExternalIdentityProvider.cs
│   ├── IClaimsFactory.cs
│   ├── IUsernameSuggester.cs
│   └── IGitHubCodeExchanger.cs
├── DTOs/
│   ├── UserDto.cs
│   ├── AuthTokensDto.cs
│   ├── LoginResultDto.cs
│   ├── TwoFactorSetupDto.cs
│   ├── ExternalLoginDto.cs
│   └── ExternalIdentity.cs                  (NUEVO)
├── Commands/
│   ├── Login/
│   ├── OAuthLogin/
│   │   └── OAuthLoginCommandHandler.cs      (~80 líneas, antes 256)
│   ├── ExchangeGitHubCode/                  (NUEVO)
│   ├── Register/
│   ├── RefreshToken/
│   ├── Logout/
│   ├── ChangePassword/
│   ├── EnableTwoFactor/
│   ├── DisableTwoFactor/
│   ├── SetupTwoFactor/
│   ├── ConfirmTwoFactorSetup/
│   ├── RequestTwoFactorCode/
│   ├── VerifyEmail/
│   ├── RequestPasswordReset/
│   ├── ResetPassword/
│   ├── LinkExternalLogin/
│   ├── UnlinkExternalLogin/
│   ├── DeactivateAccount/
│   └── DeleteAccount/
├── Queries/
│   ├── GetCurrentUser/
│   ├── GetUserById/
│   └── GetUserExternalLogins/
├── Services/
│   ├── UserAuthenticationValidator.cs
│   ├── UserClaimsFactory.cs                 (NUEVO)
│   ├── UsernameSuggester.cs                 (NUEVO)
│   └── OAuth/                               (NUEVO)
│       ├── ExternalIdentityResolver.cs
│       ├── UserOAuthProvisioner.cs
│       └── SessionIssuer.cs
├── EventHandlers/
│   ├── SendVerificationEmailOnUserRegisteredHandler.cs
│   ├── SendPasswordResetEmailHandler.cs
│   └── SendTwoFactorCodeEmailHandler.cs
└── Mappings/
    └── UserMappings.cs

Infrastructure/Identity/
├── Configuration/
│   ├── EmailSettings.cs
│   ├── JwtSettings.cs
│   ├── OAuthSettings.cs
│   ├── LockoutSettings.cs
│   └── TwoFactorSettings.cs
├── Persistence/
│   ├── Context/
│   │   └── UserContext.cs
│   ├── Configurations/
│   │   └── UserConfiguration.cs
│   ├── Repositories/
│   │   ├── UserRepository.cs                (~150 líneas, EF Core puro, antes 274)
│   │   └── UserUnitOfWork.cs
│   └── Migrations/
└── Services/
    ├── JwtTokenGenerator.cs                 (solo firma, claims via IClaimsFactory)
    ├── PasswordHasher.cs
    ├── EmailService.cs
    ├── CurrentUserService.cs
    ├── TwoFactorAuthenticator.cs            (catch tipados)
    ├── GitHubCodeExchanger.cs               (NUEVO — Infra)
    └── OAuth/
        ├── IOAuthProviderValidator.cs       (renombrar a IExternalIdentityProvider en Application)
        ├── OAuthTokenValidator.cs           (orchestrator/strategy resolver)
        ├── GoogleExternalIdentityProvider.cs (renombrado)
        └── GitHubExternalIdentityProvider.cs (renombrado)

API/Endpoints/Identity/
├── AuthEndpoints.cs
├── OAuthEndpoints.cs                        (sin DTOs inline, sin IConfiguration, sin HttpClient)
└── UserEndpoints.cs

API/Contracts/Identity/
├── Requests/                                (sin cambios)
├── Responses/                               (sin cambios)
└── OAuth/                                   (NUEVO)
    ├── GitHubCodeExchangeRequest.cs
    ├── GitHubTokenResponse.cs
    └── GitHubOAuthResponse.cs
```

### 10.1 Beneficios concretos esperados

- `User.cs` baja de 554 → ~350 líneas; métodos < 40 líneas.
- `OAuthLoginCommandHandler` baja de 256 → ~80 líneas; complejidad ciclomática < 10.
- `UserRepository` baja de 274 → ~150 líneas; un único modelo de acceso a datos.
- 0 comentarios no-doc en el módulo (regla obligatoria).
- 0 archivos con clases múltiples (regla obligatoria).
- Cierre del leak de email enumeration en login.
- Logging reducido a 3–4 puntos clave por handler.
- Posibilidad real de añadir nuevos proveedores OAuth (Microsoft, Apple) implementando `IExternalIdentityProvider` sin tocar handlers.
- Política de claims JWT desacoplada de firma; cambio de claims no toca Infrastructure.
- Mutation score Domain/Identity > 70%.
- Cobertura Application/Identity ≥ 85%.

### 10.2 Lo que NO va a cambiar (deliberadamente)

- Result Pattern, MediatR, Clean Architecture base, layout de carpetas a nivel de capa, smart enumerations, value objects existentes que ya funcionan.
- Nombres de endpoints públicos, contratos JSON externos, formato JWT (mismos claims).
- Stack tecnológico (EF Core, PostgreSQL, JWT con HMAC-SHA256, BCrypt, Otp.NET).
- Comportamiento observable desde el cliente: cualquier flujo (login, register, refresh, password reset, email verify, 2FA setup/verify, OAuth Google/GitHub, link/unlink) debe responder igual antes y después.

---

## 11. Suposiciones y preguntas abiertas

### 11.1 Suposiciones explícitas
- `dotnet test` está verde antes de comenzar. Si no, ejecutar primero las correcciones necesarias.
- No hay alertas de monitorización activas que dependan literalmente de los mensajes de log que se reducirán en 1.7 o que dependan de la diferencia de log en login (1.3). **Verificar antes**.
- La BD de producción usa el mapeo EF actual; cambios en mapeo (Tarea 2.2 LoginAttemptTracker, Tarea 2.7 promoción de RefreshToken a entidad) requieren migración EF + plan de despliegue cuidadoso.
- No hay clientes externos consumiendo claims JWT con dependencia de orden o presencia condicional de claims no documentados.

### 11.2 Preguntas pendientes (sugeridas para confirmar antes de Fase 2)
- ¿Hay clientes que consumen `email_verified` o `name` con expectativa de presencia? Afecta a Tarea 2.4 (snapshot de claims).
- ¿Existe contrato externo OpenAPI generado que se utilice como fuente de verdad? Si sí, validar tras cada tarea.
- ¿Refresh tokens tienen rotación hoy? Confirmado: parece que **NO** — anotado como deuda en §3.8 / Fase 3.2.
- ¿Hay límite de cuántos refresh tokens por usuario se mantienen? Si no, evaluar `MaxActiveRefreshTokens` config.
- ¿El email verification token y password reset token son hashables (almacenar hash en BD en vez del valor) para mitigar exposición en backup? — fuera del alcance pero a anotar.

---

## 12. Próximo paso

Confirmar la aprobación del plan. Una vez aprobado, ejecutar **Fase 1 — Tarea 1.3 (cierre de email enumeration leak)** primero por su carácter de seguridad, seguida del resto de Fase 1 en el orden indicado en §7. Cada tarea es un commit atómico con `dotnet test` y `dotnet build` (0 warnings) verdes.
