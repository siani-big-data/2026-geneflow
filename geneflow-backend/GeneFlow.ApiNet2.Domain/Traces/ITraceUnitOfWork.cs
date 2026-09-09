namespace GeneFlow.ApiNet2.Domain.Traces;

/// <summary>
/// Unit of work interface for Trace bounded context.
/// Coordinates persistence and event dispatching.
/// </summary>
public interface ITraceUnitOfWork
{
    /// <summary>
    /// Gets the trace repository.
    /// </summary>
    ITraceRepository Traces { get; }

    /// <summary>
    /// Saves all changes and dispatches domain events.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
