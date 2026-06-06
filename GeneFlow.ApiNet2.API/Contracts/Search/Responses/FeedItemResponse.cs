namespace GeneFlow.ApiNet2.API.Contracts.Search.Responses;

public sealed record FeedItemResponse(
    string ObjectType,
    string ObjectId,
    string? OwnerId,
    string Title,
    string? Body,
    string? Tags,
    DateTime UpdatedAt,
    string Reason);
