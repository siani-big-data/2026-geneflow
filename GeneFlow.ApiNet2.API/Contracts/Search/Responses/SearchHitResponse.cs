namespace GeneFlow.ApiNet2.API.Contracts.Search.Responses;

public sealed record SearchHitResponse(
    string ObjectType,
    string ObjectId,
    string? OwnerId,
    string Title,
    string? Snippet,
    string? Tags,
    bool IsPublic,
    DateTime UpdatedAt,
    double Rank);
