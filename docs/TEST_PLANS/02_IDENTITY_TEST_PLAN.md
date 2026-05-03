# Plan de Tests: Módulo Identity

## Cobertura Actual: ~25%
## Tests Estimados: ~345 tests

### Objetivos de Cobertura por Capa

| Capa | Archivos | Line | Branch | Method |
|------|----------|------|--------|--------|
| **Domain** | User.cs, Entities/*, ValueObjects/*, Validators/* | **≥95%** | **≥90%** | **≥98%** |
| **Application** | Commands/*, Queries/* | **≥90%** | **≥85%** | **≥95%** |
| **Infrastructure** | Repositories/*, Services/* | **≥75%** | **≥70%** | **≥85%** |
| **API** | Endpoints/* | **≥85%** | **≥80%** | **≥90%** |

### Objetivos Específicos por Archivo

| Archivo | Line | Branch | Prioridad |
|---------|------|--------|-----------|
| `User.cs` (554 LOC) | 95% | 92% | P0 |
| `TwoFactorCode.cs` (93 LOC) | 95% | 90% | P0 |
| `ExternalLogin.cs` (65 LOC) | 95% | 90% | P1 |
| `Email.cs` (43 LOC) | 100% | 100% | P1 |
| `Username.cs` (46 LOC) | 100% | 100% | P1 |
| `Password.cs` (47 LOC) | 100% | 100% | P0 |
| `PasswordHash.cs` (50 LOC) | 100% | 100% | P1 |
| `TwoFactorAuth.cs` (164 LOC) | 95% | 92% | P0 |
| `TwoFactorSecret.cs` (54 LOC) | 98% | 95% | P0 |
| `EmailVerification.cs` (92 LOC) | 95% | 90% | P0 |
| `PasswordReset.cs` (73 LOC) | 95% | 90% | P0 |
| `RefreshToken.cs` (102 LOC) | 95% | 90% | P0 |
| `AccountLockout.cs` (65 LOC) | 95% | 90% | P1 |
| `PasswordValidator.cs` (49 LOC) | 100% | 100% | P0 |
| Command Handlers (OAuth, 2FA) | 90% | 85% | P0 |
| `AuthEndpoints.cs` | 85% | 80% | P0 |
| `OAuthEndpoints.cs` | 85% | 80% | P0 |

---

## 1. ANÁLISIS DEL MÓDULO

### 1.1 Archivos de Dominio (2,161 LOC)

| Archivo | LOC | Tests Existentes | Faltantes |
|---------|-----|------------------|-----------|
| `User.cs` | 554 | ~15 | ~25 |
| `UserErrors.cs` | 197 | 0 | 0 (no requiere) |
| `UserId.cs` | 40 | ~5 | 0 |
| `Entities/TwoFactorCode.cs` | 93 | 0 | 8 |
| `Entities/ExternalLogin.cs` | 65 | 0 | 6 |
| `ValueObjects/Email.cs` | 43 | ~5 | 0 |
| `ValueObjects/Username.cs` | 46 | ~5 | 0 |
| `ValueObjects/PasswordHash.cs` | 50 | ~5 | 0 |
| `ValueObjects/RefreshToken.cs` | 102 | ~5 | 3 |
| `ValueObjects/AccountLockout.cs` | 65 | ~5 | 3 |
| `ValueObjects/Password.cs` | 47 | 0 | 8 |
| `ValueObjects/EmailVerification.cs` | 92 | 0 | 10 |
| `ValueObjects/PasswordReset.cs` | 73 | 0 | 8 |
| `ValueObjects/TwoFactorAuth.cs` | 164 | 0 | 12 |
| `ValueObjects/TwoFactorSecret.cs` | 54 | 0 | 6 |
| `Validators/PasswordValidator.cs` | 49 | 0 | 10 |
| `Validators/EmailValidator.cs` | 37 | 0 | 6 |
| `Validators/UsernameValidator.cs` | 40 | 0 | 6 |
| Enumerations (2) | 42 | 0 | 8 |
| Events (13) | ~140 | 0 | 0 (no requiere) |

### 1.2 Archivos de Aplicación (~1,100 LOC)

**Commands Existentes con Tests:**
- LoginCommandHandler ✓
- RegisterUserCommandHandler ✓
- RefreshTokenCommandHandler ✓
- ResetPasswordCommandHandler ✓
- VerifyEmailCommandHandler ✓

**Commands SIN Tests:**
| Handler | LOC | Prioridad |
|---------|-----|-----------|
| `ChangePasswordCommandHandler` | ~50 | P0 |
| `DeleteAccountCommandHandler` | ~60 | P0 |
| `DeactivateAccountCommandHandler` | ~40 | P1 |
| `SetupTwoFactorCommandHandler` | ~80 | P0 |
| `ConfirmTwoFactorSetupCommandHandler` | ~70 | P0 |
| `DisableTwoFactorCommandHandler` | ~50 | P1 |
| `ValidateTwoFactorCodeCommandHandler` | ~50 | P0 |
| `GenerateRecoveryCodesCommandHandler` | ~60 | P1 |
| `ForgotPasswordCommandHandler` | ~50 | P1 |
| `UnlockAccountCommandHandler` | ~40 | P2 |

**Queries SIN Tests:**
| Handler | LOC | Prioridad |
|---------|-----|-----------|
| `GetTwoFactorStatusQueryHandler` | ~30 | P1 |
| `IsEmailAvailableQueryHandler` | ~25 | P2 |
| `IsUsernameAvailableQueryHandler` | ~25 | P2 |

**OAuth Handlers SIN Tests:**
| Handler | LOC | Prioridad |
|---------|-----|-----------|
| `OAuthLoginCommandHandler` | ~100 | P0 |
| `OAuthLinkAccountCommandHandler` | ~80 | P1 |
| `OAuthUnlinkAccountCommandHandler` | ~50 | P1 |

### 1.3 Archivos de API

| Endpoint | Tests Existentes | Faltantes |
|----------|------------------|-----------|
| `AuthEndpoints.cs` | ~5 | ~15 |
| `UserEndpoints.cs` | ~3 | ~12 |
| `OAuthEndpoints.cs` | 0 | ~10 |

---

## 2. TESTS EXISTENTES

### Domain Tests
- `UserTests.cs` - ~15 tests
- `UserIdTests.cs` - ~5 tests
- `EmailTests.cs` - ~5 tests
- `UsernameTests.cs` - ~5 tests
- `PasswordHashTests.cs` - ~5 tests
- `RefreshTokenTests.cs` - ~5 tests
- `AccountLockoutTests.cs` - ~5 tests

### Handler Tests
- `LoginCommandHandlerTests.cs` - ~6 tests
- `RegisterUserCommandHandlerTests.cs` - ~6 tests
- `RefreshTokenCommandHandlerTests.cs` - ~5 tests
- `ResetPasswordCommandHandlerTests.cs` - ~5 tests
- `VerifyEmailCommandHandlerTests.cs` - ~5 tests
- `GetCurrentUserQueryHandlerTests.cs` - ~4 tests
- `GetUserByIdQueryHandlerTests.cs` - ~4 tests
- `UserAuthenticationValidatorTests.cs` - ~8 tests

### API Tests
- `AuthEndpointsTests.cs` - ~5 tests
- `UserEndpointsTests.cs` - ~3 tests

**Total Existentes: ~91 tests**

---

## 3. TESTS UNITARIOS DE DOMINIO FALTANTES

### 3.1 UserTests.cs - Tests Adicionales (25 tests)

| # | Test | Descripción |
|---|------|-------------|
| 1 | `SetupTwoFactor_ShouldGenerateSecret` | Configurar 2FA |
| 2 | `ConfirmTwoFactor_ValidCode_ShouldEnableTwoFactor` | Confirmar 2FA |
| 3 | `ConfirmTwoFactor_InvalidCode_ShouldReturnError` | Código inválido |
| 4 | `DisableTwoFactor_WhenEnabled_ShouldDisable` | Deshabilitar 2FA |
| 5 | `DisableTwoFactor_WhenNotEnabled_ShouldReturnError` | Ya deshabilitado |
| 6 | `ValidateTwoFactorCode_ValidCode_ShouldReturnTrue` | Validar código |
| 7 | `ValidateTwoFactorCode_InvalidCode_ShouldReturnFalse` | Código inválido |
| 8 | `ValidateTwoFactorCode_ExpiredCode_ShouldReturnFalse` | Código expirado |
| 9 | `GenerateRecoveryCodes_ShouldGenerateCodes` | Generar códigos |
| 10 | `UseRecoveryCode_ValidCode_ShouldInvalidateCode` | Usar código recuperación |
| 11 | `ChangePassword_ValidOldPassword_ShouldChangePassword` | Cambiar contraseña |
| 12 | `ChangePassword_InvalidOldPassword_ShouldReturnError` | Contraseña incorrecta |
| 13 | `ChangePassword_ShouldInvalidateRefreshTokens` | Invalidar tokens |
| 14 | `ChangePassword_ShouldRaisePasswordChangedEvent` | Evento de cambio |
| 15 | `LinkExternalLogin_NewProvider_ShouldLink` | Vincular OAuth |
| 16 | `LinkExternalLogin_AlreadyLinked_ShouldReturnError` | Ya vinculado |
| 17 | `UnlinkExternalLogin_Existing_ShouldUnlink` | Desvincular OAuth |
| 18 | `UnlinkExternalLogin_NotLinked_ShouldReturnError` | No vinculado |
| 19 | `UnlinkExternalLogin_OnlyLogin_ShouldReturnError` | Último método login |
| 20 | `Deactivate_ShouldSetIsActiveFalse` | Desactivar cuenta |
| 21 | `Deactivate_ShouldRaiseUserDeactivatedEvent` | Evento desactivación |
| 22 | `Reactivate_ShouldSetIsActiveTrue` | Reactivar cuenta |
| 23 | `Lock_ShouldSetLockoutEndDate` | Bloquear cuenta |
| 24 | `Unlock_ShouldClearLockout` | Desbloquear cuenta |
| 25 | `IsLockedOut_WhenLocked_ShouldReturnTrue` | Verificar bloqueo |

### 3.2 TwoFactorCodeTests.cs (8 tests)

```
Tests/Domain/Identity/Entities/TwoFactorCodeTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_ShouldGenerateCode` | Crear código |
| 2 | `Create_ShouldSetExpiresAt` | Establecer expiración |
| 3 | `IsExpired_BeforeExpiry_ShouldReturnFalse` | No expirado |
| 4 | `IsExpired_AfterExpiry_ShouldReturnTrue` | Expirado |
| 5 | `IsUsed_Default_ShouldReturnFalse` | No usado por defecto |
| 6 | `MarkAsUsed_ShouldSetIsUsedTrue` | Marcar como usado |
| 7 | `MarkAsUsed_ShouldSetUsedAt` | Timestamp de uso |
| 8 | `Code_ShouldBeSixDigits` | Formato de código |

### 3.3 ExternalLoginTests.cs (6 tests)

```
Tests/Domain/Identity/Entities/ExternalLoginTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_WithValidData_ShouldCreate` | Crear login externo |
| 2 | `Create_ShouldSetProvider` | Establecer proveedor |
| 3 | `Create_ShouldSetProviderKey` | Establecer key |
| 4 | `UpdateTokens_ShouldUpdateAccessToken` | Actualizar access token |
| 5 | `UpdateTokens_ShouldUpdateRefreshToken` | Actualizar refresh token |
| 6 | `Equality_SameProviderAndKey_ShouldBeEqual` | Igualdad |

### 3.4 Value Objects Faltantes

#### PasswordTests.cs (8 tests)
```
Tests/Domain/Identity/ValueObjects/PasswordTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_WithValidPassword_ShouldReturnSuccess` | Password válida |
| 2 | `Create_TooShort_ShouldReturnError` | < 8 chars |
| 3 | `Create_NoUppercase_ShouldReturnError` | Sin mayúscula |
| 4 | `Create_NoLowercase_ShouldReturnError` | Sin minúscula |
| 5 | `Create_NoDigit_ShouldReturnError` | Sin dígito |
| 6 | `Create_NoSpecialChar_ShouldReturnError` | Sin carácter especial |
| 7 | `Create_CommonPassword_ShouldReturnError` | Password común |
| 8 | `Validate_ShouldReturnAllErrors` | Múltiples errores |

#### EmailVerificationTests.cs (10 tests)
```
Tests/Domain/Identity/ValueObjects/EmailVerificationTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_ShouldGenerateToken` | Generar token |
| 2 | `Create_ShouldSetExpiresAt` | Establecer expiración |
| 3 | `Create_ShouldSetIsVerifiedFalse` | No verificado |
| 4 | `Verify_ValidToken_ShouldSetIsVerifiedTrue` | Verificar exitoso |
| 5 | `Verify_InvalidToken_ShouldReturnError` | Token inválido |
| 6 | `Verify_ExpiredToken_ShouldReturnError` | Token expirado |
| 7 | `Verify_AlreadyVerified_ShouldReturnError` | Ya verificado |
| 8 | `RegenerateToken_ShouldGenerateNewToken` | Regenerar token |
| 9 | `IsExpired_BeforeExpiry_ShouldReturnFalse` | No expirado |
| 10 | `IsExpired_AfterExpiry_ShouldReturnTrue` | Expirado |

#### PasswordResetTests.cs (8 tests)
```
Tests/Domain/Identity/ValueObjects/PasswordResetTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_ShouldGenerateToken` | Generar token |
| 2 | `Create_ShouldSetExpiresAt` | Establecer expiración |
| 3 | `IsValid_BeforeExpiry_ShouldReturnTrue` | Válido |
| 4 | `IsValid_AfterExpiry_ShouldReturnFalse` | Expirado |
| 5 | `IsValid_WhenUsed_ShouldReturnFalse` | Usado |
| 6 | `Use_ShouldMarkAsUsed` | Marcar como usado |
| 7 | `Use_ShouldSetUsedAt` | Timestamp de uso |
| 8 | `ValidateToken_CorrectToken_ShouldReturnTrue` | Validar token |

#### TwoFactorAuthTests.cs (12 tests)
```
Tests/Domain/Identity/ValueObjects/TwoFactorAuthTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Create_ShouldSetIsEnabledFalse` | Deshabilitado por defecto |
| 2 | `Setup_ShouldGenerateSecret` | Generar secreto |
| 3 | `Setup_ShouldSetIsPending` | Estado pendiente |
| 4 | `Confirm_ValidCode_ShouldEnable` | Confirmar y habilitar |
| 5 | `Confirm_InvalidCode_ShouldReturnError` | Código inválido |
| 6 | `Confirm_NotPending_ShouldReturnError` | No pendiente |
| 7 | `Disable_WhenEnabled_ShouldDisable` | Deshabilitar |
| 8 | `Disable_WhenDisabled_ShouldReturnError` | Ya deshabilitado |
| 9 | `ValidateCode_ValidCode_ShouldReturnTrue` | Validar código TOTP |
| 10 | `ValidateCode_InvalidCode_ShouldReturnFalse` | Código inválido |
| 11 | `GenerateRecoveryCodes_ShouldGenerateCodes` | Generar códigos |
| 12 | `UseRecoveryCode_ValidCode_ShouldInvalidate` | Usar código |

#### TwoFactorSecretTests.cs (6 tests)
```
Tests/Domain/Identity/ValueObjects/TwoFactorSecretTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Generate_ShouldCreateValidSecret` | Generar secreto |
| 2 | `Generate_ShouldBeBase32Encoded` | Formato Base32 |
| 3 | `GetQrCodeUri_ShouldReturnOtpauthUri` | URI QR code |
| 4 | `ValidateCode_ValidCode_ShouldReturnTrue` | Validar TOTP |
| 5 | `ValidateCode_InvalidCode_ShouldReturnFalse` | TOTP inválido |
| 6 | `Equality_SameValue_ShouldBeEqual` | Igualdad |

### 3.5 Validators Tests

#### PasswordValidatorTests.cs (10 tests)
```
Tests/Domain/Identity/Validators/PasswordValidatorTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Validate_ValidPassword_ShouldReturnEmpty` | Password válida |
| 2 | `Validate_TooShort_ShouldReturnMinLengthError` | Muy corta |
| 3 | `Validate_TooLong_ShouldReturnMaxLengthError` | Muy larga |
| 4 | `Validate_NoUppercase_ShouldReturnError` | Sin mayúscula |
| 5 | `Validate_NoLowercase_ShouldReturnError` | Sin minúscula |
| 6 | `Validate_NoDigit_ShouldReturnError` | Sin dígito |
| 7 | `Validate_NoSpecialChar_ShouldReturnError` | Sin especial |
| 8 | `Validate_CommonPassword_ShouldReturnError` | Muy común |
| 9 | `Validate_ContainsUsername_ShouldReturnError` | Contiene username |
| 10 | `Validate_MultipleErrors_ShouldReturnAll` | Múltiples errores |

#### EmailValidatorTests.cs (6 tests)
#### UsernameValidatorTests.cs (6 tests)

### 3.6 Enumerations Tests

#### RoleTests.cs (4 tests)
#### ExternalProviderTests.cs (4 tests)

---

## 4. TESTS DE HANDLERS FALTANTES

### 4.1 Command Handler Tests

#### ChangePasswordCommandHandlerTests.cs (8 tests)
```
Tests/Application/Identity/Commands/ChangePasswordCommandHandlerTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Handle_ValidData_ShouldChangePassword` | Cambiar exitosamente |
| 2 | `Handle_UserNotFound_ShouldReturnNotFoundError` | Usuario no encontrado |
| 3 | `Handle_InvalidOldPassword_ShouldReturnError` | Contraseña incorrecta |
| 4 | `Handle_WeakNewPassword_ShouldReturnValidationError` | Password débil |
| 5 | `Handle_SameAsOldPassword_ShouldReturnError` | Misma contraseña |
| 6 | `Handle_ShouldInvalidateRefreshTokens` | Invalidar tokens |
| 7 | `Handle_ShouldPersistChanges` | Persistir cambios |
| 8 | `Handle_ShouldSendEmailNotification` | Notificar por email |

#### SetupTwoFactorCommandHandlerTests.cs (6 tests)
#### ConfirmTwoFactorSetupCommandHandlerTests.cs (8 tests)
#### DisableTwoFactorCommandHandlerTests.cs (6 tests)
#### ValidateTwoFactorCodeCommandHandlerTests.cs (6 tests)
#### GenerateRecoveryCodesCommandHandlerTests.cs (5 tests)
#### ForgotPasswordCommandHandlerTests.cs (6 tests)
#### DeleteAccountCommandHandlerTests.cs (7 tests)
#### DeactivateAccountCommandHandlerTests.cs (5 tests)
#### UnlockAccountCommandHandlerTests.cs (5 tests)

#### OAuthLoginCommandHandlerTests.cs (10 tests)
```
Tests/Application/Identity/Commands/OAuthLoginCommandHandlerTests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Handle_NewUser_ShouldCreateUserAndLogin` | Nuevo usuario |
| 2 | `Handle_ExistingUser_ShouldLogin` | Usuario existente |
| 3 | `Handle_InvalidProvider_ShouldReturnError` | Proveedor inválido |
| 4 | `Handle_InvalidToken_ShouldReturnError` | Token inválido |
| 5 | `Handle_DeactivatedUser_ShouldReturnError` | Usuario desactivado |
| 6 | `Handle_LockedUser_ShouldReturnError` | Usuario bloqueado |
| 7 | `Handle_ShouldGenerateJwtToken` | Generar JWT |
| 8 | `Handle_ShouldGenerateRefreshToken` | Generar refresh token |
| 9 | `Handle_ShouldRaiseOAuthLoginEvent` | Evento de login |
| 10 | `Handle_EmailConflict_ShouldReturnError` | Conflicto de email |

#### OAuthLinkAccountCommandHandlerTests.cs (6 tests)
#### OAuthUnlinkAccountCommandHandlerTests.cs (6 tests)

### 4.2 Query Handler Tests

#### GetTwoFactorStatusQueryHandlerTests.cs (4 tests)
#### IsEmailAvailableQueryHandlerTests.cs (3 tests)
#### IsUsernameAvailableQueryHandlerTests.cs (3 tests)

---

## 5. TESTS DE INTEGRACIÓN API FALTANTES

### 5.1 AuthEndpointsTests.cs - Tests Adicionales (15 tests)

| # | Test | Endpoint | HTTP | Descripción |
|---|------|----------|------|-------------|
| 1 | `Login_With2FA_ShouldRequireCode` | POST /auth/login | 200 | Login con 2FA |
| 2 | `Login_2FACode_Valid_ShouldReturn200` | POST /auth/login/2fa | 200 | Código válido |
| 3 | `Login_2FACode_Invalid_ShouldReturn401` | POST /auth/login/2fa | 401 | Código inválido |
| 4 | `ChangePassword_ValidData_ShouldReturn200` | POST /auth/change-password | 200 | Cambiar password |
| 5 | `ChangePassword_InvalidOld_ShouldReturn400` | POST /auth/change-password | 400 | Password incorrecta |
| 6 | `ForgotPassword_ValidEmail_ShouldReturn200` | POST /auth/forgot-password | 200 | Solicitar reset |
| 7 | `ForgotPassword_InvalidEmail_ShouldReturn200` | POST /auth/forgot-password | 200 | Email no existe (no revelar) |
| 8 | `Setup2FA_ShouldReturnQrCode` | POST /auth/2fa/setup | 200 | Obtener QR |
| 9 | `Confirm2FA_ValidCode_ShouldReturn200` | POST /auth/2fa/confirm | 200 | Confirmar 2FA |
| 10 | `Disable2FA_ValidPassword_ShouldReturn200` | POST /auth/2fa/disable | 200 | Deshabilitar 2FA |
| 11 | `GetRecoveryCodes_ShouldReturn200` | GET /auth/2fa/recovery-codes | 200 | Obtener códigos |
| 12 | `RegenerateRecoveryCodes_ShouldReturn200` | POST /auth/2fa/recovery-codes | 200 | Regenerar códigos |
| 13 | `Logout_ShouldInvalidateToken` | POST /auth/logout | 200 | Cerrar sesión |
| 14 | `LogoutAll_ShouldInvalidateAllTokens` | POST /auth/logout-all | 200 | Cerrar todas |
| 15 | `DeleteAccount_ValidPassword_ShouldReturn204` | DELETE /auth/account | 204 | Eliminar cuenta |

### 5.2 UserEndpointsTests.cs - Tests Adicionales (12 tests)

| # | Test | Endpoint | HTTP | Descripción |
|---|------|----------|------|-------------|
| 1 | `UpdateEmail_ValidEmail_ShouldReturn200` | PUT /users/me/email | 200 | Actualizar email |
| 2 | `UpdateEmail_DuplicateEmail_ShouldReturn409` | PUT /users/me/email | 409 | Email duplicado |
| 3 | `UpdateUsername_ValidUsername_ShouldReturn200` | PUT /users/me/username | 200 | Actualizar username |
| 4 | `UpdateUsername_DuplicateUsername_ShouldReturn409` | PUT /users/me/username | 409 | Username duplicado |
| 5 | `Get2FAStatus_ShouldReturn200` | GET /users/me/2fa-status | 200 | Estado 2FA |
| 6 | `GetLoginHistory_ShouldReturn200` | GET /users/me/login-history | 200 | Historial |
| 7 | `GetActiveSessions_ShouldReturn200` | GET /users/me/sessions | 200 | Sesiones activas |
| 8 | `RevokeSession_ShouldReturn200` | DELETE /users/me/sessions/{id} | 200 | Revocar sesión |
| 9 | `DeactivateAccount_ShouldReturn200` | POST /users/me/deactivate | 200 | Desactivar |
| 10 | `IsEmailAvailable_Available_ShouldReturnTrue` | GET /users/check-email | 200 | Verificar disponibilidad |
| 11 | `IsEmailAvailable_Taken_ShouldReturnFalse` | GET /users/check-email | 200 | Email ocupado |
| 12 | `IsUsernameAvailable_ShouldReturn200` | GET /users/check-username | 200 | Verificar username |

### 5.3 OAuthEndpointsTests.cs (10 tests)

```
Tests/API/Identity/OAuthEndpointsTests.cs
```

| # | Test | Endpoint | HTTP | Descripción |
|---|------|----------|------|-------------|
| 1 | `GetAuthUrl_GitHub_ShouldReturn200` | GET /oauth/github/url | 200 | URL de GitHub |
| 2 | `GetAuthUrl_Google_ShouldReturn200` | GET /oauth/google/url | 200 | URL de Google |
| 3 | `Callback_ValidCode_ShouldReturn200` | POST /oauth/callback | 200 | Callback válido |
| 4 | `Callback_InvalidCode_ShouldReturn401` | POST /oauth/callback | 401 | Código inválido |
| 5 | `LinkAccount_NewProvider_ShouldReturn200` | POST /oauth/link | 200 | Vincular |
| 6 | `LinkAccount_AlreadyLinked_ShouldReturn409` | POST /oauth/link | 409 | Ya vinculado |
| 7 | `UnlinkAccount_ShouldReturn200` | DELETE /oauth/unlink/{provider} | 200 | Desvincular |
| 8 | `UnlinkAccount_OnlyLogin_ShouldReturn400` | DELETE /oauth/unlink/{provider} | 400 | Único método |
| 9 | `GetLinkedAccounts_ShouldReturn200` | GET /oauth/linked-accounts | 200 | Listar vinculados |
| 10 | `RefreshOAuthTokens_ShouldReturn200` | POST /oauth/refresh | 200 | Refrescar tokens |

---

## 6. TESTS E2E

### 6.1 AuthenticationFlowE2ETests.cs (12 tests)

```
Tests/E2E/AuthenticationFlowE2ETests.cs
```

| # | Test | Descripción |
|---|------|-------------|
| 1 | `Register_VerifyEmail_Login` | Flujo completo de registro |
| 2 | `Register_Without2FA_Login_ShouldSucceed` | Login sin 2FA |
| 3 | `Setup2FA_Login_RequireCode` | Configurar 2FA y login |
| 4 | `Login_Invalid2FACode_ShouldFail` | Código 2FA inválido |
| 5 | `Login_UseRecoveryCode_ShouldSucceed` | Usar código de recuperación |
| 6 | `ForgotPassword_ResetPassword_Login` | Flujo de reset password |
| 7 | `ChangePassword_InvalidateOldSessions` | Cambiar contraseña invalida sesiones |
| 8 | `OAuthLogin_NewUser_ShouldCreateAccount` | OAuth nuevo usuario |
| 9 | `OAuthLogin_ExistingUser_ShouldLink` | OAuth usuario existente |
| 10 | `DeleteAccount_ShouldInvalidateAllTokens` | Eliminar cuenta |
| 11 | `LockAccount_AfterFailedAttempts` | Bloqueo por intentos fallidos |
| 12 | `UnlockAccount_AfterLockoutPeriod` | Desbloqueo automático |

---

## 7. RESUMEN DE TESTS

| Categoría | Existentes | Nuevos | Total |
|-----------|------------|--------|-------|
| Domain - User | 15 | 25 | 40 |
| Domain - Entities | 0 | 14 | 14 |
| Domain - Value Objects (existentes) | 25 | 6 | 31 |
| Domain - Value Objects (nuevos) | 0 | 44 | 44 |
| Domain - Validators | 0 | 22 | 22 |
| Domain - Enumerations | 0 | 8 | 8 |
| **Subtotal Domain** | **40** | **119** | **159** |
| Handlers - Commands (existentes) | 27 | 0 | 27 |
| Handlers - Commands (nuevos) | 0 | 84 | 84 |
| Handlers - Queries | 8 | 10 | 18 |
| **Subtotal Handlers** | **35** | **94** | **129** |
| API - Endpoints | 8 | 37 | 45 |
| **Subtotal API** | **8** | **37** | **45** |
| E2E | 0 | 12 | 12 |
| **Subtotal E2E** | **0** | **12** | **12** |
| **TOTAL** | **~91** | **~262** | **~345** |

---

## 8. ARCHIVOS A CREAR

```
GeneFlow.ApiNet2.Tests/
├── Domain/
│   └── Identity/
│       ├── Entities/
│       │   ├── TwoFactorCodeTests.cs
│       │   └── ExternalLoginTests.cs
│       ├── ValueObjects/
│       │   ├── PasswordTests.cs
│       │   ├── EmailVerificationTests.cs
│       │   ├── PasswordResetTests.cs
│       │   ├── TwoFactorAuthTests.cs
│       │   └── TwoFactorSecretTests.cs
│       ├── Validators/
│       │   ├── PasswordValidatorTests.cs
│       │   ├── EmailValidatorTests.cs
│       │   └── UsernameValidatorTests.cs
│       └── Enumerations/
│           ├── RoleTests.cs
│           └── ExternalProviderTests.cs
├── Application/
│   └── Identity/
│       ├── Commands/
│       │   ├── ChangePasswordCommandHandlerTests.cs
│       │   ├── SetupTwoFactorCommandHandlerTests.cs
│       │   ├── ConfirmTwoFactorSetupCommandHandlerTests.cs
│       │   ├── DisableTwoFactorCommandHandlerTests.cs
│       │   ├── ValidateTwoFactorCodeCommandHandlerTests.cs
│       │   ├── GenerateRecoveryCodesCommandHandlerTests.cs
│       │   ├── ForgotPasswordCommandHandlerTests.cs
│       │   ├── DeleteAccountCommandHandlerTests.cs
│       │   ├── DeactivateAccountCommandHandlerTests.cs
│       │   ├── UnlockAccountCommandHandlerTests.cs
│       │   ├── OAuthLoginCommandHandlerTests.cs
│       │   ├── OAuthLinkAccountCommandHandlerTests.cs
│       │   └── OAuthUnlinkAccountCommandHandlerTests.cs
│       └── Queries/
│           ├── GetTwoFactorStatusQueryHandlerTests.cs
│           ├── IsEmailAvailableQueryHandlerTests.cs
│           └── IsUsernameAvailableQueryHandlerTests.cs
├── API/
│   └── Identity/
│       └── OAuthEndpointsTests.cs
└── E2E/
    └── AuthenticationFlowE2ETests.cs
```

---

## 9. ORDEN DE IMPLEMENTACIÓN

1. **Día 1:** PasswordTests, PasswordValidatorTests
2. **Día 2:** TwoFactorSecretTests, TwoFactorAuthTests
3. **Día 3:** EmailVerificationTests, PasswordResetTests
4. **Día 4:** TwoFactorCodeTests, ExternalLoginTests
5. **Día 5:** UserTests adicionales (2FA, OAuth)
6. **Día 6-7:** Command handlers 2FA
7. **Día 8-9:** Command handlers OAuth
8. **Día 10:** Command handlers restantes
9. **Día 11:** Query handlers, API tests adicionales
10. **Día 12:** OAuthEndpointsTests
11. **Día 13-14:** E2E tests

**Tiempo estimado:** 2.5 semanas
