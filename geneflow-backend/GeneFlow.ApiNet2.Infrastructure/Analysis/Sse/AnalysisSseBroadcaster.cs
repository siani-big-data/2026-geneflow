using System.Text.Json;
using GeneFlow.ApiNet2.Application.Analysis.Events;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace GeneFlow.ApiNet2.Infrastructure.Analysis.Sse;

/// <summary>
/// Background service that listens to <c>geneflow:events:analysis</c> and
/// fans every <c>*Completed</c> / <c>*Failed</c> event out to in-process SSE
/// subscribers via <see cref="AnalysisEventBroker"/>.
/// </summary>
/// <remarks>
/// Uses an independent consumer group so it does not steal events from the
/// existing <c>analysis-event-processor</c> or <c>pipeline-orchestrator-events</c>
/// consumers — Redis Streams delivers a copy of every event to each group.
/// </remarks>
public sealed class AnalysisSseBroadcaster : BackgroundService
{
    private const string Category = "analysis";
    private const string ConsumerGroup = "analysis-sse-broadcaster";

    private readonly IEventBusSubscriber _subscriber;
    private readonly AnalysisEventBroker _broker;
    private readonly ILogger<AnalysisSseBroadcaster> _logger;

    public AnalysisSseBroadcaster(
        IEventBusSubscriber subscriber,
        AnalysisEventBroker broker,
        ILogger<AnalysisSseBroadcaster> logger)
    {
        _subscriber = subscriber;
        _broker = broker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Analysis SSE broadcaster starting...");

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
            _logger.LogError(ex, "Redis error in analysis SSE broadcaster");
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error in analysis SSE broadcaster");
        }

        _logger.LogInformation("Analysis SSE broadcaster stopped");
    }

    private async Task HandleEventAsync(EventMessage message)
    {
        try
        {
            if (!AnalysisEventTypeMap.TryResolve(message.EventType, out var analysisKey, out var success))
            {
                // Not a completion event we care about (could be a domain
                // notification consumed by another group). Ignore silently.
                return;
            }

            var (traceId, error) = ExtractPayloadFields(message.Data);
            if (string.IsNullOrEmpty(traceId))
            {
                _logger.LogWarning(
                    "Analysis event {EventType} (ID {EventId}) is missing traceId; skipping SSE fan-out",
                    message.EventType, message.EventId);
                return;
            }

            var dto = new AnalysisEventDto(
                EventType: message.EventType,
                AnalysisKey: analysisKey,
                Success: success,
                Error: success ? null : error,
                OccurredAt: new DateTimeOffset(DateTime.SpecifyKind(message.OccurredAt, DateTimeKind.Utc), TimeSpan.Zero));

            await _broker.PublishAsync(traceId, dto);
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Failed to parse analysis event {EventType} (ID {EventId}) for SSE fan-out",
                message.EventType, message.EventId);
            // Do not rethrow: the canonical processors will handle/retry; we
            // do not want SSE failures to block the redis ack here.
        }
    }

    private static (string? TraceId, string? Error) ExtractPayloadFields(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return (null, null);

        using var doc = JsonDocument.Parse(data);
        var root = doc.RootElement;

        var traceId = ReadString(root, "traceId") ?? ReadString(root, "trace_id");
        var error = ReadString(root, "error") ?? ReadString(root, "errorMessage") ?? ReadString(root, "message");
        return (traceId, error);
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;
        if (!element.TryGetProperty(propertyName, out var prop))
            return null;
        return prop.ValueKind == JsonValueKind.String ? prop.GetString() : null;
    }
}
