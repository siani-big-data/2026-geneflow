using GeneFlow.ApiNet2.Application.Search.Common;
using GeneFlow.ApiNet2.Application.Search.DTOs;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Notifications;
using GeneFlow.ApiNet2.Domain.Notifications.Enumerations;
using GeneFlow.ApiNet2.Domain.Search;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Search.Queries.Feed;

public sealed class FeedQueryHandler
    : IQueryHandler<FeedQuery, Result<CursorPagedList<FeedItemDto>>>
{
    private const int MaxPageSize = 50;
    private readonly ISearchIndexRepository _searchIndex;
    private readonly IUserRepository _userRepository;
    private readonly IWatchRepository _watchRepository;

    public FeedQueryHandler(
        ISearchIndexRepository searchIndex,
        IUserRepository userRepository,
        IWatchRepository watchRepository)
    {
        _searchIndex = searchIndex;
        _userRepository = userRepository;
        _watchRepository = watchRepository;
    }

    public async Task<Result<CursorPagedList<FeedItemDto>>> Handle(
        FeedQuery request, CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<CursorPagedList<FeedItemDto>>(SearchErrors.NotFound);

        DateTime? cursorUpdatedBefore = null;
        Guid? cursorId = null;
        if (!string.IsNullOrWhiteSpace(request.Cursor))
        {
            if (!SearchCursor.TryDecode(request.Cursor, out var ts, out var id))
                return Result.Failure<CursorPagedList<FeedItemDto>>(SearchErrors.InvalidCursor);
            cursorUpdatedBefore = ts;
            cursorId = id;
        }

        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var followingIds = await _userRepository.GetFollowingIdsAsync(userId, cancellationToken);
        var watchedStudyIds = await _watchRepository.GetWatchedStudyIdsAsync(
            userId, WatchLevel.All, cancellationToken);

        var followedOwnerIds = followingIds.Select(u => u.ToString()).ToList();
        var watchedIds = watchedStudyIds.Select(s => s.ToString()).ToList();

        var hits = await _searchIndex.GetFeedAsync(
            followedOwnerIds,
            watchedIds,
            userId.ToString(),
            cursorUpdatedBefore,
            cursorId,
            pageSize + 1,
            cancellationToken);

        var hasMore = hits.Count > pageSize;
        var page = hasMore ? hits.Take(pageSize).ToList() : hits.ToList();

        var followedSet = new HashSet<string>(followedOwnerIds);
        var watchedSet = new HashSet<string>(watchedIds);

        var items = page
            .Select(h => new FeedItemDto(
                h.ObjectType.Name,
                h.ObjectId,
                h.OwnerId,
                h.Title,
                h.Body,
                h.Tags,
                h.UpdatedAt,
                Reason: h.OwnerId == userId.ToString()
                    ? "self"
                    : h.OwnerId is not null && followedSet.Contains(h.OwnerId)
                        ? "follow"
                        : watchedSet.Contains(h.ObjectId)
                            ? "watch"
                            : "other"))
            .ToList();

        string? next = null;
        if (hasMore && page.Count > 0)
        {
            var last = page[^1];
            next = SearchCursor.Encode(last.UpdatedAt, last.Id);
        }

        return Result.Success(new CursorPagedList<FeedItemDto>(items, next));
    }
}
