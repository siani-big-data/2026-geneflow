using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Studies;

/// <summary>
/// Strongly-typed identifier for StudyInvitation aggregate.
/// Format: I00000001
/// </summary>
public sealed class StudyInvitationId : PrefixedId<StudyInvitationId>
{
    /// <summary>
    /// Sequence name for ID generation.
    /// </summary>
    public const string SequenceName = "study_invitations";

    /// <inheritdoc />
    protected override char Prefix => 'I';

    /// <inheritdoc />
    protected override int NumericLength => 8;

    /// <summary>
    /// Initializes a new StudyInvitationId.
    /// </summary>
    public StudyInvitationId(long value) : base(value) { }

    /// <summary>
    /// Parses a string ID into a StudyInvitationId.
    /// </summary>
    public static StudyInvitationId Parse(string id) => Parse(id, v => new StudyInvitationId(v));

    /// <summary>
    /// Tries to parse a string ID into a StudyInvitationId.
    /// </summary>
    public static bool TryParse(string? id, out StudyInvitationId? result) => TryParse(id, v => new StudyInvitationId(v), out result);

    /// <summary>
    /// Creates a StudyInvitationId from a sequence value.
    /// </summary>
    public static StudyInvitationId FromSequence(long sequenceValue) => new(sequenceValue);
}
