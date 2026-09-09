using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Application.Traces.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetTraceTrims;

/// <summary>
/// Handler for GetTraceTrimsQuery.
/// </summary>
public sealed class GetTraceTrimsQueryHandler
    : IQueryHandler<GetTraceTrimsQuery, Result<IReadOnlyList<TraceTrimDto>>>
{
    private readonly ITraceUnitOfWork _unitOfWork;

    public GetTraceTrimsQueryHandler(ITraceUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<TraceTrimDto>>> Handle(
        GetTraceTrimsQuery request,
        CancellationToken cancellationToken)
    {
        // Parse user ID
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<IReadOnlyList<TraceTrimDto>>(TraceErrors.InvalidUserId);

        // Parse trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure<IReadOnlyList<TraceTrimDto>>(TraceErrors.NotFound);

        // Get trace with trims
        var trace = await _unitOfWork.Traces.GetByIdWithTrimsAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure<IReadOnlyList<TraceTrimDto>>(TraceErrors.NotFound);

        // Get trims (filter by active if requested)
        var trims = request.ActiveOnly
            ? trace.GetActiveTrims()
            : trace.Trims;

        return Result.Success(trims.ToDtos());
    }
}
