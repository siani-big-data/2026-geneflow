using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.CreatePipeline;

/// <summary>
/// Command to create a new pipeline.
/// </summary>
public sealed record CreatePipelineCommand(
    string UserId,
    string StudyId,
    string Name,
    string? Description) : ICommand<Result<PipelineDto>>, IRequireAuthentication, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
}
