using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;

/// <summary>
/// Individual step execution status within a pipeline execution.
/// </summary>
public sealed class StepExecutionStatus : Enumeration<StepExecutionStatus>
{
    public static readonly StepExecutionStatus Pending = new(1, nameof(Pending), "Pending", false, false);
    public static readonly StepExecutionStatus Running = new(2, nameof(Running), "Running", true, false);
    public static readonly StepExecutionStatus Completed = new(3, nameof(Completed), "Completed", false, true);
    public static readonly StepExecutionStatus Failed = new(4, nameof(Failed), "Failed", false, true);
    public static readonly StepExecutionStatus Skipped = new(5, nameof(Skipped), "Skipped", false, true);

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Whether the step is currently in progress.
    /// </summary>
    public bool IsInProgress { get; }

    /// <summary>
    /// Whether the step has reached a terminal state.
    /// </summary>
    public bool IsTerminal { get; }

    private StepExecutionStatus(int id, string name, string displayName, bool isInProgress, bool isTerminal)
        : base(id, name)
    {
        DisplayName = displayName;
        IsInProgress = isInProgress;
        IsTerminal = isTerminal;
    }

    /// <summary>
    /// Checks if transition to the given status is valid.
    /// </summary>
    public bool CanTransitionTo(StepExecutionStatus newStatus)
    {
        return (this, newStatus) switch
        {
            // From Pending
            _ when this == Pending && newStatus == Running => true,
            _ when this == Pending && newStatus == Skipped => true,

            // From Running
            _ when this == Running && newStatus == Completed => true,
            _ when this == Running && newStatus == Failed => true,

            // Terminal states cannot transition
            _ => false
        };
    }

    /// <summary>
    /// Whether the step execution was successful.
    /// </summary>
    public bool IsSuccess => this == Completed;
}
