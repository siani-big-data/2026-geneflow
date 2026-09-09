using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Queries.GetPipelineExecutions;

/// <summary>
/// Query to get executions for a pipeline.
/// </summary>
public sealed record GetPipelineExecutionsQuery(
    string UserId,
    string StudyId,
    string PipelineId,
    int PageNumber = 1,
    int PageSize = 20,
    int? StatusId = null) : IQuery<Result<PagedList<PipelineExecutionSummaryDto>>>, IRequireAuthentication, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
}
