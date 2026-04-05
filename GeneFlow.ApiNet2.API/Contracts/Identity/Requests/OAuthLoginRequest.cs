using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Identity.Requests;

/// <summary>
/// Request model for OAuth login.
/// </summary>
public sealed record OAuthLoginRequest
{
    /// <summary>The access token or ID token from the OAuth provider.</summary>
    [Required]
    public required string Token { get; init; }
}
