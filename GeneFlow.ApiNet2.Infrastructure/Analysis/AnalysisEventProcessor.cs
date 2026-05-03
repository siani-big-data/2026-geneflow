using System.Text.Json;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace GeneFlow.ApiNet2.Infrastructure.Analysis;

/// <summary>
/// Background service that processes events from the Analysis worker (Python).
/// Subscribes to trace processing, alignment, and analysis events from Redis Streams
/// and updates the corresponding domain entities.
/// </summary>
/// <remarks>
/// The per-message handler catch is intentionally broad and rethrows the
/// exception so the message is not acknowledged and is redelivered. Handlers
/// can fail in unbounded ways (JSON parsing, repository errors, etc.).
/// </remarks>
public sealed class AnalysisEventProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEventBusSubscriber _subscriber;
    private readonly ILogger<AnalysisEventProcessor> _logger;

    private const string ConsumerGroup = "analysis-event-processor";

    private static readonly HashSet<string> TraceProcessingEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "TraceProcessed",
        "TraceProcessingFailed"
    };

    private static readonly HashSet<string> AlignmentEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "AlignmentCompleted",
        "AlignmentFailed"
    };

    private static readonly HashSet<string> AnalysisEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "TrimmingCompleted",
        "HeterozygoteDetectionCompleted",
        "MotifSearchCompleted",
        "TranslationCompleted",
        "ORFDetectionCompleted",
        "RestrictionAnalysisCompleted"
    };

    public AnalysisEventProcessor(
        IServiceScopeFactory scopeFactory,
        IEventBusSubscriber subscriber,
        ILogger<AnalysisEventProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _subscriber = subscriber;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Analysis Event Processor starting...");

        var tasks = new[]
        {
            ProcessCategoryAsync("traces", stoppingToken),
            ProcessCategoryAsync("alignments", stoppingToken),
            ProcessCategoryAsync("analysis", stoppingToken)
        };

        await Task.WhenAll(tasks);

        _logger.LogInformation("Analysis Event Processor stopped");
    }

    private async Task ProcessCategoryAsync(string category, CancellationToken cancellationToken)
    {
        try
        {
            await _subscriber.SubscribeAsync(
                category,
                ConsumerGroup,
                async message => await HandleEventAsync(message, category),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Redis error in {Category} event processor", category);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error in {Category} event processor", category);
        }
    }

    private async Task HandleEventAsync(EventMessage message, string category)
    {
        _logger.LogDebug(
            "Processing event {EventType} from {Category} (ID: {EventId})",
            message.EventType, category, message.EventId);

        try
        {
            var eventData = ParseEventData(message.Data);
            if (eventData is null)
            {
                _logger.LogWarning("Could not parse event data for {EventType}", message.EventType);
                return;
            }

            if (TraceProcessingEvents.Contains(message.EventType))
            {
                await HandleTraceProcessingEventAsync(message.EventType, eventData);
            }
            else if (AlignmentEvents.Contains(message.EventType))
            {
                await HandleAlignmentEventAsync(message.EventType, eventData);
            }
            else if (AnalysisEvents.Contains(message.EventType))
            {
                await HandleAnalysisEventAsync(message.EventType, eventData);
            }
            else
            {
                _logger.LogDebug("Ignoring unhandled event type: {EventType}", message.EventType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process event {EventType} (ID: {EventId})",
                message.EventType, message.EventId);
            throw;
        }
    }

    private async Task HandleTraceProcessingEventAsync(string eventType, JsonDocument eventData)
    {
        var traceId = GetStringProperty(eventData, "traceId");
        if (string.IsNullOrEmpty(traceId))
        {
            _logger.LogWarning("TraceId missing in {EventType} event", eventType);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<ITraceUnitOfWork>();

        if (!TraceId.TryParse(traceId, out var parsedTraceId) || parsedTraceId is null)
        {
            _logger.LogWarning("Invalid TraceId format: {TraceId}", traceId);
            return;
        }

        var trace = await unitOfWork.Traces.GetByIdAsync(parsedTraceId);
        if (trace is null)
        {
            _logger.LogWarning("Trace not found: {TraceId}", traceId);
            return;
        }

        switch (eventType)
        {
            case "TraceProcessed":
                await ProcessTraceProcessedAsync(trace, eventData, unitOfWork);
                break;

            case "TraceProcessingFailed":
                await ProcessTraceFailedAsync(trace, eventData, unitOfWork);
                break;
        }
    }

    private async Task ProcessTraceProcessedAsync(
        Trace trace,
        JsonDocument eventData,
        ITraceUnitOfWork unitOfWork)
    {
        var sequenceLength = GetIntProperty(eventData, "sequenceLength");
        var meanQuality = GetDecimalProperty(eventData, "meanQuality");
        var hasChromatogram = GetBoolProperty(eventData, "hasChromatogram");

        var qualityMetricsResult = QualityMetrics.Create(
            averageQualityScore: meanQuality ?? 0m,
            totalBases: sequenceLength,
            qualityAboveQ20Percentage: meanQuality >= 20 ? 80m : 50m,
            qualityAboveQ30Percentage: meanQuality >= 30 ? 60m : 30m,
            trimmedLength: sequenceLength,
            gcContentPercentage: 50m);

        if (qualityMetricsResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to create quality metrics for trace {TraceId}: {Error}",
                trace.Id, qualityMetricsResult.Error.Message);
            return;
        }

        if (trace.Status == Domain.Traces.Enumerations.TraceStatus.Uploaded ||
            trace.Status == Domain.Traces.Enumerations.TraceStatus.Validating)
        {
            trace.StartProcessing();
            trace.TransitionToProcessing();
        }

        var result = trace.CompleteProcessing(qualityMetricsResult.Value, hasChromatogram);
        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to complete processing for trace {TraceId}: {Error}",
                trace.Id, result.Error.Message);
            return;
        }

        unitOfWork.Traces.Update(trace);
        await unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Trace {TraceId} processing completed. Length: {Length}, Quality: {Quality}",
            trace.Id, sequenceLength, meanQuality);
    }

    private async Task ProcessTraceFailedAsync(
        Trace trace,
        JsonDocument eventData,
        ITraceUnitOfWork unitOfWork)
    {
        var error = GetStringProperty(eventData, "error") ?? "Unknown error";
        var errorType = GetStringProperty(eventData, "errorType") ?? "UnknownError";

        var failureReason = $"[{errorType}] {error}";
        if (failureReason.Length > Trace.MaxFailureReasonLength)
        {
            failureReason = failureReason[..Trace.MaxFailureReasonLength];
        }

        if (trace.Status == Domain.Traces.Enumerations.TraceStatus.Uploaded)
        {
            trace.StartProcessing();
        }

        var result = trace.FailProcessing(failureReason);
        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to mark trace {TraceId} as failed: {Error}",
                trace.Id, result.Error.Message);
            return;
        }

        unitOfWork.Traces.Update(trace);
        await unitOfWork.SaveChangesAsync();

        _logger.LogWarning(
            "Trace {TraceId} processing failed: {Reason}",
            trace.Id, failureReason);
    }

    private async Task HandleAlignmentEventAsync(string eventType, JsonDocument eventData)
    {
        var alignmentId = GetStringProperty(eventData, "alignmentId");
        if (string.IsNullOrEmpty(alignmentId))
        {
            _logger.LogWarning("AlignmentId missing in {EventType} event", eventType);
            return;
        }

        _logger.LogInformation(
            "Alignment event received: {EventType} for alignment {AlignmentId}",
            eventType, alignmentId);

        switch (eventType)
        {
            case "AlignmentCompleted":
                var score = GetDecimalProperty(eventData, "score");
                var identity = GetDecimalProperty(eventData, "identity");
                _logger.LogInformation(
                    "Alignment {AlignmentId} completed. Score: {Score}, Identity: {Identity}%",
                    alignmentId, score, identity);
                break;

            case "AlignmentFailed":
                var error = GetStringProperty(eventData, "error");
                _logger.LogWarning(
                    "Alignment {AlignmentId} failed: {Error}",
                    alignmentId, error);
                break;
        }
    }

    private async Task HandleAnalysisEventAsync(string eventType, JsonDocument eventData)
    {
        var traceId = GetStringProperty(eventData, "traceId");
        if (string.IsNullOrEmpty(traceId))
        {
            _logger.LogWarning("TraceId missing in {EventType} event", eventType);
            return;
        }

        _logger.LogInformation(
            "Analysis event received: {EventType} for trace {TraceId}",
            eventType, traceId);

        switch (eventType)
        {
            case "TrimmingCompleted":
                var trimStart = GetIntProperty(eventData, "trimStart");
                var trimEnd = GetIntProperty(eventData, "trimEnd");
                var originalLength = GetIntProperty(eventData, "originalLength");
                var trimmedLength = GetIntProperty(eventData, "trimmedLength");
                _logger.LogInformation(
                    "Trimming completed for trace {TraceId}: {Original} -> {Trimmed} bases (trim: {Start}-{End})",
                    traceId, originalLength, trimmedLength, trimStart, trimEnd);
                break;

            case "HeterozygoteDetectionCompleted":
                var hetCount = GetIntProperty(eventData, "heterozygoteCount");
                _logger.LogInformation(
                    "Heterozygote detection completed for trace {TraceId}: {Count} heterozygotes found",
                    traceId, hetCount);
                break;

            case "MotifSearchCompleted":
                var pattern = GetStringProperty(eventData, "pattern");
                var matchCount = GetIntProperty(eventData, "matchCount");
                _logger.LogInformation(
                    "Motif search completed for trace {TraceId}: {Count} matches for pattern '{Pattern}'",
                    traceId, matchCount, pattern);
                break;

            case "TranslationCompleted":
                var frame = GetIntProperty(eventData, "frame");
                var proteinLength = GetIntProperty(eventData, "proteinLength");
                _logger.LogInformation(
                    "Translation completed for trace {TraceId}: frame {Frame}, {Length} amino acids",
                    traceId, frame, proteinLength);
                break;

            case "ORFDetectionCompleted":
                var orfCount = GetIntProperty(eventData, "orfCount");
                var longestOrf = GetIntProperty(eventData, "longestOrfLength");
                _logger.LogInformation(
                    "ORF detection completed for trace {TraceId}: {Count} ORFs, longest: {Longest} bp",
                    traceId, orfCount, longestOrf);
                break;

            case "RestrictionAnalysisCompleted":
                var enzymeCount = GetIntProperty(eventData, "enzymeCount");
                var totalSites = GetIntProperty(eventData, "totalSites");
                _logger.LogInformation(
                    "Restriction analysis completed for trace {TraceId}: {Enzymes} enzymes, {Sites} total sites",
                    traceId, enzymeCount, totalSites);
                break;
        }
    }

    private JsonDocument? ParseEventData(string data)
    {
        try
        {
            return JsonDocument.Parse(data);
        }
        catch (JsonException)
        {
            try
            {
                var unescaped = data
                    .Replace("'", "\"")
                    .Replace("None", "null")
                    .Replace("True", "true")
                    .Replace("False", "false");
                return JsonDocument.Parse(unescaped);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse event data: {Data}", data);
                return null;
            }
        }
    }

    private string? GetStringProperty(JsonDocument doc, params string[] propertyNames)
    {
        foreach (var propName in propertyNames)
        {
            if (doc.RootElement.TryGetProperty(propName, out var prop))
            {
                return prop.GetString();
            }
        }
        return null;
    }

    private int GetIntProperty(JsonDocument doc, params string[] propertyNames)
    {
        foreach (var propName in propertyNames)
        {
            if (doc.RootElement.TryGetProperty(propName, out var prop))
            {
                if (prop.TryGetInt32(out var value))
                {
                    return value;
                }
            }
        }
        return 0;
    }

    private decimal? GetDecimalProperty(JsonDocument doc, params string[] propertyNames)
    {
        foreach (var propName in propertyNames)
        {
            if (doc.RootElement.TryGetProperty(propName, out var prop))
            {
                if (prop.TryGetDecimal(out var value))
                {
                    return value;
                }
                if (prop.TryGetDouble(out var doubleValue))
                {
                    return (decimal)doubleValue;
                }
            }
        }
        return null;
    }

    private bool GetBoolProperty(JsonDocument doc, params string[] propertyNames)
    {
        foreach (var propName in propertyNames)
        {
            if (doc.RootElement.TryGetProperty(propName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.True)
                    return true;
                if (prop.ValueKind == JsonValueKind.False)
                    return false;
            }
        }
        return false;
    }
}
