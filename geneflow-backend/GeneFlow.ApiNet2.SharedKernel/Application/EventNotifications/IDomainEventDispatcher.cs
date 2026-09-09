namespace GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

/// <summary>
/// Dispatches domain events to their handlers.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches all provided domain events to their handlers.
    /// </summary>
    Task DispatchEventsAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default);
}
