using GeneFlow.ApiNet2.Application.Search.Common;
using GeneFlow.ApiNet2.Application.Search.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Search.Queries.GlobalSearch;

/// <summary>
/// Full-text search across the corpus. Filters and cursor pagination
/// are optional. <paramref name="Q"/> is required.
/// </summary>
public sealed record GlobalSearchQuery(
    string Q,
    string? Type,
    string? Owner,
    string? Tag,
    string? Cursor,
    int PageSize = 20) : IQuery<Result<CursorPagedList<SearchHitDto>>>;
