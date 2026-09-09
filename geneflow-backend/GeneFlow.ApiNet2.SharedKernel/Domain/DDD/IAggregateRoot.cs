using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.SharedKernel.Domain.DDD;

/// <summary>
/// Non-generic interface for aggregate roots.
/// Used for collecting domain events across all aggregate types.
/// </summary>
public interface IAggregateRoot
{
    /// <summary>
    /// Domain events raised by this aggregate.
    /// </summary>
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clears all pending domain events.
    /// </summary>
    void ClearDomainEvents();
}

/// <summary>
/// Interface for aggregate roots.
/// Aggregate roots are the entry point to an aggregate and maintain consistency boundaries.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root's identifier.</typeparam>
public interface IAggregateRoot<out TId> : IEntity<TId>, IAggregateRoot
    where TId : notnull
{
}
