using GeneFlow.ApiNet2.Application.Subscriptions.DTOs;
using GeneFlow.ApiNet2.Domain.Subscriptions;

namespace GeneFlow.ApiNet2.Application.Subscriptions.Mappings;

/// <summary>
/// Extension methods for mapping Subscription entities to DTOs.
/// </summary>
public static class SubscriptionMappings
{
    /// <summary>
    /// Maps a Subscription to SubscriptionDto.
    /// </summary>
    public static SubscriptionDto ToDto(this Subscription subscription)
    {
        return new SubscriptionDto(
            subscription.Id.ToString(),
            subscription.UserId.ToString(),
            subscription.PlanId.ToString(),
            subscription.PlanName,
            subscription.Status.Name,
            subscription.BillingCycle.Name,
            new SubscriptionPeriodDto(
                subscription.CurrentPeriod.StartDate,
                subscription.CurrentPeriod.EndDate,
                subscription.CurrentPeriod.DaysRemaining),
            subscription.AutoRenew,
            subscription.GrantsAccess,
            subscription.IsFree,
            subscription.IsInTrial,
            subscription.TrialEndDate,
            subscription.CancelledAt,
            subscription.CancellationReason,
            subscription.CreatedAt,
            subscription.ModifiedAt);
    }

    /// <summary>
    /// Maps a Subscription to SubscriptionSummaryDto.
    /// </summary>
    public static SubscriptionSummaryDto ToSummaryDto(this Subscription subscription)
    {
        return new SubscriptionSummaryDto(
            subscription.Id.ToString(),
            subscription.PlanName,
            subscription.Status.Name,
            subscription.CurrentPeriod.StartDate,
            subscription.CurrentPeriod.EndDate,
            subscription.GrantsAccess);
    }

    /// <summary>
    /// Maps a collection of Subscriptions to Summary DTOs.
    /// </summary>
    public static IReadOnlyList<SubscriptionSummaryDto> ToSummaryDtos(this IEnumerable<Subscription> subscriptions)
    {
        return subscriptions.Select(s => s.ToSummaryDto()).ToList();
    }
}
