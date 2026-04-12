using GeneFlow.ApiNet2.API.Contracts.Studies.Responses;
using GeneFlow.ApiNet2.Application.Studies.DTOs;

namespace GeneFlow.ApiNet2.API.Extensions;

/// <summary>
/// Extension methods for mapping study DTOs to API responses.
/// </summary>
public static class StudyMappingExtensions
{
    /// <summary>
    /// Converts a StudyDto to a StudyResponse.
    /// </summary>
    public static StudyResponse ToResponse(this StudyDto dto) => new(
        dto.Id,
        dto.OwnerId,
        dto.Title,
        dto.Description,
        dto.ResearchFieldId,
        dto.ResearchField,
        dto.StatusId,
        dto.Status,
        dto.Institution,
        dto.PrincipalInvestigator,
        dto.IsFeatured,
        new StudySettingsResponse(
            dto.AllowPublicComments,
            dto.AllowDataDownload,
            dto.RequireApprovalToJoin),
        new StudyMetricsResponse(
            dto.ViewsCount,
            dto.StarsCount),
        dto.Tags,
        dto.Members.ToResponses(),
        dto.Papers.ToResponses(),
        dto.CreatedAt,
        dto.ModifiedAt);

    /// <summary>
    /// Converts a StudySummaryDto to a StudySummaryResponse.
    /// </summary>
    public static StudySummaryResponse ToResponse(this StudySummaryDto dto) => new(
        dto.Id,
        dto.OwnerId,
        dto.Title,
        dto.Description,
        dto.ResearchFieldId,
        dto.ResearchField,
        dto.StatusId,
        dto.Status,
        dto.Institution,
        dto.PrincipalInvestigator,
        dto.IsFeatured,
        dto.MemberCount,
        dto.PaperCount,
        dto.ViewsCount,
        dto.StarsCount,
        dto.Tags,
        dto.CreatedAt);

    /// <summary>
    /// Converts a StudyMemberDto to a StudyMemberResponse.
    /// </summary>
    public static StudyMemberResponse ToResponse(this StudyMemberDto dto) => new(
        dto.UserId,
        dto.RoleId,
        dto.Role,
        dto.JoinedAt,
        dto.InvitedBy);

    /// <summary>
    /// Converts a StudyPaperDto to a StudyPaperResponse.
    /// </summary>
    public static StudyPaperResponse ToResponse(this StudyPaperDto dto) => new(
        dto.Id,
        dto.Title,
        dto.Authors,
        dto.Doi,
        dto.Abstract,
        dto.Journal,
        dto.PublicationYear,
        dto.FileId,
        dto.FileName,
        dto.FileSizeBytes,
        dto.CreatedBy ?? "",
        dto.CreatedAt);

    /// <summary>
    /// Converts a StudyStatsDto to a StudyStatsResponse.
    /// </summary>
    public static StudyStatsResponse ToResponse(this StudyStatsDto dto) => new(
        dto.ViewsCount,
        dto.StarsCount,
        dto.IsStarredByCurrentUser);

    /// <summary>
    /// Converts a StudyInvitationDto to a StudyInvitationResponse.
    /// </summary>
    public static StudyInvitationResponse ToResponse(this StudyInvitationDto dto) => new(
        dto.Id,
        dto.StudyId,
        dto.Email,
        dto.RoleId,
        dto.Role,
        dto.StatusId,
        dto.Status,
        dto.StatusId == 1 ? null : null, // Token is only included for pending invitations via GetByToken
        dto.InvitedBy,
        dto.ExpiresAt,
        dto.RespondedAt,
        dto.Message,
        DateTime.UtcNow > dto.ExpiresAt,
        dto.StatusId == 1 && DateTime.UtcNow <= dto.ExpiresAt,
        dto.CreatedAt);

    /// <summary>
    /// Converts a ResearchFieldDto to a ResearchFieldResponse.
    /// </summary>
    public static ResearchFieldResponse ToResponse(this ResearchFieldDto dto) => new(
        dto.Id,
        dto.Name,
        dto.DisplayName);

    /// <summary>
    /// Converts a list of StudySummaryDto to a list of StudySummaryResponse.
    /// </summary>
    public static IReadOnlyList<StudySummaryResponse> ToResponses(
        this IEnumerable<StudySummaryDto> dtos) =>
        dtos.Select(dto => dto.ToResponse()).ToList();

    /// <summary>
    /// Converts a list of StudyMemberDto to a list of StudyMemberResponse.
    /// </summary>
    public static IReadOnlyList<StudyMemberResponse> ToResponses(
        this IEnumerable<StudyMemberDto> dtos) =>
        dtos.Select(dto => dto.ToResponse()).ToList();

    /// <summary>
    /// Converts a list of StudyPaperDto to a list of StudyPaperResponse.
    /// </summary>
    public static IReadOnlyList<StudyPaperResponse> ToResponses(
        this IEnumerable<StudyPaperDto> dtos) =>
        dtos.Select(dto => dto.ToResponse()).ToList();

    /// <summary>
    /// Converts a list of StudyInvitationDto to a list of StudyInvitationResponse.
    /// </summary>
    public static IReadOnlyList<StudyInvitationResponse> ToResponses(
        this IEnumerable<StudyInvitationDto> dtos) =>
        dtos.Select(dto => dto.ToResponse()).ToList();

    /// <summary>
    /// Converts a list of ResearchFieldDto to a list of ResearchFieldResponse.
    /// </summary>
    public static IReadOnlyList<ResearchFieldResponse> ToResponses(
        this IEnumerable<ResearchFieldDto> dtos) =>
        dtos.Select(dto => dto.ToResponse()).ToList();
}
