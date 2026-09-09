using System.Text.Json;
using GeneFlow.ApiNet2.Application.Pipelines.Events;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace GeneFlow.ApiNet2.Infrastructure.Pipelines.Sse;

/// <summary>
/// Background service that listens to <c>geneflow:events:pipelines</c> and
/// fans every pipeline execution lifecycle event out to in-process SSE
/// subscribers via <see cref="PipelineExecutionEventBroker"/>.
/// </summary>
/// <remarks>
/// Uses an independent consumer group so it does not steal events from the
/// existing pipeline projections — Redis Streams delivers a copy of every
/// event to each consumer group.
/// </remarks>
public sealed class PipelineExecutionSseBroadcaster : BackgroundService
{
    private const string Category = "pipelines";
    private const string ConsumerGroup = "pipeline-execution-sse-broadcaster";

    // The pipelines category receives two flavors of the same lifecycle event:
    //   * Worker-published (Python): bare class names like "PipelineStepCompleted".
    //   * .NET-republished by DomainEventDispatcher: same names with the
    //     "Event" suffix because RedisEventBusPublisher uses Type.Name.
    // Accept both so the SSE fan-out works regardless of which side fires.
    // Note: the Python worker also emits PipelineStepStarted / PipelineStepFailed
    // (no execution-level "started" event), so we map step-started → execution.started
    // and step-failed → execution.failed for the SSE clients.
    private const string ExecutionStartedEventType = "PipelineExecutionStarted";
    private const string ExecutionStartedEventTypeNet = "PipelineExecutionStartedEvent";
    private const string StepStartedEventType = "PipelineStepStarted";
    private const string StepStartedEventTypeNet = "PipelineStepStartedEvent";
    private const string StepCompletedEventType = "PipelineStepCompleted";
    private const string StepCompletedEventTypeNet = "PipelineStepCompletedEvent";
    private const string StepFailedEventType = "PipelineStepFailed";
    private const string StepFailedEventTypeNet = "PipelineStepFailedEvent";
    private const string ExecutionCompletedEventType = "PipelineExecutionCompleted";
    private const string ExecutionCompletedEventTypeNet = "PipelineExecutionCompletedEvent";
    private const string ExecutionFailedEventType = "PipelineExecutionFailed";
    private const string ExecutionFailedEventTypeNet = "PipelineExecutionFailedEvent";

    private const string StartedClientName = "execution.started";
    private const string StepCompletedClientName = "execution.step.completed";
    private const string CompletedClientName = "execution.completed";
    private const string FailedClientName = "execution.failed";

    private readonly IEventBusSubscriber _subscriber;
    private readonly PipelineExecutionEventBroker _broker;
    private readonly ILogger<PipelineExecutionSseBroadcaster> _logger;

    public PipelineExecutionSseBroadcaster(
        IEventBusSubscriber subscriber,
        PipelineExecutionEventBroker broker,
        ILogger<PipelineExecutionSseBroadcaster> logger)
    {
        _subscriber = subscriber;
        _broker = broker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Pipeline execution SSE broadcaster starting...");

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
            _logger.LogError(ex, "Redis error in pipeline execution SSE broadcaster");
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error in pipeline execution SSE broadcaster");
        }

        _logger.LogInformation("Pipeline execution SSE broadcaster stopped");
    }

    private async Task HandleEventAsync(EventMessage message)
    {
        try
        {
            if (!TryMapEventName(message.EventType, out var clientEventName, out var success))
            {
                return;
            }

            var fields = ExtractPayloadFields(message.Data);
            if (string.IsNullOrEmpty(fields.ExecutionId))
            {
                _logger.LogWarning(
                    "Pipeline event {EventType} (ID {EventId}) is missing executionId; skipping SSE fan-out",
                    message.EventType, message.EventId);
                return;
            }

            var dto = new PipelineExecutionEventDto(
                EventType: clientEventName,
                ExecutionId: fields.ExecutionId,
                PipelineId: fields.PipelineId,
                TraceId: fields.TraceId,
                CompletedSteps: fields.CompletedSteps,
                TotalSteps: fields.TotalSteps,
                StepType: fields.StepType,
                Success: success,
                Error: success ? null : fields.Error,
                OccurredAt: new DateTimeOffset(DateTime.SpecifyKind(message.OccurredAt, DateTimeKind.Utc), TimeSpan.Zero));

            await _broker.PublishAsync(fields.ExecutionId, dto);
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Failed to parse pipeline event {EventType} (ID {EventId}) for SSE fan-out",
                message.EventType, message.EventId);
            // Do not rethrow — domain projections handle persistence; SSE is best-effort.
        }
    }

    private static bool TryMapEventName(string eventType, out string clientEventName, out bool success)
    {
        switch (eventType)
        {
            case ExecutionStartedEventType:
            case ExecutionStartedEventTypeNet:
            case StepStartedEventType:
            case StepStartedEventTypeNet:
                // Worker emits PipelineStepStarted (one per step), no execution-level
                // "started" — surface the first step start as execution.started for clients.
                clientEventName = StartedClientName;
                success = true;
                return true;
            case StepCompletedEventType:
            case StepCompletedEventTypeNet:
                clientEventName = StepCompletedClientName;
                success = true;
                return true;
            case ExecutionCompletedEventType:
            case ExecutionCompletedEventTypeNet:
                clientEventName = CompletedClientName;
                success = true;
                return true;
            case StepFailedEventType:
            case StepFailedEventTypeNet:
            case ExecutionFailedEventType:
            case ExecutionFailedEventTypeNet:
                clientEventName = FailedClientName;
                success = false;
                return true;
            default:
                clientEventName = string.Empty;
                success = false;
                return false;
        }
    }

    private readonly record struct PayloadFields(
        string? ExecutionId,
        string? PipelineId,
        string? TraceId,
        int? CompletedSteps,
        int? TotalSteps,
        string? StepType,
        string? Error);

    private static PayloadFields ExtractPayloadFields(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return new PayloadFields(null, null, null, null, null, null, null);

        using var doc = JsonDocument.Parse(data);
        var root = doc.RootElement;

        return new PayloadFields(
            ExecutionId: ReadString(root, "executionId") ?? ReadString(root, "execution_id"),
            PipelineId: ReadString(root, "pipelineId") ?? ReadString(root, "pipeline_id"),
            TraceId: ReadString(root, "traceId") ?? ReadString(root, "trace_id"),
            CompletedSteps: ReadInt(root, "completedSteps") ?? ReadInt(root, "completed_steps"),
            TotalSteps: ReadInt(root, "totalSteps") ?? ReadInt(root, "total_steps"),
            StepType: ReadString(root, "stepType") ?? ReadString(root, "step_type"),
            Error: ReadString(root, "errorMessage")
                ?? ReadString(root, "error_message")
                ?? ReadString(root, "error")
                ?? ReadString(root, "reason")
                ?? ReadString(root, "message"));
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;
        if (!element.TryGetProperty(propertyName, out var prop))
            return null;
        return ExtractString(prop);
    }

    // Strongly-typed IDs (ExecutionId/PipelineId/TraceId) round-trip through
    // SnakeCaseLower as { "value": "..." } when the .NET side republishes
    // the event. Worker-published events send a flat string. Accept both.
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

    private static int? ReadInt(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;
        if (!element.TryGetProperty(propertyName, out var prop))
            return null;
        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var value))
            return value;
        return null;
    }
}
