using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetTraceById;

/// <summary>
/// Query to get a trace by its ID.
/// </summary>
public sealed record GetTraceByIdQuery(
    string UserId,
    string TraceId) : IQuery<Result<TraceDto>>;
