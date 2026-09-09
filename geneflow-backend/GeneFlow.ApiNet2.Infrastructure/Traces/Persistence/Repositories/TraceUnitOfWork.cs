using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Infrastructure.Traces.Persistence.Context;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Infrastructure.Traces.Persistence.Repositories;

/// <summary>
/// Unit of work implementation for Traces context.
/// </summary>
public sealed class TraceUnitOfWork : ITraceUnitOfWork
{
    private readonly TraceContext _context;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ITraceRepository _traces;

    /// <summary>
    /// Initializes a new instance of the TraceUnitOfWork.
    /// </summary>
    public TraceUnitOfWork(
        TraceContext context,
        IDomainEventDispatcher eventDispatcher,
        ITraceRepository traces)
    {
        _context = context;
        _eventDispatcher = eventDispatcher;
        _traces = traces;
    }

    /// <inheritdoc />
    public ITraceRepository Traces => _traces;

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Get all aggregate roots with pending domain events
        var aggregateRoots = _context.ChangeTracker
            .Entries()
            .Where(e => e.Entity is IAggregateRoot)
            .Select(e => (IAggregateRoot)e.Entity)
            .ToList();

        var domainEvents = aggregateRoots
            .SelectMany(ar => ar.DomainEvents)
            .ToList();

        var result = await _context.SaveChangesAsync(cancellationToken);

        // Dispatch events after successful save
        foreach (var domainEvent in domainEvents)
        {
            await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
        }

        // Clear events from all aggregate roots
        foreach (var aggregateRoot in aggregateRoots)
        {
            aggregateRoot.ClearDomainEvents();
        }

        return result;
    }
}
