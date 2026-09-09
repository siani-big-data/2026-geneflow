using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Queries.GetPipelineById;

/// <summary>
/// Query to get a pipeline by ID.
/// </summary>
public sealed record GetPipelineByIdQuery(
    string UserId,
    string StudyId,
    string PipelineId) : IQuery<Result<PipelineDto>>, IRequireAuthentication, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
}
