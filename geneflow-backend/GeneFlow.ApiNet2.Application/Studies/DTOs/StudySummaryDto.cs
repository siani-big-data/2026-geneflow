namespace GeneFlow.ApiNet2.Application.Studies.DTOs;

/// <summary>
/// Summary study DTO for list views.
/// </summary>
public sealed record StudySummaryDto
{
    public required string Id { get; init; }
    public required string OwnerId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string ResearchField { get; init; }
    public required int ResearchFieldId { get; init; }
    public required string Status { get; init; }
    public required int StatusId { get; init; }
    public string? Institution { get; init; }
    public string? PrincipalInvestigator { get; init; }
    public required bool IsFeatured { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
    public required int ViewsCount { get; init; }
    public required int StarsCount { get; init; }
    public required int MemberCount { get; init; }
    public required int PaperCount { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
}
