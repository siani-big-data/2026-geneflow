using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Identity.Requests;

/// <summary>
/// Request model for confirming TOTP 2FA setup.
/// The secret is retrieved from server-side cache for security.
/// </summary>
public sealed record ConfirmTwoFactorSetupRequest
{
    /// <summary>The 6-digit code from the authenticator app.</summary>
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public required string Code { get; init; }
}
