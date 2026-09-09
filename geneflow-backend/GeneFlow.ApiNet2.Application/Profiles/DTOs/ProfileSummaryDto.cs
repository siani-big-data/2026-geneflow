namespace GeneFlow.ApiNet2.Application.Profiles.DTOs;

/// <summary>
/// Data transfer object for profile summary information.
/// </summary>
public sealed record ProfileSummaryDto
{
    /// <summary>Gets the profile's unique identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the user ID this profile belongs to.</summary>
    public required string UserId { get; init; }

    /// <summary>Gets the full name.</summary>
    public required string FullName { get; init; }

    /// <summary>Gets the initials.</summary>
    public required string Initials { get; init; }

    /// <summary>Gets the professional role.</summary>
    public string? ProfessionalRole { get; init; }

    /// <summary>Gets the institution display name.</summary>
    public string? InstitutionDisplayName { get; init; }

    /// <summary>Gets the photo URL.</summary>
    public string? PhotoUrl { get; init; }

    /// <summary>Gets the photo thumbnail URL.</summary>
    public string? PhotoThumbnailUrl { get; init; }
}
