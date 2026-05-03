using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetTrimmedSequence;

/// <summary>
/// Query to get the trimmed sequence for a trace.
/// Requires membership in the trace's parent study (allows public study access).
/// </summary>
public sealed record GetTrimmedSequenceQuery(string TraceId) : IQuery<Result<TrimmedSequenceDto>>, IRequireTraceAccess
{
    string IRequireTraceAccess.TraceId => TraceId;
}
