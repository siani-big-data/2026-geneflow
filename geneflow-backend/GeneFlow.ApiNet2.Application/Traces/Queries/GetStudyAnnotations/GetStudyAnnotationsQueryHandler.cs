using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Application.Traces.Mappings;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetStudyAnnotations;

/// <summary>
/// Handler for GetStudyAnnotationsQuery.
/// Returns all shared annotations across all traces in a study.
/// </summary>
public sealed class GetStudyAnnotationsQueryHandler
    : IQueryHandler<GetStudyAnnotationsQuery, Result<IReadOnlyList<AnnotationDto>>>
{
    private readonly ITraceRepository _repository;

    public GetStudyAnnotationsQueryHandler(ITraceRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<AnnotationDto>>> Handle(
        GetStudyAnnotationsQuery request,
        CancellationToken cancellationToken)
    {
        // Parse study ID
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure<IReadOnlyList<AnnotationDto>>(TraceErrors.InvalidStudyId);

        // Get all traces for the study with annotations
        var traces = await _repository.GetByStudyIdAsync(studyId, cancellationToken);

        // Collect all shared annotations
        var annotations = traces
            .SelectMany(trace => trace.GetSharedAnnotations()
                .Select(a => a.ToDto(trace.Id.Value.ToString())))
            .OrderBy(a => a.TraceId)
            .ThenBy(a => a.StartPosition)
            .ToList();

        return Result.Success<IReadOnlyList<AnnotationDto>>(annotations);
    }
}
