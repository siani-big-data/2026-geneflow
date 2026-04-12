using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Studies;

/// <summary>
/// Strongly-typed identifier for Study aggregate.
/// </summary>
public sealed class StudyId : PrefixedId<StudyId>
{
    /// <summary>
    /// Sequence name for ID generation.
    /// </summary>
    public const string SequenceName = "studies";

    /// <inheritdoc />
    protected override char Prefix => 'S';

    /// <inheritdoc />
    protected override int NumericLength => 8;

    /// <summary>
    /// Initializes a new StudyId.
    /// </summary>
    public StudyId(long value) : base(value) { }

    /// <summary>
    /// Parses a string ID into a StudyId.
    /// </summary>
    public static StudyId Parse(string id) => Parse(id, v => new StudyId(v));

    /// <summary>
    /// Tries to parse a string ID into a StudyId.
    /// </summary>
    public static bool TryParse(string? id, out StudyId? result) => TryParse(id, v => new StudyId(v), out result);

    /// <summary>
    /// Creates a StudyId from a sequence value.
    /// </summary>
    public static StudyId FromSequence(long sequenceValue) => new(sequenceValue);
}
