namespace GeneFlow.ApiNet2.Application.Identity.DTOs;

/// <summary>
/// Data transfer object for external login information.
/// </summary>
public sealed record ExternalLoginDto
{
    /// <summary>Gets the OAuth provider name.</summary>
    public required string Provider { get; init; }

    /// <summary>Gets the display name from the provider, if available.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Gets when the external login was linked.</summary>
    public required DateTime LinkedAt { get; init; }
}
