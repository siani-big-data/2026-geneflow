using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Application.Analysis.Commands.RequestAlignment;

/// <summary>
/// Handler for RequestAlignmentCommand.
/// Publishes alignment jobs to the Analysis worker via Redis Streams.
/// </summary>
public sealed class RequestAlignmentCommandHandler
    : ICommandHandler<RequestAlignmentCommand, Result<string>>
{
    private readonly ITraceUnitOfWork _unitOfWork;
    private readonly IJobPublisher _jobPublisher;

    public RequestAlignmentCommandHandler(
        ITraceUnitOfWork unitOfWork,
        IJobPublisher jobPublisher)
    {
        _unitOfWork = unitOfWork;
        _jobPublisher = jobPublisher;
    }

    public async Task<Result<string>> Handle(
        RequestAlignmentCommand request,
        CancellationToken cancellationToken)
    {
        // Validate alignment type
        if (!AlignmentTypes.IsValid(request.Type))
            return Result.Failure<string>(AlignmentErrors.InvalidAlignmentType(request.Type));

        // Validate trace IDs count
        if (request.TraceIds.Count < 2)
            return Result.Failure<string>(AlignmentErrors.InsufficientTraces);

        // Validate and verify all traces exist and are processed
        var traceIds = new List<TraceId>();
        foreach (var traceIdStr in request.TraceIds)
        {
            if (!TraceId.TryParse(traceIdStr, out var traceId) || traceId is null)
                return Result.Failure<string>(AlignmentErrors.InvalidTraceId(traceIdStr));

            traceIds.Add(traceId);
        }

        // Verify all traces exist
        var traces = await _unitOfWork.Traces.GetByIdsAsync(traceIds, cancellationToken);
        if (traces.Count != traceIds.Count)
        {
            var foundIds = traces.Select(t => t.Id.ToString()).ToHashSet();
            var missingId = request.TraceIds.First(id => !foundIds.Contains(id));
            return Result.Failure<string>(AlignmentErrors.TraceNotFound(missingId));
        }

        // Verify all traces are processed
        var unprocessedTrace = traces.FirstOrDefault(t => !t.IsProcessed);
        if (unprocessedTrace is not null)
            return Result.Failure<string>(AlignmentErrors.TraceNotProcessed(unprocessedTrace.Id.ToString()));

        // Generate alignment ID
        var alignmentId = $"ALN{Guid.NewGuid():N}"[..16].ToUpperInvariant();

        // Publish alignment job
        await _jobPublisher.PublishAlignmentJobAsync(new AlignmentJob(
            AlignmentId: alignmentId,
            Type: request.Type,
            TraceIds: request.TraceIds,
            Sequences: null, // Worker will fetch from storage
            Options: new AlignmentJobOptions(
                BuildConsensus: request.BuildConsensus,
                ConsensusMethod: request.ConsensusMethod,
                MatchScore: request.MatchScore,
                MismatchPenalty: request.MismatchPenalty,
                GapPenalty: request.GapPenalty)
        ), cancellationToken);

        return Result.Success(alignmentId);
    }
}
