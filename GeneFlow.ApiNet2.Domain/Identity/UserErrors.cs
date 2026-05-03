using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Identity;

/// <summary>
/// Domain errors for the Identity context. Errors are split across partials by
/// subdomain to keep each file focused and within the project size guideline.
/// This file groups credential, identity and account-state errors. Other partials:
/// <see cref="UserErrors"/> in <c>UserErrors.TwoFactor.cs</c>,
/// <c>UserErrors.Tokens.cs</c> and <c>UserErrors.External.cs</c>.
/// </summary>
public static partial class UserErrors
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

    /// <summary>Email send failed.</summary>
    public static readonly Error EmailSendFailed = Error.Failure(
        "User.EmailSendFailed", "Failed to send email. Please try again later.");
}
