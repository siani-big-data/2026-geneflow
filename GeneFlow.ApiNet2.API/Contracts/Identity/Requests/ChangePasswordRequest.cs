using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Identity.Requests;

/// <summary>
/// Request model for changing password (authenticated user).
/// </summary>
public sealed record ChangePasswordRequest
{
    /// <summary>The current password for verification.</summary>
    [Required]
    public required string CurrentPassword { get; init; }

    /// <summary>The new password.</summary>
    [Required]
    [StringLength(100, MinimumLength = 8)]
    public required string NewPassword { get; init; }
}
