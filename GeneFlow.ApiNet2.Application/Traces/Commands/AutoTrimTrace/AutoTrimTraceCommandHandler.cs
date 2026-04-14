using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.Application.Traces.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.AutoTrimTrace;

/// <summary>
/// Handler for AutoTrimTraceCommand.
/// Uses Mott algorithm (sliding window) for quality-based trimming.
/// </summary>
public sealed class AutoTrimTraceCommandHandler
    : ICommandHandler<AutoTrimTraceCommand, Result<TrimRegionDto>>
{
    private readonly ITraceUnitOfWork _unitOfWork;
    private readonly ITraceAnalysisService _analysisService;

    public AutoTrimTraceCommandHandler(
        ITraceUnitOfWork unitOfWork,
        ITraceAnalysisService analysisService)
    {
        _unitOfWork = unitOfWork;
        _analysisService = analysisService;
    }

    public async Task<Result<TrimRegionDto>> Handle(
        AutoTrimTraceCommand request,
        CancellationToken cancellationToken)
    {
        // Parse user ID
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<TrimRegionDto>(TraceErrors.InvalidUserId);

        // Parse trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure<TrimRegionDto>(TraceErrors.NotFound);

        // Get trace
        var trace = await _unitOfWork.Traces.GetByIdAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure<TrimRegionDto>(TraceErrors.NotFound);

        // Check if can be edited
        if (!trace.CanBeEdited)
            return Result.Failure<TrimRegionDto>(TraceErrors.CannotEditInCurrentStatus);

        // Get quality scores from analysis file
        var qualityResult = await _analysisService.GetQualityScoresAsync(trace, cancellationToken);
        if (qualityResult.IsFailure)
            return Result.Failure<TrimRegionDto>(qualityResult.Error);

        var qualityScores = qualityResult.Value;
        var sequenceLength = qualityScores.Length;

        if (sequenceLength == 0)
            return Result.Failure<TrimRegionDto>(TraceErrors.InvalidTotalBases);

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
            return Result.Failure<TrimRegionDto>(Error.Validation(
                "Trace.TrimmedTooShort",
                $"Trimmed sequence would be {trimmedLength} bases, which is less than the minimum of {minLength} bases."));
        }

        var algorithm = $"Mott(Q{threshold},W{windowSize})";

        // Create trim region
        var trimResult = TrimRegion.Create(
            0,               // start5Prime
            end5Prime,       // end5Prime (where good quality starts)
            start3Prime,     // start3Prime (where good quality ends)
            sequenceLength,  // end3Prime
            algorithm,
            userId.Value.ToString(),
            sequenceLength);

        if (trimResult.IsFailure)
            return Result.Failure<TrimRegionDto>(trimResult.Error);

        // Apply trim
        var applyResult = trace.ApplyTrim(trimResult.Value, userId);
        if (applyResult.IsFailure)
            return Result.Failure<TrimRegionDto>(applyResult.Error);

        // Persist
        _unitOfWork.Traces.Update(trace);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(trimResult.Value.ToDto());
    }

    /// <summary>
    /// Finds the 5' trim point using sliding window average quality.
    /// Scans from the start to find the first position where average quality exceeds threshold.
    /// </summary>
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

    /// <summary>
    /// Finds the 3' trim point using sliding window average quality.
    /// Scans from the end to find the last position where average quality exceeds threshold.
    /// </summary>
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

    /// <summary>
    /// Calculates the average quality score for a window.
    /// </summary>
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
