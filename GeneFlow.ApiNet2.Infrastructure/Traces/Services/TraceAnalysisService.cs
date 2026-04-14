using System.Text.Json;
using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Infrastructure.Traces.Services;

/// <summary>
/// Service for reading trace analysis data from storage.
/// Analysis files are created by the python worker and contain sequence and quality scores.
/// </summary>
public sealed class TraceAnalysisService : ITraceAnalysisService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<TraceAnalysisService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
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
        var analysisResult = await GetAnalysisJsonAsync(trace, cancellationToken);
        if (analysisResult.IsFailure)
            return Result.Failure<int[]>(analysisResult.Error);

        return Result.Success(analysisResult.Value.Quality);
    }

    /// <inheritdoc />
    public async Task<Result<string>> GetSequenceAsync(
        Trace trace,
        CancellationToken cancellationToken = default)
    {
        var analysisResult = await GetAnalysisJsonAsync(trace, cancellationToken);
        if (analysisResult.IsFailure)
            return Result.Failure<string>(analysisResult.Error);

        return Result.Success(analysisResult.Value.Bases);
    }

    /// <inheritdoc />
    public async Task<Result<(string Sequence, int[] QualityScores)>> GetAnalysisDataAsync(
        Trace trace,
        CancellationToken cancellationToken = default)
    {
        var analysisResult = await GetAnalysisJsonAsync(trace, cancellationToken);
        if (analysisResult.IsFailure)
            return Result.Failure<(string, int[])>(analysisResult.Error);

        var data = analysisResult.Value;
        return Result.Success((data.Bases, data.Quality));
    }

    /// <summary>
    /// Gets the analysis JSON file path from the trace's storage path.
    /// </summary>
    private static string GetAnalysisPath(string storagePath)
    {
        var dotIndex = storagePath.LastIndexOf('.');
        var basePath = dotIndex >= 0 ? storagePath[..dotIndex] : storagePath;
        return $"{basePath}.analysis.json";
    }

    /// <summary>
    /// Reads and parses the analysis JSON file.
    /// </summary>
    private async Task<Result<AnalysisJson>> GetAnalysisJsonAsync(
        Trace trace,
        CancellationToken cancellationToken)
    {
        var analysisPath = GetAnalysisPath(trace.File.StoragePath);

        try
        {
            // Check if analysis file exists
            var exists = await _fileStorageService.ExistsAsync(analysisPath, cancellationToken);
            if (!exists)
            {
                _logger.LogWarning(
                    "Analysis file not found for trace {TraceId} at path {Path}",
                    trace.Id,
                    analysisPath);

                return Result.Failure<AnalysisJson>(
                    Error.NotFound("Trace.AnalysisNotFound", "Analysis data not found for this trace."));
            }

            // Download and parse the analysis JSON
            await using var stream = await _fileStorageService.DownloadAsync(analysisPath, cancellationToken);
            var analysisData = await JsonSerializer.DeserializeAsync<AnalysisJson>(stream, JsonOptions, cancellationToken);

            if (analysisData is null)
            {
                _logger.LogError(
                    "Failed to parse analysis JSON for trace {TraceId}",
                    trace.Id);

                return Result.Failure<AnalysisJson>(
                    Error.Failure("Trace.AnalysisParsingFailed", "Failed to parse analysis data."));
            }

            return Result.Success(analysisData);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error reading analysis file for trace {TraceId}",
                trace.Id);

            return Result.Failure<AnalysisJson>(
                Error.Failure("Trace.AnalysisReadFailed", "Failed to read analysis data."));
        }
    }

    /// <summary>
    /// Internal class to deserialize the analysis JSON structure.
    /// Matches the format created by the python worker.
    /// </summary>
    private sealed class AnalysisJson
    {
        public string TraceId { get; set; } = string.Empty;
        public string Bases { get; set; } = string.Empty;
        public int[] Quality { get; set; } = [];
        public int Length { get; set; }
    }
}
