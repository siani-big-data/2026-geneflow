namespace GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something significant that happened in the domain.
/// They are dispatched after the aggregate state has been persisted.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// When this event occurred.
    /// </summary>
    DateTime OccurredAt { get; }
}
