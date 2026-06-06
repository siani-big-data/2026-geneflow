namespace GeneFlow.ApiNet2.API.Contracts.Discussions.Responses;

public sealed record DiscussionResponse(
    string Id,
    string StudyId,
    string AuthorId,
    string Title,
    string? Category,
    bool IsLocked,
    DateTime? LockedAt,
    string? LockedBy,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    int CommentCount,
    IReadOnlyList<CommentResponse>? Comments);
