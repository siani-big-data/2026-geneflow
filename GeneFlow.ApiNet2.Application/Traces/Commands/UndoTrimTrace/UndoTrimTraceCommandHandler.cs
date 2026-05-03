using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.UndoTrimTrace;

/// <summary>
/// Handler for UndoTrimTraceCommand.
/// Undoes a specific trim or all trims from a trace.
/// </summary>
public sealed class UndoTrimTraceCommandHandler
    : ICommandHandler<UndoTrimTraceCommand, Result>
{
    private readonly ITraceUnitOfWork _unitOfWork;

    public UndoTrimTraceCommandHandler(ITraceUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        UndoTrimTraceCommand request,
        CancellationToken cancellationToken)
    {
        // Parse user ID
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure(TraceErrors.InvalidUserId);

        // Parse trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure(TraceErrors.NotFound);

        // Get trace with trims
        var trace = await _unitOfWork.Traces.GetByIdWithTrimsAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure(TraceErrors.NotFound);

        Result undoResult;

        if (request.UndoAll)
        {
            // Undo all trims
            undoResult = trace.UndoAllTrims(userId);
        }
        else if (!string.IsNullOrWhiteSpace(request.TrimId))
        {
            // Undo specific trim
            if (!Guid.TryParse(request.TrimId, out var trimId))
                return Result.Failure(TraceErrors.TrimNotFound);

            undoResult = trace.UndoTrim(trimId, userId);
        }
        else
        {
            // Must provide either TrimId or UndoAll
            return Result.Failure(TraceErrors.TrimNotFound);
        }

        if (undoResult.IsFailure)
            return undoResult;

        // Persist
        _unitOfWork.Traces.Update(trace);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
