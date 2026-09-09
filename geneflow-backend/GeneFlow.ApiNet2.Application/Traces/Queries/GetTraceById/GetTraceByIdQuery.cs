using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetTraceById;

/// <summary>
/// Query to get a trace by its ID.
/// Requires membership in the trace's parent study (allows public study access).
/// </summary>
public sealed record GetTraceByIdQuery(
    string UserId,
    string TraceId) : IQuery<Result<TraceDto>>, IRequireTraceAccess
{
    string IRequireTraceAccess.TraceId => TraceId;
}
