using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Infrastructure.Profiles.Persistence.Context;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Infrastructure.Profiles.Persistence.Repositories;

/// <summary>
/// Unit of work implementation for Profiles context.
/// </summary>
public sealed class ProfileUnitOfWork : IProfileUnitOfWork
{
    private readonly ProfileContext _context;
    private readonly IDomainEventDispatcher _eventDispatcher;

    /// <summary>
    /// Initializes a new instance of the ProfileUnitOfWork.
    /// </summary>
    public ProfileUnitOfWork(ProfileContext context, IDomainEventDispatcher eventDispatcher)
    {
        _context = context;
        _eventDispatcher = eventDispatcher;
    }

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
