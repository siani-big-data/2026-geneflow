using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetEditedSequence;

/// <summary>
/// Query to get the sequence with edits applied.
/// Requires membership in the trace's parent study (allows public study access).
/// </summary>
public sealed record GetEditedSequenceQuery(string TraceId) : IQuery<Result<EditedSequenceDto>>, IRequireTraceAccess
{
    string IRequireTraceAccess.TraceId => TraceId;
}
