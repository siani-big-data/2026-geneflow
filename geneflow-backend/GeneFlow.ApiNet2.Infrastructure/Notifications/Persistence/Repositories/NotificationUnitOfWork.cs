using GeneFlow.ApiNet2.Domain.Notifications;
using GeneFlow.ApiNet2.Infrastructure.Notifications.Persistence.Context;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Infrastructure.Notifications.Persistence.Repositories;

public sealed class NotificationUnitOfWork : INotificationUnitOfWork
{
    private readonly NotificationContext _context;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<NotificationUnitOfWork> _logger;

    public NotificationUnitOfWork(
        NotificationContext context,
        IDomainEventDispatcher eventDispatcher,
        ILogger<NotificationUnitOfWork> logger)
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
                "Concurrency conflict saving Notifications context ({EntryCount} entries)",
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
