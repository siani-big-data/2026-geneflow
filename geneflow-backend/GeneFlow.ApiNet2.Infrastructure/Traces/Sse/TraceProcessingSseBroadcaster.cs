using System.Text.Json;
using GeneFlow.ApiNet2.Application.Traces.Events;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace GeneFlow.ApiNet2.Infrastructure.Traces.Sse;

/// <summary>
/// Background service that listens to <c>geneflow:events:traces</c> and fans
/// every <c>TraceProcessingStartedEvent</c>, <c>TraceProcessedEvent</c>, and
/// <c>TraceProcessingFailedEvent</c> out to in-process SSE subscribers via
/// <see cref="TraceProcessingEventBroker"/>.
/// </summary>
/// <remarks>
/// Uses an independent consumer group so it does not steal events from the
/// existing trace projections — Redis Streams delivers a copy of every event
/// to each consumer group.
/// </remarks>
public sealed class TraceProcessingSseBroadcaster : BackgroundService
{
    private const string Category = "traces";
    private const string ConsumerGroup = "trace-processing-sse-broadcaster";

    // The trace category receives two flavors of the same lifecycle event:
    //   * Worker-published (Python): bare class names like "TraceProcessed".
    //   * .NET-republished by DomainEventDispatcher: same names with the
    //     "Event" suffix because RedisEventBusPublisher uses Type.Name.
    // Accept both so the SSE fan-out works regardless of which side fires.
    private const string StartedEventType = "TraceProcessingStarted";
    private const string StartedEventTypeNet = "TraceProcessingStartedEvent";
    private const string CompletedEventType = "TraceProcessed";
    private const string CompletedEventTypeNet = "TraceProcessedEvent";
    private const string FailedEventType = "TraceProcessingFailed";
    private const string FailedEventTypeNet = "TraceProcessingFailedEvent";

    private const string StartedClientName = "trace.processing.started";
    private const string CompletedClientName = "trace.processing.completed";
    private const string FailedClientName = "trace.processing.failed";

    private readonly IEventBusSubscriber _subscriber;
    private readonly TraceProcessingEventBroker _broker;
    private readonly ILogger<TraceProcessingSseBroadcaster> _logger;

    public TraceProcessingSseBroadcaster(
        IEventBusSubscriber subscriber,
        TraceProcessingEventBroker broker,
        ILogger<TraceProcessingSseBroadcaster> logger)
    {
        _subscriber = subscriber;
        _broker = broker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Trace processing SSE broadcaster starting...");

        try
        {
            await _subscriber.SubscribeAsync(
                Category,
                ConsumerGroup,
                HandleEventAsync,
                stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Redis error in trace processing SSE broadcaster");
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error in trace processing SSE broadcaster");
        }

        _logger.LogInformation("Trace processing SSE broadcaster stopped");
    }

    private async Task HandleEventAsync(EventMessage message)
    {
        try
        {
            if (!TryMapEventName(message.EventType, out var clientEventName, out var success, out var isTerminal))
            {
                // Not a trace-processing transition we care about; ignore.
                return;
            }

            var (traceId, studyId, qualityScore, error) = ExtractPayloadFields(message.Data);
            if (string.IsNullOrEmpty(traceId))
            {
                _logger.LogWarning(
                    "Trace event {EventType} (ID {EventId}) is missing traceId; skipping SSE fan-out",
                    message.EventType, message.EventId);
                return;
            }

            var dto = new TraceProcessingEventDto(
                EventType: clientEventName,
                TraceId: traceId,
                StudyId: studyId,
                Success: success,
                Error: isTerminal && !success ? error : null,
                QualityScore: success && isTerminal ? qualityScore : null,
                OccurredAt: new DateTimeOffset(DateTime.SpecifyKind(message.OccurredAt, DateTimeKind.Utc), TimeSpan.Zero));

            await _broker.PublishAsync(traceId, dto);
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Failed to parse trace event {EventType} (ID {EventId}) for SSE fan-out",
                message.EventType, message.EventId);
            // Do not rethrow — domain projections handle persistence; SSE is best-effort.
        }
    }

    private static bool TryMapEventName(string eventType, out string clientEventName, out bool success, out bool isTerminal)
    {
        switch (eventType)
        {
            case StartedEventType:
            case StartedEventTypeNet:
                clientEventName = StartedClientName;
                success = true;
                isTerminal = false;
                return true;
            case CompletedEventType:
            case CompletedEventTypeNet:
                clientEventName = CompletedClientName;
                success = true;
                isTerminal = true;
                return true;
            case FailedEventType:
            case FailedEventTypeNet:
                clientEventName = FailedClientName;
                success = false;
                isTerminal = true;
                return true;
            default:
                clientEventName = string.Empty;
                success = false;
                isTerminal = false;
                return false;
        }
    }

    private static (string? TraceId, string? StudyId, double? QualityScore, string? Error) ExtractPayloadFields(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return (null, null, null, null);

        using var doc = JsonDocument.Parse(data);
        var root = doc.RootElement;

        var traceId = ReadString(root, "traceId") ?? ReadString(root, "trace_id");
        var studyId = ReadString(root, "studyId") ?? ReadString(root, "study_id");
        var qualityScore = ReadDouble(root, "qualityScore")
            ?? ReadDouble(root, "averageQualityScore")
            ?? ReadDouble(root, "average_quality_score");
        var error = ReadString(root, "reason")
            ?? ReadString(root, "error")
            ?? ReadString(root, "errorMessage")
            ?? ReadString(root, "message");

        return (traceId, studyId, qualityScore, error);
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;
        if (!element.TryGetProperty(propertyName, out var prop))
            return null;
        return ExtractString(prop);
    }

    // Strongly-typed IDs (TraceId/StudyId) round-trip through SnakeCaseLower
    // as { "value": "trace_xxx" } when the .NET side republishes the event.
    // Worker-published events send a flat string. Accept both shapes.
    private static string? ExtractString(JsonElement prop)
    {
        if (prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        if (prop.ValueKind == JsonValueKind.Object)
        {
            if (prop.TryGetProperty("value", out var inner) && inner.ValueKind == JsonValueKind.String)
                return inner.GetString();
            if (prop.TryGetProperty("Value", out var innerPascal) && innerPascal.ValueKind == JsonValueKind.String)
                return innerPascal.GetString();
        }
        return null;
    }

    private static double? ReadDouble(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;
        if (!element.TryGetProperty(propertyName, out var prop))
            return null;
        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDouble(out var value))
            return value;
        if (prop.ValueKind == JsonValueKind.String &&
            double.TryParse(prop.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        return null;
    }
}
