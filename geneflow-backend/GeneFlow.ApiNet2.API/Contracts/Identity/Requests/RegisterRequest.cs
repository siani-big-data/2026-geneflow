using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Identity.Requests;

/// <summary>
/// Request model for user registration.
/// </summary>
public sealed record RegisterRequest
{
    /// <summary>The user's email address.</summary>
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    /// <summary>The user's chosen username.</summary>
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public required string Username { get; init; }

    /// <summary>The user's password.</summary>
    [Required]
    [StringLength(100, MinimumLength = 8)]
    public required string Password { get; init; }
}
