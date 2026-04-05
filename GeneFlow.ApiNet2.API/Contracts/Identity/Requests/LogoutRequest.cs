using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Identity.Requests;

/// <summary>
/// Request model for user logout.
/// </summary>
public sealed record LogoutRequest
{
    /// <summary>The refresh token to revoke.</summary>
    [Required]
    public required string RefreshToken { get; init; }
}
