using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetStudyAnnotations;

/// <summary>
/// Query to get all shared annotations for a study.
/// </summary>
public sealed record GetStudyAnnotationsQuery(string StudyId) : IQuery<Result<IReadOnlyList<AnnotationDto>>>;
