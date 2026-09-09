namespace GeneFlow.ApiNet2.API.Contracts.Profiles.Responses;

/// <summary>
/// Response model for profile statistics.
/// </summary>
public sealed record ProfileStatsResponse(
    int TotalStudies,
    int OwnedStudies,
    int TotalTraces,
    int TotalAlignments,
    int CompletedAlignments,
    DateTime? LastActivityAt,
    DateTime MemberSince);
