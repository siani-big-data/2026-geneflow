using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using MediatR;
using Microsoft.Extensions.Logging;
using IDomainEventDispatcher = GeneFlow.ApiNet2.SharedKernel.Infrastructure.IDomainEventDispatcher;

namespace GeneFlow.ApiNet2.Infrastructure.Events;

/// <summary>
/// Domain event dispatcher that:
/// 1. Dispatches events via MediatR to internal handlers
/// 2. Publishes events to Redis Streams for external consumers (Datalake, Workers)
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublisher _publisher;
    private readonly IEventBusPublisher _eventBusPublisher;
    private readonly IEventCategoryResolver _categoryResolver;
    private readonly ILogger<DomainEventDispatcher> _logger;

    /// <summary>
    /// Initializes a new instance of the DomainEventDispatcher.
    /// </summary>
    public DomainEventDispatcher(
        IPublisher publisher,
        IEventBusPublisher eventBusPublisher,
        IEventCategoryResolver categoryResolver,
        ILogger<DomainEventDispatcher> logger)
    {
        _publisher = publisher;
        _eventBusPublisher = eventBusPublisher;
        _categoryResolver = categoryResolver;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var eventType = domainEvent.GetType().Name;
        var category = _categoryResolver.Resolve(domainEvent);

        _logger.LogDebug(
            "Dispatching domain event {EventType} (ID: {EventId}) to category {Category}",
            eventType,
            domainEvent.EventId,
            category);

        try
        {
            // 1. Dispatch via MediatR to internal handlers
            await _publisher.Publish(domainEvent, cancellationToken);

            // 2. Publish to Redis Streams for external consumers
            await _eventBusPublisher.PublishAsync(domainEvent, category, cancellationToken);

            _logger.LogDebug(
                "Successfully dispatched domain event {EventType} with ID {EventId}",
                eventType,
                domainEvent.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error dispatching domain event {EventType} with ID {EventId}",
                eventType,
                domainEvent.EventId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        var eventsList = domainEvents.ToList();

        if (eventsList.Count == 0)
        {
            return;
        }

        _logger.LogDebug("Dispatching {Count} domain events", eventsList.Count);

        foreach (var domainEvent in eventsList)
        {
            await DispatchAsync(domainEvent, cancellationToken);
        }
    }
}
