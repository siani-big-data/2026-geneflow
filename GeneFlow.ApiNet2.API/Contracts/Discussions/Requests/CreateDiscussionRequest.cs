namespace GeneFlow.ApiNet2.API.Contracts.Discussions.Requests;

public sealed record CreateDiscussionRequest(
    string Title,
    string? Category,
    string FirstCommentBody);
