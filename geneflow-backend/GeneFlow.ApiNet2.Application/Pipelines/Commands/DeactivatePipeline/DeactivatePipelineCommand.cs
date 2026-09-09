using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.DeactivatePipeline;

/// <summary>
/// Command to deactivate a pipeline (Active -> Draft).
/// </summary>
public sealed record DeactivatePipelineCommand(
    string UserId,
    string StudyId,
    string PipelineId) : ICommand<Result<PipelineDto>>, IRequireAuthentication, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
}
