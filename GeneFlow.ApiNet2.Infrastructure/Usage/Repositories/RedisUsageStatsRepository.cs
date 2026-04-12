using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Usage;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace GeneFlow.ApiNet2.Infrastructure.Usage.Repositories;

/// <summary>
/// Redis-based repository for usage statistics.
/// Uses Redis hashes for efficient atomic operations.
/// </summary>
public sealed class RedisUsageStatsRepository : IUsageStatsRepository
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisUsageStatsRepository> _logger;

    // Hash field names
    private const string FieldStudiesOwned = "studies_owned";
    private const string FieldStudiesTotal = "studies_total";
    private const string FieldTracesThisPeriod = "traces_period";
    private const string FieldTracesTotal = "traces_total";
    private const string FieldMaxMembers = "max_members";
    private const string FieldAlignmentsPeriod = "alignments_period";
    private const string FieldAlignmentsTotal = "alignments_total";
    private const string FieldAlignmentsCompleted = "alignments_completed";
    private const string FieldTracesPending = "traces_pending";
    private const string FieldLastActivity = "last_activity";
    private const string FieldPeriodKey = "period_key";
    private const string FieldUpdatedAt = "updated_at";

    public RedisUsageStatsRepository(
        IConnectionMultiplexer redis,
        ILogger<RedisUsageStatsRepository> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UsageStats?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await GetByUserIdAndPeriodAsync(userId, BillingPeriodKey.Current(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UsageStats?> GetByUserIdAndPeriodAsync(
        UserId userId,
        BillingPeriodKey periodKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = GetRedisKey(userId);

            var hash = await db.HashGetAllAsync(key);
            if (hash.Length == 0)
                return null;

            var stats = UsageStats.Create(userId, periodKey);

            // Check if we need to reset for a new period
            var storedPeriod = GetHashValue(hash, FieldPeriodKey);
            var currentPeriod = periodKey.ToString();

            int tracesThisPeriod = 0;
            int alignmentsThisPeriod = 0;

            if (storedPeriod != currentPeriod)
            {
                // New period - reset period-specific counters
                tracesThisPeriod = 0;
                alignmentsThisPeriod = 0;

                // Update the stored period
                await db.HashSetAsync(key, new[]
                {
                    new HashEntry(FieldPeriodKey, currentPeriod),
                    new HashEntry(FieldTracesThisPeriod, 0),
                    new HashEntry(FieldAlignmentsPeriod, 0)
                });
            }
            else
            {
                tracesThisPeriod = GetHashIntValue(hash, FieldTracesThisPeriod);
                alignmentsThisPeriod = GetHashIntValue(hash, FieldAlignmentsPeriod);
            }

            stats.SetStats(
                studiesOwned: GetHashIntValue(hash, FieldStudiesOwned),
                studiesTotal: GetHashIntValue(hash, FieldStudiesTotal),
                tracesThisPeriod: tracesThisPeriod,
                tracesTotal: GetHashLongValue(hash, FieldTracesTotal),
                maxMembersInStudy: GetHashIntValue(hash, FieldMaxMembers),
                alignmentsThisPeriod: alignmentsThisPeriod,
                alignmentsTotal: GetHashLongValue(hash, FieldAlignmentsTotal),
                alignmentsCompleted: GetHashLongValue(hash, FieldAlignmentsCompleted),
                tracesPending: GetHashIntValue(hash, FieldTracesPending),
                lastActivityAt: GetHashDateTimeValue(hash, FieldLastActivity));

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get usage stats for user {UserId}", userId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(UsageStats stats, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = GetRedisKey(stats.Id);

            var entries = new HashEntry[]
            {
                new(FieldStudiesOwned, stats.StudiesOwned),
                new(FieldStudiesTotal, stats.StudiesTotal),
                new(FieldTracesThisPeriod, stats.TracesThisPeriod),
                new(FieldTracesTotal, stats.TracesTotal),
                new(FieldMaxMembers, stats.MaxMembersInStudy),
                new(FieldAlignmentsPeriod, stats.AlignmentsThisPeriod),
                new(FieldAlignmentsTotal, stats.AlignmentsTotal),
                new(FieldAlignmentsCompleted, stats.AlignmentsCompleted),
                new(FieldTracesPending, stats.TracesPending),
                new(FieldLastActivity, stats.LastActivityAt?.ToString("O") ?? ""),
                new(FieldPeriodKey, stats.PeriodKey.ToString()),
                new(FieldUpdatedAt, DateTime.UtcNow.ToString("O"))
            };

            await db.HashSetAsync(key, entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save usage stats for user {UserId}", stats.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task IncrementStudiesOwnedAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        await IncrementFieldAsync(userId, FieldStudiesOwned);
        await IncrementFieldAsync(userId, FieldStudiesTotal);
        await TouchActivityAsync(userId);
    }

    /// <inheritdoc />
    public async Task DecrementStudiesOwnedAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        await DecrementFieldAsync(userId, FieldStudiesOwned);
        await DecrementFieldAsync(userId, FieldStudiesTotal);
        await TouchActivityAsync(userId);
    }

    /// <inheritdoc />
    public async Task IncrementStudiesTotalAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        await IncrementFieldAsync(userId, FieldStudiesTotal);
        await TouchActivityAsync(userId);
    }

    /// <inheritdoc />
    public async Task DecrementStudiesTotalAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        await DecrementFieldAsync(userId, FieldStudiesTotal);
        await TouchActivityAsync(userId);
    }

    /// <inheritdoc />
    public async Task IncrementTracesAsync(UserId userId, int count = 1, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = GetRedisKey(userId);

        await EnsurePeriodKeyAsync(db, key);
        await db.HashIncrementAsync(key, FieldTracesThisPeriod, count);
        await db.HashIncrementAsync(key, FieldTracesTotal, count);
        await TouchActivityAsync(userId);
    }

    /// <inheritdoc />
    public async Task SetTracesPendingAsync(UserId userId, int count, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = GetRedisKey(userId);
        await db.HashSetAsync(key, FieldTracesPending, count);
        await TouchActivityAsync(userId);
    }

    /// <inheritdoc />
    public async Task UpdateMaxMembersInStudyAsync(UserId userId, int memberCount, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = GetRedisKey(userId);

        // Get current max
        var currentMax = (int)await db.HashGetAsync(key, FieldMaxMembers);
        if (memberCount > currentMax)
        {
            await db.HashSetAsync(key, FieldMaxMembers, memberCount);
        }
        await TouchActivityAsync(userId);
    }

    /// <inheritdoc />
    public async Task IncrementAlignmentsAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = GetRedisKey(userId);

        await EnsurePeriodKeyAsync(db, key);
        await db.HashIncrementAsync(key, FieldAlignmentsPeriod);
        await db.HashIncrementAsync(key, FieldAlignmentsTotal);
        await TouchActivityAsync(userId);
    }

    /// <inheritdoc />
    public async Task IncrementCompletedAlignmentsAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        await IncrementFieldAsync(userId, FieldAlignmentsCompleted);
        await TouchActivityAsync(userId);
    }

    #region Private Helpers

    private static string GetRedisKey(UserId userId) => $"usage:{userId}";

    private async Task IncrementFieldAsync(UserId userId, string field)
    {
        var db = _redis.GetDatabase();
        var key = GetRedisKey(userId);
        await db.HashIncrementAsync(key, field);
    }

    private async Task DecrementFieldAsync(UserId userId, string field)
    {
        var db = _redis.GetDatabase();
        var key = GetRedisKey(userId);
        var newValue = await db.HashDecrementAsync(key, field);

        // Ensure non-negative
        if (newValue < 0)
        {
            await db.HashSetAsync(key, field, 0);
        }
    }

    private async Task TouchActivityAsync(UserId userId)
    {
        var db = _redis.GetDatabase();
        var key = GetRedisKey(userId);
        await db.HashSetAsync(key, new[]
        {
            new HashEntry(FieldLastActivity, DateTime.UtcNow.ToString("O")),
            new HashEntry(FieldUpdatedAt, DateTime.UtcNow.ToString("O"))
        });
    }

    private async Task EnsurePeriodKeyAsync(IDatabase db, string key)
    {
        var storedPeriod = await db.HashGetAsync(key, FieldPeriodKey);
        var currentPeriod = BillingPeriodKey.Current().ToString();

        if (storedPeriod.IsNullOrEmpty || storedPeriod.ToString() != currentPeriod)
        {
            // Reset period-specific counters
            await db.HashSetAsync(key, new[]
            {
                new HashEntry(FieldPeriodKey, currentPeriod),
                new HashEntry(FieldTracesThisPeriod, 0),
                new HashEntry(FieldAlignmentsPeriod, 0)
            });
        }
    }

    private static string GetHashValue(HashEntry[] hash, string field)
    {
        var entry = Array.Find(hash, h => h.Name == field);
        return entry.Value.ToString() ?? "";
    }

    private static int GetHashIntValue(HashEntry[] hash, string field)
    {
        var value = GetHashValue(hash, field);
        return int.TryParse(value, out var result) ? result : 0;
    }

    private static long GetHashLongValue(HashEntry[] hash, string field)
    {
        var value = GetHashValue(hash, field);
        return long.TryParse(value, out var result) ? result : 0;
    }

    private static DateTime? GetHashDateTimeValue(HashEntry[] hash, string field)
    {
        var value = GetHashValue(hash, field);
        if (string.IsNullOrEmpty(value)) return null;
        return DateTime.TryParse(value, out var result) ? result : null;
    }

    #endregion
}
