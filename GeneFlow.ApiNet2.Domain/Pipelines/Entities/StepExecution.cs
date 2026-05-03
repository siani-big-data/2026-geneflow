using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Pipelines.Entities;

/// <summary>
/// Represents the execution of a single step within a pipeline execution.
/// </summary>
public sealed class StepExecution : Entity<Guid>
{
    public const int MaxErrorMessageLength = 2000;
    public const int MaxResultSummaryLength = 4000;

    public Guid PipelineStepId { get; private set; }
    public int Order { get; private set; }
    public StepType StepType { get; private set; } = null!;
    public StepExecutionStatus Status { get; private set; } = null!;
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ResultSummary { get; private set; }
    public string? ResultData { get; private set; }

    private StepExecution() : base() { }

    private StepExecution(
        Guid id,
        Guid pipelineStepId,
        int order,
        StepType stepType) : base(id)
    {
        PipelineStepId = pipelineStepId;
        Order = order;
        StepType = stepType;
        Status = StepExecutionStatus.Pending;
    }

    internal static StepExecution Create(
        Guid pipelineStepId,
        int order,
        StepType stepType)
    {
        return new StepExecution(
            Guid.NewGuid(),
            pipelineStepId,
            order,
            stepType);
    }

    public Result Start()
    {
        if (!Status.CanTransitionTo(StepExecutionStatus.Running))
            return Result.Failure(PipelineErrors.InvalidStepExecutionStatusTransition(Status, StepExecutionStatus.Running));

        Status = StepExecutionStatus.Running;
        StartedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Complete(string? resultSummary, string? resultData)
    {
        if (!Status.CanTransitionTo(StepExecutionStatus.Completed))
            return Result.Failure(PipelineErrors.InvalidStepExecutionStatusTransition(Status, StepExecutionStatus.Completed));

        Status = StepExecutionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        ResultSummary = resultSummary?.Length > MaxResultSummaryLength
            ? resultSummary[..MaxResultSummaryLength]
            : resultSummary;
        ResultData = resultData;
        return Result.Success();
    }

    public Result Fail(string? errorMessage)
    {
        if (!Status.CanTransitionTo(StepExecutionStatus.Failed))
            return Result.Failure(PipelineErrors.InvalidStepExecutionStatusTransition(Status, StepExecutionStatus.Failed));

        Status = StepExecutionStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage?.Length > MaxErrorMessageLength
            ? errorMessage[..MaxErrorMessageLength]
            : errorMessage;
        return Result.Success();
    }

    public Result Skip()
    {
        if (!Status.CanTransitionTo(StepExecutionStatus.Skipped))
            return Result.Failure(PipelineErrors.InvalidStepExecutionStatusTransition(Status, StepExecutionStatus.Skipped));

        Status = StepExecutionStatus.Skipped;
        CompletedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Duration of the step execution if completed.
    /// </summary>
    public TimeSpan? Duration => StartedAt.HasValue && CompletedAt.HasValue
        ? CompletedAt.Value - StartedAt.Value
        : null;
}
