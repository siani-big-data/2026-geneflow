namespace GeneFlow.ApiNet2.API.Contracts.Studies.Responses;

/// <summary>
/// Response model for study summary (list views).
/// </summary>
public sealed record StudySummaryResponse(
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
    int MemberCount,
    int PaperCount,
    int ViewsCount,
    int StarsCount,
    IReadOnlyList<string> Tags,
    DateTime CreatedAt);
