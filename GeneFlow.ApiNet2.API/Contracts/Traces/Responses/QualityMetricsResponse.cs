namespace GeneFlow.ApiNet2.API.Contracts.Traces.Responses;

/// <summary>
/// Quality metrics response.
/// </summary>
public sealed class QualityMetricsResponse
{
    public decimal AverageQualityScore { get; init; }
    public int TotalBases { get; init; }
    public decimal QualityAboveQ20Percentage { get; init; }
    public decimal QualityAboveQ30Percentage { get; init; }
    public int TrimmedLength { get; init; }
    public decimal GcContentPercentage { get; init; }
}
