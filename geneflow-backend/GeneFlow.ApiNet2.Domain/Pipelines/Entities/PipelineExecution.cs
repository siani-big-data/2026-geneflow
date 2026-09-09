using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Pipelines.Entities;

/// <summary>
/// Represents an execution of a pipeline against a specific trace.
/// </summary>
public sealed class PipelineExecution : Entity<PipelineExecutionId>
{
    public const int MaxErrorMessageLength = 2000;

    private readonly List<StepExecution> _stepExecutions = new();

    public PipelineId PipelineId { get; private set; } = null!;
    public TraceId TraceId { get; private set; } = null!;
    public UserId StartedBy { get; private set; } = null!;
    public ExecutionStatus Status { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int TotalSteps { get; private set; }
    public int CompletedSteps { get; private set; }

    public IReadOnlyList<StepExecution> StepExecutions => _stepExecutions.AsReadOnly();

    private PipelineExecution() : base() { }

    private PipelineExecution(
        PipelineExecutionId id,
        PipelineId pipelineId,
        TraceId traceId,
        UserId startedBy,
        IEnumerable<PipelineStep> steps) : base(id)
    {
        PipelineId = pipelineId;
        TraceId = traceId;
        StartedBy = startedBy;
        Status = ExecutionStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        CompletedSteps = 0;

        var enabledSteps = steps.Where(s => s.IsEnabled).OrderBy(s => s.Order).ToList();
        TotalSteps = enabledSteps.Count;

        foreach (var step in enabledSteps)
        {
            _stepExecutions.Add(StepExecution.Create(step.Id, step.Order, step.StepType));
        }
    }

    internal static PipelineExecution Create(
        PipelineExecutionId id,
        PipelineId pipelineId,
        TraceId traceId,
        UserId startedBy,
        IEnumerable<PipelineStep> steps)
    {
        return new PipelineExecution(id, pipelineId, traceId, startedBy, steps);
    }

    public Result Start()
    {
        if (!Status.CanTransitionTo(ExecutionStatus.Running))
            return Result.Failure(PipelineErrors.InvalidExecutionStatusTransition(Status, ExecutionStatus.Running));

        Status = ExecutionStatus.Running;
        StartedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Complete()
    {
        if (!Status.CanTransitionTo(ExecutionStatus.Completed))
            return Result.Failure(PipelineErrors.InvalidExecutionStatusTransition(Status, ExecutionStatus.Completed));

        Status = ExecutionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        CompletedSteps = _stepExecutions.Count(s => s.Status == StepExecutionStatus.Completed);
        return Result.Success();
    }

    public Result Fail(string? errorMessage)
    {
        if (!Status.CanTransitionTo(ExecutionStatus.Failed))
            return Result.Failure(PipelineErrors.InvalidExecutionStatusTransition(Status, ExecutionStatus.Failed));

        Status = ExecutionStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage?.Length > MaxErrorMessageLength
            ? errorMessage[..MaxErrorMessageLength]
            : errorMessage;
        CompletedSteps = _stepExecutions.Count(s => s.Status == StepExecutionStatus.Completed);
        return Result.Success();
    }

    public Result Cancel()
    {
        if (!Status.CanCancel)
            return Result.Failure(PipelineErrors.ExecutionNotCancellable);

        Status = ExecutionStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
        CompletedSteps = _stepExecutions.Count(s => s.Status == StepExecutionStatus.Completed);

        foreach (var step in _stepExecutions.Where(s => s.Status == StepExecutionStatus.Pending))
        {
            step.Skip();
        }

        return Result.Success();
    }

    /// <summary>
    /// Gets a step execution by ID.
    /// </summary>
    public StepExecution? GetStepExecution(Guid stepExecutionId)
    {
        return _stepExecutions.FirstOrDefault(s => s.Id == stepExecutionId);
    }

    /// <summary>
    /// Gets the current step being executed.
    /// </summary>
    public StepExecution? CurrentStep => _stepExecutions
        .Where(s => s.Status == StepExecutionStatus.Running)
        .FirstOrDefault();

    /// <summary>
    /// Gets the next step to be executed.
    /// </summary>
    public StepExecution? NextStep => _stepExecutions
        .Where(s => s.Status == StepExecutionStatus.Pending)
        .OrderBy(s => s.Order)
        .FirstOrDefault();

    /// <summary>
    /// Duration of the execution if completed.
    /// </summary>
    public TimeSpan? Duration => StartedAt.HasValue && CompletedAt.HasValue
        ? CompletedAt.Value - StartedAt.Value
        : null;

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public int ProgressPercentage => TotalSteps > 0
        ? (int)Math.Round((double)CompletedSteps / TotalSteps * 100)
        : 0;

    /// <summary>
    /// Increments the completed steps counter.
    /// </summary>
    public void IncrementCompletedSteps()
    {
        CompletedSteps++;
    }
}
