using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Application.Traces.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.CreateSequenceEdit;

/// <summary>
/// Handler for CreateSequenceEditCommand.
/// Creates a new sequence edit on a trace.
/// </summary>
public sealed class CreateSequenceEditCommandHandler
    : ICommandHandler<CreateSequenceEditCommand, Result<SequenceEditDto>>
{
    private readonly ITraceUnitOfWork _unitOfWork;

    public CreateSequenceEditCommandHandler(ITraceUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SequenceEditDto>> Handle(
        CreateSequenceEditCommand request,
        CancellationToken cancellationToken)
    {
        // Parse user ID
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<SequenceEditDto>(TraceErrors.InvalidUserId);

        // Parse trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure<SequenceEditDto>(TraceErrors.NotFound);

        // Get edit type
        var editType = EditType.FromValue(request.EditType);
        if (editType is null)
            return Result.Failure<SequenceEditDto>(TraceErrors.InvalidEditType);

        // Get trace
        var trace = await _unitOfWork.Traces.GetByIdAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure<SequenceEditDto>(TraceErrors.NotFound);

        // Add edit
        var editResult = trace.AddEdit(
            editType,
            request.Position,
            request.OriginalBase,
            request.NewBase,
            request.Reason,
            userId);

        if (editResult.IsFailure)
            return Result.Failure<SequenceEditDto>(editResult.Error);

        // Persist
        _unitOfWork.Traces.Update(trace);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(editResult.Value.ToDto());
    }
}
