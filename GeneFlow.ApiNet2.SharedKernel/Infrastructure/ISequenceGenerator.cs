namespace GeneFlow.ApiNet2.SharedKernel.Infrastructure;

/// <summary>
/// Generates sequential numeric values for entity IDs.
/// Implementation uses Redis INCR for atomic, distributed sequences.
/// </summary>
public interface ISequenceGenerator
{
    /// <summary>
    /// Gets the next value in the sequence for the specified entity type.
    /// </summary>
    /// <param name="sequenceName">The name of the sequence (e.g., "users", "studies", "traces").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next sequential value.</returns>
    Task<long> NextAsync(string sequenceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current value of a sequence without incrementing it.
    /// </summary>
    /// <param name="sequenceName">The name of the sequence.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current value, or 0 if the sequence doesn't exist.</returns>
    Task<long> CurrentAsync(string sequenceName, CancellationToken cancellationToken = default);
}
