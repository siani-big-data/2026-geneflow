using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Infrastructure.Pipelines.Persistence.Context;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Infrastructure.Pipelines.Persistence.Repositories;

/// <summary>
/// Unit of work implementation for Pipelines context.
/// </summary>
public sealed class PipelineUnitOfWork : IPipelineUnitOfWork
{
    private readonly PipelineContext _context;
    private readonly IDomainEventDispatcher _eventDispatcher;

    /// <summary>
    /// Initializes a new instance of the PipelineUnitOfWork.
    /// </summary>
    public PipelineUnitOfWork(
        PipelineContext context,
        IDomainEventDispatcher eventDispatcher,
        IPipelineRepository pipelineRepository,
        IPipelineExecutionRepository executionRepository)
    {
        _context = context;
        _eventDispatcher = eventDispatcher;
        Pipelines = pipelineRepository;
        Executions = executionRepository;
    }

    /// <inheritdoc />
    public IPipelineRepository Pipelines { get; }

    /// <inheritdoc />
    public IPipelineExecutionRepository Executions { get; }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregateRoots = _context.ChangeTracker
            .Entries()
            .Where(e => e.Entity is IAggregateRoot)
            .Select(e => (IAggregateRoot)e.Entity)
            .ToList();

        var domainEvents = aggregateRoots
            .SelectMany(ar => ar.DomainEvents)
            .ToList();

        var result = await _context.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in domainEvents)
        {
            await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
        }

        foreach (var aggregateRoot in aggregateRoots)
        {
            aggregateRoot.ClearDomainEvents();
        }

        return result;
    }
}
