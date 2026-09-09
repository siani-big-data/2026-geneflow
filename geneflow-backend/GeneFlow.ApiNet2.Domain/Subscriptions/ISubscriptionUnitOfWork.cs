namespace GeneFlow.ApiNet2.Domain.Subscriptions;

/// <summary>
/// Unit of work interface for Subscription bounded context.
/// </summary>
public interface ISubscriptionUnitOfWork
{
    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
