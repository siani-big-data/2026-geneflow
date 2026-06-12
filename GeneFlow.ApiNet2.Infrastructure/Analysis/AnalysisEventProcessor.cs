using System.Text.Json;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.CompletePipelineExecution;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.CompleteStepExecution;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.FailStepExecution;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.StartStepExecution;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using MediatR;
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

    /// <summary>
    /// Events emitted by the analysis worker on the traces stream when a result has been
    /// fully persisted. Carry the complete analysis payload that the API exposes.
    /// </summary>
    private static readonly HashSet<string> AnalysisResultStoredEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "AnalysisResultStored"
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

    private static readonly HashSet<string> PipelineEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "PipelineStepStarted",
        "PipelineStepCompleted",
        "PipelineStepFailed",
        "PipelineExecutionCompleted",
        "PipelineExecutionFailed"
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
            ProcessCategoryAsync("analysis", stoppingToken),
            ProcessCategoryAsync("pipelines", stoppingToken)
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
            else if (AnalysisResultStoredEvents.Contains(message.EventType))
            {
                await HandleAnalysisResultStoredAsync(eventData);
            }
            else if (AlignmentEvents.Contains(message.EventType))
            {
                await HandleAlignmentEventAsync(message.EventType, eventData);
            }
            else if (AnalysisEvents.Contains(message.EventType))
            {
                await HandleAnalysisEventAsync(message.EventType, eventData);
            }
            else if (PipelineEvents.Contains(message.EventType))
            {
                await HandlePipelineEventAsync(message.EventType, eventData);
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

    private Task HandleAlignmentEventAsync(string eventType, JsonDocument eventData)
    {
        var alignmentId = GetStringProperty(eventData, "alignmentId");
        if (string.IsNullOrEmpty(alignmentId))
        {
            _logger.LogWarning("AlignmentId missing in {EventType} event", eventType);
            return Task.CompletedTask;
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

        return Task.CompletedTask;
    }

    private Task HandleAnalysisEventAsync(string eventType, JsonDocument eventData)
    {
        var traceId = GetStringProperty(eventData, "traceId");
        if (string.IsNullOrEmpty(traceId))
        {
            _logger.LogWarning("TraceId missing in {EventType} event", eventType);
            return Task.CompletedTask;
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

        return Task.CompletedTask;
    }

    /// <summary>
    /// Persists an analysis result emitted by the worker. The Python worker publishes
    /// snake_case payloads under <c>resultData</c> (or <c>result_data</c>); we normalise
    /// keys to camelCase before storing so downstream consumers see a consistent shape.
    /// </summary>
    private async Task HandleAnalysisResultStoredAsync(JsonDocument eventData)
    {
        var traceId = GetStringProperty(eventData, "traceId", "trace_id");
        if (string.IsNullOrEmpty(traceId))
        {
            _logger.LogWarning("TraceId missing in AnalysisResultStored event");
            return;
        }

        var analysisType = GetStringProperty(eventData, "analysisType", "analysis_type");
        if (string.IsNullOrEmpty(analysisType))
        {
            _logger.LogWarning(
                "AnalysisType missing in AnalysisResultStored event for trace {TraceId}",
                traceId);
            return;
        }

        if (!eventData.RootElement.TryGetProperty("resultData", out var resultDataElement) &&
            !eventData.RootElement.TryGetProperty("result_data", out resultDataElement))
        {
            _logger.LogWarning(
                "resultData missing in AnalysisResultStored event for trace {TraceId} ({AnalysisType})",
                traceId, analysisType);
            return;
        }

        string payloadJson;
        try
        {
            payloadJson = SerializeAsCamelCase(resultDataElement);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "Failed to serialise resultData for trace {TraceId} ({AnalysisType})",
                traceId, analysisType);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IAnalysisResultStore>();
        await store.SaveAsync(traceId, analysisType, payloadJson);

        _logger.LogInformation(
            "Stored analysis result {AnalysisType} for trace {TraceId} ({Bytes} bytes)",
            analysisType, traceId, payloadJson.Length);
    }

    /// <summary>
    /// Serialises a <see cref="JsonElement"/> to JSON, converting every object key from
    /// <c>snake_case</c> (or <c>kebab-case</c>) to <c>camelCase</c>.
    /// </summary>
    private static string SerializeAsCamelCase(JsonElement element)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteCamelCase(writer, element);
        }
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteCamelCase(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var prop in element.EnumerateObject())
                {
                    writer.WritePropertyName(ToCamelCase(prop.Name));
                    WriteCamelCase(writer, prop.Value);
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteCamelCase(writer, item);
                }
                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;

            case JsonValueKind.Number:
                if (element.TryGetInt64(out var l))
                    writer.WriteNumberValue(l);
                else if (element.TryGetDecimal(out var d))
                    writer.WriteNumberValue(d);
                else
                    writer.WriteNumberValue(element.GetDouble());
                break;

            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;

            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;

            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;

            default:
                writer.WriteRawValue(element.GetRawText());
                break;
        }
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var separators = new[] { '_', '-' };
        if (name.IndexOfAny(separators) < 0)
        {
            return char.IsUpper(name[0])
                ? char.ToLowerInvariant(name[0]) + name[1..]
                : name;
        }

        var parts = name.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return name;

        var first = parts[0].ToLowerInvariant();
        var sb = new System.Text.StringBuilder(first);
        for (var i = 1; i < parts.Length; i++)
        {
            var part = parts[i];
            if (part.Length == 0)
                continue;
            sb.Append(char.ToUpperInvariant(part[0]));
            if (part.Length > 1)
                sb.Append(part[1..].ToLowerInvariant());
        }
        return sb.ToString();
    }

    private async Task HandlePipelineEventAsync(string eventType, JsonDocument eventData)
    {
        var executionId = GetStringProperty(eventData, "executionId", "execution_id");
        if (string.IsNullOrEmpty(executionId))
        {
            _logger.LogWarning("Pipeline event {EventType} missing executionId", eventType);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        switch (eventType)
        {
            case "PipelineStepStarted":
            {
                var stepExecutionId = GetStringProperty(eventData, "stepExecutionId", "step_execution_id");
                if (string.IsNullOrEmpty(stepExecutionId))
                {
                    _logger.LogWarning("PipelineStepStarted missing stepExecutionId");
                    return;
                }
                var result = await sender.Send(new StartStepExecutionCommand(executionId, stepExecutionId));
                if (result.IsFailure)
                    _logger.LogWarning("StartStepExecutionCommand failed: {Error}", result.Error.Message);
                break;
            }
            case "PipelineStepCompleted":
            {
                var stepExecutionId = GetStringProperty(eventData, "stepExecutionId", "step_execution_id");
                if (string.IsNullOrEmpty(stepExecutionId))
                {
                    _logger.LogWarning("PipelineStepCompleted missing stepExecutionId");
                    return;
                }
                var resultSummary = GetStringProperty(eventData, "resultSummary", "result_summary");
                var resultData = GetStringProperty(eventData, "resultData", "result_data");
                var result = await sender.Send(new CompleteStepExecutionCommand(
                    executionId, stepExecutionId, resultSummary, resultData));
                if (result.IsFailure)
                    _logger.LogWarning("CompleteStepExecutionCommand failed: {Error}", result.Error.Message);
                break;
            }
            case "PipelineStepFailed":
            {
                var stepExecutionId = GetStringProperty(eventData, "stepExecutionId", "step_execution_id");
                if (string.IsNullOrEmpty(stepExecutionId))
                {
                    _logger.LogWarning("PipelineStepFailed missing stepExecutionId");
                    return;
                }
                var errorMessage = GetStringProperty(eventData, "errorMessage", "error_message", "error")
                    ?? "Step failed";
                var result = await sender.Send(new FailStepExecutionCommand(
                    executionId, stepExecutionId, errorMessage));
                if (result.IsFailure)
                    _logger.LogWarning("FailStepExecutionCommand failed: {Error}", result.Error.Message);
                break;
            }
            case "PipelineExecutionCompleted":
            {
                var result = await sender.Send(new CompletePipelineExecutionCommand(executionId));
                if (result.IsFailure)
                    _logger.LogWarning("CompletePipelineExecutionCommand failed: {Error}", result.Error.Message);
                break;
            }
            case "PipelineExecutionFailed":
            {
                _logger.LogDebug(
                    "Pipeline execution {ExecutionId} reported failed; FailStepExecution already cascades",
                    executionId);
                break;
            }
            default:
                _logger.LogDebug("Unhandled pipeline event type: {EventType}", eventType);
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
