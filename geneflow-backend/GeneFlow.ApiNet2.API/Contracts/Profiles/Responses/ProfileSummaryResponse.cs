namespace GeneFlow.ApiNet2.API.Contracts.Profiles.Responses;

/// <summary>
/// Response model for profile summary information (lightweight version).
/// </summary>
public sealed record ProfileSummaryResponse(
    string Id,
    string UserId,
    string FullName,
    string Initials,
    string? PhotoUrl,
    string? PhotoThumbnailUrl,
    string? ProfessionalRole,
    string? InstitutionDisplayName);
