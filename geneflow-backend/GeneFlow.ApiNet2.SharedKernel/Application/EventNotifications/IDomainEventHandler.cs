namespace GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

/// <summary>
/// Handler for domain events.
/// Multiple handlers can subscribe to the same event.
/// </summary>
/// <typeparam name="TEvent">The type of domain event to handle.</typeparam>
public interface IDomainEventHandler<TEvent> : INotificationHandler<TEvent>
    where TEvent : IDomainEvent;
