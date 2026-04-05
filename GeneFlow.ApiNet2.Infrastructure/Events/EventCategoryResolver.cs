using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Infrastructure.Events;

/// <summary>
/// Resolves event categories based on the event's namespace.
/// Maps domain events to their corresponding Redis Stream category.
/// </summary>
public sealed class EventCategoryResolver : IEventCategoryResolver
{
    private static readonly Dictionary<string, string> NamespaceToCategoryMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Identity", "users" },
        { "Studies", "studies" },
        { "Traces", "traces" },
        { "Alignments", "alignments" },
        { "Subscriptions", "subscriptions" },
        { "Plans", "plans" },
        { "Pipelines", "pipelines" },
        { "Profiles", "profiles" }
    };

    /// <inheritdoc />
    public string Resolve(IDomainEvent domainEvent)
    {
        var eventType = domainEvent.GetType();
        var namespaceParts = eventType.Namespace?.Split('.') ?? [];

        // Look for a known bounded context in the namespace
        // e.g., GeneFlow.ApiNet2.Domain.Identity.Events -> "users"
        foreach (var part in namespaceParts)
        {
            if (NamespaceToCategoryMap.TryGetValue(part, out var category))
            {
                return category;
            }
        }

        // Fallback: use the second-to-last namespace part in lowercase
        // e.g., GeneFlow.ApiNet2.Domain.NewContext.Events -> "newcontext"
        if (namespaceParts.Length >= 2)
        {
            return namespaceParts[^2].ToLowerInvariant();
        }

        return "unknown";
    }
}
