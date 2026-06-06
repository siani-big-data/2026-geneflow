namespace GeneFlow.ApiNet2.Application.Discussions.DTOs;

public sealed record CommentDto
{
    public required string Id { get; init; }
    public required string ParentType { get; init; }
    public required string ParentId { get; init; }
    public required string AuthorId { get; init; }
    public required string BodyMarkdown { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? EditedAt { get; init; }
    public bool IsDeleted { get; init; }
    public bool CanEdit { get; init; }
    public bool CanDelete { get; init; }
    public required IReadOnlyList<ReactionSummaryDto> Reactions { get; init; }
}
