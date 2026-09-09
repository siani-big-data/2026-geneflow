using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetTraceAnnotations;

/// <summary>
/// Query to get all annotations for a trace.
/// Requires membership in the trace's parent study (allows public study access).
/// </summary>
public sealed record GetTraceAnnotationsQuery(string TraceId) : IQuery<Result<IReadOnlyList<AnnotationDto>>>, IRequireTraceAccess
{
    string IRequireTraceAccess.TraceId => TraceId;
}
