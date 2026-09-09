using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Plans;

namespace GeneFlow.ApiNet2.Domain.Subscriptions;

/// <summary>
/// Repository interface for Subscription aggregate.
/// </summary>
public interface ISubscriptionRepository
{
    /// <summary>
    /// Gets a subscription by its ID.
    /// </summary>
    Task<Subscription?> GetByIdAsync(SubscriptionId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active subscription for a user.
    /// </summary>
    Task<Subscription?> GetActiveByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all subscriptions for a user.
    /// </summary>
    Task<IReadOnlyList<Subscription>> GetAllByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has an active subscription.
    /// </summary>
    Task<bool> HasActiveSubscriptionAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets subscriptions expiring within the specified days.
    /// </summary>
    Task<IReadOnlyList<Subscription>> GetExpiringSubscriptionsAsync(int daysUntilExpiration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expired subscriptions that need to be marked as expired.
    /// </summary>
    Task<IReadOnlyList<Subscription>> GetExpiredSubscriptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets subscriptions by plan ID.
    /// </summary>
    Task<IReadOnlyList<Subscription>> GetByPlanIdAsync(PlanId planId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts active subscriptions for a plan.
    /// </summary>
    Task<int> CountActiveByPlanIdAsync(PlanId planId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new subscription.
    /// </summary>
    Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing subscription.
    /// </summary>
    void Update(Subscription subscription);
}
