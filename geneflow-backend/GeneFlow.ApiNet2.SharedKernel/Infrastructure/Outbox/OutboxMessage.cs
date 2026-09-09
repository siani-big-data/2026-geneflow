namespace GeneFlow.ApiNet2.SharedKernel.Infrastructure.Outbox;

/// <summary>
/// Represents a message in the outbox for reliable event delivery.
/// Used to implement the Transactional Outbox pattern.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>
    /// Unique identifier for this message.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The fully qualified type name of the event.
    /// </summary>
    public string Type { get; private set; }

    /// <summary>
    /// The serialized event content (typically JSON).
    /// </summary>
    public string Content { get; private set; }

    /// <summary>
    /// When this message was created.
    /// </summary>
    public DateTime OccurredAt { get; private set; }

    /// <summary>
    /// When this message was processed (null if not yet processed).
    /// </summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>
    /// Error message if processing failed.
    /// </summary>
    public string? Error { get; private set; }

    /// <summary>
    /// Number of processing attempts.
    /// </summary>
    public int RetryCount { get; private set; }

    private OutboxMessage()
    {
        Type = string.Empty;
        Content = string.Empty;
    }

    /// <summary>
    /// Creates a new outbox message.
    /// </summary>
    /// <param name="type">The event type name.</param>
    /// <param name="content">The serialized event content.</param>
    /// <param name="occurredAt">When the event occurred.</param>
    /// <returns>A new outbox message.</returns>
    public static OutboxMessage Create(string type, string content, DateTime occurredAt)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Content = content,
            OccurredAt = occurredAt,
            ProcessedAt = null,
            Error = null,
            RetryCount = 0
        };
    }

    /// <summary>
    /// Marks the message as successfully processed.
    /// </summary>
    /// <param name="processedAt">The processing timestamp.</param>
    public void MarkAsProcessed(DateTime processedAt)
    {
        ProcessedAt = processedAt;
        Error = null;
    }

    /// <summary>
    /// Marks the message as failed and increments retry count.
    /// </summary>
    /// <param name="error">The error message.</param>
    public void MarkAsFailed(string error)
    {
        Error = error;
        RetryCount++;
    }
}
