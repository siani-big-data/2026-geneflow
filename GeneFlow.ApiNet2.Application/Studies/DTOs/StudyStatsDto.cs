namespace GeneFlow.ApiNet2.Application.Studies.DTOs;

/// <summary>
/// Study statistics data transfer object.
/// </summary>
public sealed record StudyStatsDto
{
    public required string StudyId { get; init; }
    public required int ViewsCount { get; init; }
    public required int StarsCount { get; init; }
    public required int MemberCount { get; init; }
    public required int PaperCount { get; init; }
    public required bool IsStarredByCurrentUser { get; init; }
}
