# Módulo Identity

## 1. Propósito y Límites

### 1.1 Propósito

El módulo Identity es responsable de toda la gestión de identidad, autenticación y autorización de usuarios en el sistema GeneFlow.ApiNet2. Maneja el ciclo de vida completo del usuario desde el registro hasta la desactivación, incluyendo autenticación multi-factor (TOTP y códigos por email), gestión de sesiones con JWT + refresh tokens, OAuth con proveedores externos, y control de acceso basado en roles.

### 1.2 Límites del Bounded Context

**Dentro del alcance:**
- Registro y verificación de usuarios
- Autenticación (login/logout)
- Gestión de tokens (access + refresh)
- Autenticación de dos factores (TOTP con app authenticator + códigos por email)
- Recuperación de contraseña
- Gestión de roles de usuario
- Login con proveedores externos (Google, GitHub)
- Bloqueo de cuenta por intentos fallidos
- Vinculación/desvinculación de OAuth

**Fuera del alcance:**
- Perfiles extendidos de usuario (delegado a Profiles)
- Permisos específicos de estudios (delegado a Studies)
- Notificaciones (delegado a módulo externo)

### 1.3 Dependencias

- **Depende de**: SharedKernel (building blocks, Result pattern, CQRS, IDs)
- **Es consumido por**: Profiles (para crear perfil al registrar), Studies, Traces
- **Integra con**: Servicios de email (para verificación y 2FA por email)

---

## 2. Modelo de Dominio

### 2.1 Aggregate Root: User

El usuario es el agregado central y único del módulo. Gestiona toda la información de identidad y autenticación.

```
User (FullAuditableAggregateRoot<UserId>)
├── Id: UserId (PrefixedId, formato: U00000001)
├── Email: Email (value object)
├── Username: Username (value object)
├── PasswordHash: PasswordHash (value object)
├── IsActive: bool
│
├── EmailVerification: EmailVerification (owned value object)
│   ├── IsVerified: bool
│   ├── Token: string?
│   └── TokenExpiry: DateTime?
│
├── PasswordReset: PasswordReset (owned value object)
│   ├── Token: string?
│   └── TokenExpiry: DateTime?
│
├── TwoFactorAuth: TwoFactorAuth (owned value object)
│   ├── IsEnabled: bool
│   ├── TotpSecret: TwoFactorSecret?
│   └── Codes: List<TwoFactorCode>
│
├── Lockout: AccountLockout (owned value object)
│   ├── FailedAttempts: int
│   └── LockoutEnd: DateTime?
│
├── Roles: List<Role> (backing field, almacenado como JSON)
├── RefreshTokens: List<RefreshToken> (owned collection)
└── ExternalLogins: List<ExternalLogin> (owned collection)

Campos de Auditoría (heredados de FullAuditableAggregateRoot):
├── CreatedAt: DateTime
├── CreatedBy: string?
├── ModifiedAt: DateTime?
├── ModifiedBy: string?
├── IsDeleted: bool
├── DeletedAt: DateTime?
└── DeletedBy: string?
```

**Invariantes del agregado:**
- Email debe ser único en el sistema
- Username debe ser único en el sistema
- Un usuario puede tener múltiples roles
- Solo un refresh token activo por sesión
- Password reset token expira en 24 horas
- Email verification token expira en 24 horas
- Two-factor codes expiran en 5 minutos
- Cuenta se bloquea tras 5 intentos fallidos
- Soft delete mantiene registro para auditoría

### 2.2 Value Objects

#### Email
```csharp
public sealed class Email : ValueObject
{
    public const int MaxLength = 256;
    public string Value { get; }

    // Validaciones:
    // - No nulo ni vacío
    // - Máximo 256 caracteres
    // - Formato válido (regex)
    // - Normalizado a lowercase

    public static Result<Email> Create(string? email);
}
```

#### Username
```csharp
public sealed class Username : ValueObject
{
    public const int MinLength = 3;
    public const int MaxLength = 50;
    public string Value { get; }

    // Validaciones:
    // - No nulo ni vacío
    // - Entre 3 y 50 caracteres
    // - Solo alfanumérico (letras y números)
    // - Normalizado a lowercase

    public static Result<Username> Create(string? username);
}
```

