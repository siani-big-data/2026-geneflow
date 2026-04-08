using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;

namespace GeneFlow.ApiNet2.Domain.Subscriptions;

/// <summary>
/// Strongly-typed identifier for Subscription aggregate.
/// </summary>
public sealed class SubscriptionId : SingleValueObject<Guid>
{
    /// <summary>
    /// Initializes a new instance of SubscriptionId.
    /// </summary>
    public SubscriptionId(Guid value) : base(value)
    {
    }

    /// <summary>
    /// Creates a new unique SubscriptionId.
    /// </summary>
    public static SubscriptionId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a SubscriptionId from an existing Guid.
    /// </summary>
    public static SubscriptionId From(Guid value) => new(value);

    /// <summary>
    /// Parses a string representation to SubscriptionId.
    /// </summary>
    public static SubscriptionId Parse(string value) => new(Guid.Parse(value));

    /// <summary>
    /// Tries to parse a string to SubscriptionId.
    /// </summary>
    public static bool TryParse(string? value, out SubscriptionId? subscriptionId)
    {
        subscriptionId = null;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!Guid.TryParse(value, out var guid))
            return false;

        subscriptionId = new SubscriptionId(guid);
        return true;
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
