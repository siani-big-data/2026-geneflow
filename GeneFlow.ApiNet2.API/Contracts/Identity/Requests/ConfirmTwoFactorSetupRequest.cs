using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Identity.Requests;

/// <summary>
/// Request model for confirming TOTP 2FA setup.
/// </summary>
public sealed record ConfirmTwoFactorSetupRequest
{
    /// <summary>The Base32-encoded secret from the setup step.</summary>
    [Required]
    public required string Secret { get; init; }

    /// <summary>The 6-digit code from the authenticator app.</summary>
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public required string Code { get; init; }
}