#### PasswordHash
```csharp
public sealed class PasswordHash : ValueObject
{
    public string Value { get; }
    public bool IsPlaceholder { get; }

    // Invariantes:
    // - Nunca contiene password en texto plano
    // - IsPlaceholder true para usuarios OAuth sin password
    // - Creado solo desde hash o placeholder

    public static Result<PasswordHash> Create(string? hash);
    public static PasswordHash CreatePlaceholder();
}
```

#### EmailVerification
```csharp
public sealed class EmailVerification : ValueObject
{
    public bool IsVerified { get; }
    public string? Token { get; }
    public DateTime? TokenExpiry { get; }

    public static EmailVerification CreatePending();
    public static EmailVerification CreateVerified();
    public Result<EmailVerification> Verify(string token);
    public EmailVerification RegenerateToken();
}
```

#### PasswordReset
```csharp
public sealed class PasswordReset : ValueObject
{
    public string? Token { get; }
    public DateTime? TokenExpiry { get; }

    public static PasswordReset None { get; }
    public static PasswordReset Request();
    public Result Validate(string token);
    public PasswordReset Clear();
}
```

#### TwoFactorAuth
```csharp
public sealed class TwoFactorAuth : ValueObject
{
    public bool IsEnabled { get; }
    public TwoFactorSecret? TotpSecret { get; }
    public IReadOnlyList<TwoFactorCode> Codes { get; }
    public bool IsTotpConfigured { get; }

    public static TwoFactorAuth Disabled { get; }
    public Result<TwoFactorAuth> Enable();
    public Result<TwoFactorAuth> Disable();
    public Result<TwoFactorAuth> EnableWithTotp(TwoFactorSecret secret);
    public (TwoFactorAuth, TwoFactorCode) GenerateCode();
    public Result<TwoFactorAuth> ValidateCode(string code);
}
```

#### TwoFactorSecret
```csharp
public sealed class TwoFactorSecret : ValueObject
{
    public string EncryptedSecret { get; }
    public DateTime CreatedAt { get; }

    public static Result<TwoFactorSecret> Create(string encryptedSecret);
}
```

#### AccountLockout
```csharp
public sealed class AccountLockout : ValueObject
{
    public int FailedAttempts { get; }
    public DateTime? LockoutEnd { get; }
    public bool IsLockedOut { get; }

    public static AccountLockout None { get; }
    public (AccountLockout, bool wasLockedOut) RecordFailedAttempt();
    public AccountLockout Reset();
}
```

#### RefreshToken
```csharp
public sealed class RefreshToken : ValueObject
{
    public string Token { get; }
    public DateTime ExpiresAt { get; }
    public DateTime CreatedAt { get; }
    public bool IsRevoked { get; }
    public DateTime? RevokedAt { get; }
    public string? ReplacedByToken { get; }
    public bool IsActive { get; }

    public static RefreshToken Create(string token, DateTime expiresAt);
    public RefreshToken Revoke(string? replacedByToken = null);
}
```

### 2.3 Enumerations (Smart Enums)

#### Role
```csharp
public sealed class Role : Enumeration<Role>
{
    public static readonly Role User = new(1, "User");
    public static readonly Role Admin = new(2, "Admin");

    private Role(int id, string name) : base(id, name) { }
}
```

**Semántica de roles:**
- **User**: Rol por defecto. Puede crear estudios, subir trazas.
- **Admin**: Acceso administrativo. Gestión de usuarios.

#### ExternalProvider
```csharp
public sealed class ExternalProvider : Enumeration<ExternalProvider>
{
    public static readonly ExternalProvider Google = new(1, "Google");
    public static readonly ExternalProvider GitHub = new(2, "GitHub");

    private ExternalProvider(int id, string name) : base(id, name) { }
}
```

### 2.4 Entities (Owned)

#### ExternalLogin
```csharp
public sealed class ExternalLogin
{
    public ExternalProvider Provider { get; }
    public string ProviderKey { get; }
    public string? ProviderDisplayName { get; }
    public DateTime LinkedAt { get; }

    public static ExternalLogin Create(
        ExternalProvider provider,
        string providerKey,
        string? displayName = null);
}
```

