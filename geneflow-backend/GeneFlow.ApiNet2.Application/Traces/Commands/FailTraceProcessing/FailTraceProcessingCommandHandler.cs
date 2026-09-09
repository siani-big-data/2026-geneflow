using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.FailTraceProcessing;

/// <summary>
/// Handler for FailTraceProcessingCommand.
/// </summary>
public sealed class FailTraceProcessingCommandHandler
    : ICommandHandler<FailTraceProcessingCommand, Result>
{
    private readonly ITraceUnitOfWork _unitOfWork;

    public FailTraceProcessingCommandHandler(ITraceUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        FailTraceProcessingCommand request,
        CancellationToken cancellationToken)
    {
        // Parse trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure(TraceErrors.NotFound);

        // Get trace
        var trace = await _unitOfWork.Traces.GetByIdAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure(TraceErrors.NotFound);

        // Fail processing
        var failResult = trace.FailProcessing(request.Reason);
        if (failResult.IsFailure)
            return failResult;

        // Persist
        _unitOfWork.Traces.Update(trace);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
