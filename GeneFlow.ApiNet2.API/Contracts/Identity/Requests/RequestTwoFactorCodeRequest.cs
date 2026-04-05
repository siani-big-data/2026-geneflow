using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Identity.Requests;

/// <summary>
/// Request model for requesting a two-factor authentication code.
/// </summary>
public sealed record RequestTwoFactorCodeRequest
{
    /// <summary>The user's email or username.</summary>
    [Required]
    public required string EmailOrUsername { get; init; }
}