#### TwoFactorCode
```csharp
public sealed class TwoFactorCode
{
    public string Code { get; }
    public DateTime CreatedAt { get; }
    public DateTime ExpiresAt { get; }
    public bool IsUsed { get; }
    public DateTime? UsedAt { get; }

    public bool IsValid => !IsUsed && ExpiresAt > DateTime.UtcNow;
}
```

---

## 3. Identificadores

### 3.1 UserId (Strongly-Typed ID)

```csharp
public sealed class UserId : PrefixedId<UserId>
{
    public const string SequenceName = "users";
    protected override char Prefix => 'U';
    protected override int NumericLength => 8;

    public static UserId Parse(string id);
    public static bool TryParse(string? id, out UserId? result);
    public static UserId FromSequence(long sequenceValue);
}
```

**Formato**: `U00000001`, `U00000002`, etc.

**Generación**: Via Redis sequence generator con el nombre `users`.

---

## 4. Casos de Uso

### 4.1 Commands (Operaciones de Escritura)

| Command | Descripción | Auth | Eventos |
|---------|-------------|------|---------|
| RegisterUserCommand | Registra nuevo usuario | No | UserRegisteredEvent |
| LoginCommand | Autentica usuario | No | UserLockedOutEvent (si bloquea) |
| LoginWithOAuthCommand | Login/registro via OAuth | No | UserRegisteredViaOAuthEvent |
| LogoutCommand | Cierra sesión | Sí | - |
| RefreshTokenCommand | Obtiene nuevo access token | No | - |
| RequestPasswordResetCommand | Solicita reset | No | PasswordResetRequestedEvent |
| ResetPasswordCommand | Cambia contraseña con token | No | UserPasswordChangedEvent |
| VerifyEmailCommand | Verifica email con token | No | UserEmailVerifiedEvent |
| EnableTwoFactorCommand | Activa 2FA por email | Sí | UserTwoFactorEnabledEvent |
| DisableTwoFactorCommand | Desactiva 2FA | Sí | UserTwoFactorDisabledEvent |
| SetupTwoFactorCommand | Genera secreto TOTP | Sí | - |
| ConfirmTwoFactorSetupCommand | Confirma TOTP | Sí | UserTwoFactorEnabledEvent |
| RequestTwoFactorCodeCommand | Solicita código por email | No | TwoFactorCodeGeneratedEvent |
| LinkExternalLoginCommand | Vincula OAuth | Sí | ExternalLoginLinkedEvent |
| UnlinkExternalLoginCommand | Desvincula OAuth | Sí | - |

### 4.2 Queries (Operaciones de Lectura)

| Query | Descripción | Auth |
|-------|-------------|------|
| GetCurrentUserQuery | Obtiene usuario autenticado | Sí |
| GetUserByIdQuery | Obtiene usuario por ID | Admin |
| GetUserExternalLoginsQuery | Lista OAuth vinculados | Sí |

### 4.3 Flujos Principales

#### Registro de Usuario
1. POST `/api/v1/auth/register` con email, username, password
2. Handler valida Email.Create() y Username.Create()
3. Verifica unicidad de email y username
4. Genera UserId via sequence generator
5. Hashea password con IPasswordHasher (bcrypt)
6. Crea User con factory method
7. Usuario tiene EmailVerification pendiente con token
8. Persiste via repositorio + UnitOfWork
9. UoW despacha UserRegisteredEvent
10. Event handler envía email de verificación
11. Retorna UserDto con tokens

#### Login con 2FA (TOTP)
1. POST `/api/v1/auth/login` con identifier, password, twoFactorCode
2. Busca usuario por email o username (raw SQL para performance)
3. Verifica cuenta activa y no bloqueada
4. Valida password con IPasswordHasher
5. Si 2FA habilitado y no hay código, retorna RequiresTwoFactor
6. Si 2FA habilitado, valida código TOTP o código temporal
7. Genera access token (JWT) y refresh token
8. Persiste refresh token en User
9. Retorna LoginResultDto con tokens + UserDto

