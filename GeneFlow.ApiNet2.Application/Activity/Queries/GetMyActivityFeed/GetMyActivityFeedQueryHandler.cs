using GeneFlow.ApiNet2.Application.Activity.DTOs;
using GeneFlow.ApiNet2.Application.Activity.Mappings;
using GeneFlow.ApiNet2.Domain.Activity;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Activity.Queries.GetMyActivityFeed;

/// <summary>
/// Handler for <see cref="GetMyActivityFeedQuery"/>.
/// </summary>
public sealed class GetMyActivityFeedQueryHandler
    : IQueryHandler<GetMyActivityFeedQuery, Result<CursorPaged<ActivityEventDto>>>
{
    /// <summary>Default page size when the caller does not pass one.</summary>
    public const int DefaultLimit = 20;

    /// <summary>Minimum allowed page size.</summary>
    public const int MinLimit = 1;

    /// <summary>Maximum allowed page size.</summary>
    public const int MaxLimit = 100;

    private readonly IActivityEventRepository _activityRepository;
    private readonly IStudyRepository _studyRepository;
    private readonly ILogger<GetMyActivityFeedQueryHandler> _logger;

    public GetMyActivityFeedQueryHandler(
        IActivityEventRepository activityRepository,
        IStudyRepository studyRepository,
        ILogger<GetMyActivityFeedQueryHandler> logger)
    {
        _activityRepository = activityRepository;
        _studyRepository = studyRepository;
        _logger = logger;
    }

    public async Task<Result<CursorPaged<ActivityEventDto>>> Handle(
        GetMyActivityFeedQuery request,
        CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
        {
            _logger.LogWarning("GetMyActivityFeed: invalid user id {UserId}", request.UserId);
            return Result.Failure<CursorPaged<ActivityEventDto>>(ActivityErrors.InvalidUserId);
        }

        // The cursor is opaque to clients; a malformed value is a client bug, so we
        // surface it as a validation failure instead of silently restarting the feed.
        ActivityCursor? cursor = null;
        if (!string.IsNullOrWhiteSpace(request.Cursor))
        {
            cursor = ActivityCursor.Decode(request.Cursor);
            if (cursor is null)
            {
                return Result.Failure<CursorPaged<ActivityEventDto>>(ActivityErrors.InvalidCursor);
            }
        }

        var limit = ClampLimit(request.Limit);

        // The activity feed must include study-scoped events only for studies the
        // caller is currently a member of. We re-evaluate membership on every read
        // instead of caching it, so that revoked access is reflected immediately.
        var visibleStudyIds = await _studyRepository.GetVisibleStudyIdsForUserAsync(
            userId, cancellationToken);

        var actorId = userId.ToString();
        var events = await _activityRepository.GetUserFeedAsync(
            actorId!,
            visibleStudyIds,
            limit,
            cursor,
            cancellationToken);

        var dtos = events.ToDtos();
        var hasMore = events.Count == limit;
        var nextCursor = hasMore && events.Count > 0
            ? ActivityCursor.From(events[^1].OccurredAt, events[^1].Id.Value).Encode()
            : null;

        _logger.LogInformation(
            "GetMyActivityFeed: UserId={UserId} returned {Count} events (hasMore={HasMore})",
            actorId, dtos.Count, hasMore);

        return Result.Success(new CursorPaged<ActivityEventDto>(dtos, nextCursor, hasMore));
    }

    private static int ClampLimit(int limit)
    {
        if (limit < MinLimit) return DefaultLimit;
        if (limit > MaxLimit) return MaxLimit;
        return limit;
    }
}
