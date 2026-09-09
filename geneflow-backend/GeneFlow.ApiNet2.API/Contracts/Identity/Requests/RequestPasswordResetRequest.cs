using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Identity.Requests;

/// <summary>
/// Request model for initiating password reset.
/// </summary>
public sealed record RequestPasswordResetRequest
{
    /// <summary>The user's email address.</summary>
    [Required]
    [EmailAddress]
    public required string Email { get; init; }
}
