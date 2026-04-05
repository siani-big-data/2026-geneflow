using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.SharedKernel.Infrastructure;

/// <summary>
/// Resolves the category/stream name for a domain event.
/// Used to route events to the correct Redis Stream.
/// </summary>
public interface IEventCategoryResolver
{
    /// <summary>
    /// Gets the category name for a domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event.</param>
    /// <returns>The category name (e.g., "users", "studies", "traces").</returns>
    string Resolve(IDomainEvent domainEvent);
}
