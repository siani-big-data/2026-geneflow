namespace GeneFlow.ApiNet2.API.Contracts.Discussions.Responses;

public sealed record CommentResponse(
    Guid Id,
    string ParentType,
    string ParentId,
    string AuthorId,
    string BodyMarkdown,
    DateTime CreatedAt,
    DateTime? EditedAt,
    bool IsDeleted,
    bool CanEdit,
    bool CanDelete,
    IReadOnlyList<ReactionSummaryResponse> Reactions);
