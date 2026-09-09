using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Identity;

/// <summary>
/// Two-factor authentication related domain errors. Partial of <see cref="UserErrors"/>.
/// </summary>
public static partial class UserErrors
{
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
}
