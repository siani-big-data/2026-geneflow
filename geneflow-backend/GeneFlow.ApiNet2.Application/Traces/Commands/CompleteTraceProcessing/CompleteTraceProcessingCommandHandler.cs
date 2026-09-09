using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.CompleteTraceProcessing;

/// <summary>
/// Handler for CompleteTraceProcessingCommand.
/// </summary>
public sealed class CompleteTraceProcessingCommandHandler
    : ICommandHandler<CompleteTraceProcessingCommand, Result>
{
    private readonly ITraceUnitOfWork _unitOfWork;

    public CompleteTraceProcessingCommandHandler(ITraceUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        CompleteTraceProcessingCommand request,
        CancellationToken cancellationToken)
    {
        // Parse trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure(TraceErrors.NotFound);

        // Get trace
        var trace = await _unitOfWork.Traces.GetByIdAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure(TraceErrors.NotFound);

        // Create quality metrics
        var metricsResult = QualityMetrics.Create(
            request.AverageQualityScore,
            request.TotalBases,
            request.QualityAboveQ20Percentage,
            request.QualityAboveQ30Percentage,
            request.TrimmedLength,
            request.GcContentPercentage);

        if (metricsResult.IsFailure)
            return metricsResult;

        // Complete processing
        var completeResult = trace.CompleteProcessing(metricsResult.Value, request.HasChromatogramData);
        if (completeResult.IsFailure)
            return completeResult;

        // Persist
        _unitOfWork.Traces.Update(trace);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
