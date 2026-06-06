using GeneFlow.ApiNet2.Application.Activity.DTOs;
using GeneFlow.ApiNet2.Application.Activity.Mappings;
using GeneFlow.ApiNet2.Domain.Activity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Activity.Queries.GetStudyTimeline;

/// <summary>
/// Handler for <see cref="GetStudyTimelineQuery"/>.
/// </summary>
/// <remarks>
/// Mirrors <see cref="GetMyActivityFeed.GetMyActivityFeedQueryHandler"/> but skips the
/// visibility-set computation: the repository's <c>GetStudyTimelineAsync</c> already
/// filters by <c>StudyId</c> and excludes <c>Private</c> events, so callers see both
/// <c>StudyMembers</c> and <c>Public</c> events without any extra membership lookup.
/// Authorization (member vs public-study fallback) is enforced by the pipeline
/// behavior before this handler runs.
/// </remarks>
public sealed class GetStudyTimelineQueryHandler
    : IQueryHandler<GetStudyTimelineQuery, Result<CursorPaged<ActivityEventDto>>>
{
    /// <summary>Default page size when the caller does not pass one.</summary>
    public const int DefaultLimit = 20;

    /// <summary>Minimum allowed page size.</summary>
    public const int MinLimit = 1;

    /// <summary>Maximum allowed page size.</summary>
    public const int MaxLimit = 100;

    private readonly IActivityEventRepository _activityRepository;
    private readonly ILogger<GetStudyTimelineQueryHandler> _logger;

    public GetStudyTimelineQueryHandler(
        IActivityEventRepository activityRepository,
        ILogger<GetStudyTimelineQueryHandler> logger)
    {
        _activityRepository = activityRepository;
        _logger = logger;
    }

    public async Task<Result<CursorPaged<ActivityEventDto>>> Handle(
        GetStudyTimelineQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.StudyId))
        {
            _logger.LogWarning("GetStudyTimeline: missing study id");
            return Result.Failure<CursorPaged<ActivityEventDto>>(ActivityErrors.InvalidStudyId);
        }

        // Cursor is opaque to clients; a malformed value is a client bug we surface
        // instead of silently restarting the feed, mirroring GetMyActivityFeed.
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

        var events = await _activityRepository.GetStudyTimelineAsync(
            request.StudyId,
            limit,
            cursor,
            cancellationToken);

        var dtos = events.ToDtos();
        var hasMore = events.Count == limit;
        var nextCursor = hasMore && events.Count > 0
            ? ActivityCursor.From(events[^1].OccurredAt, events[^1].Id.Value).Encode()
            : null;

        _logger.LogInformation(
            "GetStudyTimeline: StudyId={StudyId} returned {Count} events (hasMore={HasMore})",
            request.StudyId, dtos.Count, hasMore);

        return Result.Success(new CursorPaged<ActivityEventDto>(dtos, nextCursor, hasMore));
    }

    private static int ClampLimit(int limit)
    {
        if (limit < MinLimit) return DefaultLimit;
        if (limit > MaxLimit) return MaxLimit;
        return limit;
    }
}
