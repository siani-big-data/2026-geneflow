using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Discussions;

/// <summary>
/// Strongly-typed identifier for a Discussion aggregate.
/// Format: D########  (eight zero-padded digits).
/// </summary>
public sealed class DiscussionId : PrefixedId<DiscussionId>
{
    /// <summary>
    /// Sequence name for ID generation.
    /// </summary>
    public const string SequenceName = "discussions";

    /// <inheritdoc />
    protected override char Prefix => 'D';

    /// <inheritdoc />
    protected override int NumericLength => 8;

    public DiscussionId(long value) : base(value) { }

    public static DiscussionId Parse(string id) => Parse(id, v => new DiscussionId(v));

    public static bool TryParse(string? id, out DiscussionId? result)
        => TryParse(id, v => new DiscussionId(v), out result);

    public static DiscussionId FromSequence(long sequenceValue) => new(sequenceValue);
}
