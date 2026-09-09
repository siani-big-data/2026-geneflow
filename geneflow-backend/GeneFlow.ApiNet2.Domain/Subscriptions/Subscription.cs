using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Domain.Subscriptions.Enumerations;
using GeneFlow.ApiNet2.Domain.Subscriptions.Events;
using GeneFlow.ApiNet2.Domain.Subscriptions.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Subscriptions;

/// <summary>
/// Subscription aggregate root.
/// </summary>
public sealed class Subscription : AggregateRoot<SubscriptionId>
{
    /// <summary>Default trial period in days.</summary>
    public const int DefaultTrialDays = 14;

    /// <summary>Gets the user ID.</summary>
    public UserId UserId { get; private set; } = null!;

    /// <summary>Gets the plan ID.</summary>
    public PlanId PlanId { get; private set; } = null!;

    /// <summary>Gets the plan name (cached for display).</summary>
    public string PlanName { get; private set; } = null!;

    /// <summary>Gets the subscription status.</summary>
    public SubscriptionStatus Status { get; private set; } = null!;

    /// <summary>Gets the billing cycle.</summary>
    public BillingCycle BillingCycle { get; private set; } = null!;

    /// <summary>Gets the current period.</summary>
    public SubscriptionPeriod CurrentPeriod { get; private set; } = null!;

    /// <summary>Gets whether auto-renew is enabled.</summary>
    public bool AutoRenew { get; private set; }

    /// <summary>Gets the creation date.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Gets the last modification date.</summary>
    public DateTime? ModifiedAt { get; private set; }

    /// <summary>Gets the cancellation date.</summary>
    public DateTime? CancelledAt { get; private set; }

    /// <summary>Gets the cancellation reason.</summary>
    public string? CancellationReason { get; private set; }

    /// <summary>Gets the trial end date.</summary>
    public DateTime? TrialEndDate { get; private set; }

    /// <summary>Gets whether the subscription grants access.</summary>
    public bool GrantsAccess => Status.GrantsAccess && !CurrentPeriod.HasExpired;

