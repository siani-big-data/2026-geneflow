namespace GeneFlow.ApiNet2.Application.Subscriptions.DTOs;

/// <summary>
/// DTO for Subscription aggregate.
/// </summary>
public sealed record SubscriptionDto(
    Guid SubscriptionId,
    string UserId,
    Guid PlanId,
    string PlanName,
    string Status,
    string BillingCycle,
    SubscriptionPeriodDto CurrentPeriod,
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
/// DTO for subscription period.
/// </summary>
public sealed record SubscriptionPeriodDto(
    DateTime StartDate,
    DateTime EndDate,
    int DaysRemaining);

/// <summary>
/// Summary DTO for subscription lists.
/// </summary>
public sealed record SubscriptionSummaryDto(
    Guid SubscriptionId,
    string PlanName,
    string Status,
    DateTime StartDate,
    DateTime EndDate,
    bool GrantsAccess);