#### OAuth Login (Google/GitHub)
1. POST `/api/v1/auth/oauth/{provider}` con token del proveedor
2. IOAuthTokenValidator valida token con API del proveedor
3. Obtiene email y profile info del proveedor
4. Si usuario existe con ese external login, retorna tokens
5. Si email existe pero sin OAuth vinculado, error (debe vincular)
6. Si no existe, crea usuario nuevo con email verificado
7. Retorna tokens + UserDto

---

## 5. Estructura de Carpetas

### 5.1 Domain Layer

```
GeneFlow.ApiNet2.Domain/Identity/
├── User.cs                           # Aggregate root
├── UserId.cs                         # Strongly-typed ID
├── UserErrors.cs                     # Domain errors
├── IUserRepository.cs                # Repository interface
├── IUserUnitOfWork.cs                # Unit of Work interface
├── ValueObjects/
│   ├── Email.cs
│   ├── Username.cs
│   ├── PasswordHash.cs
│   ├── EmailVerification.cs
│   ├── PasswordReset.cs
│   ├── RefreshToken.cs
│   ├── TwoFactorAuth.cs
│   ├── TwoFactorSecret.cs
│   └── AccountLockout.cs
├── Entities/
│   ├── ExternalLogin.cs
│   └── TwoFactorCode.cs
├── Enumerations/
│   ├── Role.cs
│   └── ExternalProvider.cs
├── Events/
│   ├── UserRegisteredEvent.cs
│   ├── UserRegisteredViaOAuthEvent.cs
│   ├── UserEmailVerifiedEvent.cs
│   ├── UserPasswordChangedEvent.cs
│   ├── PasswordResetRequestedEvent.cs
│   ├── UserTwoFactorEnabledEvent.cs
│   ├── UserTwoFactorDisabledEvent.cs
│   ├── TwoFactorCodeGeneratedEvent.cs
│   ├── UserLockedOutEvent.cs
│   ├── UserDeactivatedEvent.cs
│   ├── UserRoleAddedEvent.cs
│   ├── UserRoleRemovedEvent.cs
│   └── ExternalLoginLinkedEvent.cs
└── Validators/
    ├── EmailValidator.cs
    └── UsernameValidator.cs
```

### 5.2 Application Layer

```
GeneFlow.ApiNet2.Application/Identity/
├── Commands/
│   ├── Register/
│   │   ├── RegisterUserCommand.cs
│   │   └── RegisterUserCommandHandler.cs
│   ├── Login/
│   ├── LoginWithOAuth/
│   ├── Logout/
│   ├── RefreshToken/
│   ├── RequestPasswordReset/
│   ├── ResetPassword/
│   ├── VerifyEmail/
│   ├── EnableTwoFactor/
│   ├── DisableTwoFactor/
│   ├── SetupTwoFactor/
│   ├── ConfirmTwoFactorSetup/
│   ├── LinkExternalLogin/
│   └── UnlinkExternalLogin/
├── Queries/
│   ├── GetCurrentUser/
│   ├── GetUserById/
│   └── GetUserExternalLogins/
├── EventHandlers/
│   ├── UserRegisteredEventHandler.cs
│   ├── PasswordResetRequestedEventHandler.cs
│   └── TwoFactorCodeGeneratedEventHandler.cs
├── Services/
│   └── UserAuthenticationValidator.cs
├── Interfaces/
│   ├── ICurrentUserService.cs
│   ├── IPasswordHasher.cs
│   ├── IJwtTokenGenerator.cs
│   ├── ITwoFactorAuthenticator.cs
│   ├── IUserAuthenticationValidator.cs
│   ├── IEmailService.cs
│   └── IOAuthTokenValidator.cs
├── DTOs/
│   ├── UserDto.cs
│   ├── AuthTokensDto.cs
│   ├── LoginResultDto.cs
│   ├── TwoFactorSetupDto.cs
│   └── ExternalLoginDto.cs
└── Mappings/
    └── UserMappings.cs
```

### 5.3 Infrastructure Layer

