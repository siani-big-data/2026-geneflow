using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.UpdatePipelineStep;

/// <summary>
/// Command to update a pipeline step.
/// </summary>
public sealed record UpdatePipelineStepCommand(
    string UserId,
    string StudyId,
    string PipelineId,
    string StepId,
    string? Label,
    string? Configuration,
    bool IsEnabled) : ICommand<Result<PipelineStepDto>>, IRequireAuthentication, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
}
