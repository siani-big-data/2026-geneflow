using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Identity.Requests;

/// <summary>
/// Request model for email verification.
/// </summary>
public sealed record VerifyEmailRequest
{
    /// <summary>The verification token.</summary>
    [Required]
    public required string Token { get; init; }
}
