namespace GeneFlow.ApiNet2.SharedKernel.Infrastructure;

/// <summary>
/// Abstraction for date/time operations.
/// Allows for deterministic testing by replacing with a test implementation.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    DateTime UtcNow { get; }

    /// <summary>
    /// Gets the current local date and time.
    /// </summary>
    DateTime Now { get; }

    /// <summary>
    /// Gets today's date (UTC).
    /// </summary>
    DateOnly Today { get; }
}

/// <summary>
/// Default implementation using system clock.
/// </summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;

    /// <inheritdoc />
    public DateTime Now => DateTime.Now;

    /// <inheritdoc />
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
