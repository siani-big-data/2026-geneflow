using GeneFlow.ApiNet2.Application.Subscriptions.DTOs;
using GeneFlow.ApiNet2.Application.Subscriptions.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Domain.Subscriptions;
using GeneFlow.ApiNet2.Domain.Subscriptions.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Subscriptions.Commands.CreateSubscription;

/// <summary>
/// Handler for CreateSubscriptionCommand.
/// </summary>
public sealed class CreateSubscriptionCommandHandler
    : ICommandHandler<CreateSubscriptionCommand, Result<SubscriptionDto>>
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionUnitOfWork _unitOfWork;
    private readonly IPlanRepository _planRepository;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public CreateSubscriptionCommandHandler(
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
        CreateSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        // Parse UserId
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<SubscriptionDto>(SubscriptionErrors.NotFound);

        // Parse PlanId
        if (!PlanId.TryParse(request.PlanId, out var planId) || planId is null)
            return Result.Failure<SubscriptionDto>(SubscriptionErrors.PlanNotFound);

        // Get billing cycle
        var billingCycle = BillingCycle.FromId(request.BillingCycleId);
        if (billingCycle is null)
            return Result.Failure<SubscriptionDto>(SubscriptionErrors.InvalidBillingCycle);

        // Check if user already has an active subscription
        if (await _subscriptionRepository.HasActiveSubscriptionAsync(userId, cancellationToken))
            return Result.Failure<SubscriptionDto>(SubscriptionErrors.UserAlreadyHasActiveSubscription);

        // Get plan
        var plan = await _planRepository.GetByIdAsync(planId, cancellationToken);
        if (plan is null)
            return Result.Failure<SubscriptionDto>(SubscriptionErrors.PlanNotFound);

        if (!plan.IsActive)
            return Result.Failure<SubscriptionDto>(SubscriptionErrors.PlanNotActive);

        // Create subscription
        Result<Subscription> subscriptionResult;
        if (plan.IsFree)
        {
            subscriptionResult = Subscription.CreateFree(userId, planId);
        }
        else
        {
            subscriptionResult = Subscription.Create(
                userId,
                planId,
                plan.Name.Value,
                billingCycle,
                request.StartWithTrial);
        }

        if (subscriptionResult.IsFailure)
            return Result.Failure<SubscriptionDto>(subscriptionResult.Error);

        var subscription = subscriptionResult.Value;

        await _subscriptionRepository.AddAsync(subscription, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return subscription.ToDto();
    }
}
