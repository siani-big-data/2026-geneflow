using GeneFlow.ApiNet2.Domain.Identity;

namespace GeneFlow.ApiNet2.Domain.Usage;

/// <summary>
/// Repository interface for usage statistics.
/// </summary>
public interface IUsageStatsRepository
{
    /// <summary>
    /// Gets the usage stats for a user in the current billing period.
    /// </summary>
    Task<UsageStats?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the usage stats for a user in a specific billing period.
    /// </summary>
    Task<UsageStats?> GetByUserIdAndPeriodAsync(UserId userId, BillingPeriodKey periodKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the usage stats.
    /// </summary>
    Task SaveAsync(UsageStats stats, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the studies owned counter atomically.
    /// </summary>
    Task IncrementStudiesOwnedAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrements the studies owned counter atomically.
    /// </summary>
    Task DecrementStudiesOwnedAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the total studies counter atomically.
    /// </summary>
    Task IncrementStudiesTotalAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrements the total studies counter atomically.
    /// </summary>
    Task DecrementStudiesTotalAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the traces counter atomically.
    /// </summary>
    Task IncrementTracesAsync(UserId userId, int count = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the pending traces count.
    /// </summary>
    Task SetTracesPendingAsync(UserId userId, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the max members in study if the new value is higher.
    /// </summary>
    Task UpdateMaxMembersInStudyAsync(UserId userId, int memberCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the alignments counter atomically.
    /// </summary>
    Task IncrementAlignmentsAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the completed alignments counter atomically.
    /// </summary>
    Task IncrementCompletedAlignmentsAsync(UserId userId, CancellationToken cancellationToken = default);
}
