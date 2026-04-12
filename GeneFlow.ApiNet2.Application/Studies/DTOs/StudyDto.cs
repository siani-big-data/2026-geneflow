using GeneFlow.ApiNet2.Domain.Studies.Enumerations;

namespace GeneFlow.ApiNet2.Application.Studies.DTOs;

/// <summary>
/// Full study data transfer object.
/// </summary>
public sealed record StudyDto
{
    public required string Id { get; init; }
    public required string OwnerId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string ResearchField { get; init; }
    public required int ResearchFieldId { get; init; }
    public required string Status { get; init; }
    public required int StatusId { get; init; }

    // Settings
    public required bool AllowPublicComments { get; init; }
    public required bool AllowDataDownload { get; init; }
    public required bool RequireApprovalToJoin { get; init; }

    // New fields
    public string? Institution { get; init; }
    public string? PrincipalInvestigator { get; init; }
    public required bool IsFeatured { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }

    // Metrics
    public required int ViewsCount { get; init; }
    public required int StarsCount { get; init; }

    // Members
    public required IReadOnlyList<StudyMemberDto> Members { get; init; }

    // Papers
    public required IReadOnlyList<StudyPaperDto> Papers { get; init; }

    // Audit
    public required DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? ModifiedAt { get; init; }
    public string? ModifiedBy { get; init; }
}
