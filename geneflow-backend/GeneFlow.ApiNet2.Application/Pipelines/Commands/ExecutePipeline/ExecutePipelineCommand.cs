using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.ExecutePipeline;

/// <summary>
/// Command to execute a pipeline on a trace.
/// </summary>
public sealed record ExecutePipelineCommand(
    string UserId,
    string StudyId,
    string PipelineId,
    string TraceId) : ICommand<Result<PipelineExecutionDto>>, IRequireAuthentication, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
}
