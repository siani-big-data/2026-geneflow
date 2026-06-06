namespace GeneFlow.ApiNet2.Application.Search.DTOs;

/// <summary>
/// Item returned by the explore endpoints (trending / recent / featured).
/// </summary>
public sealed record ExploreItemDto(
    string ObjectType,
    string ObjectId,
    string? OwnerId,
    string Title,
    string? Body,
    string? Tags,
    DateTime UpdatedAt,
    int Score);
