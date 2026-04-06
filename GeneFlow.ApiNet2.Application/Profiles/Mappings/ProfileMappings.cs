using GeneFlow.ApiNet2.Application.Profiles.DTOs;
using GeneFlow.ApiNet2.Domain.Profiles;

namespace GeneFlow.ApiNet2.Application.Profiles.Mappings;

/// <summary>
/// Extension methods for mapping Profile domain objects to DTOs.
/// </summary>
public static class ProfileMappings
{
    /// <summary>
    /// Maps a Profile to a complete ProfileDto.
    /// </summary>
    public static ProfileDto ToDto(this Profile profile)
    {
        return new ProfileDto
        {
            Id = profile.Id.ToString(),
            UserId = profile.UserId.ToString(),
            FirstName = profile.Name.FirstName,
            LastName = profile.Name.LastName,
            FullName = profile.Name.FullName,
            Initials = profile.Name.Initials,
            Bio = profile.Bio.Value,
            Location = profile.Location.Value,
            ProfessionalRole = profile.ProfessionalRole.Value,
            InstitutionName = profile.Institution.Name,
            InstitutionDepartment = profile.Institution.Department,
            InstitutionDisplayName = profile.Institution.DisplayName,
            ResearchField = profile.ResearchField?.Name,
            OrcidId = profile.ResearchIdentifiers.OrcidId,
            OrcidUrl = profile.ResearchIdentifiers.OrcidUrl,
            Website = profile.ResearchIdentifiers.Website,
            PhotoUrl = profile.Photo.Url,
            PhotoThumbnailUrl = profile.Photo.ThumbnailUrl,
            IsComplete = profile.IsComplete,
            CreatedAt = profile.CreatedAt,
            ModifiedAt = profile.ModifiedAt
        };
    }

    /// <summary>
    /// Maps a Profile to a summary ProfileSummaryDto.
    /// </summary>
    public static ProfileSummaryDto ToSummaryDto(this Profile profile)
    {
        return new ProfileSummaryDto
        {
            Id = profile.Id.ToString(),
            UserId = profile.UserId.ToString(),
            FullName = profile.Name.FullName,
            Initials = profile.Name.Initials,
            ProfessionalRole = profile.ProfessionalRole.Value,
            InstitutionDisplayName = profile.Institution.DisplayName,
            PhotoUrl = profile.Photo.Url,
            PhotoThumbnailUrl = profile.Photo.ThumbnailUrl
        };
    }

    /// <summary>
    /// Maps a collection of Profiles to summary DTOs.
    /// </summary>
    public static IReadOnlyList<ProfileSummaryDto> ToSummaryDtos(this IEnumerable<Profile> profiles)
    {
        return profiles.Select(p => p.ToSummaryDto()).ToList();
    }
}
