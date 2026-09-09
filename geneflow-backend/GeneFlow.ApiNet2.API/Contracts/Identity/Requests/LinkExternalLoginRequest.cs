using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Identity.Requests;

/// <summary>
/// Request model for linking an external OAuth login.
/// </summary>
public sealed record LinkExternalLoginRequest
{
    /// <summary>The OAuth provider name (Google, GitHub).</summary>
    [Required]
    public required string Provider { get; init; }

    /// <summary>The access token or ID token from the OAuth provider.</summary>
    [Required]
    public required string Token { get; init; }
}
