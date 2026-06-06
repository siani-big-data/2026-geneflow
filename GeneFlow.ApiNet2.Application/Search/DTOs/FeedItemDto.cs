namespace GeneFlow.ApiNet2.Application.Search.DTOs;

/// <summary>
/// Item returned by the personal feed (follows + watches + own activity).
/// </summary>
public sealed record FeedItemDto(
    string ObjectType,
    string ObjectId,
    string? OwnerId,
    string Title,
    string? Body,
    string? Tags,
    DateTime UpdatedAt,
    string Reason);
