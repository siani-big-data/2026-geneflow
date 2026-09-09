using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.CompleteStepExecution;

/// <summary>
/// Command to mark a step execution as completed (called by worker).
/// </summary>
public sealed record CompleteStepExecutionCommand(
    string ExecutionId,
    string StepExecutionId,
    string? ResultSummary,
    string? ResultData) : ICommand<Result>;
