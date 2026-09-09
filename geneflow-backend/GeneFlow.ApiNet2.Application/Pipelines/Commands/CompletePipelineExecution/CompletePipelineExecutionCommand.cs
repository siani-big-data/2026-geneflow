using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.CompletePipelineExecution;

/// <summary>
/// Command to mark a pipeline execution as completed (called by worker after last step).
/// </summary>
public sealed record CompletePipelineExecutionCommand(
    string ExecutionId) : ICommand<Result>;
