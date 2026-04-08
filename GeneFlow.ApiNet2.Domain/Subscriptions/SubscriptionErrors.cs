using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Subscriptions;

/// <summary>
/// Domain errors for Subscription aggregate.
/// </summary>
public static class SubscriptionErrors
{
    /// <summary>Subscription was not found.</summary>
    public static Error NotFound => Error.NotFound(
        "Subscription.NotFound",
        "The specified subscription was not found.");

    /// <summary>Invalid subscription period.</summary>
    public static Error InvalidPeriod => Error.Validation(
        "Subscription.InvalidPeriod",
        "End date must be after start date.");

    /// <summary>User already has an active subscription.</summary>
    public static Error UserAlreadyHasActiveSubscription => Error.Conflict(
        "Subscription.UserAlreadyHasActiveSubscription",
        "User already has an active subscription.");

    /// <summary>Plan not found.</summary>
    public static Error PlanNotFound => Error.NotFound(
        "Subscription.PlanNotFound",
        "The specified plan was not found.");

    /// <summary>Plan is not active.</summary>
    public static Error PlanNotActive => Error.Validation(
        "Subscription.PlanNotActive",
        "The specified plan is not active.");

    /// <summary>Cannot cancel expired subscription.</summary>
    public static Error CannotCancelExpired => Error.Validation(
        "Subscription.CannotCancelExpired",
        "Cannot cancel an expired subscription.");

    /// <summary>Cannot cancel already cancelled subscription.</summary>
    public static Error AlreadyCancelled => Error.Validation(
        "Subscription.AlreadyCancelled",
        "Subscription is already cancelled.");

    /// <summary>Cannot renew active subscription.</summary>
    public static Error CannotRenewActive => Error.Validation(
        "Subscription.CannotRenewActive",
        "Cannot renew an active subscription. Wait until it expires or cancel first.");

    /// <summary>Cannot change plan on expired subscription.</summary>
    public static Error CannotChangePlanOnExpired => Error.Validation(
        "Subscription.CannotChangePlanOnExpired",
        "Cannot change plan on an expired subscription. Please renew first.");

    /// <summary>Cannot downgrade to free during paid period.</summary>
    public static Error CannotDowngradeToFreeDuringPaidPeriod => Error.Validation(
        "Subscription.CannotDowngradeToFreeDuringPaidPeriod",
        "Cannot downgrade to free plan during an active paid period.");

    /// <summary>Cannot activate from non-trial status.</summary>
    public static Error CannotActivateFromNonTrial => Error.Validation(
        "Subscription.CannotActivateFromNonTrial",
        "Can only activate a subscription that is in trial status.");

    /// <summary>Trial has expired.</summary>
    public static Error TrialExpired => Error.Validation(
        "Subscription.TrialExpired",
        "The trial period has expired.");

    /// <summary>Cannot suspend non-active subscription.</summary>
    public static Error CannotSuspendNonActive => Error.Validation(
        "Subscription.CannotSuspendNonActive",
        "Can only suspend an active subscription.");

    /// <summary>Cannot reactivate non-suspended subscription.</summary>
    public static Error CannotReactivateNonSuspended => Error.Validation(
        "Subscription.CannotReactivateNonSuspended",
        "Can only reactivate a suspended subscription.");

    /// <summary>Invalid billing cycle.</summary>
    public static Error InvalidBillingCycle => Error.Validation(
        "Subscription.InvalidBillingCycle",
        "Invalid billing cycle specified.");
}
