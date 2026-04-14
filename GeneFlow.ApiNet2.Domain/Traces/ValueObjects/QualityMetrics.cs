using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

/// <summary>
/// Represents quality metrics calculated during trace processing.
/// </summary>
public sealed class QualityMetrics : ValueObject
{
    /// <summary>
    /// Average Phred quality score across all bases.
    /// </summary>
    public decimal AverageQualityScore { get; }

    /// <summary>
    /// Total number of bases in the sequence.
    /// </summary>
    public int TotalBases { get; }

    /// <summary>
    /// Percentage of bases with quality score >= 20.
    /// </summary>
    public decimal QualityAboveQ20Percentage { get; }

    /// <summary>
    /// Percentage of bases with quality score >= 30.
    /// </summary>
    public decimal QualityAboveQ30Percentage { get; }

    /// <summary>
    /// Length of the sequence after trimming.
    /// </summary>
    public int TrimmedLength { get; }

    /// <summary>
    /// GC content percentage (0-100).
    /// </summary>
    public decimal GcContentPercentage { get; }

    private QualityMetrics(
        decimal averageQualityScore,
        int totalBases,
        decimal qualityAboveQ20Percentage,
        decimal qualityAboveQ30Percentage,
        int trimmedLength,
        decimal gcContentPercentage)
    {
        AverageQualityScore = averageQualityScore;
        TotalBases = totalBases;
        QualityAboveQ20Percentage = qualityAboveQ20Percentage;
        QualityAboveQ30Percentage = qualityAboveQ30Percentage;
        TrimmedLength = trimmedLength;
        GcContentPercentage = gcContentPercentage;
    }

    public static Result<QualityMetrics> Create(
        decimal averageQualityScore,
        int totalBases,
        decimal qualityAboveQ20Percentage,
        decimal qualityAboveQ30Percentage,
        int trimmedLength,
        decimal gcContentPercentage)
    {
        if (averageQualityScore < 0)
            return Result.Failure<QualityMetrics>(TraceErrors.InvalidQualityScore);

        if (totalBases < 0)
            return Result.Failure<QualityMetrics>(TraceErrors.InvalidTotalBases);

        if (qualityAboveQ20Percentage < 0 || qualityAboveQ20Percentage > 100)
            return Result.Failure<QualityMetrics>(TraceErrors.InvalidPercentage("Q20"));

        if (qualityAboveQ30Percentage < 0 || qualityAboveQ30Percentage > 100)
            return Result.Failure<QualityMetrics>(TraceErrors.InvalidPercentage("Q30"));

        if (trimmedLength < 0 || trimmedLength > totalBases)
            return Result.Failure<QualityMetrics>(TraceErrors.InvalidTrimmedLength);

        if (gcContentPercentage < 0 || gcContentPercentage > 100)
            return Result.Failure<QualityMetrics>(TraceErrors.InvalidPercentage("GC content"));

        return new QualityMetrics(
            averageQualityScore,
            totalBases,
            qualityAboveQ20Percentage,
            qualityAboveQ30Percentage,
            trimmedLength,
            gcContentPercentage);
    }

    /// <summary>
    /// Creates an empty/default quality metrics instance.
    /// Used before processing is complete.
    /// </summary>
    public static QualityMetrics Empty => new(0, 0, 0, 0, 0, 0);

    /// <summary>
    /// Determines if the trace has good quality (average Q >= 20).
    /// </summary>
    public bool IsGoodQuality => AverageQualityScore >= 20;

    /// <summary>
    /// Determines if the trace has high quality (average Q >= 30).
    /// </summary>
    public bool IsHighQuality => AverageQualityScore >= 30;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AverageQualityScore;
        yield return TotalBases;
        yield return QualityAboveQ20Percentage;
        yield return QualityAboveQ30Percentage;
        yield return TrimmedLength;
        yield return GcContentPercentage;
    }
}
