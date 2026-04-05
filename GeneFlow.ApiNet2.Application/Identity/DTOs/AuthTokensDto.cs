namespace GeneFlow.ApiNet2.Application.Identity.DTOs;

/// <summary>
/// Data transfer object for authentication tokens.
/// </summary>
public sealed record AuthTokensDto
{
    /// <summary>Gets the JWT access token.</summary>
    public required string AccessToken { get; init; }

    /// <summary>Gets the refresh token.</summary>
    public required string RefreshToken { get; init; }

    /// <summary>Gets when the access token expires.</summary>
    public required DateTime ExpiresAt { get; init; }
}
