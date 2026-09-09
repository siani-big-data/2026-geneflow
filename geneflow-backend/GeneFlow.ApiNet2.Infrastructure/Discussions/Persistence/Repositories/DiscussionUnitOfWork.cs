using GeneFlow.ApiNet2.Domain.Discussions;
using GeneFlow.ApiNet2.Infrastructure.Discussions.Persistence.Context;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Infrastructure.Discussions.Persistence.Repositories;

/// <summary>
/// Unit of work for the Discussions context. Collects domain events from
/// every tracked aggregate root, persists, then dispatches the events so
/// downstream handlers (e.g. the WatchNotifier) run inside the same
/// transactional boundary as the change that triggered them.
/// </summary>
public sealed class DiscussionUnitOfWork : IDiscussionUnitOfWork
{
    private readonly DiscussionContext _context;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<DiscussionUnitOfWork> _logger;

    public DiscussionUnitOfWork(
        DiscussionContext context,
        IDomainEventDispatcher eventDispatcher,
        ILogger<DiscussionUnitOfWork> logger)
    {
        _context = context;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

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

        int result;
        try
        {
            result = await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex,
                "Concurrency conflict saving Discussions context ({EntryCount} entries)",
                ex.Entries.Count);
            throw;
        }

        foreach (var domainEvent in domainEvents)
        {
            await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
        }

        foreach (var ar in aggregateRoots)
        {
            ar.ClearDomainEvents();
        }

        return result;
    }
}
