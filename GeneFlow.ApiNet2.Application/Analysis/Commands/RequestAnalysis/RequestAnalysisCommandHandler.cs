using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Application.Analysis.Commands.RequestAnalysis;

/// <summary>
/// Handler for RequestAnalysisCommand.
/// Publishes analysis jobs to the Analysis worker via Redis Streams.
/// </summary>
public sealed class RequestAnalysisCommandHandler
    : ICommandHandler<RequestAnalysisCommand, Result>
{
    private readonly ITraceUnitOfWork _unitOfWork;
    private readonly IJobPublisher _jobPublisher;

    public RequestAnalysisCommandHandler(
        ITraceUnitOfWork unitOfWork,
        IJobPublisher jobPublisher)
    {
        _unitOfWork = unitOfWork;
        _jobPublisher = jobPublisher;
    }

    public async Task<Result> Handle(
        RequestAnalysisCommand request,
        CancellationToken cancellationToken)
    {
        // Validate analysis type
        if (!AnalysisTypes.IsValid(request.AnalysisType))
            return Result.Failure(AnalysisErrors.InvalidAnalysisType(request.AnalysisType));

        // Parse and validate trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure(AnalysisErrors.InvalidTraceId);

        // Get the trace
        var trace = await _unitOfWork.Traces.GetByIdAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure(AnalysisErrors.TraceNotFound(request.TraceId));

        // Ensure trace is processed
        if (!trace.IsProcessed)
            return Result.Failure(AnalysisErrors.TraceNotProcessed);

        // Publish analysis job
        await _jobPublisher.PublishAnalysisJobAsync(new AnalysisJob(
            TraceId: request.TraceId,
            AnalysisType: request.AnalysisType,
            Sequence: null, // Worker will fetch from storage
            Quality: null,  // Worker will fetch from storage
            Options: request.Options
        ), cancellationToken);

        return Result.Success();
    }
}
