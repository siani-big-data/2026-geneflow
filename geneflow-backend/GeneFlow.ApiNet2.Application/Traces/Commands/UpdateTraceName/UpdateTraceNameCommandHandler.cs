using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.UpdateTraceName;

/// <summary>
/// Handler for UpdateTraceNameCommand.
/// </summary>
public sealed class UpdateTraceNameCommandHandler
    : ICommandHandler<UpdateTraceNameCommand, Result>
{
    private readonly ITraceUnitOfWork _unitOfWork;

    public UpdateTraceNameCommandHandler(ITraceUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        UpdateTraceNameCommand request,
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

        // Create name value object
        var nameResult = TraceName.Create(request.Name);
        if (nameResult.IsFailure)
            return nameResult;

        // Update
        var updateResult = trace.UpdateName(nameResult.Value, userId);
        if (updateResult.IsFailure)
            return updateResult;

        // Persist
        _unitOfWork.Traces.Update(trace);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
