using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetSequenceEdits;

/// <summary>
/// Query to get all active sequence edits for a trace.
/// Requires membership in the trace's parent study (allows public study access).
/// </summary>
public sealed record GetSequenceEditsQuery(
    string TraceId,
    bool IncludeInactive = false) : IQuery<Result<IReadOnlyList<SequenceEditDto>>>, IRequireTraceAccess
{
    string IRequireTraceAccess.TraceId => TraceId;
}
