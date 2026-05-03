namespace GeneFlow.ApiNet2.API.Contracts.Traces.Requests;

/// <summary>
/// Request to add a trim to a trace.
/// </summary>
public sealed record AddTrimRequest(
    int StartPosition,
    int EndPosition,
    string TrimEnd,
    string? Reason = null);
