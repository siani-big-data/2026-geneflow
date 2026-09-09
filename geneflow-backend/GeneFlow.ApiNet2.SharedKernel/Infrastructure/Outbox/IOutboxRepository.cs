namespace GeneFlow.ApiNet2.SharedKernel.Infrastructure.Outbox;

/// <summary>
/// Repository for outbox messages.
/// Implemented in Infrastructure with the persistence technology.
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Adds a message to the outbox.
    /// </summary>
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unprocessed messages ready for dispatch.
    /// </summary>
    /// <param name="batchSize">Maximum number of messages to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets failed messages eligible for retry.
    /// </summary>
    /// <param name="maxRetryCount">Maximum retry attempts before giving up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<OutboxMessage>> GetFailedAsync(int maxRetryCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a message after processing.
    /// </summary>
    Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes processed messages older than the specified date.
    /// </summary>
    Task DeleteProcessedAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}
