namespace GeneFlow.ApiNet2.Application.Traces.DTOs;

/// <summary>
/// Quality metrics data transfer object.
/// </summary>
public sealed record QualityMetricsDto
{
    public required decimal AverageQualityScore { get; init; }
    public required int TotalBases { get; init; }
    public required decimal QualityAboveQ20Percentage { get; init; }
    public required decimal QualityAboveQ30Percentage { get; init; }
    public required int TrimmedLength { get; init; }
    public required decimal GcContentPercentage { get; init; }

    // Computed flags
    public bool IsGoodQuality => AverageQualityScore >= 20;
    public bool IsHighQuality => AverageQualityScore >= 30;
}
