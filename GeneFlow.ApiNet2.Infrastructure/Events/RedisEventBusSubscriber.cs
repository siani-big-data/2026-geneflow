using System.Text.Json;
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
/// <remarks>
/// The per-message handler catch is intentionally broad: handlers come from
/// arbitrary application code and we cannot enumerate the exception types they
/// may raise. Failed messages are not acknowledged so they are redelivered.
/// </remarks>
public sealed class RedisEventBusSubscriber : IEventBusSubscriber
{
    /// <summary>How many messages to fetch per <c>XREADGROUP</c> call.</summary>
    private const int ReadBatchSize = 10;

    /// <summary>Idle-poll delay when the stream returns no new messages.</summary>
    private static readonly TimeSpan IdlePollDelay = TimeSpan.FromSeconds(1);

    /// <summary>Back-off delay before retrying after a transient stream error.</summary>
    private static readonly TimeSpan ErrorBackoffDelay = TimeSpan.FromSeconds(5);

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

        await EnsureConsumerGroupAsync(db, streamName, consumerGroup);

        _logger.LogInformation(
            "Starting subscription to stream {Stream} with consumer group {Group}",
            streamName, consumerGroup);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var entries = await db.StreamReadGroupAsync(
                    streamName,
                    consumerGroup,
                    _consumerName,
                    ">",
                    count: ReadBatchSize);

                if (entries.Length == 0)
                {
                    await Task.Delay(IdlePollDelay, cancellationToken);
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
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (RedisException ex)
            {
                _logger.LogError(ex, "Redis error reading from stream {Stream}", streamName);
                await Task.Delay(ErrorBackoffDelay, cancellationToken);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "I/O error reading from stream {Stream}", streamName);
                await Task.Delay(ErrorBackoffDelay, cancellationToken);
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
            await db.StreamCreateConsumerGroupAsync(
                streamName,
                consumerGroup,
                "0",
                createStream: true);

            _logger.LogDebug(
                "Created consumer group {Group} for stream {Stream}",
                consumerGroup, streamName);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
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

            if (values.ContainsKey("event_type") || values.ContainsKey("event_id"))
            {
                return new EventMessage(
                    MessageId: entry.Id!,
                    EventId: values.GetValueOrDefault("event_id", ""),
                    EventType: values.GetValueOrDefault("event_type", ""),
                    OccurredAt: DateTime.TryParse(values.GetValueOrDefault("occurred_at"), out var dt)
                        ? dt
                        : DateTime.UtcNow,
                    Data: values.GetValueOrDefault("data", "{}"));
            }

            var rawData = values.GetValueOrDefault("data", "{}");
            var eventData = ParsePythonDict(rawData);

            if (eventData is null)
            {
                _logger.LogWarning("Could not parse Python event data: {Data}", rawData);
                return null;
            }

            var eventId = GetJsonString(eventData, "eventId") ?? "";
            var eventType = GetJsonString(eventData, "type") ?? "";
            var innerData = GetJsonString(eventData, "data") ?? "{}";

            var occurredAt = DateTime.UtcNow;
            if (eventData.RootElement.TryGetProperty("timestamp", out var timestampProp))
            {
                if (timestampProp.TryGetInt64(out var timestampMs))
                {
                    occurredAt = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs).UtcDateTime;
                }
            }

            return new EventMessage(
                MessageId: entry.Id!,
                EventId: eventId,
                EventType: eventType,
                OccurredAt: occurredAt,
                Data: innerData);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse event message {MessageId} (invalid JSON)", entry.Id);
            return null;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to parse event message {MessageId} (invalid structure)", entry.Id);
            return null;
        }
    }

    private JsonDocument? ParsePythonDict(string data)
    {
        try
        {
            return JsonDocument.Parse(data);
        }
        catch (JsonException)
        {
            try
            {
                var jsonStr = ConvertPythonDictToJson(data);
                return JsonDocument.Parse(jsonStr);
            }
            catch (JsonException ex)
            {
                _logger.LogDebug(ex, "Failed to parse as Python dict: {Data}", data);
                return null;
            }
        }
    }

    private static string ConvertPythonDictToJson(string pythonDict)
    {
        var result = new System.Text.StringBuilder();
        var inDoubleQuotedString = false;
        var inSingleQuotedString = false;

        for (int i = 0; i < pythonDict.Length; i++)
        {
            char c = pythonDict[i];
            char? prev = i > 0 ? pythonDict[i - 1] : null;

            if (c == '"' && prev != '\\' && !inSingleQuotedString)
            {
                inDoubleQuotedString = !inDoubleQuotedString;
                result.Append(c);
                continue;
            }

            if (c == '"' && inSingleQuotedString && prev != '\\')
            {
                result.Append("\\\"");
                continue;
            }

            if (c == '\'' && !inDoubleQuotedString)
            {
                inSingleQuotedString = !inSingleQuotedString;
                result.Append('"');
                continue;
            }

            if (!inDoubleQuotedString && !inSingleQuotedString)
            {
                if (i + 4 <= pythonDict.Length && pythonDict.Substring(i, 4) == "None")
                {
                    result.Append("null");
                    i += 3;
                    continue;
                }
                if (i + 4 <= pythonDict.Length && pythonDict.Substring(i, 4) == "True")
                {
                    result.Append("true");
                    i += 3;
                    continue;
                }
                if (i + 5 <= pythonDict.Length && pythonDict.Substring(i, 5) == "False")
                {
                    result.Append("false");
                    i += 4;
                    continue;
                }
            }

            result.Append(c);
        }

        return result.ToString();
    }

    private static string? GetJsonString(JsonDocument doc, string propertyName)
    {
        if (doc.RootElement.TryGetProperty(propertyName, out var prop))
        {
            return prop.ValueKind == JsonValueKind.String
                ? prop.GetString()
                : prop.GetRawText();
        }
        return null;
    }
}