```
GeneFlow.ApiNet2.Infrastructure/Identity/
├── Persistence/
│   ├── Context/
│   │   └── UserContext.cs
│   ├── Configurations/
│   │   └── UserConfiguration.cs
│   ├── Repositories/
│   │   ├── UserRepository.cs
│   │   └── UserUnitOfWork.cs
│   └── Migrations/
├── Services/
│   ├── PasswordHasher.cs
│   ├── JwtTokenGenerator.cs
│   ├── TwoFactorAuthenticator.cs
│   ├── CurrentUserService.cs
│   ├── EmailService.cs
│   └── OAuth/
│       ├── OAuthTokenValidator.cs
│       ├── GoogleTokenValidator.cs
│       └── GitHubTokenValidator.cs
└── Configuration/
    ├── JwtSettings.cs
    ├── LockoutSettings.cs
    ├── TwoFactorSettings.cs
    ├── EmailSettings.cs
    └── OAuthSettings.cs
```

### 5.4 API Layer

```
GeneFlow.ApiNet2.API/
├── Endpoints/Identity/
│   ├── AuthEndpoints.cs
│   └── UserEndpoints.cs
├── Contracts/Identity/
│   ├── Requests/
│   │   ├── RegisterRequest.cs
│   │   ├── LoginRequest.cs
│   │   ├── RefreshTokenRequest.cs
│   │   ├── RequestPasswordResetRequest.cs
│   │   ├── ResetPasswordRequest.cs
│   │   ├── VerifyEmailRequest.cs
│   │   ├── EnableTwoFactorRequest.cs
│   │   ├── DisableTwoFactorRequest.cs
│   │   ├── ConfirmTwoFactorSetupRequest.cs
│   │   ├── RequestTwoFactorCodeRequest.cs
│   │   ├── OAuthLoginRequest.cs
│   │   └── LinkExternalLoginRequest.cs
│   └── Responses/
│       ├── UserResponse.cs
│       ├── AuthTokensResponse.cs
│       ├── LoginResponse.cs
│       ├── TwoFactorSetupResponse.cs
│       └── ExternalLoginResponse.cs
└── Extensions/
    └── UserMappingExtensions.cs
```

---

## 6. Interfaces de Servicios

### 6.1 IUserRepository

```csharp
public interface IUserRepository : IRepository<User, UserId>
{
    Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default);
    Task<User?> GetByEmailStringAsync(string email, CancellationToken ct = default);
    Task<User?> GetByUsernameAsync(Username username, CancellationToken ct = default);
    Task<User?> GetByEmailOrUsernameAsync(string identifier, CancellationToken ct = default);
    Task<bool> ExistsWithEmailAsync(Email email, CancellationToken ct = default);
    Task<bool> ExistsWithUsernameAsync(Username username, CancellationToken ct = default);
    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<User?> GetByEmailVerificationTokenAsync(string token, CancellationToken ct = default);
    Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetByIdsAsync(IEnumerable<UserId> ids, CancellationToken ct = default);
    Task<User?> GetByExternalLoginAsync(ExternalProvider provider, string providerKey, CancellationToken ct = default);
}
```

### 6.2 IPasswordHasher

```csharp
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
```

**Implementación**: Utiliza BCrypt con work factor configurable.

### 6.3 IJwtTokenGenerator

```csharp
public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    TimeSpan GetAccessTokenExpiry();
    TimeSpan GetRefreshTokenExpiry();
}
```

**Claims generados:**
- `sub`: UserId (string)
- `email`: Email del usuario
- `name`: Username
- `roles`: Lista de roles (JSON array)
- `jti`: Token ID único
- `iat`: Issued at
- `exp`: Expiration

### 6.4 ITwoFactorAuthenticator

```csharp
public interface ITwoFactorAuthenticator
{
    string GenerateSecret();
    string GetQrCodeUri(string secret, string email, string issuer);
    bool ValidateTotp(string secret, string code);
    string GenerateEmailCode();
    string EncryptSecret(string secret);
    string DecryptSecret(string encryptedSecret);
}
```

### 6.5 ICurrentUserService

```csharp
public interface ICurrentUserService
{
    bool IsAuthenticated { get; }
    UserId? UserId { get; }
    string? UserEmail { get; }
    IReadOnlyList<string> UserRoles { get; }
}
```

### 6.6 IOAuthTokenValidator

