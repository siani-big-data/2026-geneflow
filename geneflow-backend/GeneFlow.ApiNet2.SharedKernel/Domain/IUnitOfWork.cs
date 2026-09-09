namespace GeneFlow.ApiNet2.SharedKernel.Domain;

/// <summary>
/// Unit of Work interface for managing transactions across repositories.
/// Ensures all changes to aggregates are persisted atomically.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all changes made in this unit of work.
    /// </summary>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
