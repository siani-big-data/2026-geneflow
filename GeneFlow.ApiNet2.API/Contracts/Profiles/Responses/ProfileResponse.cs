namespace GeneFlow.ApiNet2.API.Contracts.Profiles.Responses;

/// <summary>
/// Response model for complete profile information.
/// </summary>
public sealed record ProfileResponse(
    string Id,
    string UserId,
    string FirstName,
    string? LastName,
    string FullName,
    string Initials,
    string? Bio,
    string? Location,
    string? ProfessionalRole,
    string? InstitutionName,
    string? InstitutionDepartment,
    string? InstitutionDisplayName,
    string? ResearchField,
    string? OrcidId,
    string? OrcidUrl,
    string? Website,
    string? PhotoUrl,
    string? PhotoThumbnailUrl,
    bool IsComplete,
    DateTime CreatedAt,
    DateTime? ModifiedAt);
