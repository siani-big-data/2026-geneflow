using GeneFlow.ApiNet2.Domain.Orgs;
using GeneFlow.ApiNet2.Infrastructure.Orgs.Persistence.Context;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Infrastructure.Orgs.Persistence.Repositories;

/// <summary>
/// Unit of work for the Orgs context. Collects domain events from every
/// tracked aggregate root, persists, then dispatches the events so
/// downstream projectors run inside the same transactional boundary.
/// </summary>
public sealed class OrgUnitOfWork : IOrgUnitOfWork
{
    private readonly OrgsContext _context;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<OrgUnitOfWork> _logger;

    public OrgUnitOfWork(
        OrgsContext context,
        IDomainEventDispatcher eventDispatcher,
        ILogger<OrgUnitOfWork> logger)
    {
        _context = context;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
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
            result = await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex,
                "Concurrency conflict saving Orgs context ({EntryCount} entries)",
                ex.Entries.Count);
            throw;
        }

        foreach (var domainEvent in domainEvents)
        {
            await _eventDispatcher.DispatchAsync(domainEvent, ct);
        }

        foreach (var ar in aggregateRoots)
        {
            ar.ClearDomainEvents();
        }

        return result;
    }
}
