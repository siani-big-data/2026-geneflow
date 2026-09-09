using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.AddPipelineStep;

/// <summary>
/// Command to add a step to a pipeline.
/// </summary>
public sealed record AddPipelineStepCommand(
    string UserId,
    string StudyId,
    string PipelineId,
    int StepTypeId,
    string? Label,
    string? Configuration,
    bool IsEnabled = true) : ICommand<Result<PipelineStepDto>>, IRequireAuthentication, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
}
