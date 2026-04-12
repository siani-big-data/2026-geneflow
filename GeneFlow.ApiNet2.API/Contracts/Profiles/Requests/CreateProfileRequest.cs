using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Profiles.Requests;

/// <summary>
/// Request model for creating a new profile.
/// </summary>
public sealed record CreateProfileRequest
{
    /// <summary>The user's first name.</summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string FirstName { get; init; }

    /// <summary>The user's last name (optional).</summary>
    [StringLength(100)]
    public string? LastName { get; init; }

    /// <summary>The user's bio (optional, max 500 characters).</summary>
    [StringLength(500)]
    public string? Bio { get; init; }

    /// <summary>The user's location (optional).</summary>
    [StringLength(200)]
    public string? Location { get; init; }

    /// <summary>The user's professional role (optional).</summary>
    [StringLength(100)]
    public string? ProfessionalRole { get; init; }

    /// <summary>The institution name (optional).</summary>
    [StringLength(200)]
    public string? InstitutionName { get; init; }

    /// <summary>The institution department (optional).</summary>
    [StringLength(200)]
    public string? InstitutionDepartment { get; init; }

    /// <summary>The research field (optional). Must be a valid research field name.</summary>
    [StringLength(50)]
    public string? ResearchField { get; init; }

    /// <summary>The ORCID iD (optional). Format: 0000-0000-0000-000X.</summary>
    [StringLength(19)]
    [RegularExpression(@"^\d{4}-\d{4}-\d{4}-\d{3}[\dX]$", ErrorMessage = "Invalid ORCID format")]
    public string? OrcidId { get; init; }

    /// <summary>The personal/lab website URL (optional).</summary>
    [StringLength(500)]
    [Url]
    public string? Website { get; init; }
}
