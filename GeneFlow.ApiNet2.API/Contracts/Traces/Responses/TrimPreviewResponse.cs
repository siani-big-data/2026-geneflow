namespace GeneFlow.ApiNet2.API.Contracts.Traces.Responses;

/// <summary>
/// Trim preview response.
/// </summary>
public sealed class TrimPreviewResponse
{
    public string TraceId { get; init; } = null!;
    public int Start5Prime { get; init; }
    public int End5Prime { get; init; }
    public int Start3Prime { get; init; }
    public int End3Prime { get; init; }
    public string Algorithm { get; init; } = null!;
    public int OriginalLength { get; init; }
    public int TrimmedLength { get; init; }
    public string PreviewSequence { get; init; } = null!;
    public double AverageQualityInTrimmedRegion { get; init; }
}
