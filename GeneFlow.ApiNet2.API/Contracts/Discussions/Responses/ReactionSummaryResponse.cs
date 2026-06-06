namespace GeneFlow.ApiNet2.API.Contracts.Discussions.Responses;

public sealed record ReactionSummaryResponse(
    string Emoji,
    int Count,
    bool ReactedByMe);
