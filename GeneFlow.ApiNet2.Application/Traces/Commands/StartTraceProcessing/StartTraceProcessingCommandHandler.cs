using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.StartTraceProcessing;

/// <summary>
/// Handler for StartTraceProcessingCommand.
/// </summary>
public sealed class StartTraceProcessingCommandHandler
    : ICommandHandler<StartTraceProcessingCommand, Result>
{
    private readonly ITraceUnitOfWork _unitOfWork;

    public StartTraceProcessingCommandHandler(ITraceUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        StartTraceProcessingCommand request,
        CancellationToken cancellationToken)
    {
        // Parse trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure(TraceErrors.NotFound);

        // Get trace
        var trace = await _unitOfWork.Traces.GetByIdAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure(TraceErrors.NotFound);

        // Start processing (Uploaded -> Validating)
        var startResult = trace.StartProcessing();
        if (startResult.IsFailure)
            return startResult;

        // Transition to Processing (Validating -> Processing)
        var transitionResult = trace.TransitionToProcessing();
        if (transitionResult.IsFailure)
            return transitionResult;

        // Persist
        _unitOfWork.Traces.Update(trace);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
