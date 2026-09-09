namespace GeneFlow.ApiNet2.Application.Profiles.DTOs;

/// <summary>
/// Data transfer object for complete profile information.
/// </summary>
public sealed record ProfileDto
{
    /// <summary>Gets the profile's unique identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the user ID this profile belongs to.</summary>
    public required string UserId { get; init; }

    /// <summary>Gets the first name.</summary>
    public required string FirstName { get; init; }

    /// <summary>Gets the last name.</summary>
    public string? LastName { get; init; }

    /// <summary>Gets the full name.</summary>
    public required string FullName { get; init; }

    /// <summary>Gets the initials.</summary>
    public required string Initials { get; init; }

    /// <summary>Gets the bio.</summary>
    public string? Bio { get; init; }

    /// <summary>Gets the location.</summary>
    public string? Location { get; init; }

    /// <summary>Gets the professional role.</summary>
    public string? ProfessionalRole { get; init; }

    /// <summary>Gets the institution name.</summary>
    public string? InstitutionName { get; init; }

    /// <summary>Gets the institution department.</summary>
    public string? InstitutionDepartment { get; init; }

    /// <summary>Gets the institution display name.</summary>
    public string? InstitutionDisplayName { get; init; }

    /// <summary>Gets the research field.</summary>
    public string? ResearchField { get; init; }

    /// <summary>Gets the ORCID ID.</summary>
    public string? OrcidId { get; init; }

    /// <summary>Gets the ORCID URL.</summary>
    public string? OrcidUrl { get; init; }

    /// <summary>Gets the website URL.</summary>
    public string? Website { get; init; }

    /// <summary>Gets the photo URL.</summary>
    public string? PhotoUrl { get; init; }

    /// <summary>Gets the photo thumbnail URL.</summary>
    public string? PhotoThumbnailUrl { get; init; }

    /// <summary>Gets whether the profile is complete.</summary>
    public required bool IsComplete { get; init; }

    /// <summary>Gets when the profile was created.</summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>Gets when the profile was last modified.</summary>
    public DateTime? ModifiedAt { get; init; }
}
