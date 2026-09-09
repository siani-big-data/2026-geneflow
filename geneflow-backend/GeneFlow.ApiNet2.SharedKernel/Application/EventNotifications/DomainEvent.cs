namespace GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

/// <summary>
/// Base record for domain events providing default implementations.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <inheritdoc />
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
