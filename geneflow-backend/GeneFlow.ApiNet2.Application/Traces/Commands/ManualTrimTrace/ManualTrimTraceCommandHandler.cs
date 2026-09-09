using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Application.Traces.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.ManualTrimTrace;

/// <summary>
/// Handler for ManualTrimTraceCommand.
/// Adds a manual trim to a trace sequence.
/// </summary>
public sealed class ManualTrimTraceCommandHandler
    : ICommandHandler<ManualTrimTraceCommand, Result<TraceTrimDto>>
{
    private readonly ITraceUnitOfWork _unitOfWork;

    public ManualTrimTraceCommandHandler(ITraceUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TraceTrimDto>> Handle(
        ManualTrimTraceCommand request,
        CancellationToken cancellationToken)
    {
        // Parse user ID
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<TraceTrimDto>(TraceErrors.InvalidUserId);

        // Parse trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure<TraceTrimDto>(TraceErrors.NotFound);

        // Parse trim end
        var trimEnd = TrimEnd.FromName(request.TrimEnd);
        if (trimEnd is null)
            return Result.Failure<TraceTrimDto>(TraceErrors.InvalidTrimEnd);

        // Get trace with trims
        var trace = await _unitOfWork.Traces.GetByIdWithTrimsAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure<TraceTrimDto>(TraceErrors.NotFound);

        // Add trim
        var trimResult = trace.AddTrim(
            TrimType.Manual,
            request.StartPosition,
            request.EndPosition,
            trimEnd,
            "Manual",
            userId,
            request.Reason);

        if (trimResult.IsFailure)
            return Result.Failure<TraceTrimDto>(trimResult.Error);

        // Persist
        _unitOfWork.Traces.Update(trace);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(trimResult.Value.ToDto());
    }
}
