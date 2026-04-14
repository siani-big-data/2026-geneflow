using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Application.Traces.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetTraceById;

/// <summary>
/// Handler for GetTraceByIdQuery.
/// </summary>
public sealed class GetTraceByIdQueryHandler
    : IQueryHandler<GetTraceByIdQuery, Result<TraceDto>>
{
    private readonly ITraceRepository _traceRepository;

    public GetTraceByIdQueryHandler(ITraceRepository traceRepository)
    {
        _traceRepository = traceRepository;
    }

    public async Task<Result<TraceDto>> Handle(
        GetTraceByIdQuery request,
        CancellationToken cancellationToken)
    {
        // Parse user ID (for access control, if needed in the future)
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<TraceDto>(TraceErrors.InvalidUserId);

        // Parse trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure<TraceDto>(TraceErrors.NotFound);

        // Get trace with all relations
        var trace = await _traceRepository.GetByIdWithAllAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure<TraceDto>(TraceErrors.NotFound);

        return Result.Success(trace.ToDto());
    }
}
