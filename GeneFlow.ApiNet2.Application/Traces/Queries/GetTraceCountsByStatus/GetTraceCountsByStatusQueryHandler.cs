using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Application.Traces.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetTraceCountsByStatus;

/// <summary>
/// Handler for GetTraceCountsByStatusQuery.
/// </summary>
public sealed class GetTraceCountsByStatusQueryHandler
    : IQueryHandler<GetTraceCountsByStatusQuery, Result<TraceCountsDto>>
{
    private readonly ITraceRepository _traceRepository;

    public GetTraceCountsByStatusQueryHandler(ITraceRepository traceRepository)
    {
        _traceRepository = traceRepository;
    }

    public async Task<Result<TraceCountsDto>> Handle(
        GetTraceCountsByStatusQuery request,
        CancellationToken cancellationToken)
    {
        // Parse user ID
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<TraceCountsDto>(TraceErrors.InvalidUserId);

        // Parse study ID
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure<TraceCountsDto>(TraceErrors.InvalidStudyId);

        // Get counts
        var counts = await _traceRepository.GetCountsByStatusAsync(studyId, cancellationToken);

        return Result.Success(counts.ToCountsDto());
    }
}
