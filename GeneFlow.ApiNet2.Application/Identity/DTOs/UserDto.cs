namespace GeneFlow.ApiNet2.Application.Identity.DTOs;

/// <summary>
/// Data transfer object for user information.
/// </summary>
public sealed record UserDto
{
    /// <summary>Gets the user's unique identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the user's email address.</summary>
    public required string Email { get; init; }

    /// <summary>Gets the user's username.</summary>
    public required string Username { get; init; }

    /// <summary>Gets whether the user's email is verified.</summary>
    public required bool EmailVerified { get; init; }

    /// <summary>Gets whether the user account is active.</summary>
    public required bool IsActive { get; init; }

    /// <summary>Gets whether two-factor authentication is enabled.</summary>
    public required bool TwoFactorEnabled { get; init; }

    /// <summary>Gets whether the user has a password set (false for OAuth-only accounts).</summary>
    public required bool HasPassword { get; init; }

    /// <summary>Gets the user's roles.</summary>
    public required IReadOnlyList<string> Roles { get; init; }

    /// <summary>Gets when the user was created.</summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>Gets when the user was last modified.</summary>
    public DateTime? ModifiedAt { get; init; }
}
