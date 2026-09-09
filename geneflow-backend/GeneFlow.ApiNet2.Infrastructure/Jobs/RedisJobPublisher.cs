using System.Text.Json;
using System.Text.Json.Serialization;
using GeneFlow.ApiNet2.Infrastructure.Redis.Configuration;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace GeneFlow.ApiNet2.Infrastructure.Jobs;

/// <summary>
/// Publishes jobs to Redis Streams for the Analysis worker to consume.
/// Jobs are published to streams named {prefix}{category} (e.g., geneflow:jobs:traces).
/// </summary>
public sealed class RedisJobPublisher : IJobPublisher
{
    private readonly IConnectionMultiplexer _redis;
    private readonly RedisSettings _settings;
    private readonly ILogger<RedisJobPublisher> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the RedisJobPublisher.
    /// </summary>
    public RedisJobPublisher(
        IConnectionMultiplexer redis,
        IOptions<RedisSettings> settings,
        ILogger<RedisJobPublisher> logger)
    {
        _redis = redis;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PublishTraceJobAsync(TraceProcessingJob job, CancellationToken cancellationToken = default)
    {
        var streamName = GetStreamName("traces");

        try
        {
            var db = _redis.GetDatabase();

            var payload = new
            {
                traceId = job.TraceId,
                studyId = job.StudyId,
                fileName = job.FileName,
                storagePath = job.StoragePath,
                format = job.Format,
                options = job.Options ?? new Dictionary<string, object>()
            };

            var entries = new NameValueEntry[]
            {
                new("data", JsonSerializer.Serialize(payload, JsonOptions))
            };

            var messageId = await db.StreamAddAsync(streamName, entries);

            _logger.LogInformation(
                "Published trace processing job for {TraceId} to stream {Stream} with ID {MessageId}",
                job.TraceId,
                streamName,
                messageId);
        }
        catch (RedisException ex)
        {
            _logger.LogError(
                ex,
                "Redis error publishing trace processing job for {TraceId} to stream {Stream}",
                job.TraceId,
                streamName);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Failed to serialize trace processing job for {TraceId}",
                job.TraceId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task PublishAlignmentJobAsync(AlignmentJob job, CancellationToken cancellationToken = default)
    {
        var streamName = GetStreamName("alignments");

        try
        {
            var db = _redis.GetDatabase();

            var payload = new
            {
                alignmentId = job.AlignmentId,
                type = job.Type,
                traceIds = job.TraceIds,
                sequences = job.Sequences,
                options = job.Options is not null ? new
                {
                    build_consensus = job.Options.BuildConsensus,
                    consensus_method = job.Options.ConsensusMethod,
                    match_score = job.Options.MatchScore,
                    mismatch_penalty = job.Options.MismatchPenalty,
                    gap_penalty = job.Options.GapPenalty
                } : null
            };

            var entries = new NameValueEntry[]
            {
                new("data", JsonSerializer.Serialize(payload, JsonOptions))
            };

            var messageId = await db.StreamAddAsync(streamName, entries);

            _logger.LogInformation(
                "Published alignment job {AlignmentId} ({Type}) to stream {Stream} with ID {MessageId}",
                job.AlignmentId,
                job.Type,
                streamName,
                messageId);
        }
        catch (RedisException ex)
        {
            _logger.LogError(
                ex,
                "Redis error publishing alignment job {AlignmentId} to stream {Stream}",
                job.AlignmentId,
                streamName);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Failed to serialize alignment job {AlignmentId}",
                job.AlignmentId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task PublishAnalysisJobAsync(AnalysisJob job, CancellationToken cancellationToken = default)
    {
        var streamName = GetStreamName("analysis");

        try
        {
            var db = _redis.GetDatabase();

            var payload = new
            {
                traceId = job.TraceId,
                analysisType = job.AnalysisType,
                sequence = job.Sequence,
                quality = job.Quality,
                options = job.Options ?? new Dictionary<string, object>()
            };

            var entries = new NameValueEntry[]
            {
                new("data", JsonSerializer.Serialize(payload, JsonOptions))
            };

            var messageId = await db.StreamAddAsync(streamName, entries);

            _logger.LogInformation(
                "Published analysis job ({AnalysisType}) for trace {TraceId} to stream {Stream} with ID {MessageId}",
                job.AnalysisType,
                job.TraceId,
                streamName,
                messageId);
        }
        catch (RedisException ex)
        {
            _logger.LogError(
                ex,
                "Redis error publishing analysis job ({AnalysisType}) for trace {TraceId} to stream {Stream}",
                job.AnalysisType,
                job.TraceId,
                streamName);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Failed to serialize analysis job ({AnalysisType}) for trace {TraceId}",
                job.AnalysisType,
                job.TraceId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task PublishPipelineJobAsync(PipelineJob job, CancellationToken cancellationToken = default)
    {
        var streamName = GetStreamName("pipelines");

        try
        {
            var db = _redis.GetDatabase();

            var payload = new
            {
                executionId = job.ExecutionId,
                pipelineId = job.PipelineId,
                traceId = job.TraceId,
                studyId = job.StudyId,
                steps = job.Steps.Select(s => new
                {
                    stepExecutionId = s.StepExecutionId,
                    order = s.Order,
                    stepType = s.StepType,
                    configuration = s.Configuration
                }).ToList()
            };

            var entries = new NameValueEntry[]
            {
                new("data", JsonSerializer.Serialize(payload, JsonOptions))
            };

            var messageId = await db.StreamAddAsync(streamName, entries);

            _logger.LogInformation(
                "Published pipeline job for execution {ExecutionId} (pipeline {PipelineId}, trace {TraceId}) to stream {Stream} with ID {MessageId}",
                job.ExecutionId,
                job.PipelineId,
                job.TraceId,
                streamName,
                messageId);
        }
        catch (RedisException ex)
        {
            _logger.LogError(
                ex,
                "Redis error publishing pipeline job for execution {ExecutionId} to stream {Stream}",
                job.ExecutionId,
                streamName);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Failed to serialize pipeline job for execution {ExecutionId}",
                job.ExecutionId);
            throw;
        }
    }

    private string GetStreamName(string category)
    {
        return $"{_settings.JobStreamPrefix}{category}";
    }
}
