using GeneFlow.ApiNet2.Application.Search.Common;
using GeneFlow.ApiNet2.Application.Search.DTOs;
using GeneFlow.ApiNet2.Domain.Search;
using GeneFlow.ApiNet2.Domain.Search.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Search.Queries.ExploreRecent;

public sealed class ExploreRecentQueryHandler
    : IQueryHandler<ExploreRecentQuery, Result<CursorPagedList<ExploreItemDto>>>
{
    private const int MaxPageSize = 50;
    private readonly ISearchIndexRepository _repository;

    public ExploreRecentQueryHandler(ISearchIndexRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CursorPagedList<ExploreItemDto>>> Handle(
        ExploreRecentQuery request, CancellationToken cancellationToken)
    {
        SearchObjectType? type = null;
        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            if (!SearchObjectType.TryFromName(request.Type, out var parsed))
                return Result.Failure<CursorPagedList<ExploreItemDto>>(SearchErrors.InvalidObjectType);
            type = parsed;
        }

        DateTime? cursorUpdatedBefore = null;
        Guid? cursorId = null;
        if (!string.IsNullOrWhiteSpace(request.Cursor))
        {
            if (!SearchCursor.TryDecode(request.Cursor, out var ts, out var id))
                return Result.Failure<CursorPagedList<ExploreItemDto>>(SearchErrors.InvalidCursor);
            cursorUpdatedBefore = ts;
            cursorId = id;
        }

        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var hits = await _repository.GetRecentPublicAsync(
            type, cursorUpdatedBefore, cursorId, pageSize + 1, cancellationToken);

        var hasMore = hits.Count > pageSize;
        var page = hasMore ? hits.Take(pageSize).ToList() : hits.ToList();

        var items = page
            .Select(h => new ExploreItemDto(
                h.ObjectType.Name, h.ObjectId, h.OwnerId, h.Title, h.Body, h.Tags, h.UpdatedAt, 0))
            .ToList();

        string? next = null;
        if (hasMore && page.Count > 0)
        {
            var last = page[^1];
            next = SearchCursor.Encode(last.UpdatedAt, last.Id);
        }

        return Result.Success(new CursorPagedList<ExploreItemDto>(items, next));
    }
}
