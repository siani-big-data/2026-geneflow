using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;

namespace GeneFlow.ApiNet2.SharedKernel.Domain;

/// <summary>
/// Generic repository interface for aggregate roots.
/// Repositories provide collection-like access to aggregates.
/// </summary>
/// <typeparam name="TEntity">The aggregate root type.</typeparam>
/// <typeparam name="TId">The type of the aggregate's identifier.</typeparam>
public interface IRepository<TEntity, in TId>
    where TEntity : IAggregateRoot<TId>
    where TId : notnull
{
    /// <summary>
    /// Gets an aggregate by its identifier.
    /// </summary>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new aggregate to the repository.
    /// </summary>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing aggregate in the repository.
    /// </summary>
    void Update(TEntity entity);

    /// <summary>
    /// Removes an aggregate from the repository.
    /// </summary>
    void Remove(TEntity entity);
}
