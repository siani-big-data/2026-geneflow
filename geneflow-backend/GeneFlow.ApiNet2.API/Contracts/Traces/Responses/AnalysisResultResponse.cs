namespace GeneFlow.ApiNet2.API.Contracts.Traces.Responses;

/// <summary>
/// Response containing a list of available analysis result types.
/// </summary>
public sealed class AnalysisResultsListResponse
{
    public string TraceId { get; init; } = null!;
    public IReadOnlyList<string> AvailableTypes { get; init; } = Array.Empty<string>();
}
