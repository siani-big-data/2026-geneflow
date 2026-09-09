using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Traces.Entities;

/// <summary>
/// Represents an edit made to a trace's sequence.
/// Edits are reversible and applied on top of the original sequence.
/// </summary>
public sealed class SequenceEdit : Entity<Guid>
{
    public const int MaxReasonLength = 500;
    public const string ValidBases = "ACGTN";

    /// <summary>
    /// Type of edit (Change, Insert, Delete).
    /// </summary>
    public EditType EditType { get; private set; } = null!;

    /// <summary>
    /// Position in the sequence (0-based).
    /// </summary>
    public int Position { get; private set; }

    /// <summary>
    /// Original base at this position (for Change/Delete).
    /// </summary>
    public char? OriginalBase { get; private set; }

    /// <summary>
    /// New base to insert or change to (for Change/Insert).
    /// </summary>
    public char? NewBase { get; private set; }

    /// <summary>
    /// Optional reason for the edit.
    /// </summary>
    public string? Reason { get; private set; }

    /// <summary>
    /// User who made the edit.
    /// </summary>
    public UserId EditedBy { get; private set; } = null!;

    /// <summary>
    /// When the edit was made.
    /// </summary>
    public DateTime EditedAt { get; private set; }

    /// <summary>
    /// Whether the edit is currently active (not undone).
    /// </summary>
    public bool IsActive { get; private set; }

    private SequenceEdit() : base() { }

    private SequenceEdit(
        Guid id,
        EditType editType,
        int position,
        char? originalBase,
        char? newBase,
        string? reason,
        UserId editedBy) : base(id)
    {
        EditType = editType;
        Position = position;
        OriginalBase = originalBase;
        NewBase = newBase;
        Reason = reason;
        EditedBy = editedBy;
        EditedAt = DateTime.UtcNow;
        IsActive = true;
    }

    internal static Result<SequenceEdit> Create(
        EditType editType,
        int position,
        char? originalBase,
        char? newBase,
        string? reason,
        UserId editedBy,
        int sequenceLength)
    {
        if (position < 0 || position >= sequenceLength)
            return Result.Failure<SequenceEdit>(TraceErrors.InvalidPosition);

        if (editType.RequiresOriginalBase && originalBase is null)
            return Result.Failure<SequenceEdit>(TraceErrors.OriginalBaseRequired);

        if (editType.RequiresNewBase && newBase is null)
            return Result.Failure<SequenceEdit>(TraceErrors.NewBaseRequired);

        if (originalBase.HasValue && !IsValidBase(originalBase.Value))
            return Result.Failure<SequenceEdit>(TraceErrors.InvalidBase);

        if (newBase.HasValue && !IsValidBase(newBase.Value))
            return Result.Failure<SequenceEdit>(TraceErrors.InvalidBase);

        if (reason?.Length > MaxReasonLength)
            return Result.Failure<SequenceEdit>(TraceErrors.ReasonTooLong(MaxReasonLength));

        return new SequenceEdit(
            Guid.NewGuid(),
            editType,
            position,
            originalBase.HasValue ? char.ToUpperInvariant(originalBase.Value) : null,
            newBase.HasValue ? char.ToUpperInvariant(newBase.Value) : null,
            reason?.Trim(),
            editedBy);
    }

    /// <summary>
    /// Marks this edit as undone (inactive).
    /// </summary>
    internal Result Undo()
    {
        if (!IsActive)
            return Result.Failure(TraceErrors.EditAlreadyUndone);

        IsActive = false;
        return Result.Success();
    }

    private static bool IsValidBase(char @base)
    {
        return ValidBases.Contains(char.ToUpperInvariant(@base));
    }
}
