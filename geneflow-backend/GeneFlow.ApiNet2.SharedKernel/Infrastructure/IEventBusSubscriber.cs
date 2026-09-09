namespace GeneFlow.ApiNet2.SharedKernel.Infrastructure;

/// <summary>
/// Interface for subscribing to domain events from an event bus.
/// </summary>
public interface IEventBusSubscriber
{
    /// <summary>
    /// Subscribes to events from a specific category/stream.
    /// </summary>
    /// <param name="category">The event category (e.g., "studies", "traces").</param>
    /// <param name="consumerGroup">The consumer group name.</param>
    /// <param name="handler">The handler to process received events.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SubscribeAsync(
        string category,
        string consumerGroup,
        Func<EventMessage, Task> handler,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acknowledges that an event has been processed.
    /// </summary>
    Task AcknowledgeAsync(
        string category,
        string consumerGroup,
        string messageId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a message received from the event bus.
/// </summary>
public sealed record EventMessage(
    string MessageId,
    string EventId,
    string EventType,
    DateTime OccurredAt,
    string Data);
