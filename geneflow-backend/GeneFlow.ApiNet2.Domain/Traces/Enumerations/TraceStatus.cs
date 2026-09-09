using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Traces.Enumerations;

/// <summary>
/// Trace processing lifecycle status.
/// </summary>
public sealed class TraceStatus : Enumeration<TraceStatus>
{
    public static readonly TraceStatus Uploaded = new(1, nameof(Uploaded), "Uploaded", false);
    public static readonly TraceStatus Validating = new(2, nameof(Validating), "Validating", true);
    public static readonly TraceStatus Processing = new(3, nameof(Processing), "Processing", true);
    public static readonly TraceStatus Processed = new(4, nameof(Processed), "Processed", false);
    public static readonly TraceStatus Failed = new(5, nameof(Failed), "Failed", false);
    public static readonly TraceStatus Archived = new(6, nameof(Archived), "Archived", false);

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Whether the trace is currently being processed.
    /// </summary>
    public bool IsProcessing { get; }

    private TraceStatus(int id, string name, string displayName, bool isProcessing)
        : base(id, name)
    {
        DisplayName = displayName;
        IsProcessing = isProcessing;
    }

    /// <summary>
    /// Checks if transition to the given status is valid.
    /// </summary>
    public bool CanTransitionTo(TraceStatus newStatus)
    {
        return (this, newStatus) switch
        {
            // From Uploaded
            _ when this == Uploaded && newStatus == Validating => true,
            _ when this == Uploaded && newStatus == Archived => true,

            // From Validating
            _ when this == Validating && newStatus == Processing => true,
            _ when this == Validating && newStatus == Failed => true,

            // From Processing
            _ when this == Processing && newStatus == Processed => true,
            _ when this == Processing && newStatus == Failed => true,

            // From Processed
            _ when this == Processed && newStatus == Archived => true,

            // From Failed - can retry
            _ when this == Failed && newStatus == Uploaded => true,
            _ when this == Failed && newStatus == Archived => true,

            // From Archived - can restore
            _ when this == Archived && newStatus == Uploaded => true,

            _ => false
        };
    }

    /// <summary>
    /// Whether the trace can be edited (trimmed, annotated, etc.)
    /// </summary>
    public bool CanEdit => this == Processed;

    /// <summary>
    /// Whether the trace can be retried for processing.
    /// </summary>
    public bool CanRetry => this == Failed;

    /// <summary>
    /// Whether the trace can be deleted.
    /// </summary>
    public bool CanDelete => this != Processing && this != Validating;
}
