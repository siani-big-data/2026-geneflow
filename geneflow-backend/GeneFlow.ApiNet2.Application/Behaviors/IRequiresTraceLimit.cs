namespace GeneFlow.ApiNet2.Application.Behaviors;

/// <summary>
/// Marker interface for commands that require validation of the trace upload limit.
/// When a request implements this interface, the SubscriptionLimitBehavior will
/// validate that the study owner hasn't exceeded their plan's MaxTracesPerMonth limit.
/// </summary>
public interface IRequiresTraceLimit : IRequireAuthentication
{
    /// <summary>
    /// The ID of the study where traces are being uploaded.
    /// Used to look up the study owner's subscription limits.
    /// </summary>
    string StudyId { get; }

    /// <summary>
    /// The number of traces being uploaded in this operation.
    /// Defaults to 1 for single trace uploads.
    /// </summary>
    int TraceCount => 1;
}
