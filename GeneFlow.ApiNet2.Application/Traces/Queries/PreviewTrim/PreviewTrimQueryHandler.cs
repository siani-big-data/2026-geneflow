using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.PreviewTrim;

/// <summary>
/// Handler for PreviewTrimQuery.
/// Calculates trim boundaries without applying them.
/// </summary>
public sealed class PreviewTrimQueryHandler
    : IQueryHandler<PreviewTrimQuery, Result<TrimPreviewDto>>
{
    private readonly ITraceRepository _repository;
    private readonly ITraceAnalysisService _analysisService;

    public PreviewTrimQueryHandler(
        ITraceRepository repository,
        ITraceAnalysisService analysisService)
    {
        _repository = repository;
        _analysisService = analysisService;
    }

    public async Task<Result<TrimPreviewDto>> Handle(
        PreviewTrimQuery request,
        CancellationToken cancellationToken)
    {
        // Parse trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure<TrimPreviewDto>(TraceErrors.NotFound);

        // Get trace
        var trace = await _repository.GetByIdAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure<TrimPreviewDto>(TraceErrors.NotFound);

        // Get analysis data
        var analysisResult = await _analysisService.GetAnalysisDataAsync(trace, cancellationToken);
        if (analysisResult.IsFailure)
            return Result.Failure<TrimPreviewDto>(analysisResult.Error);

        var (sequence, qualityScores) = analysisResult.Value;
        var sequenceLength = qualityScores.Length;

        if (sequenceLength == 0)
            return Result.Failure<TrimPreviewDto>(TraceErrors.InvalidTotalBases);

        var threshold = request.QualityThreshold;
        var windowSize = request.WindowSize;
        var minLength = request.MinimumLength;

        // Calculate trim boundaries using sliding window algorithm (Mott's algorithm)
        var (_, end5Prime) = FindTrim5Prime(qualityScores, threshold, windowSize);
        var (start3Prime, _) = FindTrim3Prime(qualityScores, threshold, windowSize, sequenceLength);

        // Ensure minimum length
        var trimmedLength = start3Prime - end5Prime;
        if (trimmedLength < minLength)
        {
            return Result.Failure<TrimPreviewDto>(Error.Validation(
                "Trace.TrimmedTooShort",
                $"Trimmed sequence would be {trimmedLength} bases, which is less than the minimum of {minLength} bases."));
        }

        // Get preview sequence (max 100 chars from start)
        var previewLength = Math.Min(100, trimmedLength);
        var previewSequence = sequence.Substring(end5Prime, previewLength);
        if (trimmedLength > previewLength)
            previewSequence += "...";

        // Calculate average quality in trimmed region
        var trimmedQuality = qualityScores
            .Skip(end5Prime)
            .Take(trimmedLength)
            .Average();

        var algorithm = $"Mott(Q{threshold},W{windowSize})";

        return Result.Success(new TrimPreviewDto(
            traceId.Value.ToString(),
            0,               // start5Prime
            end5Prime,       // end5Prime
            start3Prime,     // start3Prime
            sequenceLength,  // end3Prime
            algorithm,
            sequenceLength,
            trimmedLength,
            previewSequence,
            Math.Round(trimmedQuality, 2)));
    }

    private static (int start, int end) FindTrim5Prime(int[] qualityScores, int threshold, int windowSize)
    {
        for (int i = 0; i <= qualityScores.Length - windowSize; i++)
        {
            var windowAvg = CalculateWindowAverage(qualityScores, i, windowSize);
            if (windowAvg >= threshold)
                return (0, i);
        }
        return (0, 0);
    }

    private static (int start, int end) FindTrim3Prime(int[] qualityScores, int threshold, int windowSize, int length)
    {
        for (int i = length - 1; i >= windowSize - 1; i--)
        {
            var windowAvg = CalculateWindowAverage(qualityScores, i - windowSize + 1, windowSize);
            if (windowAvg >= threshold)
                return (i + 1, length);
        }
        return (length, length);
    }

    private static double CalculateWindowAverage(int[] qualityScores, int start, int windowSize)
    {
        double sum = 0;
        for (int i = start; i < start + windowSize && i < qualityScores.Length; i++)
        {
            sum += qualityScores[i];
        }
        return sum / windowSize;
    }
}
