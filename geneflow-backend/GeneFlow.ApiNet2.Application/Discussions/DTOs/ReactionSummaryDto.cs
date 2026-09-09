namespace GeneFlow.ApiNet2.Application.Discussions.DTOs;

/// <summary>
/// Aggregated reaction summary for a single comment + emoji.
/// </summary>
public sealed record ReactionSummaryDto(
    string Emoji,
    int Count,
    bool ReactedByMe);
