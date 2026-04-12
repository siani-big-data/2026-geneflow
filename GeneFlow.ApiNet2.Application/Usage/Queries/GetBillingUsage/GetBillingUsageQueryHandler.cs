using GeneFlow.ApiNet2.Application.Usage.DTOs;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Domain.Subscriptions;
using GeneFlow.ApiNet2.Domain.Usage;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Usage.Queries.GetBillingUsage;

/// <summary>
/// Handler for GetBillingUsageQuery.
/// </summary>
public sealed class GetBillingUsageQueryHandler
    : IQueryHandler<GetBillingUsageQuery, Result<BillingUsageDto>>
{
    private readonly IUsageStatsRepository _usageRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IPlanRepository _planRepository;

    public GetBillingUsageQueryHandler(
        IUsageStatsRepository usageRepository,
        ISubscriptionRepository subscriptionRepository,
        IPlanRepository planRepository)
    {
        _usageRepository = usageRepository;
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
    }

    public async Task<Result<BillingUsageDto>> Handle(
        GetBillingUsageQuery request,
        CancellationToken cancellationToken)
    {
        // Parse UserId
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<BillingUsageDto>(SubscriptionErrors.NotFound);

        // Get current subscription to determine plan limits
        var subscription = await _subscriptionRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        if (subscription is null)
            return Result.Failure<BillingUsageDto>(SubscriptionErrors.NotFound);

        // Get plan limits
        var plan = await _planRepository.GetByIdAsync(subscription.PlanId, cancellationToken);
        if (plan is null)
            return Result.Failure<BillingUsageDto>(SubscriptionErrors.PlanNotFound);

        // Get usage stats (or create empty if not exists)
        var stats = await _usageRepository.GetByUserIdAsync(userId, cancellationToken);

        // Calculate period info
        var periodStart = subscription.CurrentPeriod.StartDate;
        var periodEnd = subscription.CurrentPeriod.EndDate;
        var totalDays = (int)(periodEnd - periodStart).TotalDays;
        var daysRemaining = subscription.CurrentPeriod.DaysRemaining;

        // Build response
        var studiesUsed = stats?.StudiesOwned ?? 0;
        var tracesUsed = stats?.TracesThisPeriod ?? 0;
        var membersUsed = stats?.MaxMembersInStudy ?? 0;

        return new BillingUsageDto(
            Studies: new UsageItemDto(
                Used: studiesUsed,
                Total: plan.Limits.MaxStudies,
                Percentage: CalculatePercentage(studiesUsed, plan.Limits.MaxStudies)),
            Traces: new UsageItemDto(
                Used: tracesUsed,
                Total: plan.Limits.MaxTracesPerMonth,
                Percentage: CalculatePercentage(tracesUsed, plan.Limits.MaxTracesPerMonth)),
            Members: new UsageItemDto(
                Used: membersUsed,
                Total: plan.Limits.MaxMembersPerStudy,
                Percentage: CalculatePercentage(membersUsed, plan.Limits.MaxMembersPerStudy)),
            Period: new BillingPeriodDto(
                StartDate: periodStart,
                EndDate: periodEnd,
                DaysRemaining: daysRemaining,
                TotalDays: totalDays));
    }

    private static int CalculatePercentage(int used, int total)
    {
        if (total <= 0) return 0;
        return Math.Min((int)Math.Round((double)used / total * 100), 100);
    }
}
