using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Application.Analysis.Commands.RequestAnalysis;

/// <summary>
/// Handler for RequestAnalysisCommand.
/// Loads sequence + quality from the trace's processed data file
/// and publishes the analysis job to the Analysis worker via Redis Streams.
/// Also records an <c>AnalysisRequested</c> domain event so the activity
/// projector can surface the action in the user feed and study timeline.
/// </summary>
public sealed class RequestAnalysisCommandHandler
    : ICommandHandler<RequestAnalysisCommand, Result>
{
    private readonly ITraceUnitOfWork _unitOfWork;
    private readonly IJobPublisher _jobPublisher;
    private readonly ITraceAnalysisService _analysisService;
    private readonly ICurrentUserService _currentUserService;

    public RequestAnalysisCommandHandler(
        ITraceUnitOfWork unitOfWork,
        IJobPublisher jobPublisher,
        ITraceAnalysisService analysisService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _jobPublisher = jobPublisher;
        _analysisService = analysisService;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(
        RequestAnalysisCommand request,
        CancellationToken cancellationToken)
    {
        if (!AnalysisTypes.IsValid(request.AnalysisType))
            return Result.Failure(AnalysisErrors.InvalidAnalysisType(request.AnalysisType));

        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure(AnalysisErrors.InvalidTraceId);

        var trace = await _unitOfWork.Traces.GetByIdAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure(AnalysisErrors.TraceNotFound(request.TraceId));

        if (!trace.IsProcessed)
            return Result.Failure(AnalysisErrors.TraceNotProcessed);

        var dataResult = await _analysisService.GetAnalysisDataAsync(trace, cancellationToken);
        if (dataResult.IsFailure)
            return Result.Failure(dataResult.Error);

        var (sequence, quality) = dataResult.Value;

        await _jobPublisher.PublishAnalysisJobAsync(new AnalysisJob(
            TraceId: request.TraceId,
            AnalysisType: request.AnalysisType,
            Sequence: sequence,
            Quality: quality,
            Options: request.Options
        ), cancellationToken);

        // Record the analysis request in the activity feed. The trace is
        // already tracked by EF; SaveChangesAsync flushes the domain event to
        // the dispatcher without mutating trace state.
        var actor = _currentUserService.UserId;
        if (actor is not null)
        {
            trace.RecordAnalysisRequested(request.AnalysisType, actor);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
