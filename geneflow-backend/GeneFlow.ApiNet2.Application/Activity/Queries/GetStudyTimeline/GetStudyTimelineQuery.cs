using GeneFlow.ApiNet2.Application.Activity.DTOs;
using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Activity.Queries.GetStudyTimeline;

/// <summary>
/// Returns the timeline of activity events scoped to a single study. The repository
/// filters by <c>StudyId</c> and excludes <c>Private</c> events, so callers see both
/// <c>StudyMembers</c> and <c>Public</c> events for that study, ordered by
/// <c>OccurredAt DESC</c> with keyset pagination.
/// </summary>
/// <remarks>
/// Authorization is enforced by <see cref="StudyMembershipBehavior{TRequest,TResponse}"/>:
/// members of the study are allowed; non-members are only allowed when the study is
/// public (because <see cref="MinimumRole"/> is left at <c>null</c>).
/// </remarks>
/// <param name="StudyId">Prefixed identifier of the study whose timeline is requested.</param>
/// <param name="Limit">Maximum number of events to return in this page.</param>
/// <param name="Cursor">Opaque cursor returned by a previous page, or <c>null</c> for the first page.</param>
public sealed record GetStudyTimelineQuery(
    string StudyId,
    int Limit = 20,
    string? Cursor = null)
    : IQuery<Result<CursorPaged<ActivityEventDto>>>, IRequireStudyMembership
{
    /// <inheritdoc />
    string IRequireStudyMembership.StudyId => StudyId;

    /// <inheritdoc />
    /// <remarks>
    /// Read-only operation: leaving the minimum role as <c>null</c> lets the
    /// behavior fall back to the public-study check for non-members.
    /// </remarks>
    StudyRole? IRequireStudyMembership.MinimumRole => null;
}
