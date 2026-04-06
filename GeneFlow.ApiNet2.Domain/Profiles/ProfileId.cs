using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Profiles;

/// <summary>
/// Strongly-typed identifier for Profile aggregate.
/// </summary>
public sealed class ProfileId : PrefixedId<ProfileId>
{
    /// <summary>
    /// Sequence name for ID generation.
    /// </summary>
    public const string SequenceName = "profiles";

    /// <inheritdoc />
    protected override char Prefix => 'P';

    /// <inheritdoc />
    protected override int NumericLength => 8;

    /// <summary>
    /// Initializes a new ProfileId.
    /// </summary>
    public ProfileId(long value) : base(value) { }

    /// <summary>
    /// Parses a string ID into a ProfileId.
    /// </summary>
    public static ProfileId Parse(string id) => Parse(id, v => new ProfileId(v));

    /// <summary>
    /// Tries to parse a string ID into a ProfileId.
    /// </summary>
    public static bool TryParse(string? id, out ProfileId? result) => TryParse(id, v => new ProfileId(v), out result);

    /// <summary>
    /// Creates a ProfileId from a sequence value.
    /// </summary>
    public static ProfileId FromSequence(long sequenceValue) => new(sequenceValue);
}
