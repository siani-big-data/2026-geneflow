namespace GeneFlow.ApiNet2.SharedKernel.Infrastructure.Outbox;

/// <summary>
/// Processes outbox messages and dispatches them.
/// Typically runs as a background service.
/// </summary>
public interface IOutboxProcessor
{
    /// <summary>
    /// Processes pending outbox messages.
    /// </summary>
    /// <param name="batchSize">Number of messages to process in this batch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of messages successfully processed.</returns>
    Task<int> ProcessAsync(int batchSize = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries failed messages.
    /// </summary>
    /// <param name="maxRetryCount">Maximum retry attempts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of messages retried.</returns>
    Task<int> RetryFailedAsync(int maxRetryCount = 3, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old processed messages.
    /// </summary>
    /// <param name="olderThan">Delete messages processed before this date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of messages deleted.</returns>
    Task<int> CleanupAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}
