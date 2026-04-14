using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Application.Traces.Mappings;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetTraceAnnotations;

/// <summary>
/// Handler for GetTraceAnnotationsQuery.
/// Returns all annotations for a trace.
/// </summary>
public sealed class GetTraceAnnotationsQueryHandler
    : IQueryHandler<GetTraceAnnotationsQuery, Result<IReadOnlyList<AnnotationDto>>>
{
    private readonly ITraceRepository _repository;

    public GetTraceAnnotationsQueryHandler(ITraceRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<AnnotationDto>>> Handle(
        GetTraceAnnotationsQuery request,
        CancellationToken cancellationToken)
    {
        // Parse trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure<IReadOnlyList<AnnotationDto>>(TraceErrors.NotFound);

        // Get trace with annotations
        var trace = await _repository.GetByIdAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure<IReadOnlyList<AnnotationDto>>(TraceErrors.NotFound);

        var annotations = trace.Annotations
            .OrderBy(a => a.StartPosition)
            .Select(a => a.ToDto(traceId.Value.ToString()))
            .ToList();

        return Result.Success<IReadOnlyList<AnnotationDto>>(annotations);
    }
}
