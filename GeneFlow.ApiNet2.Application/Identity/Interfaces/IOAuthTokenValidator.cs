using GeneFlow.ApiNet2.Domain.Identity.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Interfaces;

/// <summary>
/// Service for validating OAuth tokens from external providers.
/// </summary>
public interface IOAuthTokenValidator
{
    /// <summary>
    /// Validates an OAuth token and retrieves user information.
    /// </summary>
    /// <param name="provider">The OAuth provider.</param>
    /// <param name="token">The access token or ID token from the provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the validated OAuth user info.</returns>
    Task<Result<OAuthUserInfo>> ValidateTokenAsync(
        ExternalProvider provider,
        string token,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// User information extracted from OAuth provider.
/// </summary>
public sealed record OAuthUserInfo
{
    /// <summary>The unique identifier from the provider.</summary>
    public required string ProviderKey { get; init; }

    /// <summary>The user's email address.</summary>
    public required string Email { get; init; }

    /// <summary>The user's display name or full name.</summary>
    public string? DisplayName { get; init; }

    /// <summary>The user's avatar/profile picture URL.</summary>
    public string? AvatarUrl { get; init; }
}
