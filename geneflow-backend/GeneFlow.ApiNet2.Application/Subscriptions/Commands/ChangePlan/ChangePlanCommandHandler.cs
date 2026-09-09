using GeneFlow.ApiNet2.Application.Subscriptions.DTOs;
using GeneFlow.ApiNet2.Application.Subscriptions.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Domain.Subscriptions;
using GeneFlow.ApiNet2.Domain.Subscriptions.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Subscriptions.Commands.ChangePlan;

/// <summary>
/// Handler for ChangePlanCommand.
/// </summary>
public sealed class ChangePlanCommandHandler
    : ICommandHandler<ChangePlanCommand, Result<SubscriptionDto>>
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionUnitOfWork _unitOfWork;
    private readonly IPlanRepository _planRepository;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public ChangePlanCommandHandler(
        ISubscriptionRepository subscriptionRepository,
        ISubscriptionUnitOfWork unitOfWork,
        IPlanRepository planRepository)
    {
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
        _planRepository = planRepository;
    }

    /// <inheritdoc />
    public async Task<Result<SubscriptionDto>> Handle(
        ChangePlanCommand request,
        CancellationToken cancellationToken)
    {
        // Parse UserId
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<SubscriptionDto>(SubscriptionErrors.NotFound);

        // Parse PlanId
        if (!PlanId.TryParse(request.NewPlanId, out var newPlanId) || newPlanId is null)
            return Result.Failure<SubscriptionDto>(SubscriptionErrors.PlanNotFound);

        // Get billing cycle
        var billingCycle = BillingCycle.FromId(request.BillingCycleId);
        if (billingCycle is null)
            return Result.Failure<SubscriptionDto>(SubscriptionErrors.InvalidBillingCycle);

        // Get active subscription
        var subscription = await _subscriptionRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        if (subscription is null)
            return Result.Failure<SubscriptionDto>(SubscriptionErrors.NotFound);

        // Get new plan
        var newPlan = await _planRepository.GetByIdAsync(newPlanId, cancellationToken);
        if (newPlan is null)
            return Result.Failure<SubscriptionDto>(SubscriptionErrors.PlanNotFound);

        if (!newPlan.IsActive)
            return Result.Failure<SubscriptionDto>(SubscriptionErrors.PlanNotActive);

        // Get current plan for price comparison
        var currentPlan = await _planRepository.GetByIdAsync(subscription.PlanId, cancellationToken);
        var isUpgrade = currentPlan is null ||
                        newPlan.Pricing.MonthlyPrice > currentPlan.Pricing.MonthlyPrice;

        // Change plan
        var changeResult = subscription.ChangePlan(
            newPlanId,
            newPlan.Name.Value,
            billingCycle,
            isUpgrade);

        if (changeResult.IsFailure)
            return Result.Failure<SubscriptionDto>(changeResult.Error);

        _subscriptionRepository.Update(subscription);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return subscription.ToDto();
    }
}
