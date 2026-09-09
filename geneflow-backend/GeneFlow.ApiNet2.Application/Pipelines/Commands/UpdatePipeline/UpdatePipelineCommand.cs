using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.UpdatePipeline;

/// <summary>
/// Command to update a pipeline's name and description.
/// </summary>
public sealed record UpdatePipelineCommand(
    string UserId,
    string StudyId,
    string PipelineId,
    string Name,
    string? Description) : ICommand<Result<PipelineDto>>, IRequireAuthentication, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
}
