namespace GeneFlow.ApiNet2.Application.Discussions.DTOs;

public sealed record DiscussionDto
{
    public required string Id { get; init; }
    public required string StudyId { get; init; }
    public required string AuthorId { get; init; }
    public required string Title { get; init; }
    public string? Category { get; init; }
    public bool IsLocked { get; init; }
    public DateTime? LockedAt { get; init; }
    public string? LockedBy { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
    public int CommentCount { get; init; }
    public IReadOnlyList<CommentDto>? Comments { get; init; }
}
