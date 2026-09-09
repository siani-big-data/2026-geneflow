using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;

namespace GeneFlow.ApiNet2.Domain.Usage;

/// <summary>
/// Represents usage statistics for a user within a billing period.
/// This is a read model optimized for fast queries, stored in Redis.
/// </summary>
public sealed class UsageStats : Entity<UserId>
{
    /// <summary>
    /// The billing period these stats belong to.
    /// </summary>
    public BillingPeriodKey PeriodKey { get; private set; }

    /// <summary>
    /// Number of active studies owned by the user.
    /// </summary>
    public int StudiesOwned { get; private set; }

    /// <summary>
    /// Number of studies the user is a member of (including owned).
    /// </summary>
    public int StudiesTotal { get; private set; }

    /// <summary>
    /// Number of traces uploaded in the current billing period.
    /// </summary>
    public int TracesThisPeriod { get; private set; }

    /// <summary>
    /// Total traces across all studies (lifetime).
    /// </summary>
    public long TracesTotal { get; private set; }

    /// <summary>
    /// Maximum number of members in any single study owned by this user.
    /// </summary>
    public int MaxMembersInStudy { get; private set; }

    /// <summary>
    /// Number of alignments created in the current period.
    /// </summary>
    public int AlignmentsThisPeriod { get; private set; }

    /// <summary>
    /// Total alignments (lifetime).
    /// </summary>
    public long AlignmentsTotal { get; private set; }

    /// <summary>
    /// Completed alignments (lifetime).
    /// </summary>
    public long AlignmentsCompleted { get; private set; }

    /// <summary>
    /// Pending traces awaiting processing.
    /// </summary>
    public int TracesPending { get; private set; }

    /// <summary>
    /// Last activity timestamp.
    /// </summary>
    public DateTime? LastActivityAt { get; private set; }

    /// <summary>
    /// When these stats were last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    private UsageStats() : base(default!) { }

    private UsageStats(UserId userId, BillingPeriodKey periodKey) : base(userId)
    {
        PeriodKey = periodKey;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new usage stats instance for a user.
    /// </summary>
    public static UsageStats Create(UserId userId, BillingPeriodKey periodKey)
    {
        return new UsageStats(userId, periodKey);
    }

    /// <summary>
    /// Increments the studies owned counter.
    /// </summary>
    public void IncrementStudiesOwned()
    {
        StudiesOwned++;
        StudiesTotal++;
        TouchActivity();
    }

    /// <summary>
    /// Decrements the studies owned counter.
    /// </summary>
    public void DecrementStudiesOwned()
    {
        if (StudiesOwned > 0)
            StudiesOwned--;
        if (StudiesTotal > 0)
            StudiesTotal--;
        TouchActivity();
    }

    /// <summary>
    /// Increments the total studies counter (when user joins a study).
    /// </summary>
    public void IncrementStudiesTotal()
    {
        StudiesTotal++;
        TouchActivity();
    }

    /// <summary>
    /// Decrements the total studies counter (when user leaves a study).
    /// </summary>
    public void DecrementStudiesTotal()
    {
        if (StudiesTotal > 0)
            StudiesTotal--;
        TouchActivity();
    }

    /// <summary>
    /// Increments the traces counter for the current period.
    /// </summary>
    public void IncrementTraces(int count = 1)
    {
        TracesThisPeriod += count;
        TracesTotal += count;
        TouchActivity();
    }

    /// <summary>
    /// Sets the pending traces count.
    /// </summary>
    public void SetTracesPending(int count)
    {
        TracesPending = count;
        TouchActivity();
    }

    /// <summary>
    /// Updates the max members in study if the new value is higher.
    /// </summary>
    public void UpdateMaxMembersInStudy(int memberCount)
    {
        if (memberCount > MaxMembersInStudy)
        {
            MaxMembersInStudy = memberCount;
        }
        TouchActivity();
    }

    /// <summary>
    /// Increments the alignments counter.
    /// </summary>
    public void IncrementAlignments()
    {
        AlignmentsThisPeriod++;
        AlignmentsTotal++;
        TouchActivity();
    }

    /// <summary>
    /// Increments the completed alignments counter.
    /// </summary>
    public void IncrementCompletedAlignments()
    {
        AlignmentsCompleted++;
        TouchActivity();
    }

    /// <summary>
    /// Resets period-specific counters for a new billing period.
    /// </summary>
    public void ResetForNewPeriod(BillingPeriodKey newPeriodKey)
    {
        PeriodKey = newPeriodKey;
        TracesThisPeriod = 0;
        AlignmentsThisPeriod = 0;
        TouchActivity();
    }

    /// <summary>
    /// Sets all stats at once (used when loading from storage).
    /// </summary>
    public void SetStats(
        int studiesOwned,
        int studiesTotal,
        int tracesThisPeriod,
        long tracesTotal,
        int maxMembersInStudy,
        int alignmentsThisPeriod,
        long alignmentsTotal,
        long alignmentsCompleted,
        int tracesPending,
        DateTime? lastActivityAt)
    {
        StudiesOwned = studiesOwned;
        StudiesTotal = studiesTotal;
        TracesThisPeriod = tracesThisPeriod;
        TracesTotal = tracesTotal;
        MaxMembersInStudy = maxMembersInStudy;
        AlignmentsThisPeriod = alignmentsThisPeriod;
        AlignmentsTotal = alignmentsTotal;
        AlignmentsCompleted = alignmentsCompleted;
        TracesPending = tracesPending;
        LastActivityAt = lastActivityAt;
        UpdatedAt = DateTime.UtcNow;
    }

    private void TouchActivity()
    {
        LastActivityAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
