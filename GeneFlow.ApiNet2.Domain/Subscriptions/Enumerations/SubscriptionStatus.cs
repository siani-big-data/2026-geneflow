using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Subscriptions.Enumerations;

/// <summary>
/// Enumeration for subscription status.
/// </summary>
public sealed class SubscriptionStatus : Enumeration<SubscriptionStatus>
{
    /// <summary>Active and in good standing.</summary>
    public static readonly SubscriptionStatus Active = new(1, nameof(Active));

    /// <summary>In trial period.</summary>
    public static readonly SubscriptionStatus Trial = new(2, nameof(Trial));

    /// <summary>Payment overdue but still active.</summary>
    public static readonly SubscriptionStatus PastDue = new(3, nameof(PastDue));

    /// <summary>Cancelled but access continues until period end.</summary>
    public static readonly SubscriptionStatus Cancelled = new(4, nameof(Cancelled));

    /// <summary>Expired and no longer active.</summary>
    public static readonly SubscriptionStatus Expired = new(5, nameof(Expired));

    /// <summary>Suspended due to payment failure.</summary>
    public static readonly SubscriptionStatus Suspended = new(6, nameof(Suspended));

    private SubscriptionStatus(int id, string name) : base(id, name)
    {
    }

    /// <summary>Gets whether this status grants access.</summary>
    public bool GrantsAccess => this == Active || this == Trial || this == PastDue || this == Cancelled;

    /// <summary>Gets whether the subscription can be renewed.</summary>
    public bool CanRenew => this == Cancelled || this == Expired || this == Suspended;

    /// <summary>Gets whether the subscription can be cancelled.</summary>
    public bool CanCancel => this == Active || this == Trial || this == PastDue;
}
