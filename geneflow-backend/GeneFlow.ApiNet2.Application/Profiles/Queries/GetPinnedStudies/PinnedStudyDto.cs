namespace GeneFlow.ApiNet2.Application.Profiles.Queries.GetPinnedStudies;

/// <summary>
/// A user's pinned study as exposed to the frontend.
/// Study metadata (title, etc.) is fetched separately by id to keep
/// the Profiles context independent of the Studies context.
/// </summary>
public sealed record PinnedStudyDto(
    string StudyId,
    int Order,
    DateTime PinnedAt);
