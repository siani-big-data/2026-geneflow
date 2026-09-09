using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.SharedKernel.Domain.DDD;

/// <summary>
/// Base class for aggregate roots.
/// Aggregate roots maintain consistency boundaries and raise domain events.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <inheritdoc />
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of <see cref="AggregateRoot{TId}"/>.
    /// </summary>
    protected AggregateRoot() { }

    /// <summary>
    /// Initializes a new instance of <see cref="AggregateRoot{TId}"/> with the specified identifier.
    /// </summary>
    /// <param name="id">The aggregate identifier.</param>
    protected AggregateRoot(TId id) : base(id) { }

    /// <summary>
    /// Raises a domain event to be dispatched after the aggregate is persisted.
    /// </summary>
    /// <param name="domainEvent">The domain event to raise.</param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <inheritdoc />
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
