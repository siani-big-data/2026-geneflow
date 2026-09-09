using System.Text.Json;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace GeneFlow.ApiNet2.Infrastructure.Pipelines.Orchestration;

/// <summary>
/// Background service that listens to <c>geneflow:events:analysis</c> events
/// (Trimming/Heterozygote/Motif/Translation/ORF/Restriction completed/failed)
/// and resolves the matching waiter on
/// <see cref="PipelineStepCompletionRegistry"/>. It uses an independent
/// consumer group so it does not steal events from the existing
/// <c>analysis-event-processor</c>; both services receive every event.
/// Unhandled events are silently ignored (registry returns false).
/// </summary>
/// <remarks>
/// The per-message handler catch is intentionally broad and rethrows so the
/// message is not acknowledged and is redelivered. Mirrors
/// <c>AnalysisEventProcessor</c>.
/// </remarks>
public sealed class PipelineEventListener : BackgroundService
{
    private readonly IEventBusSubscriber _subscriber;
    private readonly PipelineStepCompletionRegistry _registry;
    private readonly ILogger<PipelineEventListener> _logger;

    private const string Category = "analysis";
    private const string ConsumerGroup = "pipeline-orchestrator-events";

    /// <summary>
    /// Maps each Python analysis-completed event to its corresponding
    /// <see cref="Domain.Pipelines.Enumerations.StepType.AnalysisKey"/> so the
    /// orchestrator can look up the waiting step.
    /// </summary>
    private static readonly Dictionary<string, string> CompletedEventToAnalysisKey =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["TrimmingCompleted"] = "trimming",
            ["HeterozygoteDetectionCompleted"] = "heterozygote",
            ["MotifSearchCompleted"] = "motif",
            ["TranslationCompleted"] = "translation",
            ["ORFDetectionCompleted"] = "orf",
            ["RestrictionAnalysisCompleted"] = "restriction"
        };

    private static readonly Dictionary<string, string> FailedEventToAnalysisKey =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["TrimmingFailed"] = "trimming",
            ["HeterozygoteDetectionFailed"] = "heterozygote",
            ["MotifSearchFailed"] = "motif",
            ["TranslationFailed"] = "translation",
            ["ORFDetectionFailed"] = "orf",
            ["RestrictionAnalysisFailed"] = "restriction"
        };

    public PipelineEventListener(
        IEventBusSubscriber subscriber,
        PipelineStepCompletionRegistry registry,
        ILogger<PipelineEventListener> logger)
    {
        _subscriber = subscriber;
        _registry = registry;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Pipeline Event Listener starting...");

        try
        {
            await _subscriber.SubscribeAsync(
                Category,
                ConsumerGroup,
                async message => await HandleEventAsync(message),
                stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Redis error in pipeline event listener");
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error in pipeline event listener");
        }

        _logger.LogInformation("Pipeline Event Listener stopped");
    }

    private Task HandleEventAsync(EventMessage message)
    {
        try
        {
            if (CompletedEventToAnalysisKey.TryGetValue(message.EventType, out var successKey))
            {
                NotifyRegistry(message, successKey, success: true, error: null);
            }
            else if (FailedEventToAnalysisKey.TryGetValue(message.EventType, out var failKey))
            {
                var error = ExtractError(message.Data);
                NotifyRegistry(message, failKey, success: false, error: error);
            }
            // Unknown / unmatched events are ignored — they belong to other consumers.
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process analysis event {EventType} (ID: {EventId})",
                message.EventType, message.EventId);
            throw;
        }
    }

    private void NotifyRegistry(EventMessage message, string analysisKey, bool success, string? error)
    {
        var traceId = ExtractTraceId(message.Data);
        if (string.IsNullOrEmpty(traceId))
        {
            _logger.LogDebug(
                "Analysis event {EventType} missing traceId; nothing to dispatch",
                message.EventType);
            return;
        }

        var notified = _registry.Complete(traceId, analysisKey, success, error);
        if (notified)
        {
            _logger.LogInformation(
                "[Orchestrator] received {EventType} for trace {TraceId}, completing step (success={Success})",
                message.EventType, traceId, success);
        }
        else
        {
            _logger.LogDebug(
                "No pipeline waiter for {EventType} on trace {TraceId} (ad-hoc analysis)",
                message.EventType, traceId);
        }
    }

    private static string? ExtractTraceId(string data)
    {
        try
        {
            using var doc = JsonDocument.Parse(data);
            if (doc.RootElement.TryGetProperty("traceId", out var t1) && t1.ValueKind == JsonValueKind.String)
                return t1.GetString();
            if (doc.RootElement.TryGetProperty("trace_id", out var t2) && t2.ValueKind == JsonValueKind.String)
                return t2.GetString();
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtractError(string data)
    {
        try
        {
            using var doc = JsonDocument.Parse(data);
            foreach (var name in new[] { "error", "errorMessage", "error_message", "message" })
            {
                if (doc.RootElement.TryGetProperty(name, out var prop) &&
                    prop.ValueKind == JsonValueKind.String)
                {
                    return prop.GetString();
                }
            }
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
