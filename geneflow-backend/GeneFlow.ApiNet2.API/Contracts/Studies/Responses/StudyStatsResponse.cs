namespace GeneFlow.ApiNet2.API.Contracts.Studies.Responses;

/// <summary>
/// Response model for study statistics.
/// </summary>
public sealed record StudyStatsResponse(
    int ViewsCount,
    int StarsCount,
    bool IsStarredByCurrentUser);
