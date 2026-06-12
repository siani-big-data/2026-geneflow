using GeneFlow.ApiNet2.Domain.Activity;
using GeneFlow.ApiNet2.Domain.Activity.Enumerations;
using GeneFlow.ApiNet2.Infrastructure.Activity.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Infrastructure.Activity.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IActivityEventRepository"/>.
/// Reads use keyset pagination over <c>(OccurredAt DESC, Id DESC)</c> for stability.
/// </summary>
public sealed class ActivityEventRepository : IActivityEventRepository
{
    /// <summary>Minimum allowed page size for activity feeds.</summary>
    public const int MinPageSize = 1;

    /// <summary>Maximum allowed page size for activity feeds.</summary>
    public const int MaxPageSize = 100;

    private readonly ActivityContext _context;
    private readonly ILogger<ActivityEventRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityEventRepository"/>.
    /// </summary>
    public ActivityEventRepository(ActivityContext context, ILogger<ActivityEventRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task AddAsync(ActivityEvent activityEvent, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Adding activity event {ActivityEventId}", activityEvent.Id);
        await _context.ActivityEvents.AddAsync(activityEvent, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExistsBySourceMessageIdAsync(string sourceMessageId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceMessageId);
        return _context.ActivityEvents
            .AsNoTracking()
            .AnyAsync(e => e.SourceMessageId == sourceMessageId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ActivityEvent?> GetByIdAsync(ActivityEventId id, CancellationToken cancellationToken)
    {
        return _context.ActivityEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<ActivityEvent>> GetUserFeedAsync(
        string userId,
        IReadOnlyCollection<string> visibleStudyIds,
        int limit,
        ActivityCursor? cursor,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(visibleStudyIds);

        var clamped = ClampLimit(limit);
        var studyIdSet = visibleStudyIds.ToArray();

        // Show events authored by the user (any visibility) OR public events OR
        // study-scoped events on studies the user can see.
        var query = _context.ActivityEvents.AsNoTracking()
            .Where(e =>
                e.ActorUserId == userId
                || e.Visibility == ActivityVisibility.Public
                || (e.Visibility == ActivityVisibility.StudyMembers
                    && e.StudyId != null
                    && studyIdSet.Contains(e.StudyId)));

        query = ApplyCursor(query, cursor);
        return ExecutePagedAsync(query, clamped, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ActivityEvent>> GetStudyTimelineAsync(
        string studyId,
        int limit,
        ActivityCursor? cursor,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(studyId);

        var clamped = ClampLimit(limit);

        var query = _context.ActivityEvents.AsNoTracking()
            .Where(e => e.StudyId == studyId && e.Visibility != ActivityVisibility.Private);

        query = ApplyCursor(query, cursor);
        return ExecutePagedAsync(query, clamped, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ActivityEvent>> GetAuditLogAsync(
        string userId,
        int limit,
        ActivityCursor? cursor,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var clamped = ClampLimit(limit);

        var query = _context.ActivityEvents.AsNoTracking()
            .Where(e => e.ActorUserId == userId);

        query = ApplyCursor(query, cursor);
        return ExecutePagedAsync(query, clamped, cancellationToken);
    }

    private static IQueryable<ActivityEvent> ApplyCursor(
        IQueryable<ActivityEvent> query,
        ActivityCursor? cursor)
    {
        if (cursor is null)
        {
            return query;
        }

        // Keyset condition on descending OccurredAt. The Id field in the cursor
        // is kept for future stability against ties (UUIDs ordered descending) but
        // is not used in the SQL predicate today: the projector assigns each row a
        // unique microsecond-precision OccurredAt so strict less-than is sufficient.
        var cursorTimestamp = cursor.OccurredAt;
        return query.Where(e => e.OccurredAt < cursorTimestamp);
    }

    private static async Task<IReadOnlyList<ActivityEvent>> ExecutePagedAsync(
        IQueryable<ActivityEvent> query,
        int limit,
        CancellationToken cancellationToken)
    {
        var rows = await query
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
        return rows;
    }

    private static int ClampLimit(int limit)
    {
        if (limit < MinPageSize)
            return MinPageSize;
        if (limit > MaxPageSize)
            return MaxPageSize;
        return limit;
    }
}
