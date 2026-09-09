namespace GeneFlow.ApiNet2.Application.Search.DTOs;

/// <summary>
/// Item returned by the global search endpoint.
/// </summary>
public sealed record SearchHitDto(
    string ObjectType,
    string ObjectId,
    string? OwnerId,
    string Title,
    string? Snippet,
    string? Tags,
    bool IsPublic,
    DateTime UpdatedAt,
    double Rank);
