using System.Text.Json;
using System.Text.Json.Serialization;
using GeneFlow.ApiNet2.Infrastructure.Redis.Configuration;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace GeneFlow.ApiNet2.Infrastructure.Events;

/// <summary>
/// Publishes domain events to Redis Streams for the event-driven architecture.
/// Events are published to streams named {prefix}{category} (e.g., geneflow:events:users).
/// </summary>
public sealed class RedisEventBusPublisher : IEventBusPublisher
{
    private readonly IConnectionMultiplexer _redis;
    private readonly RedisSettings _settings;
    private readonly ILogger<RedisEventBusPublisher> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        Converters = { new PrefixedIdJsonConverter() }
    };

    /// <summary>
    /// Initializes a new instance of the RedisEventBusPublisher.
    /// </summary>
    public RedisEventBusPublisher(
        IConnectionMultiplexer redis,
        IOptions<RedisSettings> settings,
        ILogger<RedisEventBusPublisher> logger)
    {
        _redis = redis;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PublishAsync(
        IDomainEvent domainEvent,
        string category,
        CancellationToken cancellationToken = default)
    {
        var streamName = GetStreamName(category);
        var eventType = domainEvent.GetType().Name;

        try
        {
            var db = _redis.GetDatabase();

            var payload = new EventPayload
            {
                EventId = domainEvent.EventId.ToString(),
                EventType = eventType,
                OccurredAt = domainEvent.OccurredAt.ToString("O"),
                Data = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions)
            };

            var entries = new NameValueEntry[]
            {
                new("event_id", payload.EventId),
                new("event_type", payload.EventType),
                new("occurred_at", payload.OccurredAt),
                new("data", payload.Data)
            };

            var messageId = await db.StreamAddAsync(streamName, entries);

            _logger.LogDebug(
                "Published event {EventType} to stream {Stream} with ID {MessageId}",
                eventType,
                streamName,
                messageId);
        }
        catch (RedisException ex)
        {
            _logger.LogError(
                ex,
                "Redis error publishing event {EventType} to stream {Stream}",
                eventType,
                streamName);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Failed to serialize event {EventType} for stream {Stream}",
                eventType,
                streamName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task PublishBatchAsync(
        IEnumerable<(IDomainEvent Event, string Category)> events,
        CancellationToken cancellationToken = default)
    {
        var eventsList = events.ToList();
        if (eventsList.Count == 0)
        {
            return;
        }

        _logger.LogDebug("Publishing batch of {Count} events", eventsList.Count);

        foreach (var (domainEvent, category) in eventsList)
        {
            await PublishAsync(domainEvent, category, cancellationToken);
        }
    }

    private string GetStreamName(string category)
    {
        return $"{_settings.EventStreamPrefix}{category}";
    }

    private sealed class EventPayload
    {
        public required string EventId { get; init; }
        public required string EventType { get; init; }
        public required string OccurredAt { get; init; }
        public required string Data { get; init; }
    }
}
