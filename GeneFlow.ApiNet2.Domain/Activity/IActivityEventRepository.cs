using GeneFlow.ApiNet2.Domain.Activity.Enumerations;

namespace GeneFlow.ApiNet2.Domain.Activity;

/// <summary>
/// Persistence port for activity events.
/// </summary>
public interface IActivityEventRepository
{
    /// <summary>
    /// Adds a projected activity event to the store. Idempotency is enforced through the
    /// unique index on <see cref="ActivityEvent.SourceMessageId"/>.
    /// </summary>
    Task AddAsync(ActivityEvent activityEvent, CancellationToken cancellationToken);

    /// <summary>
    /// Returns whether an event with the given source message id has already been projected.
    /// Used by the projector to guarantee at-least-once consumers do not duplicate rows.
    /// </summary>
    Task<bool> ExistsBySourceMessageIdAsync(string sourceMessageId, CancellationToken cancellationToken);

    /// <summary>
    /// Loads a single event by id.
    /// </summary>
    Task<ActivityEvent?> GetByIdAsync(ActivityEventId id, CancellationToken cancellationToken);

    /// <summary>
    /// Persists in-memory changes. Repositories do not call SaveChanges themselves so that
    /// projector batches can commit atomically.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Reads the personal feed of a user — events authored by them OR addressed to studies
    /// they are members of (membership filter is applied by the caller via <paramref name="visibleStudyIds"/>).
    /// </summary>
    /// <param name="userId">Actor user id.</param>
    /// <param name="visibleStudyIds">Studies the user is a member of (for <c>StudyMembers</c> visibility).</param>
    /// <param name="limit">Maximum number of rows to return (1-based, clamped by the repository).</param>
    /// <param name="cursor">Optional keyset cursor produced by the previous page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<ActivityEvent>> GetUserFeedAsync(
        string userId,
        IReadOnlyCollection<string> visibleStudyIds,
        int limit,
        ActivityCursor? cursor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads the activity timeline of a study (all visibilities except <see cref="ActivityVisibility.Private"/>).
    /// </summary>
    Task<IReadOnlyList<ActivityEvent>> GetStudyTimelineAsync(
        string studyId,
        int limit,
        ActivityCursor? cursor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads the audit log of a user (all events authored by them, regardless of visibility).
    /// </summary>
    Task<IReadOnlyList<ActivityEvent>> GetAuditLogAsync(
        string userId,
        int limit,
        ActivityCursor? cursor,
        CancellationToken cancellationToken);
}
