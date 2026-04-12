using GeneFlow.ApiNet2.Domain.Subscriptions.Events;
using GeneFlow.ApiNet2.Domain.Usage;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Usage.EventHandlers;

/// <summary>
/// Handles SubscriptionCreatedEvent to initialize usage stats for the user.
/// </summary>
public sealed class SubscriptionCreatedUsageHandler
    : INotificationHandler<SubscriptionCreatedEvent>
{
    private readonly IUsageStatsRepository _usageRepository;
    private readonly ILogger<SubscriptionCreatedUsageHandler> _logger;

    public SubscriptionCreatedUsageHandler(
        IUsageStatsRepository usageRepository,
        ILogger<SubscriptionCreatedUsageHandler> logger)
    {
        _usageRepository = usageRepository;
        _logger = logger;
    }

    public async Task Handle(
        SubscriptionCreatedEvent notification,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Initializing usage stats for user {UserId} after subscription creation",
            notification.UserId);

        try
        {
            // Check if stats already exist
            var existingStats = await _usageRepository.GetByUserIdAsync(
                notification.UserId,
                cancellationToken);

            if (existingStats is null)
            {
                // Create initial empty stats for the user
                var stats = UsageStats.Create(
                    notification.UserId,
                    BillingPeriodKey.Current());

                await _usageRepository.SaveAsync(stats, cancellationToken);

                _logger.LogInformation(
                    "Created initial usage stats for user {UserId}",
                    notification.UserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to initialize usage stats for user {UserId}",
                notification.UserId);
            // Don't rethrow - usage stats initialization shouldn't fail the subscription
        }
    }
}
