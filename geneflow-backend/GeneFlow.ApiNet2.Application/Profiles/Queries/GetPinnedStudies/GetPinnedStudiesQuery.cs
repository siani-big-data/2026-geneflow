using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Profiles.Queries.GetPinnedStudies;

/// <summary>
/// Returns the pinned studies for a user, ordered by display position.
/// </summary>
public sealed record GetPinnedStudiesQuery(
    string UserId) : IQuery<Result<IReadOnlyList<PinnedStudyDto>>>;
