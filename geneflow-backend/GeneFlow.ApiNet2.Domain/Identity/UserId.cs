using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Identity;

/// <summary>
/// Strongly-typed identifier for User aggregate.
/// </summary>
public sealed class UserId : PrefixedId<UserId>
{
    /// <summary>
    /// Sequence name for ID generation.
    /// </summary>
    public const string SequenceName = "users";

    /// <inheritdoc />
    protected override char Prefix => 'U';

    /// <inheritdoc />
    protected override int NumericLength => 8;

    /// <summary>
    /// Initializes a new UserId.
    /// </summary>
    public UserId(long value) : base(value) { }

    /// <summary>
    /// Parses a string ID into a UserId.
    /// </summary>
    public static UserId Parse(string id) => Parse(id, v => new UserId(v));

    /// <summary>
    /// Tries to parse a string ID into a UserId.
    /// </summary>
    public static bool TryParse(string? id, out UserId? result) => TryParse(id, v => new UserId(v), out result);

    /// <summary>
    /// Creates a UserId from a sequence value.
    /// </summary>
    public static UserId FromSequence(long sequenceValue) => new(sequenceValue);
}
