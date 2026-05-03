using GeneFlow.ApiNet2.Domain.Identity.Events;
using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Domain.Subscriptions;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Subscriptions.EventHandlers;

/// <summary>
/// Creates a free subscription when a user registers via OAuth.
/// </summary>
/// <remarks>
/// Exceptions are intentionally caught and logged without rethrowing because
/// failures in this side-effect handler must not break OAuth registration.
/// </remarks>
public sealed class CreateFreeSubscriptionOnOAuthUserRegisteredHandler
    : IDomainEventHandler<UserRegisteredViaOAuthEvent>
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionUnitOfWork _unitOfWork;
    private readonly IPlanRepository _planRepository;
    private readonly ISequenceGenerator _sequenceGenerator;
    private readonly ILogger<CreateFreeSubscriptionOnOAuthUserRegisteredHandler> _logger;

    public CreateFreeSubscriptionOnOAuthUserRegisteredHandler(
        ISubscriptionRepository subscriptionRepository,
        ISubscriptionUnitOfWork unitOfWork,
        IPlanRepository planRepository,
        ISequenceGenerator sequenceGenerator,
        ILogger<CreateFreeSubscriptionOnOAuthUserRegisteredHandler> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
        _planRepository = planRepository;
        _sequenceGenerator = sequenceGenerator;
        _logger = logger;
    }

    public async Task Handle(UserRegisteredViaOAuthEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "===> CreateFreeSubscriptionOnOAuthUserRegisteredHandler TRIGGERED for user {UserId} (OAuth: {Provider}) <===",
            notification.UserId,
            notification.Provider.Name);

        try
        {
            _logger.LogInformation(
                "Creating free subscription for newly registered OAuth user {UserId}",
                notification.UserId);

            if (await _subscriptionRepository.HasActiveSubscriptionAsync(notification.UserId, cancellationToken))
            {
                _logger.LogWarning(
                    "User {UserId} already has an active subscription, skipping free subscription creation",
                    notification.UserId);
                return;
            }

            var defaultPlan = await _planRepository.GetDefaultPlanAsync(cancellationToken);
            if (defaultPlan is null)
            {
                _logger.LogError(
                    "No default plan found, cannot create free subscription for user {UserId}",
                    notification.UserId);
                return;
            }

            _logger.LogInformation(
                "Found default plan: {PlanId} - {PlanName}",
                defaultPlan.Id,
                defaultPlan.Name.Value);

            var sequenceId = await _sequenceGenerator.NextAsync(SubscriptionId.SequenceName, cancellationToken);
            var subscriptionId = SubscriptionId.FromSequence(sequenceId);

            var subscriptionResult = Subscription.CreateFree(subscriptionId, notification.UserId, defaultPlan.Id);
            if (subscriptionResult.IsFailure)
            {
                _logger.LogError(
                    "Failed to create free subscription for user {UserId}: {Error}",
                    notification.UserId,
                    subscriptionResult.Error.Message);
                return;
            }

            var subscription = subscriptionResult.Value;

            _logger.LogInformation(
                "Adding subscription {SubscriptionId} to repository...",
                subscription.Id);

            await _subscriptionRepository.AddAsync(subscription, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Free subscription {SubscriptionId} created successfully for OAuth user {UserId} with plan {PlanName}",
                subscription.Id,
                notification.UserId,
                defaultPlan.Name.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create free subscription for OAuth user {UserId}",
                notification.UserId);
        }
    }
}
