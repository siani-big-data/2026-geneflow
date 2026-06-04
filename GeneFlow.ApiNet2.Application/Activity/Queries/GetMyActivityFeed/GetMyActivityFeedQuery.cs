using GeneFlow.ApiNet2.Application.Activity.DTOs;
using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Activity.Queries.GetMyActivityFeed;

/// <summary>
/// Returns the activity feed for the current user: events authored by the user,
/// public events from any source, and study-scoped events for studies the user
/// can see. Results are ordered by <c>OccurredAt DESC</c> with keyset pagination.
/// </summary>
/// <param name="UserId">Prefixed identifier of the requesting user.</param>
/// <param name="Limit">Maximum number of events to return in this page.</param>
/// <param name="Cursor">Opaque cursor returned by a previous page, or <c>null</c> for the first page.</param>
public sealed record GetMyActivityFeedQuery(
    string UserId,
    int Limit = 20,
    string? Cursor = null)
    : IQuery<Result<CursorPaged<ActivityEventDto>>>, IRequireAuthentication;
