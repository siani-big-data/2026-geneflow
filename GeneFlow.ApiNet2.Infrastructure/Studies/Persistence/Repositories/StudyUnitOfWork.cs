using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Infrastructure.Studies.Persistence.Context;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Infrastructure.Studies.Persistence.Repositories;

/// <summary>
/// Unit of work implementation for Studies context.
/// </summary>
public sealed class StudyUnitOfWork : IStudyUnitOfWork
{
    private readonly StudyContext _context;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<StudyUnitOfWork> _logger;

    /// <summary>
    /// Initializes a new instance of the StudyUnitOfWork.
    /// </summary>
    public StudyUnitOfWork(
        StudyContext context,
        IDomainEventDispatcher eventDispatcher,
        ILogger<StudyUnitOfWork> logger)
    {
        _context = context;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
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

        int result;
        try
        {
            result = await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            LogConcurrencyDiagnostics(ex);
            throw;
        }

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

    private void LogConcurrencyDiagnostics(DbUpdateConcurrencyException ex)
    {
        foreach (var entry in ex.Entries)
        {
            var keyValues = string.Join(", ",
                entry.Metadata.FindPrimaryKey()?.Properties
                    .Select(p => $"{p.Name}={entry.Property(p.Name).CurrentValue}")
                ?? Array.Empty<string>());

            _logger.LogError(
                "[Concurrency] Failed entry: type={EntityType}, state={State}, key=[{Keys}]",
                entry.Entity.GetType().Name, entry.State, keyValues);

            foreach (var prop in entry.Properties)
            {
                _logger.LogError(
                    "[Concurrency]   prop {Prop}: original={Original}, current={Current}, modified={Modified}",
                    prop.Metadata.Name,
                    prop.OriginalValue,
                    prop.CurrentValue,
                    prop.IsModified);
            }
        }

        // Also dump every tracked entry so we see what else was in the batch.
        foreach (EntityEntry entry in _context.ChangeTracker.Entries()
            .Where(e => e.State != EntityState.Unchanged && e.State != EntityState.Detached))
        {
            var keyValues = string.Join(", ",
                entry.Metadata.FindPrimaryKey()?.Properties
                    .Select(p => $"{p.Name}={entry.Property(p.Name).CurrentValue}")
                ?? Array.Empty<string>());

            _logger.LogError(
                "[Concurrency] Tracked: type={EntityType}, state={State}, key=[{Keys}]",
                entry.Entity.GetType().Name, entry.State, keyValues);
        }
    }
}
