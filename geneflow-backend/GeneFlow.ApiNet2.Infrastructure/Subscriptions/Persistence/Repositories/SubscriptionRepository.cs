using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Domain.Subscriptions;
using GeneFlow.ApiNet2.Domain.Subscriptions.Enumerations;
using GeneFlow.ApiNet2.Infrastructure.Subscriptions.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace GeneFlow.ApiNet2.Infrastructure.Subscriptions.Persistence.Repositories;

/// <summary>
/// Repository implementation for Subscription aggregate.
/// </summary>
public sealed class SubscriptionRepository : ISubscriptionRepository
{
    private readonly SubscriptionContext _context;

    /// <summary>
    /// Initializes a new instance of the repository.
    /// </summary>
    public SubscriptionRepository(SubscriptionContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Subscription?> GetByIdAsync(SubscriptionId id, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Subscription?> GetActiveByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .Where(s => s.UserId == userId)
            .Where(s => s.Status == SubscriptionStatus.Active ||
                        s.Status == SubscriptionStatus.Trial ||
                        s.Status == SubscriptionStatus.PastDue ||
                        s.Status == SubscriptionStatus.Cancelled)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Subscription>> GetAllByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasActiveSubscriptionAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .AnyAsync(s => s.UserId == userId &&
                          (s.Status == SubscriptionStatus.Active ||
                           s.Status == SubscriptionStatus.Trial ||
                           s.Status == SubscriptionStatus.PastDue),
                      cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Subscription>> GetExpiringSubscriptionsAsync(int daysUntilExpiration, CancellationToken cancellationToken = default)
    {
        var targetDate = DateTime.UtcNow.AddDays(daysUntilExpiration);

        return await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active)
            .Where(s => s.CurrentPeriod.EndDate <= targetDate)
            .Where(s => s.CurrentPeriod.EndDate > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Subscription>> GetExpiredSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active ||
                        s.Status == SubscriptionStatus.Trial ||
                        s.Status == SubscriptionStatus.PastDue)
            .Where(s => s.CurrentPeriod.EndDate <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Subscription>> GetByPlanIdAsync(PlanId planId, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .Where(s => s.PlanId == planId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountActiveByPlanIdAsync(PlanId planId, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .CountAsync(s => s.PlanId == planId &&
                            (s.Status == SubscriptionStatus.Active ||
                             s.Status == SubscriptionStatus.Trial),
                        cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        await _context.Subscriptions.AddAsync(subscription, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(Subscription subscription)
    {
        _context.Subscriptions.Update(subscription);
    }
}
