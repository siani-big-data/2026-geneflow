namespace GeneFlow.ApiNet2.API.Contracts.Identity.Responses;

/// <summary>
/// Response model for external login information.
/// </summary>
public sealed record ExternalLoginResponse
{
    /// <summary>Gets the OAuth provider name.</summary>
    public required string Provider { get; init; }

    /// <summary>Gets the display name from the provider, if available.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Gets when the external login was linked.</summary>
    public required DateTime LinkedAt { get; init; }
}