```csharp
public interface IOAuthTokenValidator
{
    Task<OAuthUserInfo?> ValidateTokenAsync(
        ExternalProvider provider,
        string token,
        CancellationToken ct = default);
}

public record OAuthUserInfo(
    string ProviderKey,
    string Email,
    string? DisplayName);
```

---

## 7. API Endpoints

### 7.1 Auth Endpoints (`/api/v1/auth`)

| Método | Ruta | Request | Response | Status |
|--------|------|---------|----------|--------|
| POST | `/register` | RegisterRequest | LoginResponse | 201, 400, 409 |
| POST | `/login` | LoginRequest | LoginResponse | 200, 400, 401, 423 |
| POST | `/refresh` | RefreshTokenRequest | AuthTokensResponse | 200, 401 |
| POST | `/logout` | - | - | 204, 401 |
| POST | `/request-password-reset` | RequestPasswordResetRequest | - | 204 |
| POST | `/reset-password` | ResetPasswordRequest | - | 204, 400 |
| POST | `/2fa/request-code` | RequestTwoFactorCodeRequest | - | 204, 404 |
| POST | `/oauth/{provider}` | OAuthLoginRequest | LoginResponse | 200, 400, 401 |

### 7.2 User Endpoints (`/api/v1/users`)

| Método | Ruta | Request | Response | Status |
|--------|------|---------|----------|--------|
| GET | `/me` | - | UserResponse | 200, 401 |
| GET | `/{id}` | - | UserResponse | 200, 404 (Admin) |
| POST | `/verify-email` | VerifyEmailRequest | - | 204, 400 |
| GET | `/verify-email?token=` | - | - | 200, 400 |
| POST | `/2fa/enable` | - | - | 204, 409 |
| POST | `/2fa/disable` | - | - | 204 |
| GET | `/2fa/setup` | - | TwoFactorSetupResponse | 200, 409 |
| POST | `/2fa/confirm` | ConfirmTwoFactorSetupRequest | - | 204, 400 |
| GET | `/external-logins` | - | ExternalLoginResponse[] | 200 |
| POST | `/external-logins` | LinkExternalLoginRequest | - | 204, 409 |
| DELETE | `/external-logins/{provider}` | - | - | 204, 400, 404 |

### 7.3 Contracts

```csharp
// Requests
public sealed record RegisterRequest(
    string Email,
    string Username,
    string Password);

public sealed record LoginRequest(
    string Identifier,      // Email o Username
    string Password,
    string? TwoFactorCode);

public sealed record OAuthLoginRequest(
    string Token);          // Token del proveedor OAuth

public sealed record RefreshTokenRequest(
    string RefreshToken);

// Responses
public sealed record UserResponse
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public required string Username { get; init; }
    public required bool EmailVerified { get; init; }
    public required bool IsActive { get; init; }
    public required bool TwoFactorEnabled { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
}

public sealed record AuthTokensResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    DateTime RefreshTokenExpiry);

public sealed record LoginResponse(
    AuthTokensResponse Tokens,
    UserResponse User,
    bool RequiresTwoFactor = false);
```

---

## 8. Eventos de Dominio

### 8.1 Catálogo de Eventos

| Evento | Trigger | Handlers | Efecto |
|--------|---------|----------|--------|
| UserRegisteredEvent | User.Create() | EmailHandler | Envía email verificación |
| UserRegisteredViaOAuthEvent | User.CreateFromOAuth() | - | Logging |
| UserEmailVerifiedEvent | VerifyEmail() | - | Logging |
| UserPasswordChangedEvent | ChangePassword/Reset | - | Logging |
| PasswordResetRequestedEvent | RequestPasswordReset() | EmailHandler | Envía email reset |
| TwoFactorCodeGeneratedEvent | GenerateTwoFactorCode() | EmailHandler | Envía código por email |
| UserTwoFactorEnabledEvent | EnableTwoFactor() | - | Logging |
| UserTwoFactorDisabledEvent | DisableTwoFactor() | - | Logging |
| UserLockedOutEvent | Lockout activado | AlertHandler | Alerta de seguridad |
| ExternalLoginLinkedEvent | LinkExternalLogin() | - | Logging |

### 8.2 Estructura de Eventos

