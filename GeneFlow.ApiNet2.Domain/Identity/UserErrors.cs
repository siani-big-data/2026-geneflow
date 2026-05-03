using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Identity;

/// <summary>
/// Domain errors for the Identity context.
/// </summary>
public static class UserErrors
{
    /// <summary>Email is required.</summary>
    public static readonly Error EmailRequired = Error.Validation(
        "User.EmailRequired", "Email is required.");

    /// <summary>Email exceeds maximum length.</summary>
    public static Error EmailTooLong(int maxLength) => Error.Validation(
        "User.EmailTooLong", $"Email must not exceed {maxLength} characters.");

    /// <summary>Email format is invalid.</summary>
    public static readonly Error EmailInvalidFormat = Error.Validation(
        "User.EmailInvalidFormat", "Email format is invalid.");

    /// <summary>Email already exists.</summary>
    public static readonly Error EmailAlreadyExists = Error.Conflict(
        "User.EmailAlreadyExists", "A user with this email already exists.");

    /// <summary>Email not verified.</summary>
    public static readonly Error EmailNotVerified = Error.Failure(
        "User.EmailNotVerified", "Email address has not been verified.");

    /// <summary>Username is required.</summary>
    public static readonly Error UsernameRequired = Error.Validation(
        "User.UsernameRequired", "Username is required.");

    /// <summary>Username is too short.</summary>
    public static Error UsernameTooShort(int minLength) => Error.Validation(
        "User.UsernameTooShort", $"Username must be at least {minLength} characters.");

    /// <summary>Username exceeds maximum length.</summary>
    public static Error UsernameTooLong(int maxLength) => Error.Validation(
        "User.UsernameTooLong", $"Username must not exceed {maxLength} characters.");

    /// <summary>Username contains invalid characters.</summary>
    public static readonly Error UsernameInvalidFormat = Error.Validation(
        "User.UsernameInvalidFormat", "Username must contain only alphanumeric characters.");

    /// <summary>Username already exists.</summary>
    public static readonly Error UsernameAlreadyExists = Error.Conflict(
        "User.UsernameAlreadyExists", "A user with this username already exists.");

    /// <summary>Password hash is required.</summary>
    public static readonly Error PasswordHashRequired = Error.Validation(
        "User.PasswordHashRequired", "Password hash is required.");

    /// <summary>Password is too short.</summary>
    public static Error PasswordTooShort(int minLength) => Error.Validation(
        "User.PasswordTooShort", $"Password must be at least {minLength} characters.");

    /// <summary>Password is too long.</summary>
    public static Error PasswordTooLong(int maxLength) => Error.Validation(
        "User.PasswordTooLong", $"Password must not exceed {maxLength} characters.");

    /// <summary>Password must contain an uppercase letter.</summary>
    public static readonly Error PasswordRequiresUppercase = Error.Validation(
        "User.PasswordRequiresUppercase", "Password must contain at least one uppercase letter.");

    /// <summary>Password must contain a lowercase letter.</summary>
    public static readonly Error PasswordRequiresLowercase = Error.Validation(
        "User.PasswordRequiresLowercase", "Password must contain at least one lowercase letter.");

    /// <summary>Password must contain a number.</summary>
    public static readonly Error PasswordRequiresDigit = Error.Validation(
        "User.PasswordRequiresDigit", "Password must contain at least one number.");

