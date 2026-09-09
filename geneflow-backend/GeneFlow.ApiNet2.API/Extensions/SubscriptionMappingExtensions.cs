using GeneFlow.ApiNet2.API.Contracts.Subscriptions.Responses;
using GeneFlow.ApiNet2.Application.Subscriptions.DTOs;

namespace GeneFlow.ApiNet2.API.Extensions;

/// <summary>
/// Extension methods for mapping Subscription DTOs to API responses.
/// </summary>
public static class SubscriptionMappingExtensions
{
    /// <summary>
    /// Maps a SubscriptionDto to SubscriptionResponse.
    /// </summary>
    public static SubscriptionResponse ToResponse(this SubscriptionDto dto)
    {
        return new SubscriptionResponse(
            dto.SubscriptionId,
            dto.UserId,
            dto.PlanId,
            dto.PlanName,
            dto.Status,
            dto.BillingCycle,
            new SubscriptionPeriodResponse(
                dto.CurrentPeriod.StartDate,
                dto.CurrentPeriod.EndDate,
                dto.CurrentPeriod.DaysRemaining),
            dto.AutoRenew,
            dto.GrantsAccess,
            dto.IsFree,
            dto.IsInTrial,
            dto.TrialEndDate,
            dto.CancelledAt,
            dto.CancellationReason,
            dto.CreatedAt,
            dto.ModifiedAt);
    }

    /// <summary>
    /// Maps a SubscriptionSummaryDto to SubscriptionSummaryResponse.
    /// </summary>
    public static SubscriptionSummaryResponse ToResponse(this SubscriptionSummaryDto dto)
    {
        return new SubscriptionSummaryResponse(
            dto.SubscriptionId,
            dto.PlanName,
            dto.Status,
            dto.StartDate,
            dto.EndDate,
            dto.GrantsAccess);
    }

    /// <summary>
    /// Maps a collection of SubscriptionSummaryDtos to responses.
    /// </summary>
    public static IReadOnlyList<SubscriptionSummaryResponse> ToResponses(this IEnumerable<SubscriptionSummaryDto> dtos)
    {
        return dtos.Select(d => d.ToResponse()).ToList();
    }
}
