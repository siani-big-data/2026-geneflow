using GeneFlow.ApiNet2.API.Contracts.Profiles.Responses;
using GeneFlow.ApiNet2.Application.Profiles.DTOs;

namespace GeneFlow.ApiNet2.API.Extensions;

/// <summary>
/// Extension methods for mapping profile DTOs to API responses.
/// </summary>
public static class ProfileMappingExtensions
{
    /// <summary>
    /// Converts a ProfileDto to a ProfileResponse.
    /// </summary>
    public static ProfileResponse ToResponse(this ProfileDto dto) => new(
        dto.Id,
        dto.UserId,
        dto.FirstName,
        dto.LastName,
        dto.FullName,
        dto.Initials,
        dto.Bio,
        dto.Location,
        dto.ProfessionalRole,
        dto.InstitutionName,
        dto.InstitutionDepartment,
        dto.InstitutionDisplayName,
        dto.ResearchField,
        dto.OrcidId,
        dto.OrcidUrl,
        dto.Website,
        dto.PhotoUrl,
        dto.PhotoThumbnailUrl,
        dto.IsComplete,
        dto.CreatedAt,
        dto.ModifiedAt);

    /// <summary>
    /// Converts a ProfileSummaryDto to a ProfileSummaryResponse.
    /// </summary>
    public static ProfileSummaryResponse ToResponse(this ProfileSummaryDto dto) => new(
        dto.Id,
        dto.UserId,
        dto.FullName,
        dto.Initials,
        dto.PhotoUrl,
        dto.PhotoThumbnailUrl,
        dto.ProfessionalRole,
        dto.InstitutionDisplayName);

    /// <summary>
    /// Converts a ProfileStatsDto to a ProfileStatsResponse.
    /// </summary>
    public static ProfileStatsResponse ToResponse(this ProfileStatsDto dto) => new(
        dto.TotalStudies,
        dto.OwnedStudies,
        dto.TotalTraces,
        dto.TotalAlignments,
        dto.CompletedAlignments,
        dto.LastActivityAt,
        dto.MemberSince);

    /// <summary>
    /// Converts a list of ProfileSummaryDto to a list of ProfileSummaryResponse.
    /// </summary>
    public static IReadOnlyList<ProfileSummaryResponse> ToResponses(
        this IEnumerable<ProfileSummaryDto> dtos) =>
        dtos.Select(dto => dto.ToResponse()).ToList();
}
