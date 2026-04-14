namespace GeneFlow.ApiNet2.API.Contracts.Traces.Requests;

/// <summary>
/// Request to auto-trim a trace.
/// </summary>
public sealed record AutoTrimRequest(
    int QualityThreshold = 20,
    int WindowSize = 10,
    int MinimumLength = 50);
