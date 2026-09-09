namespace GeneFlow.ApiNet2.Application.Traces.DTOs;

/// <summary>
/// Quality metrics data transfer object.
/// </summary>
public sealed record QualityMetricsDto
{
    /// <summary>
    /// Phred score threshold for "good" sequencing quality
    /// (≥ Q20 means &lt; 1% probability of error per base).
    /// </summary>
    private const decimal Q20Threshold = 20m;

    /// <summary>
    /// Phred score threshold for "high" sequencing quality
    /// (≥ Q30 means &lt; 0.1% probability of error per base).
    /// </summary>
    private const decimal Q30Threshold = 30m;

    public required decimal AverageQualityScore { get; init; }
    public required int TotalBases { get; init; }
    public required decimal QualityAboveQ20Percentage { get; init; }
    public required decimal QualityAboveQ30Percentage { get; init; }
    public required int TrimmedLength { get; init; }
    public required decimal GcContentPercentage { get; init; }

    /// <summary>True when the average quality score reaches the Q20 threshold.</summary>
    public bool IsGoodQuality => AverageQualityScore >= Q20Threshold;

    /// <summary>True when the average quality score reaches the Q30 threshold.</summary>
    public bool IsHighQuality => AverageQualityScore >= Q30Threshold;
}
