namespace GeneFlow.ApiNet2.API.Contracts.Traces.Requests;

/// <summary>
/// Request to manually trim a trace.
/// </summary>
public sealed record ManualTrimRequest(
    int Start5Prime,
    int End5Prime,
    int Start3Prime,
    int End3Prime);
