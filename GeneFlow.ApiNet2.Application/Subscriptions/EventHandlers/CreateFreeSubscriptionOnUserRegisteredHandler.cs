using GeneFlow.ApiNet2.Domain.Identity.Events;
using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Domain.Subscriptions;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Subscriptions.EventHandlers;

/// <summary>
/// Creates a free subscription when a user registers.
/// </summary>
public sealed class CreateFreeSubscriptionOnUserRegisteredHandler
    : IDomainEventHandler<UserRegisteredEvent>
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionUnitOfWork _unitOfWork;
    private readonly IPlanRepository _planRepository;
    private readonly ISequenceGenerator _sequenceGenerator;
    private readonly ILogger<CreateFreeSubscriptionOnUserRegisteredHandler> _logger;

    public CreateFreeSubscriptionOnUserRegisteredHandler(
        ISubscriptionRepository subscriptionRepository,
        ISubscriptionUnitOfWork unitOfWork,
        IPlanRepository planRepository,
        ISequenceGenerator sequenceGenerator,
        ILogger<CreateFreeSubscriptionOnUserRegisteredHandler> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
        _planRepository = planRepository;
        _sequenceGenerator = sequenceGenerator;
        _logger = logger;
    }

    public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "===> CreateFreeSubscriptionOnUserRegisteredHandler TRIGGERED for user {UserId} <===",
            notification.UserId);

        try
        {
            _logger.LogInformation(
                "Creating free subscription for newly registered user {UserId}",
                notification.UserId);
            // Check if user already has a subscription (shouldn't happen, but be safe)
            if (await _subscriptionRepository.HasActiveSubscriptionAsync(notification.UserId, cancellationToken))
            {
                _logger.LogWarning(
                    "User {UserId} already has an active subscription, skipping free subscription creation",
                    notification.UserId);
                return;
            }

            // Get the default (free) plan
            var defaultPlan = await _planRepository.GetDefaultPlanAsync(cancellationToken);
            if (defaultPlan is null)
            {
                _logger.LogError(
                    "No default plan found, cannot create free subscription for user {UserId}",
                    notification.UserId);
                return;
            }

            // Generate subscription ID
            var sequenceId = await _sequenceGenerator.NextAsync(SubscriptionId.SequenceName, cancellationToken);
            var subscriptionId = SubscriptionId.FromSequence(sequenceId);

            // Create free subscription
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

            await _subscriptionRepository.AddAsync(subscription, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Free subscription {SubscriptionId} created successfully for user {UserId} with plan {PlanName}",
                subscription.Id,
                notification.UserId,
                defaultPlan.Name.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create free subscription for user {UserId}",
                notification.UserId);
            // Don't rethrow - subscription creation failure shouldn't fail registration
        }
    }
}
