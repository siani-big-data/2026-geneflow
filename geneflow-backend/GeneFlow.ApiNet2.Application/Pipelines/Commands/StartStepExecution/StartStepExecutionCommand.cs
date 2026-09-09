using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Pipelines.Commands.StartStepExecution;

/// <summary>
/// Command to mark a step execution as running (called by worker).
/// Also transitions the parent pipeline execution to Running on the first step.
/// </summary>
public sealed record StartStepExecutionCommand(
    string ExecutionId,
    string StepExecutionId) : ICommand<Result>;
