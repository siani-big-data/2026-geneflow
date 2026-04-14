namespace GeneFlow.ApiNet2.API.Contracts.Traces.Responses;

/// <summary>
/// Trimmed sequence response.
/// </summary>
public sealed class TrimmedSequenceResponse
{
    public string TraceId { get; init; } = null!;
    public string OriginalSequence { get; init; } = null!;
    public string TrimmedSequence { get; init; } = null!;
    public TrimRegionResponse TrimRegion { get; init; } = null!;
    public int OriginalLength { get; init; }
    public int TrimmedLength { get; init; }
}
