using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.CancelExecution;

/// <summary>
/// Command to cancel a pipeline execution.
/// </summary>
public sealed record CancelExecutionCommand(
    string UserId,
    string ExecutionId) : ICommand<Result<PipelineExecutionDto>>, IRequireAuthentication;
