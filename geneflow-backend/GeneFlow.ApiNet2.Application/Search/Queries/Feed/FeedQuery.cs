using GeneFlow.ApiNet2.Application.Search.Common;
using GeneFlow.ApiNet2.Application.Search.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Search.Queries.Feed;

/// <summary>
/// Personal feed for an authenticated user. Composes:
///   - objects authored by users they follow
///   - objects belonging to studies they watch
///   - their own activity
/// Ordered by UpdatedAt desc, cursor paginated.
/// </summary>
public sealed record FeedQuery(
    string UserId,
    string? Cursor,
    int PageSize = 20) : IQuery<Result<CursorPagedList<FeedItemDto>>>;
