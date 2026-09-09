namespace GeneFlow.ApiNet2.API.Contracts.Traces.Responses;

/// <summary>
/// Trimmed sequence response with multiple trims applied.
/// </summary>
public sealed class TrimmedSequenceResponse
{
    public string TraceId { get; init; } = null!;
    public string OriginalSequence { get; init; } = null!;
    public string TrimmedSequence { get; init; } = null!;
    public IReadOnlyList<TraceTrimResponse> AppliedTrims { get; init; } = [];
    public int OriginalLength { get; init; }
    public int TrimmedLength { get; init; }
    public int TotalBasesTrimmed { get; init; }
}
