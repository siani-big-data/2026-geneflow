namespace GeneFlow.ApiNet2.API.Contracts.Studies.Responses;

/// <summary>
/// Response model for complete study information.
/// </summary>
public sealed record StudyResponse(
    string Id,
    string OwnerId,
    string Title,
    string? Description,
    int ResearchFieldId,
    string ResearchFieldName,
    int StatusId,
    string StatusName,
    string? Institution,
    string? PrincipalInvestigator,
    bool IsFeatured,
    StudySettingsResponse Settings,
    StudyMetricsResponse Metrics,
    IReadOnlyList<string> Tags,
    IReadOnlyList<StudyMemberResponse> Members,
    IReadOnlyList<StudyPaperResponse> Papers,
    DateTime CreatedAt,
    DateTime? ModifiedAt);

/// <summary>
/// Response model for study settings.
/// </summary>
public sealed record StudySettingsResponse(
    bool AllowPublicComments,
    bool AllowDataDownload,
    bool RequireApprovalToJoin);

/// <summary>
/// Response model for study metrics.
/// </summary>
public sealed record StudyMetricsResponse(
    int ViewsCount,
    int StarsCount);
