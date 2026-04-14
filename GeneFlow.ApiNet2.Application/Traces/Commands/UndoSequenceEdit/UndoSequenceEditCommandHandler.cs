using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.UndoSequenceEdit;

/// <summary>
/// Handler for UndoSequenceEditCommand.
/// Undoes a specific sequence edit.
/// </summary>
public sealed class UndoSequenceEditCommandHandler
    : ICommandHandler<UndoSequenceEditCommand, Result>
{
    private readonly ITraceUnitOfWork _unitOfWork;

    public UndoSequenceEditCommandHandler(ITraceUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        UndoSequenceEditCommand request,
        CancellationToken cancellationToken)
    {
        // Parse user ID
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure(TraceErrors.InvalidUserId);

        // Parse trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure(TraceErrors.NotFound);

        // Parse edit ID
        if (!Guid.TryParse(request.EditId, out var editId))
            return Result.Failure(TraceErrors.EditNotFound);

        // Get trace
        var trace = await _unitOfWork.Traces.GetByIdAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure(TraceErrors.NotFound);

        // Undo edit
        var undoResult = trace.UndoEdit(editId, userId);
        if (undoResult.IsFailure)
            return undoResult;

        // Persist
        _unitOfWork.Traces.Update(trace);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
