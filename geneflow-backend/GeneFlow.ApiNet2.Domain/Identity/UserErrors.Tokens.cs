using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Identity;

/// <summary>
/// Token related domain errors (refresh tokens, password reset tokens,
/// email verification tokens). Partial of <see cref="UserErrors"/>.
/// </summary>
public static partial class UserErrors
{
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

    /// <summary>
    /// Refresh token not found. Deliberately Unauthorized (not NotFound) so an
    /// unknown token is indistinguishable from an expired or revoked one.
    /// </summary>
    public static readonly Error RefreshTokenNotFound = Error.Unauthorized(
        "User.RefreshTokenNotFound", "Refresh token is invalid.");

    /// <summary>Refresh token has expired.</summary>
    public static readonly Error RefreshTokenExpired = Error.Unauthorized(
        "User.RefreshTokenExpired", "Refresh token has expired.");

    /// <summary>Refresh token has been revoked.</summary>
    public static readonly Error RefreshTokenRevoked = Error.Unauthorized(
        "User.RefreshTokenRevoked", "Refresh token has been revoked.");

    /// <summary>Invalid email verification token.</summary>
    public static readonly Error InvalidVerificationToken = Error.Validation(
        "User.InvalidVerificationToken", "Email verification token is invalid or expired.");
}
