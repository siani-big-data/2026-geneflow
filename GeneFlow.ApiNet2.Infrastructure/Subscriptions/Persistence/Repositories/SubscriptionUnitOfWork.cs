using GeneFlow.ApiNet2.Domain.Subscriptions;
using GeneFlow.ApiNet2.Infrastructure.Subscriptions.Persistence.Context;

namespace GeneFlow.ApiNet2.Infrastructure.Subscriptions.Persistence.Repositories;

/// <summary>
/// Unit of work implementation for Subscription bounded context.
/// </summary>
public sealed class SubscriptionUnitOfWork : ISubscriptionUnitOfWork
{
    private readonly SubscriptionContext _context;

    /// <summary>
    /// Initializes a new instance of the unit of work.
    /// </summary>
    public SubscriptionUnitOfWork(SubscriptionContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
