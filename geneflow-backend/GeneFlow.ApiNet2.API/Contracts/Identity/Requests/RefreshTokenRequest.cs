using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Identity.Requests;

/// <summary>
/// Request model for refreshing access tokens.
/// </summary>
public sealed record RefreshTokenRequest
{
    /// <summary>The refresh token to use.</summary>
    [Required]
    public required string RefreshToken { get; init; }
}
