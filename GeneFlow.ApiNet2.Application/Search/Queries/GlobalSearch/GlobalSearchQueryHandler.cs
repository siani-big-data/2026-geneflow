using GeneFlow.ApiNet2.Application.Search.Common;
using GeneFlow.ApiNet2.Application.Search.DTOs;
using GeneFlow.ApiNet2.Domain.Search;
using GeneFlow.ApiNet2.Domain.Search.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Search.Queries.GlobalSearch;

public sealed class GlobalSearchQueryHandler
    : IQueryHandler<GlobalSearchQuery, Result<CursorPagedList<SearchHitDto>>>
{
    private const int MaxPageSize = 50;
    private readonly ISearchIndexRepository _repository;

    public GlobalSearchQueryHandler(ISearchIndexRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CursorPagedList<SearchHitDto>>> Handle(
        GlobalSearchQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Q))
            return Result.Failure<CursorPagedList<SearchHitDto>>(SearchErrors.QueryRequired);

        SearchObjectType? type = null;
        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            if (!SearchObjectType.TryFromName(request.Type, out var parsedType))
                return Result.Failure<CursorPagedList<SearchHitDto>>(SearchErrors.InvalidObjectType);
            type = parsedType;
        }

        DateTime? cursorUpdatedBefore = null;
        Guid? cursorId = null;
        if (!string.IsNullOrWhiteSpace(request.Cursor))
        {
            if (!SearchCursor.TryDecode(request.Cursor, out var ts, out var id))
                return Result.Failure<CursorPagedList<SearchHitDto>>(SearchErrors.InvalidCursor);
            cursorUpdatedBefore = ts;
            cursorId = id;
        }

        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var hits = await _repository.SearchAsync(
            request.Q,
            type,
            request.Owner,
            request.Tag,
            cursorUpdatedBefore,
            cursorId,
            pageSize + 1, // overfetch by 1 to determine if next page exists
            cancellationToken);

        var hasMore = hits.Count > pageSize;
        var page = hasMore ? hits.Take(pageSize).ToList() : hits.ToList();

        var items = page
            .Select(h => new SearchHitDto(
                h.ObjectType.Name,
                h.ObjectId,
                h.OwnerId,
                h.Title,
                h.Body is null ? null : Snippet(h.Body, request.Q),
                h.Tags,
                h.IsPublic,
                h.UpdatedAt,
                h.Rank))
            .ToList();

        string? next = null;
        if (hasMore && page.Count > 0)
        {
            var last = page[^1];
            next = SearchCursor.Encode(last.UpdatedAt, last.Id);
        }

        return Result.Success(new CursorPagedList<SearchHitDto>(items, next));
    }

    private static string Snippet(string body, string query)
    {
        // Cheap, allocation-light snippet around the first match.
        var idx = body.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return body.Length <= 160 ? body : body[..160] + "…";
        }

        var start = Math.Max(0, idx - 60);
        var end = Math.Min(body.Length, idx + query.Length + 60);
        var slice = body[start..end];
        return (start > 0 ? "…" : string.Empty) + slice + (end < body.Length ? "…" : string.Empty);
    }
}
