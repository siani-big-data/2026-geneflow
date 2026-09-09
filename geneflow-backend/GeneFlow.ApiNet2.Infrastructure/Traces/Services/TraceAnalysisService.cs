using System.Text.Json;
using System.Text.Json.Serialization;
using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Infrastructure.Traces.Services;

/// <summary>
/// Service for reading trace analysis data (parsed.json) from datalake storage.
/// The python datalake-consumer writes parsed.json to {studyId}/{traceId}/parsed.json
/// with nested sequence/quality fields.
/// </summary>
public sealed class TraceAnalysisService : ITraceAnalysisService
{
    private const string ParsedFileName = "parsed.json";

    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<TraceAnalysisService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public TraceAnalysisService(
        IFileStorageService fileStorageService,
        ILogger<TraceAnalysisService> logger)
    {
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<int[]>> GetQualityScoresAsync(
        Trace trace,
        CancellationToken cancellationToken = default)
    {
        var analysisResult = await GetParsedJsonAsync(trace, cancellationToken);
        if (analysisResult.IsFailure)
            return Result.Failure<int[]>(analysisResult.Error);

        return Result.Success(analysisResult.Value.Sequence?.Quality ?? []);
    }

    /// <inheritdoc />
    public async Task<Result<string>> GetSequenceAsync(
        Trace trace,
        CancellationToken cancellationToken = default)
    {
        var analysisResult = await GetParsedJsonAsync(trace, cancellationToken);
        if (analysisResult.IsFailure)
            return Result.Failure<string>(analysisResult.Error);

        return Result.Success(analysisResult.Value.Sequence?.Bases ?? string.Empty);
    }

    /// <inheritdoc />
    public async Task<Result<(string Sequence, int[] QualityScores)>> GetAnalysisDataAsync(
        Trace trace,
        CancellationToken cancellationToken = default)
    {
        var analysisResult = await GetParsedJsonAsync(trace, cancellationToken);
        if (analysisResult.IsFailure)
            return Result.Failure<(string, int[])>(analysisResult.Error);

        var data = analysisResult.Value.Sequence;
        if (data is null || string.IsNullOrEmpty(data.Bases))
        {
            return Result.Failure<(string, int[])>(
                Error.Failure("Trace.AnalysisParsingFailed", "parsed.json missing sequence."));
        }

        return Result.Success((data.Bases, data.Quality ?? []));
    }

    /// <summary>
    /// Builds the datalake path where the python worker writes parsed.json.
    /// Layout: {StudyId}/{TraceId}/parsed.json
    /// </summary>
    private static string GetParsedPath(Trace trace)
        => $"{trace.StudyId}/{trace.Id}/{ParsedFileName}";

    private async Task<Result<ParsedJson>> GetParsedJsonAsync(
        Trace trace,
        CancellationToken cancellationToken)
    {
        var path = GetParsedPath(trace);

        try
        {
            var exists = await _fileStorageService.FileExistsAsync(path, cancellationToken);
            if (!exists)
            {
                _logger.LogWarning(
                    "parsed.json not found for trace {TraceId} at path {Path}",
                    trace.Id,
                    path);

                return Result.Failure<ParsedJson>(
                    Error.NotFound(
                        "Trace.AnalysisNotFound",
                        "Analysis data not found for this trace."));
            }

            var fileBytes = await _fileStorageService.GetFileAsync(path, cancellationToken);
            var data = fileBytes is not null
                ? JsonSerializer.Deserialize<ParsedJson>(fileBytes, JsonOptions)
                : null;

            if (data is null)
            {
                _logger.LogError(
                    "Failed to parse parsed.json for trace {TraceId}",
                    trace.Id);

                return Result.Failure<ParsedJson>(
                    Error.Failure(
                        "Trace.AnalysisParsingFailed",
                        "Failed to parse analysis data."));
            }

            return Result.Success(data);
        }
        catch (IOException ex)
        {
            _logger.LogError(
                ex,
                "I/O error reading parsed.json for trace {TraceId}",
                trace.Id);

            return Result.Failure<ParsedJson>(
                Error.Failure(
                    "Trace.AnalysisReadFailed",
                    "Failed to read analysis data."));
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Failed to deserialize parsed.json for trace {TraceId}",
                trace.Id);

            return Result.Failure<ParsedJson>(
                Error.Failure(
                    "Trace.AnalysisParsingFailed",
                    "Failed to parse analysis data."));
        }
    }

    /// <summary>
    /// Top-level shape of parsed.json produced by the python datalake-consumer.
    /// </summary>
    private sealed class ParsedJson
    {
        public string TraceId { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public ParsedSequence? Sequence { get; set; }
    }

    /// <summary>
    /// Nested sequence object inside parsed.json.
    /// Maps to <c>sequence.sequence</c> (bases) and <c>sequence.quality</c> (Phred scores).
    /// </summary>
    private sealed class ParsedSequence
    {
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("sequence")]
        public string Bases { get; set; } = string.Empty;

        public int[] Quality { get; set; } = [];
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
}
