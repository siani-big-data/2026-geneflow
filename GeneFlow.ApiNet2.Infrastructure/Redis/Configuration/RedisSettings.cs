namespace GeneFlow.ApiNet2.Infrastructure.Redis.Configuration;

/// <summary>
/// Redis connection settings.
/// </summary>
public sealed class RedisSettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Redis";

    /// <summary>Gets or sets the Redis connection string.</summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>Gets or sets the prefix for sequence keys.</summary>
    public string SequenceKeyPrefix { get; set; } = "geneflow:seq:";

    /// <summary>Gets or sets the prefix for event stream keys.</summary>
    public string EventStreamPrefix { get; set; } = "geneflow:events:";

    /// <summary>Gets or sets the prefix for job stream keys.</summary>
    public string JobStreamPrefix { get; set; } = "geneflow:jobs:";

    /// <summary>Gets or sets the connection timeout in milliseconds.</summary>
    public int ConnectTimeoutMs { get; set; } = 5000;

    /// <summary>Gets or sets the sync timeout in milliseconds.</summary>
    public int SyncTimeoutMs { get; set; } = 5000;
}
