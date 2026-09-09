namespace GeneFlow.ApiNet2.SharedKernel.Domain.DDD;

/// <summary>
/// Interface for entities with a strongly-typed identifier.
/// </summary>
/// <typeparam name="TId">The type of the entity's identifier.</typeparam>
public interface IEntity<out TId>
    where TId : notnull
{
    /// <summary>
    /// The unique identifier of the entity.
    /// </summary>
    TId Id { get; }
}
