using GeneFlow.ApiNet2.Application.Search.Common;
using GeneFlow.ApiNet2.Application.Search.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Search.Queries.ExploreRecent;

public sealed record ExploreRecentQuery(
    string? Type,
    string? Cursor,
    int PageSize = 20) : IQuery<Result<CursorPagedList<ExploreItemDto>>>;
