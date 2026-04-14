using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.UndoAllSequenceEdits;

/// <summary>
/// Handler for UndoAllSequenceEditsCommand.
/// Undoes all active sequence edits on a trace.
/// </summary>
public sealed class UndoAllSequenceEditsCommandHandler
    : ICommandHandler<UndoAllSequenceEditsCommand, Result>
{
    private readonly ITraceUnitOfWork _unitOfWork;

    public UndoAllSequenceEditsCommandHandler(ITraceUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        UndoAllSequenceEditsCommand request,
        CancellationToken cancellationToken)
    {
        // Parse user ID
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure(TraceErrors.InvalidUserId);

        // Parse trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure(TraceErrors.NotFound);

        // Get trace
        var trace = await _unitOfWork.Traces.GetByIdAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure(TraceErrors.NotFound);

        // Undo all edits
        var undoResult = trace.UndoAllEdits(userId);
        if (undoResult.IsFailure)
            return undoResult;

        // Persist
        _unitOfWork.Traces.Update(trace);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
