using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Identity.Requests;

/// <summary>
/// Request model for resetting password.
/// </summary>
public sealed record ResetPasswordRequest
{
    /// <summary>The password reset token.</summary>
    [Required]
    public required string Token { get; init; }

    /// <summary>The new password.</summary>
    [Required]
    [StringLength(100, MinimumLength = 8)]
    public required string NewPassword { get; init; }
}
