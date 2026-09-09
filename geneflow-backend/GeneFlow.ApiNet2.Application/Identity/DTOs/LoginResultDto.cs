namespace GeneFlow.ApiNet2.Application.Identity.DTOs;

/// <summary>
/// Data transfer object for login result.
/// </summary>
public sealed record LoginResultDto
{
    /// <summary>Gets the authenticated user.</summary>
    public required UserDto User { get; init; }

    /// <summary>Gets the authentication tokens.</summary>
    public required AuthTokensDto Tokens { get; init; }

    /// <summary>Gets whether two-factor verification is required.</summary>
    public required bool RequiresTwoFactor { get; init; }
}