```csharp
public sealed record UserRegisteredEvent(
    UserId UserId,
    string Email,
    string Username,
    string EmailVerificationToken) : DomainEvent;

public sealed record UserRegisteredViaOAuthEvent(
    UserId UserId,
    string Email,
    string Username,
    ExternalProvider Provider,
    string ProviderKey) : DomainEvent;

public sealed record PasswordResetRequestedEvent(
    UserId UserId,
    string Email,
    string Username,
    string ResetToken) : DomainEvent;

public sealed record TwoFactorCodeGeneratedEvent(
    UserId UserId,
    string Email,
    string Username,
    string Code) : DomainEvent;

public sealed record UserLockedOutEvent(
    UserId UserId,
    DateTime LockoutEnd,
    int FailedAttempts) : DomainEvent;
```

---

## 9. Persistencia

### 9.1 DbContext

```csharp
public sealed class UserContext : DbContext
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");
        modelBuilder.Ignore<Role>(); // Smart enum como JSON
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserContext).Assembly);
    }
}
```

### 9.2 Configuración de User

La configuración usa:
- **Conversiones** para value objects (Email, Username, PasswordHash)
- **OwnsOne** para value objects complejos (EmailVerification, PasswordReset, TwoFactorAuth, Lockout)
- **OwnsMany** para colecciones owned (RefreshTokens, ExternalLogins, TwoFactorCodes)
- **JSON column** para Roles (array de strings)
- **Query filter** para soft delete

### 9.3 Esquema de Base de Datos

```sql
-- Schema: identity

CREATE TABLE identity.users (
    id VARCHAR(10) PRIMARY KEY,  -- U00000001
    email VARCHAR(256) NOT NULL UNIQUE,
    username VARCHAR(50) NOT NULL UNIQUE,
    password_hash VARCHAR(256) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,

    -- EmailVerification
    email_verified BOOLEAN NOT NULL DEFAULT false,
    email_verification_token VARCHAR(128),
    email_verification_token_expiry TIMESTAMP WITH TIME ZONE,

    -- PasswordReset
    password_reset_token VARCHAR(128),
    password_reset_token_expiry TIMESTAMP WITH TIME ZONE,

    -- TwoFactorAuth
    two_factor_enabled BOOLEAN NOT NULL DEFAULT false,
    totp_secret VARCHAR(512),
    totp_secret_created_at TIMESTAMP WITH TIME ZONE,

    -- AccountLockout
    failed_login_attempts INT NOT NULL DEFAULT 0,
    lockout_end TIMESTAMP WITH TIME ZONE,

    -- Roles (JSON array)
    roles JSONB NOT NULL DEFAULT '["User"]',

    -- Auditing
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_by VARCHAR(100),
    modified_at TIMESTAMP WITH TIME ZONE,
    modified_by VARCHAR(100),
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMP WITH TIME ZONE,
    deleted_by VARCHAR(100)
);

CREATE TABLE identity.refresh_tokens (
    token VARCHAR(256) PRIMARY KEY,
    "UserId" VARCHAR(10) NOT NULL REFERENCES identity.users(id),
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    is_revoked BOOLEAN NOT NULL DEFAULT false,
    revoked_at TIMESTAMP WITH TIME ZONE,
    replaced_by_token VARCHAR(256)
);

CREATE TABLE identity.external_logins (
    id UUID PRIMARY KEY,
    "UserId" VARCHAR(10) NOT NULL REFERENCES identity.users(id),
    provider VARCHAR(50) NOT NULL,
    provider_key VARCHAR(256) NOT NULL,
    provider_display_name VARCHAR(256),
    linked_at TIMESTAMP WITH TIME ZONE NOT NULL,
    UNIQUE(provider, provider_key)
);

CREATE TABLE identity.two_factor_codes (
    id UUID PRIMARY KEY,
    "UserId" VARCHAR(10) NOT NULL REFERENCES identity.users(id),
    code VARCHAR(6) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    is_used BOOLEAN NOT NULL DEFAULT false,
    used_at TIMESTAMP WITH TIME ZONE
);
```

---

## 10. Seguridad

### 10.1 Validaciones de Dominio

