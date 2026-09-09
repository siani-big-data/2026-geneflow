using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Identity.Requests;

/// <summary>
/// Request model for user login.
/// </summary>
public sealed record LoginRequest
{
    /// <summary>The user's email or username.</summary>
    [Required]
    public required string Identifier { get; init; }

    /// <summary>The user's password.</summary>
    [Required]
    public required string Password { get; init; }

    /// <summary>Optional two-factor authentication code.</summary>
    public string? TwoFactorCode { get; init; }
}
