using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.DeletePipeline;

/// <summary>
/// Command to delete a pipeline.
/// </summary>
public sealed record DeletePipelineCommand(
    string UserId,
    string StudyId,
    string PipelineId) : ICommand<Result>, IRequireAuthentication, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
}
