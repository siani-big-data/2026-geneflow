using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;

/// <summary>
/// Pipeline execution status.
/// </summary>
public sealed class ExecutionStatus : Enumeration<ExecutionStatus>
{
    public static readonly ExecutionStatus Pending = new(1, nameof(Pending), "Pending", false, false);
    public static readonly ExecutionStatus Running = new(2, nameof(Running), "Running", true, false);
    public static readonly ExecutionStatus Completed = new(3, nameof(Completed), "Completed", false, true);
    public static readonly ExecutionStatus Failed = new(4, nameof(Failed), "Failed", false, true);
    public static readonly ExecutionStatus Cancelled = new(5, nameof(Cancelled), "Cancelled", false, true);

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Whether the execution is currently in progress.
    /// </summary>
    public bool IsInProgress { get; }

    /// <summary>
    /// Whether the execution has reached a terminal state.
    /// </summary>
    public bool IsTerminal { get; }

    private ExecutionStatus(int id, string name, string displayName, bool isInProgress, bool isTerminal)
        : base(id, name)
    {
        DisplayName = displayName;
        IsInProgress = isInProgress;
        IsTerminal = isTerminal;
    }

    /// <summary>
    /// Checks if transition to the given status is valid.
    /// </summary>
    public bool CanTransitionTo(ExecutionStatus newStatus)
    {
        return (this, newStatus) switch
        {
            // From Pending
            _ when this == Pending && newStatus == Running => true,
            _ when this == Pending && newStatus == Cancelled => true,

            // From Running
            _ when this == Running && newStatus == Completed => true,
            _ when this == Running && newStatus == Failed => true,
            _ when this == Running && newStatus == Cancelled => true,

            // Terminal states cannot transition
            _ => false
        };
    }

    /// <summary>
    /// Whether the execution can be cancelled.
    /// </summary>
    public bool CanCancel => this == Pending || this == Running;
}