    /// <summary>Invalid credentials.</summary>
    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "User.InvalidCredentials", "Invalid email/username or password.");

    /// <summary>Current password is incorrect.</summary>
    public static readonly Error InvalidCurrentPassword = Error.Unauthorized(
        "User.InvalidCurrentPassword", "Current password is incorrect.");

    /// <summary>User does not have a password (OAuth-only account).</summary>
    public static readonly Error NoPasswordSet = Error.Failure(
        "User.NoPasswordSet", "This account uses external login (OAuth) and does not have a password. Please set a password first or use your external login provider.");

    /// <summary>Invalid delete confirmation text.</summary>
    public static readonly Error InvalidDeleteConfirmation = Error.Validation(
        "User.InvalidDeleteConfirmation", "Please type DELETE to confirm account deletion.");

    /// <summary>Password reset token is invalid.</summary>
    public static readonly Error InvalidPasswordResetToken = Error.Validation(
        "User.InvalidPasswordResetToken", "Password reset token is invalid or expired.");

    /// <summary>Password reset token has expired.</summary>
    public static readonly Error PasswordResetTokenExpired = Error.Validation(
        "User.PasswordResetTokenExpired", "Password reset token has expired.");

    /// <summary>Refresh token is required.</summary>
    public static readonly Error RefreshTokenRequired = Error.Validation(
        "User.RefreshTokenRequired", "Refresh token is required.");

    /// <summary>Refresh token expiry in past.</summary>
    public static readonly Error RefreshTokenExpiryInPast = Error.Validation(
        "User.RefreshTokenExpiryInPast", "Refresh token expiry must be in the future.");

    /// <summary>Refresh token not found.</summary>
    public static readonly Error RefreshTokenNotFound = Error.NotFound(
        "User.RefreshTokenNotFound", "Refresh token was not found.");

    /// <summary>Refresh token has expired.</summary>
    public static readonly Error RefreshTokenExpired = Error.Unauthorized(
        "User.RefreshTokenExpired", "Refresh token has expired.");

    /// <summary>Refresh token has been revoked.</summary>
    public static readonly Error RefreshTokenRevoked = Error.Unauthorized(
        "User.RefreshTokenRevoked", "Refresh token has been revoked.");

    /// <summary>Invalid email verification token.</summary>
    public static readonly Error InvalidVerificationToken = Error.Validation(
        "User.InvalidVerificationToken", "Email verification token is invalid or expired.");

    /// <summary>Two-factor secret is required.</summary>
    public static readonly Error TwoFactorSecretRequired = Error.Validation(
        "User.TwoFactorSecretRequired", "Two-factor secret is required.");

    /// <summary>Two-factor already enabled.</summary>
    public static readonly Error TwoFactorAlreadyEnabled = Error.Conflict(
        "User.TwoFactorAlreadyEnabled", "Two-factor authentication is already enabled.");

    /// <summary>Two-factor not enabled.</summary>
    public static readonly Error TwoFactorNotEnabled = Error.Failure(
        "User.TwoFactorNotEnabled", "Two-factor authentication is not enabled.");

    /// <summary>Invalid two-factor code.</summary>
    public static readonly Error InvalidTwoFactorCode = Error.Unauthorized(
        "User.InvalidTwoFactorCode", "Invalid two-factor authentication code.");

    /// <summary>Two-factor code required.</summary>
    public static readonly Error TwoFactorRequired = Error.Failure(
        "User.TwoFactorRequired", "Two-factor authentication code is required.");

    /// <summary>Two-factor code has expired.</summary>
    public static readonly Error TwoFactorCodeExpired = Error.Failure(
        "User.TwoFactorCodeExpired", "Two-factor authentication code has expired.");

    /// <summary>Two-factor code already used.</summary>
    public static readonly Error TwoFactorCodeAlreadyUsed = Error.Failure(
        "User.TwoFactorCodeAlreadyUsed", "Two-factor authentication code has already been used.");

    /// <summary>Two-factor setup has expired.</summary>
    public static readonly Error TwoFactorSetupExpired = Error.Failure(
        "User.TwoFactorSetupExpired", "Two-factor setup has expired. Please start the setup process again.");

    /// <summary>Email send failed.</summary>
    public static readonly Error EmailSendFailed = Error.Failure(
        "User.EmailSendFailed", "Failed to send email. Please try again later.");

    /// <summary>Account locked.</summary>
    public static readonly Error AccountLocked = Error.Failure(
        "User.AccountLocked", "Account is temporarily locked due to too many failed login attempts.");

    /// <summary>Account locked until specified time.</summary>
    public static Error AccountLockedUntil(DateTime lockoutEnd) => Error.Failure(
        "User.AccountLockedUntil", $"Account is locked until {lockoutEnd:u}.");

    /// <summary>User account deactivated.</summary>
    public static readonly Error UserDeactivated = Error.Failure(
        "User.UserDeactivated", "User account has been deactivated.");

    /// <summary>User not found.</summary>
    public static readonly Error UserNotFound = Error.NotFound(
        "User.NotFound", "User was not found.");

    /// <summary>User not found by ID.</summary>
    public static Error UserNotFoundById(string id) => Error.NotFound(
        "User.NotFoundById", $"User with ID '{id}' was not found.");

    /// <summary>User not found by email.</summary>
    public static Error UserNotFoundByEmail(string email) => Error.NotFound(
        "User.NotFoundByEmail", $"User with email '{email}' was not found.");

    /// <summary>Role already assigned.</summary>
    public static readonly Error RoleAlreadyAssigned = Error.Conflict(
        "User.RoleAlreadyAssigned", "User already has this role.");

    /// <summary>Role not assigned.</summary>
    public static readonly Error RoleNotAssigned = Error.Failure(
        "User.RoleNotAssigned", "User does not have this role.");

    /// <summary>External login already linked.</summary>
    public static readonly Error ExternalLoginAlreadyLinked = Error.Conflict(
        "User.ExternalLoginAlreadyLinked", "This external login is already linked to an account.");

    /// <summary>External login not found.</summary>
    public static readonly Error ExternalLoginNotFound = Error.NotFound(
        "User.ExternalLoginNotFound", "External login was not found.");
}
