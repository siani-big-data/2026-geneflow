using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.Application.Traces.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.AutoTrimTrace;

/// <summary>
/// Handler for AutoTrimTraceCommand.
/// Uses Mott algorithm (sliding window) for quality-based trimming.
/// Creates separate trim operations for 5' and 3' ends.
/// </summary>
public sealed class AutoTrimTraceCommandHandler
    : ICommandHandler<AutoTrimTraceCommand, Result<IReadOnlyList<TraceTrimDto>>>
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

    public async Task<Result<IReadOnlyList<TraceTrimDto>>> Handle(
        AutoTrimTraceCommand request,
        CancellationToken cancellationToken)
    {
        // Parse user ID
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<IReadOnlyList<TraceTrimDto>>(TraceErrors.InvalidUserId);

        // Parse trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure<IReadOnlyList<TraceTrimDto>>(TraceErrors.NotFound);

        // Get trace with trims
        var trace = await _unitOfWork.Traces.GetByIdWithTrimsAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure<IReadOnlyList<TraceTrimDto>>(TraceErrors.NotFound);

        // Check if can be edited
        if (!trace.CanBeEdited)
            return Result.Failure<IReadOnlyList<TraceTrimDto>>(TraceErrors.CannotEditInCurrentStatus);

        // Get quality scores from analysis file
        var qualityResult = await _analysisService.GetQualityScoresAsync(trace, cancellationToken);
        if (qualityResult.IsFailure)
            return Result.Failure<IReadOnlyList<TraceTrimDto>>(qualityResult.Error);

        var qualityScores = qualityResult.Value;
        var sequenceLength = qualityScores.Length;

        if (sequenceLength == 0)
            return Result.Failure<IReadOnlyList<TraceTrimDto>>(TraceErrors.InvalidTotalBases);

        var threshold = request.QualityThreshold;
        var windowSize = request.WindowSize;
        var minLength = request.MinimumLength;

        // Calculate trim boundaries using sliding window algorithm (Mott's algorithm)
        var end5Prime = FindTrim5Prime(qualityScores, threshold, windowSize);
        var start3Prime = FindTrim3Prime(qualityScores, threshold, windowSize, sequenceLength);

        // Ensure minimum length
        var trimmedLength = start3Prime - end5Prime;
        if (trimmedLength < minLength)
        {
            return Result.Failure<IReadOnlyList<TraceTrimDto>>(Error.Validation(
                "Trace.TrimmedTooShort",
                $"Trimmed sequence would be {trimmedLength} bases, which is less than the minimum of {minLength} bases."));
        }

        var algorithm = $"Mott(Q{threshold},W{windowSize})";
        var createdTrims = new List<TraceTrimDto>();

        // Add 5' trim if there are bases to trim
        if (end5Prime > 0)
        {
            var trim5Result = trace.AddTrim(
                TrimType.AutoMott,
                0,
                end5Prime,
                TrimEnd.FivePrime,
                algorithm,
                userId);

            if (trim5Result.IsFailure)
                return Result.Failure<IReadOnlyList<TraceTrimDto>>(trim5Result.Error);

            createdTrims.Add(trim5Result.Value.ToDto());
        }

        // Add 3' trim if there are bases to trim
        if (start3Prime < sequenceLength)
        {
            var trim3Result = trace.AddTrim(
                TrimType.AutoMott,
                start3Prime,
                sequenceLength,
                TrimEnd.ThreePrime,
                algorithm,
                userId);

            if (trim3Result.IsFailure)
                return Result.Failure<IReadOnlyList<TraceTrimDto>>(trim3Result.Error);

            createdTrims.Add(trim3Result.Value.ToDto());
        }

        // Persist
        _unitOfWork.Traces.Update(trace);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success<IReadOnlyList<TraceTrimDto>>(createdTrims);
    }

    /// <summary>
    /// Finds the 5' trim point using sliding window average quality.
    /// Returns the position where good quality starts.
    /// </summary>
    private static int FindTrim5Prime(int[] qualityScores, int threshold, int windowSize)
    {
        for (int i = 0; i <= qualityScores.Length - windowSize; i++)
        {
            var windowAvg = CalculateWindowAverage(qualityScores, i, windowSize);
            if (windowAvg >= threshold)
                return i;
        }
        return 0;
    }

    /// <summary>
    /// Finds the 3' trim point using sliding window average quality.
    /// Returns the position where good quality ends.
    /// </summary>
    private static int FindTrim3Prime(int[] qualityScores, int threshold, int windowSize, int length)
    {
        for (int i = length - 1; i >= windowSize - 1; i--)
        {
            var windowAvg = CalculateWindowAverage(qualityScores, i - windowSize + 1, windowSize);
            if (windowAvg >= threshold)
                return i + 1;
        }
        return length;
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
