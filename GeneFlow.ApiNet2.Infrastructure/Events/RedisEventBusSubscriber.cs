using GeneFlow.ApiNet2.Infrastructure.Redis.Configuration;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace GeneFlow.ApiNet2.Infrastructure.Events;

/// <summary>
/// Subscribes to domain events from Redis Streams.
/// Uses consumer groups for reliable event processing.
/// </summary>
public sealed class RedisEventBusSubscriber : IEventBusSubscriber
{
    private readonly IConnectionMultiplexer _redis;
    private readonly RedisSettings _settings;
    private readonly ILogger<RedisEventBusSubscriber> _logger;
    private readonly string _consumerName;

    public RedisEventBusSubscriber(
        IConnectionMultiplexer redis,
        IOptions<RedisSettings> settings,
        ILogger<RedisEventBusSubscriber> logger)
    {
        _redis = redis;
        _settings = settings.Value;
        _logger = logger;
        _consumerName = $"consumer-{Environment.MachineName}-{Guid.NewGuid():N}";
    }

    /// <inheritdoc />
    public async Task SubscribeAsync(
        string category,
        string consumerGroup,
        Func<EventMessage, Task> handler,
        CancellationToken cancellationToken = default)
    {
        var streamName = GetStreamName(category);
        var db = _redis.GetDatabase();

        // Ensure consumer group exists
        await EnsureConsumerGroupAsync(db, streamName, consumerGroup);

        _logger.LogInformation(
            "Starting subscription to stream {Stream} with consumer group {Group}",
            streamName, consumerGroup);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Read pending messages first, then new messages
                var entries = await db.StreamReadGroupAsync(
                    streamName,
                    consumerGroup,
                    _consumerName,
                    ">", // Read only new messages
                    count: 10);

                if (entries.Length == 0)
                {
                    // No messages, wait a bit before polling again
                    await Task.Delay(1000, cancellationToken);
                    continue;
                }

                foreach (var entry in entries)
                {
                    try
                    {
                        var message = ParseEventMessage(entry);
                        if (message is not null)
                        {
                            await handler(message);
                            await AcknowledgeAsync(category, consumerGroup, entry.Id!, cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Failed to process message {MessageId} from stream {Stream}",
                            entry.Id, streamName);
                        // Don't acknowledge - message will be reprocessed
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading from stream {Stream}", streamName);
                await Task.Delay(5000, cancellationToken); // Back off on errors
            }
        }

        _logger.LogInformation("Stopped subscription to stream {Stream}", streamName);
    }

    /// <inheritdoc />
    public async Task AcknowledgeAsync(
        string category,
        string consumerGroup,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        var streamName = GetStreamName(category);
        var db = _redis.GetDatabase();

        await db.StreamAcknowledgeAsync(streamName, consumerGroup, messageId);
    }

    private async Task EnsureConsumerGroupAsync(IDatabase db, string streamName, string consumerGroup)
    {
        try
        {
            // Try to create the consumer group
            await db.StreamCreateConsumerGroupAsync(
                streamName,
                consumerGroup,
                "0", // Start from beginning
                createStream: true);

            _logger.LogDebug(
                "Created consumer group {Group} for stream {Stream}",
                consumerGroup, streamName);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            // Consumer group already exists, this is fine
            _logger.LogDebug(
                "Consumer group {Group} already exists for stream {Stream}",
                consumerGroup, streamName);
        }
    }

    private string GetStreamName(string category)
    {
        return $"{_settings.EventStreamPrefix}{category}";
    }

    private EventMessage? ParseEventMessage(StreamEntry entry)
    {
        try
        {
            var values = entry.Values.ToDictionary(
                v => v.Name.ToString(),
                v => v.Value.ToString());

            return new EventMessage(
                MessageId: entry.Id!,
                EventId: values.GetValueOrDefault("event_id", ""),
                EventType: values.GetValueOrDefault("event_type", ""),
                OccurredAt: DateTime.TryParse(values.GetValueOrDefault("occurred_at"), out var dt)
                    ? dt
                    : DateTime.UtcNow,
                Data: values.GetValueOrDefault("data", "{}"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse event message {MessageId}", entry.Id);
            return null;
        }
    }
}
