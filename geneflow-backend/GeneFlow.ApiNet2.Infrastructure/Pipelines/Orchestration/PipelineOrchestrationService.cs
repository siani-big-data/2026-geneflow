using System.Text.Json;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.CompletePipelineExecution;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.CompleteStepExecution;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.FailStepExecution;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.StartStepExecution;
using GeneFlow.ApiNet2.Infrastructure.Redis.Configuration;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace GeneFlow.ApiNet2.Infrastructure.Pipelines.Orchestration;

/// <summary>
/// Background service that consumes pipeline jobs from
/// <c>{JobStreamPrefix}pipelines</c> and walks each <c>PipelineExecution</c>
/// through its state machine: for every step it dispatches the underlying
/// analysis to the existing Python worker, awaits the matching completion event
/// via <see cref="PipelineStepCompletionRegistry"/>, then issues the
/// corresponding domain command (Start/Complete/Fail). When all steps succeed
/// it issues <see cref="CompletePipelineExecutionCommand"/>.
/// </summary>
/// <remarks>
/// Reads directly from Redis Streams via <see cref="IConnectionMultiplexer"/>
/// rather than <see cref="IEventBusSubscriber"/> because jobs use
/// <c>RedisSettings.JobStreamPrefix</c> while the event-bus subscriber is
/// hard-wired to <c>RedisSettings.EventStreamPrefix</c>. The job entry shape
/// is a single <c>data</c> field carrying the serialised <c>PipelineJob</c>
/// (see <c>RedisJobPublisher.PublishPipelineJobAsync</c>).
/// </remarks>
public sealed class PipelineOrchestrationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnectionMultiplexer _redis;
    private readonly RedisSettings _redisSettings;
    private readonly PipelineStepCompletionRegistry _registry;
    private readonly ILogger<PipelineOrchestrationService> _logger;
    private readonly string _consumerName;

    private const string Category = "pipelines";
    private const string ConsumerGroup = "pipeline-orchestrator";

    /// <summary>How many messages to fetch per <c>XREADGROUP</c> call.</summary>
    private const int ReadBatchSize = 4;

    /// <summary>Idle-poll delay when the stream returns no new messages.</summary>
    private static readonly TimeSpan IdlePollDelay = TimeSpan.FromSeconds(1);

    /// <summary>Back-off delay before retrying after a transient stream error.</summary>
    private static readonly TimeSpan ErrorBackoffDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Maximum amount of time the orchestrator waits for a single step to
    /// complete before declaring it failed.
    /// </summary>
    private static readonly TimeSpan StepTimeout = TimeSpan.FromMinutes(10);

    public PipelineOrchestrationService(
        IServiceScopeFactory scopeFactory,
        IConnectionMultiplexer redis,
        IOptions<RedisSettings> redisSettings,
        PipelineStepCompletionRegistry registry,
        ILogger<PipelineOrchestrationService> logger)
    {
        _scopeFactory = scopeFactory;
        _redis = redis;
        _redisSettings = redisSettings.Value;
        _registry = registry;
        _logger = logger;
        _consumerName = $"orchestrator-{Environment.MachineName}-{Guid.NewGuid():N}";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var streamName = $"{_redisSettings.JobStreamPrefix}{Category}";
        var db = _redis.GetDatabase();

        await EnsureConsumerGroupAsync(db, streamName);

        _logger.LogInformation(
            "Pipeline Orchestration Service starting. Stream: {Stream}, group: {Group}, consumer: {Consumer}",
            streamName, ConsumerGroup, _consumerName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var entries = await db.StreamReadGroupAsync(
                    streamName,
                    ConsumerGroup,
                    _consumerName,
                    ">",
                    count: ReadBatchSize);

                if (entries.Length == 0)
                {
                    await Task.Delay(IdlePollDelay, stoppingToken);
                    continue;
                }

                foreach (var entry in entries)
                {
                    var processed = await TryHandleEntryAsync(entry, stoppingToken);
                    if (processed)
                    {
                        await db.StreamAcknowledgeAsync(streamName, ConsumerGroup, entry.Id!);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (RedisException ex)
            {
                _logger.LogError(ex, "Redis error reading pipeline jobs from {Stream}", streamName);
                await Task.Delay(ErrorBackoffDelay, stoppingToken);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "I/O error reading pipeline jobs from {Stream}", streamName);
                await Task.Delay(ErrorBackoffDelay, stoppingToken);
            }
        }

        _logger.LogInformation("Pipeline Orchestration Service stopped");
    }

    private async Task<bool> TryHandleEntryAsync(StreamEntry entry, CancellationToken stoppingToken)
    {
        try
        {
            var rawData = entry.Values
                .FirstOrDefault(v => v.Name == "data")
                .Value
                .ToString();

            if (string.IsNullOrEmpty(rawData))
            {
                _logger.LogWarning(
                    "Pipeline job {MessageId} missing 'data' field; ack-ing and skipping",
                    entry.Id);
                return true; // ack to avoid redelivery loops
            }

            var job = ParsePipelineJob(rawData);
            if (job is null)
            {
                _logger.LogWarning(
                    "Could not parse pipeline job payload (message {MessageId}); ack-ing and skipping",
                    entry.Id);
                return true;
            }

            await RunPipelineAsync(job, stoppingToken);
            return true;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Don't ack on shutdown — let the message be redelivered next time.
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled error processing pipeline job {MessageId}; leaving for redelivery",
                entry.Id);
            return false;
        }
    }

    private async Task EnsureConsumerGroupAsync(IDatabase db, string streamName)
    {
        try
        {
            await db.StreamCreateConsumerGroupAsync(
                streamName,
                ConsumerGroup,
                "0",
                createStream: true);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            // Group already exists — nothing to do.
        }
    }

    private async Task RunPipelineAsync(ParsedPipelineJob job, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var publisher = scope.ServiceProvider.GetRequiredService<IJobPublisher>();
        var resolver = scope.ServiceProvider.GetRequiredService<PipelineTraceDataResolver>();

        _logger.LogInformation(
            "[Orchestrator] starting pipeline {ExecutionId}, {StepCount} steps (trace {TraceId})",
            job.ExecutionId, job.Steps.Count, job.TraceId);

        // Resolve sequence + quality once per pipeline; all non-instant steps
        // share the same trace data.
        string? sequence = null;
        int[]? quality = null;

        var orderedSteps = job.Steps.OrderBy(s => s.Order).ToList();

        foreach (var step in orderedSteps)
        {
            // 1. Move the step to Running (also moves the execution on the first call).
            var startResult = await sender.Send(
                new StartStepExecutionCommand(job.ExecutionId, step.StepExecutionId.ToString()),
                stoppingToken);

            if (startResult.IsFailure)
            {
                _logger.LogWarning(
                    "[Orchestrator] StartStepExecutionCommand failed for step {StepExecutionId} ({StepType}): {Error}",
                    step.StepExecutionId, step.StepType, startResult.Error.Message);

                await sender.Send(
                    new FailStepExecutionCommand(
                        job.ExecutionId,
                        step.StepExecutionId.ToString(),
                        startResult.Error.Message),
                    stoppingToken);
                return;
            }

            _logger.LogInformation(
                "[Orchestrator] starting step {Order} ({StepType}) on trace {TraceId}",
                step.Order, step.StepType, job.TraceId);

            // 2. Dispatch + await completion.
            StepResult result;
            try
            {
                if (PipelineStepDispatcher.IsInstant(step.StepType))
                {
                    _logger.LogInformation(
                        "[Orchestrator] step {Order} ({StepType}) treated as instant completion",
                        step.Order, step.StepType);
                    result = StepResult.Ok();
                }
                else
                {
                    if (sequence is null || quality is null)
                    {
                        var dataResult = await resolver.ResolveAsync(job.TraceId, stoppingToken);
                        if (dataResult.IsFailure)
                        {
                            _logger.LogWarning(
                                "[Orchestrator] failed to load trace data for {TraceId}: {Error}",
                                job.TraceId, dataResult.Error.Message);

                            await sender.Send(
                                new FailStepExecutionCommand(
                                    job.ExecutionId,
                                    step.StepExecutionId.ToString(),
                                    dataResult.Error.Message),
                                stoppingToken);
                            return;
                        }
                        (sequence, quality) = dataResult.Value;
                    }

                    // Refuse to publish quality-dependent jobs without quality
                    // scores. The Python worker raises ValueError but currently
                    // does NOT publish a *Failed event, so without this guard
                    // the orchestrator would hang for StepTimeout.
                    if (PipelineStepDispatcher.RequiresQuality(step.StepType) &&
                        (quality is null || quality.Length == 0))
                    {
                        var msg = $"Step '{step.StepType}' requires Phred quality scores, " +
                                  $"but trace {job.TraceId} has none in its parsed data.";
                        _logger.LogWarning("[Orchestrator] {Message}", msg);

                        await sender.Send(
                            new FailStepExecutionCommand(
                                job.ExecutionId,
                                step.StepExecutionId.ToString(),
                                msg),
                            stoppingToken);
                        return;
                    }

                    var waitTask = _registry.WaitAsync(job.TraceId, step.StepType, stoppingToken);

                    await PipelineStepDispatcher.PublishAsync(
                        publisher,
                        job.TraceId,
                        step.StepType,
                        sequence,
                        quality,
                        step.Configuration,
                        stoppingToken);

                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    timeoutCts.CancelAfter(StepTimeout);

                    var completed = await Task.WhenAny(
                        waitTask,
                        Task.Delay(Timeout.InfiniteTimeSpan, timeoutCts.Token));

                    if (completed != waitTask)
                    {
                        _registry.Complete(job.TraceId, step.StepType, success: false,
                            error: $"Step '{step.StepType}' timed out after {StepTimeout.TotalMinutes:N0} minutes");
                        result = await waitTask;
                    }
                    else
                    {
                        result = await waitTask;
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[Orchestrator] error dispatching step {StepExecutionId} ({StepType})",
                    step.StepExecutionId, step.StepType);
                result = StepResult.Fail(ex.Message);
            }

            // 3. Resolve the step.
            if (result.Success)
            {
                _logger.LogInformation(
                    "[Orchestrator] step {Order} ({StepType}) completed for trace {TraceId}",
                    step.Order, step.StepType, job.TraceId);

                var completeResult = await sender.Send(
                    new CompleteStepExecutionCommand(
                        job.ExecutionId,
                        step.StepExecutionId.ToString(),
                        ResultSummary: null,
                        ResultData: null),
                    stoppingToken);

                if (completeResult.IsFailure)
                {
                    _logger.LogWarning(
                        "[Orchestrator] CompleteStepExecutionCommand failed for {StepExecutionId}: {Error}",
                        step.StepExecutionId, completeResult.Error.Message);
                    return;
                }
            }
            else
            {
                _logger.LogWarning(
                    "[Orchestrator] step {Order} ({StepType}) failed for trace {TraceId}: {Error}",
                    step.Order, step.StepType, job.TraceId, result.Error);

                await sender.Send(
                    new FailStepExecutionCommand(
                        job.ExecutionId,
                        step.StepExecutionId.ToString(),
                        result.Error ?? "Step failed"),
                    stoppingToken);
                return; // FailStepExecutionCommandHandler cascades the pipeline to Failed.
            }
        }

        // 4. All steps succeeded → finalise the execution.
        _logger.LogInformation(
            "[Orchestrator] all steps complete, finalising execution {ExecutionId}",
            job.ExecutionId);

        var finalResult = await sender.Send(
            new CompletePipelineExecutionCommand(job.ExecutionId),
            stoppingToken);

        if (finalResult.IsFailure)
        {
            _logger.LogWarning(
                "[Orchestrator] CompletePipelineExecutionCommand failed for {ExecutionId}: {Error}",
                job.ExecutionId, finalResult.Error.Message);
        }
    }

    private ParsedPipelineJob? ParsePipelineJob(string data)
    {
        try
        {
            using var doc = JsonDocument.Parse(data);
            var root = doc.RootElement;

            var executionId = GetString(root, "executionId", "execution_id");
            var pipelineId = GetString(root, "pipelineId", "pipeline_id");
            var traceId = GetString(root, "traceId", "trace_id");
            var studyId = GetString(root, "studyId", "study_id");

            if (string.IsNullOrEmpty(executionId) ||
                string.IsNullOrEmpty(traceId))
            {
                return null;
            }

            var steps = new List<ParsedStep>();
            if (root.TryGetProperty("steps", out var stepsElement) &&
                stepsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var stepElement in stepsElement.EnumerateArray())
                {
                    var stepIdString = GetString(stepElement, "stepExecutionId", "step_execution_id");
                    if (!Guid.TryParse(stepIdString, out var stepId))
                        continue;

                    var order = GetInt(stepElement, "order");
                    var stepType = GetString(stepElement, "stepType", "step_type");
                    if (string.IsNullOrEmpty(stepType))
                        continue;

                    var configuration = new Dictionary<string, object>();
                    if (stepElement.TryGetProperty("configuration", out var configEl) &&
                        configEl.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in configEl.EnumerateObject())
                        {
                            configuration[prop.Name] = ExtractJsonValue(prop.Value);
                        }
                    }

                    steps.Add(new ParsedStep(stepId, order, stepType, configuration));
                }
            }

            return new ParsedPipelineJob(
                executionId!,
                pipelineId ?? string.Empty,
                traceId!,
                studyId ?? string.Empty,
                steps);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse pipeline job payload");
            return null;
        }
    }

    private static object ExtractJsonValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString() ?? string.Empty,
        JsonValueKind.Number when element.TryGetInt64(out var l) => l,
        JsonValueKind.Number when element.TryGetDecimal(out var d) => d,
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => string.Empty,
        _ => element.GetRawText()
    };

    private static string? GetString(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
                return prop.GetString();
        }
        return null;
    }

    private static int GetInt(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var prop) &&
                prop.ValueKind == JsonValueKind.Number &&
                prop.TryGetInt32(out var value))
            {
                return value;
            }
        }
        return 0;
    }

    private sealed record ParsedPipelineJob(
        string ExecutionId,
        string PipelineId,
        string TraceId,
        string StudyId,
        List<ParsedStep> Steps);

    private sealed record ParsedStep(
        Guid StepExecutionId,
        int Order,
        string StepType,
        Dictionary<string, object> Configuration);
}