    /// <summary>Gets whether this is a free subscription.</summary>
    public bool IsFree => PlanName.Equals("Free", StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets whether the subscription is in trial.</summary>
    public bool IsInTrial => Status == SubscriptionStatus.Trial &&
                             TrialEndDate.HasValue &&
                             DateTime.UtcNow < TrialEndDate.Value;

    private Subscription()
    {
    }

    /// <summary>
    /// Creates a new subscription.
    /// </summary>
    public static Result<Subscription> Create(
        SubscriptionId id,
        UserId userId,
        PlanId planId,
        string planName,
        BillingCycle billingCycle,
        bool startWithTrial = false)
    {
        var now = DateTime.UtcNow;
        var periodMonths = billingCycle.Months;

        var subscription = new Subscription
        {
            Id = id,
            UserId = userId,
            PlanId = planId,
            PlanName = planName,
            BillingCycle = billingCycle,
            AutoRenew = true,
            CreatedAt = now
        };

        if (startWithTrial)
        {
            subscription.Status = SubscriptionStatus.Trial;
            subscription.TrialEndDate = now.AddDays(DefaultTrialDays);
            subscription.CurrentPeriod = SubscriptionPeriod.CreateFromNow(periodMonths);
        }
        else
        {
            subscription.Status = SubscriptionStatus.Active;
            subscription.CurrentPeriod = SubscriptionPeriod.CreateFromNow(periodMonths);
        }

        subscription.RaiseDomainEvent(new SubscriptionCreatedEvent(subscription.Id, userId, planId));

        return subscription;
    }

    /// <summary>
    /// Creates a free subscription (100-year period).
    /// </summary>
    public static Result<Subscription> CreateFree(SubscriptionId id, UserId userId, PlanId planId)
    {
        var now = DateTime.UtcNow;

        var subscription = new Subscription
        {
            Id = id,
            UserId = userId,
            PlanId = planId,
            PlanName = "Free",
            Status = SubscriptionStatus.Active,
            BillingCycle = BillingCycle.Monthly,
            CurrentPeriod = SubscriptionPeriod.CreateFromDate(now, 1200), // 100 years
            AutoRenew = false,
            CreatedAt = now
        };

        subscription.RaiseDomainEvent(new SubscriptionCreatedEvent(subscription.Id, userId, planId));

        return subscription;
    }

    /// <summary>
    /// Cancels the subscription.
    /// </summary>
    public Result Cancel(string? reason = null)
    {
        if (!Status.CanCancel)
            return Result.Failure(SubscriptionErrors.CannotCancelExpired);

        Status = SubscriptionStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
        ModifiedAt = DateTime.UtcNow;
        AutoRenew = false;

        RaiseDomainEvent(new SubscriptionCancelledEvent(Id, UserId, reason));

        return Result.Success();
    }

    /// <summary>
    /// Renews the subscription.
    /// </summary>
    public Result Renew()
    {
        if (!Status.CanRenew)
            return Result.Failure(SubscriptionErrors.CannotRenewActive);

        var newPeriod = SubscriptionPeriod.CreateFromNow(BillingCycle.Months);

        Status = SubscriptionStatus.Active;
        CurrentPeriod = newPeriod;
        CancelledAt = null;
        CancellationReason = null;
        ModifiedAt = DateTime.UtcNow;
        AutoRenew = true;

        RaiseDomainEvent(new SubscriptionRenewedEvent(Id, UserId, PlanId));

        return Result.Success();
    }

    /// <summary>
    /// Changes to a different plan.
    /// </summary>
    public Result ChangePlan(PlanId newPlanId, string newPlanName, BillingCycle newBillingCycle, bool isUpgrade)
    {
        if (Status == SubscriptionStatus.Expired)
            return Result.Failure(SubscriptionErrors.CannotChangePlanOnExpired);

        // Prevent downgrade to free during paid period
        if (newPlanName.Equals("Free", StringComparison.OrdinalIgnoreCase) &&
            !IsFree &&
            GrantsAccess)
        {
            return Result.Failure(SubscriptionErrors.CannotDowngradeToFreeDuringPaidPeriod);
        }

        var oldPlanId = PlanId;
        var oldPlanName = PlanName;

        PlanId = newPlanId;
        PlanName = newPlanName;
        BillingCycle = newBillingCycle;
        ModifiedAt = DateTime.UtcNow;

        // If upgrading, extend period; if downgrading, keep current period
        if (isUpgrade)
        {
            CurrentPeriod = SubscriptionPeriod.CreateFromNow(newBillingCycle.Months);
        }

        RaiseDomainEvent(new SubscriptionPlanChangedEvent(
            Id, UserId, oldPlanId, newPlanId, oldPlanName, newPlanName, isUpgrade));

        return Result.Success();
    }

    /// <summary>
    /// Activates from trial.
    /// </summary>
    public Result ActivateFromTrial()
    {
        if (Status != SubscriptionStatus.Trial)
            return Result.Failure(SubscriptionErrors.CannotActivateFromNonTrial);

        if (TrialEndDate.HasValue && DateTime.UtcNow > TrialEndDate.Value)
            return Result.Failure(SubscriptionErrors.TrialExpired);

        Status = SubscriptionStatus.Active;
        TrialEndDate = null;
        CurrentPeriod = SubscriptionPeriod.CreateFromNow(BillingCycle.Months);
        ModifiedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Expires the subscription.
    /// </summary>
    public Result Expire()
    {
        Status = SubscriptionStatus.Expired;
        ModifiedAt = DateTime.UtcNow;
        AutoRenew = false;

        RaiseDomainEvent(new SubscriptionExpiredEvent(Id, UserId));

        return Result.Success();
    }

    /// <summary>
    /// Suspends the subscription.
    /// </summary>
    public Result Suspend()
    {
        if (Status != SubscriptionStatus.Active && Status != SubscriptionStatus.PastDue)
            return Result.Failure(SubscriptionErrors.CannotSuspendNonActive);

        Status = SubscriptionStatus.Suspended;
        ModifiedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Reactivates a suspended subscription.
    /// </summary>
    public Result Reactivate()
    {
        if (Status != SubscriptionStatus.Suspended)
            return Result.Failure(SubscriptionErrors.CannotReactivateNonSuspended);

        Status = SubscriptionStatus.Active;
        ModifiedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Sets auto-renew status.
    /// </summary>
    public Result SetAutoRenew(bool autoRenew)
    {
        AutoRenew = autoRenew;
        ModifiedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
