using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Studies;

/// <summary>
/// Strongly-typed identifier for StudyPaper entity.
/// </summary>
public sealed class StudyPaperId : PrefixedId<StudyPaperId>
{
    /// <summary>
    /// Sequence name for ID generation.
    /// </summary>
    public const string SequenceName = "study_papers";

    /// <inheritdoc />
    protected override char Prefix => 'R';

    /// <inheritdoc />
    protected override int NumericLength => 8;

    /// <summary>
    /// Initializes a new StudyPaperId.
    /// </summary>
    public StudyPaperId(long value) : base(value) { }

    /// <summary>
    /// Parses a string ID into a StudyPaperId.
    /// </summary>
    public static StudyPaperId Parse(string id) => Parse(id, v => new StudyPaperId(v));

    /// <summary>
    /// Tries to parse a string ID into a StudyPaperId.
    /// </summary>
    public static bool TryParse(string? id, out StudyPaperId? result) => TryParse(id, v => new StudyPaperId(v), out result);

    /// <summary>
    /// Creates a StudyPaperId from a sequence value.
    /// </summary>
    public static StudyPaperId FromSequence(long sequenceValue) => new(sequenceValue);
}
