namespace GeneFlow.ApiNet2.SharedKernel.Infrastructure;

/// <summary>
/// Abstraction for message broker operations (RabbitMQ, Azure Service Bus, etc.).
/// Used for integration events and async communication between services.
/// </summary>
public interface IMessageBroker
{
    /// <summary>
    /// Publishes a message to all subscribers.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="routingKey">Optional routing key for topic-based routing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<T>(T message, string? routingKey = null, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Sends a message to a specific queue.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="queueName">The target queue name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync<T>(T message, string queueName, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Sends a message with a delay.
    /// </summary>
    Task SendWithDelayAsync<T>(T message, string queueName, TimeSpan delay, CancellationToken cancellationToken = default)
        where T : class;
}

/// <summary>
/// Base interface for integration events.
/// Integration events are used for communication between bounded contexts/services.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// Unique identifier for this event.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// When this event occurred.
    /// </summary>
    DateTime OccurredAt { get; }

    /// <summary>
    /// The type name of the event for routing.
    /// </summary>
    string EventType { get; }
}

/// <summary>
/// Base class for integration events.
/// </summary>
public abstract record IntegrationEvent : IIntegrationEvent
{
    /// <inheritdoc />
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    /// <inheritdoc />
    public string EventType => GetType().Name;
}
