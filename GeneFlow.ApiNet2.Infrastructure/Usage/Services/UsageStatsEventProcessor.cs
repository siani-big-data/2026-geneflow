using System.Text.Json;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Usage;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Infrastructure.Usage.Services;

/// <summary>
/// Background service that processes domain events from Redis Streams
/// and updates usage statistics in the datamart.
/// </summary>
public sealed class UsageStatsEventProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEventBusSubscriber _subscriber;
    private readonly ILogger<UsageStatsEventProcessor> _logger;

    private const string ConsumerGroup = "usage-stats-processor";

    // Event type mappings
    private static readonly HashSet<string> StudyEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "StudyCreatedEvent",
        "StudyDeletedEvent",
        "StudyMemberAddedEvent",
        "StudyMemberRemovedEvent"
    };

    private static readonly HashSet<string> TraceEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "TraceUploadedEvent",
        "TracesUploadedEvent",
        "TraceDeletedEvent",
        "TraceProcessedEvent",
        "TraceProcessingStartedEvent"
    };

    private static readonly HashSet<string> AlignmentEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "AlignmentCreatedEvent",
        "AlignmentCompletedEvent",
        "AlignmentFailedEvent"
    };

    public UsageStatsEventProcessor(
        IServiceScopeFactory scopeFactory,
        IEventBusSubscriber subscriber,
        ILogger<UsageStatsEventProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _subscriber = subscriber;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Usage Stats Event Processor starting...");

        // Start subscriptions in parallel for each category
        var tasks = new[]
        {
            ProcessCategoryAsync("studies", stoppingToken),
            ProcessCategoryAsync("traces", stoppingToken),
            ProcessCategoryAsync("alignments", stoppingToken)
        };

        await Task.WhenAll(tasks);

        _logger.LogInformation("Usage Stats Event Processor stopped");
    }

    private async Task ProcessCategoryAsync(string category, CancellationToken cancellationToken)
    {
        try
        {
            await _subscriber.SubscribeAsync(
                category,
                ConsumerGroup,
                async message => await HandleEventAsync(message, category),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {Category} event processor", category);
        }
    }

    private async Task HandleEventAsync(EventMessage message, string category)
    {
        _logger.LogDebug(
            "Processing event {EventType} from {Category} (ID: {EventId})",
            message.EventType, category, message.EventId);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IUsageStatsRepository>();

            // Parse the event data to extract userId
            var eventData = JsonDocument.Parse(message.Data);
            var userId = ExtractUserId(eventData, message.EventType);

            if (userId is null)
            {
                _logger.LogWarning(
                    "Could not extract userId from event {EventType}",
                    message.EventType);
                return;
            }

            // Process based on event type
            await ProcessEventAsync(repository, message.EventType, eventData, userId);

            _logger.LogDebug(
                "Successfully processed event {EventType} for user {UserId}",
                message.EventType, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process event {EventType} (ID: {EventId})",
                message.EventType, message.EventId);
            throw; // Re-throw to prevent acknowledgment
        }
    }

    private async Task ProcessEventAsync(
        IUsageStatsRepository repository,
        string eventType,
        JsonDocument eventData,
        UserId userId)
    {
        switch (eventType)
        {
            // Study events
            case "StudyCreatedEvent":
                await repository.IncrementStudiesOwnedAsync(userId);
                break;

            case "StudyDeletedEvent":
                await repository.DecrementStudiesOwnedAsync(userId);
                break;

            case "StudyMemberAddedEvent":
                var memberCount = GetIntProperty(eventData, "memberCount", "member_count");
                if (memberCount > 0)
                {
                    await repository.UpdateMaxMembersInStudyAsync(userId, memberCount);
                }
                // Also increment studies total for the invited user
                var invitedUserId = GetUserIdProperty(eventData, "invitedUserId", "invited_user_id");
                if (invitedUserId is not null)
                {
                    await repository.IncrementStudiesTotalAsync(invitedUserId);
                }
                break;

            case "StudyMemberRemovedEvent":
                var removedUserId = GetUserIdProperty(eventData, "removedUserId", "removed_user_id");
                if (removedUserId is not null)
                {
                    await repository.DecrementStudiesTotalAsync(removedUserId);
                }
                break;

            // Trace events
            case "TraceUploadedEvent":
                await repository.IncrementTracesAsync(userId, 1);
                break;

            case "TracesUploadedEvent":
                var traceCount = GetIntProperty(eventData, "count", "trace_count");
                if (traceCount > 0)
                {
                    await repository.IncrementTracesAsync(userId, traceCount);
                }
                break;

            case "TraceProcessingStartedEvent":
                var pendingCount = GetIntProperty(eventData, "pendingCount", "pending_count");
                if (pendingCount >= 0)
                {
                    await repository.SetTracesPendingAsync(userId, pendingCount);
                }
                break;

            case "TraceProcessedEvent":
                // Decrement pending (set to current pending - 1 or fetch new count)
                var newPendingCount = GetIntProperty(eventData, "pendingCount", "pending_count");
                if (newPendingCount >= 0)
                {
                    await repository.SetTracesPendingAsync(userId, newPendingCount);
                }
                break;

            // Alignment events
            case "AlignmentCreatedEvent":
                await repository.IncrementAlignmentsAsync(userId);
                break;

            case "AlignmentCompletedEvent":
                await repository.IncrementCompletedAlignmentsAsync(userId);
                break;

            default:
                _logger.LogDebug("Ignoring unhandled event type: {EventType}", eventType);
                break;
        }
    }

    private UserId? ExtractUserId(JsonDocument eventData, string eventType)
    {
        // Try different property names that might contain the user ID
        var propertyNames = new[] { "userId", "user_id", "ownerId", "owner_id", "createdBy", "created_by" };

        foreach (var propName in propertyNames)
        {
            var userId = GetUserIdProperty(eventData, propName);
            if (userId is not null)
            {
                return userId;
            }
        }

        return null;
    }

    private UserId? GetUserIdProperty(JsonDocument doc, params string[] propertyNames)
    {
        foreach (var propName in propertyNames)
        {
            if (doc.RootElement.TryGetProperty(propName, out var prop))
            {
                var value = prop.GetString();
                if (!string.IsNullOrEmpty(value) && UserId.TryParse(value, out var userId))
                {
                    return userId;
                }
            }
        }
        return null;
    }

    private int GetIntProperty(JsonDocument doc, params string[] propertyNames)
    {
        foreach (var propName in propertyNames)
        {
            if (doc.RootElement.TryGetProperty(propName, out var prop))
            {
                if (prop.TryGetInt32(out var value))
                {
                    return value;
                }
            }
        }
        return -1;
    }
}
