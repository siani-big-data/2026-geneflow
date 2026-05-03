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

            // Check if we have the .NET format (separate fields)
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

            // Python format: all event data is inside a single "data" field
            var rawData = values.GetValueOrDefault("data", "{}");
            var eventData = ParsePythonDict(rawData);

            if (eventData is null)
            {
                _logger.LogWarning("Could not parse Python event data: {Data}", rawData);
                return null;
            }

            // Extract fields from the parsed Python event
            var eventId = GetJsonString(eventData, "eventId") ?? "";
            var eventType = GetJsonString(eventData, "type") ?? "";
            var innerData = GetJsonString(eventData, "data") ?? "{}";

            // Parse timestamp (Python sends milliseconds since epoch)
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse event message {MessageId}", entry.Id);
            return null;
        }
    }

    private JsonDocument? ParsePythonDict(string data)
    {
        try
        {
            // First try direct JSON parse
            return JsonDocument.Parse(data);
        }
        catch
        {
            try
            {
                // Convert Python dict string to JSON using a smarter approach
                // that handles nested JSON strings with double quotes
                var jsonStr = ConvertPythonDictToJson(data);
                return JsonDocument.Parse(jsonStr);
            }
            catch (Exception ex)
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

            // Track if we're inside a double-quoted string (only when NOT in single-quoted)
            if (c == '"' && prev != '\\' && !inSingleQuotedString)
            {
                inDoubleQuotedString = !inDoubleQuotedString;
                result.Append(c);
                continue;
            }

            // Handle double quotes INSIDE single-quoted strings - must escape them
            if (c == '"' && inSingleQuotedString && prev != '\\')
            {
                result.Append("\\\"");
                continue;
            }

            // Handle single quotes (Python dict delimiters)
            if (c == '\'' && !inDoubleQuotedString)
            {
                if (!inSingleQuotedString)
                {
                    // Starting a single-quoted string - convert to double quote
                    inSingleQuotedString = true;
                    result.Append('"');
                }
                else
                {
                    // Ending a single-quoted string - convert to double quote
                    inSingleQuotedString = false;
                    result.Append('"');
                }
                continue;
            }

            // Handle Python keywords (only outside of quoted strings)
            if (!inDoubleQuotedString && !inSingleQuotedString)
            {
                // Check for None
                if (i + 4 <= pythonDict.Length && pythonDict.Substring(i, 4) == "None")
                {
                    result.Append("null");
                    i += 3;
                    continue;
                }
                // Check for True
                if (i + 4 <= pythonDict.Length && pythonDict.Substring(i, 4) == "True")
                {
                    result.Append("true");
                    i += 3;
                    continue;
                }
                // Check for False
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