| Campo | Reglas |
|-------|--------|
| Email | No vacío, máx 256 chars, formato válido, único |
| Username | 3-50 chars, solo alfanumérico, único |
| Password | Mín 8 chars, complejidad configurable |
| RefreshToken | Único, no expirado, no revocado |
| TwoFactorCode | 6 dígitos, no expirado, no usado |
| TOTP | Código de 6 dígitos, ventana de tiempo |

### 10.2 Protecciones de Seguridad

**Contraseñas:**
- Hash con BCrypt
- Salt único por usuario
- Work factor configurable (default: 12)
- PasswordHash.IsPlaceholder para usuarios OAuth

**Tokens:**
- Access Token: JWT firmado con HS256, TTL 15 min
- Refresh Token: Criptográficamente aleatorio, TTL 7 días
- Verificación/Reset: GUID, expira en 24h
- 2FA: Código de 6 dígitos, expira en 5 min

**Bloqueo de cuenta:**
- 5 intentos fallidos activan lockout
- Lockout de 15 minutos
- Reset automático tras expiración

**JWT Security:**
- Firmado con clave simétrica (HS256)
- Validación de issuer, audience, expiration
- Claims mínimos necesarios

### 10.3 OAuth Security

- Tokens validados con APIs oficiales de providers
- No se almacena token del provider
- Solo se guarda provider + providerKey
- Email del provider se considera verificado

---

## 11. Configuración

### 11.1 JwtSettings

```csharp
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "GeneFlow";
    public string Audience { get; set; } = "GeneFlow";
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
```

### 11.2 TwoFactorSettings

```csharp
public class TwoFactorSettings
{
    public const string SectionName = "TwoFactor";

    public int CodeExpirationMinutes { get; set; } = 5;
    public string EncryptionKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "GeneFlow";
}
```

### 11.3 OAuthSettings

```csharp
public class OAuthSettings
{
    public const string SectionName = "OAuth";

    public GoogleOAuthSettings Google { get; set; } = new();
    public GitHubOAuthSettings GitHub { get; set; } = new();
}
```

---

## 12. Decisiones de Diseño

### 12.1 PrefixedId en lugar de GUID

**Decisión**: UserId usa formato `U00000001` en lugar de GUID.

**Justificación**:
- IDs más legibles en logs y debugging
- Más cortos en URLs
- Secuencia predecible para monitoreo
- Prefix identifica el tipo de entidad

**Trade-off**:
- Requiere Redis para secuencias
- No es globalmente único sin contexto

### 12.2 Value Objects Owned con Métodos de Negocio

**Decisión**: Value objects como EmailVerification tienen métodos de mutación que retornan nuevas instancias.

**Justificación**:
- Mantiene inmutabilidad
- Encapsula lógica de negocio
- Validación en un solo lugar

### 12.3 Roles como JSON Column

**Decisión**: Roles se almacenan como JSON array en columna.

**Justificación**:
- Evita tabla de unión para relación simple
- Lectura en una sola query
- Smart enum manejado en memoria

**Trade-off**:
- No se puede hacer JOIN por rol eficientemente
- Requiere conversión JSON en EF Core

### 12.4 Raw SQL para Búsquedas por Email/Username

**Decisión**: UserRepository usa ADO.NET raw SQL para búsquedas.

**Justificación**:
- EF Core tiene problemas con value object conversions en WHERE
- Performance crítica en login
- Búsqueda case-insensitive con LOWER()

---

## 13. Integración con Otros Módulos

### 13.1 Profiles

El módulo Profiles escucha `UserRegisteredEvent` para crear automáticamente un Profile cuando se registra un usuario:

```csharp
public sealed class CreateProfileOnUserRegisteredHandler
    : IDomainEventHandler<UserRegisteredEvent>
{
    public async Task Handle(UserRegisteredEvent notification, CancellationToken ct)
    {
        // Crea Profile con FirstName = Username
        // ProfileId usa mismo valor numérico que UserId
    }
}
```

### 13.2 Studies/Traces

- ICurrentUserService proporciona UserId para autorización
- Roles verificados via claims en JWT
- No hay acoplamiento directo

---

*Documentación del Módulo Identity - GeneFlow.ApiNet2*
*Última actualización: Abril 2026*
