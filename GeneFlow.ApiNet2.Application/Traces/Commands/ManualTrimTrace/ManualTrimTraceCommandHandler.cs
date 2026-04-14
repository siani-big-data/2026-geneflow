using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Application.Traces.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.ManualTrimTrace;

/// <summary>
/// Handler for ManualTrimTraceCommand.
/// Applies user-specified trim boundaries to a trace.
/// </summary>
public sealed class ManualTrimTraceCommandHandler
    : ICommandHandler<ManualTrimTraceCommand, Result<TrimRegionDto>>
{
    private readonly ITraceUnitOfWork _unitOfWork;

    public ManualTrimTraceCommandHandler(ITraceUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TrimRegionDto>> Handle(
        ManualTrimTraceCommand request,
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

        // Get sequence length
        var sequenceLength = trace.QualityMetrics?.TotalBases ?? 0;
        if (sequenceLength == 0)
            return Result.Failure<TrimRegionDto>(TraceErrors.InvalidTotalBases);

        // Create trim region
        var trimResult = TrimRegion.Create(
            request.Start5Prime,
            request.End5Prime,
            request.Start3Prime,
            request.End3Prime,
            "Manual",
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
}
