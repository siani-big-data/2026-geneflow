using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.RemovePipelineStep;

/// <summary>
/// Command to remove a step from a pipeline.
/// </summary>
public sealed record RemovePipelineStepCommand(
    string UserId,
    string StudyId,
    string PipelineId,
    string StepId) : ICommand<Result>, IRequireAuthentication, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
}
