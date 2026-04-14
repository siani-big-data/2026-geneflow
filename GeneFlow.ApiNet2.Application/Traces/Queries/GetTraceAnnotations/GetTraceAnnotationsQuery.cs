using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetTraceAnnotations;

/// <summary>
/// Query to get all annotations for a trace.
/// </summary>
public sealed record GetTraceAnnotationsQuery(string TraceId) : IQuery<Result<IReadOnlyList<AnnotationDto>>>;
