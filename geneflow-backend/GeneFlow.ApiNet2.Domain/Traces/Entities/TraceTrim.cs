using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Traces.Entities;

/// <summary>
/// Represents a trim operation applied to a trace sequence.
/// Multiple trims can be applied to a single trace.
/// </summary>
public sealed class TraceTrim : Entity<Guid>
{
    public const int MaxAlgorithmLength = 50;
    public const int MaxReasonLength = 500;

    /// <summary>
    /// Type of trim (Manual, AutoMott, AutoWindow).
    /// </summary>
    public TrimType TrimType { get; private set; } = null!;

    /// <summary>
    /// Start position of the trim region (0-based, inclusive).
    /// </summary>
    public int StartPosition { get; private set; }

    /// <summary>
    /// End position of the trim region (0-based, exclusive).
    /// </summary>
    public int EndPosition { get; private set; }

    /// <summary>
    /// Which end of the sequence this trim applies to (5' or 3').
    /// </summary>
    public TrimEnd TrimEnd { get; private set; } = null!;

    /// <summary>
    /// Algorithm or method used for trimming (e.g., "Manual", "Mott(Q20,W10)").
    /// </summary>
    public string Algorithm { get; private set; } = null!;

    /// <summary>
    /// Optional reason for the trim.
    /// </summary>
    public string? Reason { get; private set; }

    /// <summary>
    /// User who applied the trim.
    /// </summary>
    public UserId AppliedBy { get; private set; } = null!;

    /// <summary>
    /// When the trim was applied.
    /// </summary>
    public DateTime AppliedAt { get; private set; }

    /// <summary>
    /// Whether the trim is currently active (not undone).
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Length of the trimmed region.
    /// </summary>
    public int Length => EndPosition - StartPosition;

    private TraceTrim() : base() { }

    private TraceTrim(
        Guid id,
        TrimType trimType,
        int startPosition,
        int endPosition,
        TrimEnd trimEnd,
        string algorithm,
        string? reason,
        UserId appliedBy) : base(id)
    {
        TrimType = trimType;
        StartPosition = startPosition;
        EndPosition = endPosition;
        TrimEnd = trimEnd;
        Algorithm = algorithm;
        Reason = reason;
        AppliedBy = appliedBy;
        AppliedAt = DateTime.UtcNow;
        IsActive = true;
    }

    /// <summary>
    /// Creates a new TraceTrim instance.
    /// </summary>
    internal static Result<TraceTrim> Create(
        TrimType trimType,
        int startPosition,
        int endPosition,
        TrimEnd trimEnd,
        string algorithm,
        UserId appliedBy,
        string? reason,
        int sequenceLength)
    {
        if (startPosition < 0)
            return Result.Failure<TraceTrim>(TraceErrors.InvalidTrimPosition("start"));

        if (endPosition <= startPosition)
            return Result.Failure<TraceTrim>(TraceErrors.InvalidTrimPositions);

        if (endPosition > sequenceLength)
            return Result.Failure<TraceTrim>(TraceErrors.TrimExceedsSequenceLength);

        if (string.IsNullOrWhiteSpace(algorithm))
            return Result.Failure<TraceTrim>(TraceErrors.TrimAlgorithmRequired);

        if (algorithm.Length > MaxAlgorithmLength)
            return Result.Failure<TraceTrim>(TraceErrors.TrimAlgorithmTooLong(MaxAlgorithmLength));

        if (reason?.Length > MaxReasonLength)
            return Result.Failure<TraceTrim>(TraceErrors.TrimReasonTooLong(MaxReasonLength));

        return new TraceTrim(
            Guid.NewGuid(),
            trimType,
            startPosition,
            endPosition,
            trimEnd,
            algorithm.Trim(),
            reason?.Trim(),
            appliedBy);
    }

    /// <summary>
    /// Deactivates (soft-deletes) this trim.
    /// </summary>
    internal void Deactivate() => IsActive = false;

    /// <summary>
    /// Reactivates a previously deactivated trim.
    /// </summary>
    internal void Reactivate() => IsActive = true;
}
