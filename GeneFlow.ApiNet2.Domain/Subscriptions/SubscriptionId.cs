using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Subscriptions;

/// <summary>
/// Strongly-typed identifier for Subscription aggregate.
/// Format: S00000001 (S + 8 digits)
/// </summary>
public sealed class SubscriptionId : PrefixedId<SubscriptionId>
{
    /// <summary>
    /// Sequence name for ID generation.
    /// </summary>
    public const string SequenceName = "subscriptions";

    /// <inheritdoc />
    protected override char Prefix => 'S';

    /// <inheritdoc />
    protected override int NumericLength => 8;

    /// <summary>
    /// Initializes a new SubscriptionId.
    /// </summary>
    public SubscriptionId(long value) : base(value) { }

    /// <summary>
    /// Parses a string ID into a SubscriptionId.
    /// </summary>
    public static SubscriptionId Parse(string id) => Parse(id, v => new SubscriptionId(v));

    /// <summary>
    /// Tries to parse a string ID into a SubscriptionId.
    /// </summary>
    public static bool TryParse(string? id, out SubscriptionId? result) => TryParse(id, v => new SubscriptionId(v), out result);

    /// <summary>
    /// Creates a SubscriptionId from a sequence value.
    /// </summary>
    public static SubscriptionId FromSequence(long sequenceValue) => new(sequenceValue);
}
