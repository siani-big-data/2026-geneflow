using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.SharedKernel.Infrastructure;

/// <summary>
/// Publishes domain events to the event bus (Redis Streams).
/// Events are published to streams named geneflow:events:{category}.
/// </summary>
public interface IEventBusPublisher
{
    /// <summary>
    /// Publishes a domain event to the appropriate stream based on its category.
    /// </summary>
    /// <param name="domainEvent">The domain event to publish.</param>
    /// <param name="category">The event category (e.g., "users", "studies", "traces").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync(IDomainEvent domainEvent, string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes multiple domain events to their respective streams.
    /// </summary>
    /// <param name="events">The events with their categories.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishBatchAsync(
        IEnumerable<(IDomainEvent Event, string Category)> events,
        CancellationToken cancellationToken = default);
}
