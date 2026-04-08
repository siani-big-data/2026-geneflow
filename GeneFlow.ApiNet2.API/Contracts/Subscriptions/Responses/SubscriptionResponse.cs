namespace GeneFlow.ApiNet2.API.Contracts.Subscriptions.Responses;

/// <summary>
/// Response for a subscription.
/// </summary>
public sealed record SubscriptionResponse(
    Guid Id,
    string UserId,
    Guid PlanId,
    string PlanName,
    string Status,
    string BillingCycle,
    SubscriptionPeriodResponse CurrentPeriod,
    bool AutoRenew,
    bool GrantsAccess,
    bool IsFree,
    bool IsInTrial,
    DateTime? TrialEndDate,
    DateTime? CancelledAt,
    string? CancellationReason,
    DateTime CreatedAt,
    DateTime? ModifiedAt);

/// <summary>
/// Response for subscription period.
/// </summary>
public sealed record SubscriptionPeriodResponse(
    DateTime StartDate,
    DateTime EndDate,
    int DaysRemaining);

/// <summary>
/// Summary response for subscription lists.
/// </summary>
public sealed record SubscriptionSummaryResponse(
    Guid Id,
    string PlanName,
    string Status,
    DateTime StartDate,
    DateTime EndDate,
    bool GrantsAccess);
