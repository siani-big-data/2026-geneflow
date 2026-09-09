namespace GeneFlow.ApiNet2.Domain.Plans;

/// <summary>
/// Unit of work interface for Plan bounded context.
/// </summary>
public interface IPlanUnitOfWork
{
    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
