using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

/// <summary>
/// Represents the trimmed region of a trace sequence.
/// Defines the 5' and 3' trim positions.
/// </summary>
public sealed class TrimRegion : ValueObject
{
    public const int MaxAlgorithmLength = 50;

    /// <summary>
    /// Start position of the 5' trim (inclusive, 0-based).
    /// </summary>
    public int Start5Prime { get; }

    /// <summary>
    /// End position of the 5' trim (exclusive, 0-based).
    /// This marks where the good quality region begins.
    /// </summary>
    public int End5Prime { get; }

    /// <summary>
    /// Start position of the 3' trim (inclusive, 0-based).
    /// This marks where the good quality region ends.
    /// </summary>
    public int Start3Prime { get; }

    /// <summary>
    /// End position of the 3' trim (exclusive, 0-based).
    /// </summary>
    public int End3Prime { get; }

    /// <summary>
    /// Algorithm used for trimming (e.g., "Mott", "Manual").
    /// </summary>
    public string Algorithm { get; }

    /// <summary>
    /// User who performed the trim.
    /// </summary>
    public string TrimmedBy { get; }

    /// <summary>
    /// When the trim was performed.
    /// </summary>
    public DateTime TrimmedAt { get; }

    private TrimRegion(
        int start5Prime,
        int end5Prime,
        int start3Prime,
        int end3Prime,
        string algorithm,
        string trimmedBy,
        DateTime trimmedAt)
    {
        Start5Prime = start5Prime;
        End5Prime = end5Prime;
        Start3Prime = start3Prime;
        End3Prime = end3Prime;
        Algorithm = algorithm;
        TrimmedBy = trimmedBy;
        TrimmedAt = trimmedAt;
    }

    public static Result<TrimRegion> Create(
        int start5Prime,
        int end5Prime,
        int start3Prime,
        int end3Prime,
        string algorithm,
        string trimmedBy,
        int sequenceLength)
    {
        if (start5Prime < 0)
            return Result.Failure<TrimRegion>(TraceErrors.InvalidTrimPosition("5' start"));

        if (end5Prime < start5Prime)
            return Result.Failure<TrimRegion>(TraceErrors.InvalidTrimPosition("5' end"));

        if (start3Prime < end5Prime)
            return Result.Failure<TrimRegion>(TraceErrors.InvalidTrimPosition("3' start"));

        if (end3Prime < start3Prime)
            return Result.Failure<TrimRegion>(TraceErrors.InvalidTrimPosition("3' end"));

        if (end3Prime > sequenceLength)
            return Result.Failure<TrimRegion>(TraceErrors.TrimExceedsSequenceLength);

        if (string.IsNullOrWhiteSpace(algorithm))
            return Result.Failure<TrimRegion>(TraceErrors.TrimAlgorithmRequired);

        if (algorithm.Length > MaxAlgorithmLength)
            return Result.Failure<TrimRegion>(TraceErrors.TrimAlgorithmTooLong(MaxAlgorithmLength));

        if (string.IsNullOrWhiteSpace(trimmedBy))
            return Result.Failure<TrimRegion>(TraceErrors.TrimmedByRequired);

        return new TrimRegion(
            start5Prime,
            end5Prime,
            start3Prime,
            end3Prime,
            algorithm.Trim(),
            trimmedBy.Trim(),
            DateTime.UtcNow);
    }

    /// <summary>
    /// Creates a trim region for manual trimming (simple start/end).
    /// </summary>
    public static Result<TrimRegion> CreateManual(
        int trimStart,
        int trimEnd,
        string trimmedBy,
        int sequenceLength)
    {
        return Create(0, trimStart, trimEnd, sequenceLength, "Manual", trimmedBy, sequenceLength);
    }

    /// <summary>
    /// Gets the length of the trimmed sequence (the good quality region).
    /// </summary>
    public int TrimmedLength => Start3Prime - End5Prime;

    /// <summary>
    /// Gets the total bases trimmed from both ends.
    /// </summary>
    public int TotalBasesTrimmed => End5Prime + (End3Prime - Start3Prime);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start5Prime;
        yield return End5Prime;
        yield return Start3Prime;
        yield return End3Prime;
        yield return Algorithm;
        yield return TrimmedBy;
        yield return TrimmedAt;
    }
}
