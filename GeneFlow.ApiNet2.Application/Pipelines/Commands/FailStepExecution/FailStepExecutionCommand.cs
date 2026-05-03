using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.FailStepExecution;

/// <summary>
/// Command to mark a step execution as failed (called by worker).
/// </summary>
public sealed record FailStepExecutionCommand(
    string ExecutionId,
    string StepExecutionId,
    string? ErrorMessage) : ICommand<Result>;
