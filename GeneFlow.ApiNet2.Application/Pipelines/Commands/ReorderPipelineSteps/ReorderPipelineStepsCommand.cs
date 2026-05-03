using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.ReorderPipelineSteps;

/// <summary>
/// Command to reorder pipeline steps.
/// </summary>
public sealed record ReorderPipelineStepsCommand(
    string UserId,
    string StudyId,
    string PipelineId,
    IReadOnlyList<string> StepIds) : ICommand<Result<PipelineDto>>, IRequireAuthentication, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
}
