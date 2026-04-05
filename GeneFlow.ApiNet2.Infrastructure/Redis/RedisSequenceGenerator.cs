using GeneFlow.ApiNet2.Infrastructure.Redis.Configuration;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace GeneFlow.ApiNet2.Infrastructure.Redis;

/// <summary>
/// Redis-based sequence generator using INCR for atomic, distributed sequence generation.
/// </summary>
public sealed class RedisSequenceGenerator : ISequenceGenerator
{
    private readonly IConnectionMultiplexer _redis;
    private readonly RedisSettings _settings;

    /// <summary>
    /// Initializes a new instance of the RedisSequenceGenerator.
    /// </summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    /// <param name="settings">Redis settings.</param>
    public RedisSequenceGenerator(
        IConnectionMultiplexer redis,
        IOptions<RedisSettings> settings)
    {
        _redis = redis;
        _settings = settings.Value;
    }

    /// <inheritdoc />
    public async Task<long> NextAsync(string sequenceName, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = GetSequenceKey(sequenceName);

        // INCR is atomic and returns the new value
        return await db.StringIncrementAsync(key);
    }

    /// <inheritdoc />
    public async Task<long> CurrentAsync(string sequenceName, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = GetSequenceKey(sequenceName);

        var value = await db.StringGetAsync(key);
        return value.HasValue ? (long)value : 0;
    }

    private string GetSequenceKey(string sequenceName)
    {
        return $"{_settings.SequenceKeyPrefix}{sequenceName}";
    }
}
