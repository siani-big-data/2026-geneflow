using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Queries.GetStudyPipelines;

/// <summary>
/// Query to get pipelines for a study.
/// </summary>
public sealed record GetStudyPipelinesQuery(
    string UserId,
    string StudyId,
    int PageNumber = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    int? StatusId = null) : IQuery<Result<PagedList<PipelineSummaryDto>>>, IRequireAuthentication, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
}
