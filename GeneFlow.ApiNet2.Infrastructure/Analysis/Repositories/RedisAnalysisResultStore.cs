using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace GeneFlow.ApiNet2.Infrastructure.Analysis.Repositories;

/// <summary>
/// Redis-backed implementation of <see cref="IAnalysisResultStore"/>.
/// Stores each analysis result as a hash and maintains a per-trace index set
/// so listing the available result types is a single O(1) Redis call.
/// </summary>
public sealed class RedisAnalysisResultStore : IAnalysisResultStore
{
    /// <summary>Hash field holding the JSON payload.</summary>
    private const string FieldPayload = "payload";

    /// <summary>Hash field holding the canonical analysis type.</summary>
    private const string FieldAnalysisType = "analysis_type";

    /// <summary>Hash field holding the UTC ISO-8601 timestamp of the last write.</summary>
    private const string FieldStoredAt = "stored_at";

    /// <summary>TTL applied to every stored payload and to the per-trace index set.</summary>
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromDays(30);

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisAnalysisResultStore> _logger;

    public RedisAnalysisResultStore(
        IConnectionMultiplexer redis,
        ILogger<RedisAnalysisResultStore> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SaveAsync(
        string traceId,
        string analysisType,
        string payloadJson,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(traceId))
            throw new ArgumentException("Trace id cannot be empty.", nameof(traceId));
        if (string.IsNullOrWhiteSpace(analysisType))
            throw new ArgumentException("Analysis type cannot be empty.", nameof(analysisType));
        ArgumentNullException.ThrowIfNull(payloadJson);

        var normalisedType = NormaliseType(analysisType);
        var resultKey = GetResultKey(traceId, normalisedType);
        var indexKey = GetIndexKey(traceId);
        var storedAt = DateTime.UtcNow.ToString("O");

        try
        {
            var db = _redis.GetDatabase();

            var entries = new HashEntry[]
            {
                new(FieldPayload, payloadJson),
                new(FieldAnalysisType, normalisedType),
                new(FieldStoredAt, storedAt)
            };

            var batch = db.CreateBatch();
            var hashSetTask = batch.HashSetAsync(resultKey, entries);
            var hashExpireTask = batch.KeyExpireAsync(resultKey, DefaultTtl);
            var indexAddTask = batch.SetAddAsync(indexKey, normalisedType);
            var indexExpireTask = batch.KeyExpireAsync(indexKey, DefaultTtl);
            batch.Execute();

            await Task.WhenAll(hashSetTask, hashExpireTask, indexAddTask, indexExpireTask);

            _logger.LogInformation(
                "Persisted analysis result {AnalysisType} for trace {TraceId} ({Bytes} bytes)",
                normalisedType, traceId, payloadJson.Length);
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex,
                "Redis error storing analysis result {AnalysisType} for trace {TraceId}",
                normalisedType, traceId);
            throw;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex,
                "I/O error storing analysis result {AnalysisType} for trace {TraceId}",
                normalisedType, traceId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetAsync(
        string traceId,
        string analysisType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(traceId))
            return null;
        if (string.IsNullOrWhiteSpace(analysisType))
            return null;

        var normalisedType = NormaliseType(analysisType);
        var resultKey = GetResultKey(traceId, normalisedType);

        try
        {
            var db = _redis.GetDatabase();
            var payload = await db.HashGetAsync(resultKey, FieldPayload);
            return payload.IsNullOrEmpty ? null : payload.ToString();
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex,
                "Redis error reading analysis result {AnalysisType} for trace {TraceId}",
                normalisedType, traceId);
            return null;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex,
                "I/O error reading analysis result {AnalysisType} for trace {TraceId}",
                normalisedType, traceId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AnalysisResultMetadata>> ListAsync(
        string traceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(traceId))
            return Array.Empty<AnalysisResultMetadata>();

        var indexKey = GetIndexKey(traceId);

        try
        {
            var db = _redis.GetDatabase();
            var members = await db.SetMembersAsync(indexKey);
            if (members.Length == 0)
                return Array.Empty<AnalysisResultMetadata>();

            var results = new List<AnalysisResultMetadata>(members.Length);
            foreach (var member in members)
            {
                var type = member.ToString();
                if (string.IsNullOrEmpty(type))
                    continue;

                var resultKey = GetResultKey(traceId, type);
                var storedAtRaw = await db.HashGetAsync(resultKey, FieldStoredAt);

                if (storedAtRaw.IsNullOrEmpty)
                {
                    await db.SetRemoveAsync(indexKey, member);
                    continue;
                }

                var storedAt = DateTime.TryParse(
                    storedAtRaw.ToString(),
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out var parsed)
                    ? parsed
                    : DateTime.UtcNow;

                results.Add(new AnalysisResultMetadata(type, storedAt));
            }

            return results;
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Redis error listing analysis results for trace {TraceId}", traceId);
            return Array.Empty<AnalysisResultMetadata>();
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error listing analysis results for trace {TraceId}", traceId);
            return Array.Empty<AnalysisResultMetadata>();
        }
    }

    /// <inheritdoc />
    public async Task DeleteAllAsync(
        string traceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(traceId))
            return;

        var indexKey = GetIndexKey(traceId);

        try
        {
            var db = _redis.GetDatabase();
            var members = await db.SetMembersAsync(indexKey);

            foreach (var member in members)
            {
                var type = member.ToString();
                if (string.IsNullOrEmpty(type))
                    continue;
                await db.KeyDeleteAsync(GetResultKey(traceId, type));
            }

            await db.KeyDeleteAsync(indexKey);
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Redis error deleting analysis results for trace {TraceId}", traceId);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error deleting analysis results for trace {TraceId}", traceId);
        }
    }

    private static string NormaliseType(string analysisType)
        => analysisType.Trim().ToLowerInvariant();

    private static string GetResultKey(string traceId, string normalisedType)
        => $"analysis:{traceId}:{normalisedType}";

    private static string GetIndexKey(string traceId)
        => $"analysis:index:{traceId}";
}
