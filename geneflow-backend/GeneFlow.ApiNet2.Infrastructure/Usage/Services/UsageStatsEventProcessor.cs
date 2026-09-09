using System.Text.Json;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Usage;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace GeneFlow.ApiNet2.Infrastructure.Usage.Services;

/// <summary>
/// Background service that processes domain events from Redis Streams
/// and updates usage statistics in the datamart.
/// </summary>
/// <remarks>
/// The per-message handler catch is intentionally broad and rethrows the
/// exception so the message is not acknowledged and is redelivered. Handlers
/// can fail in unbounded ways (JSON parsing, repository errors, etc.).
/// </remarks>
public sealed class UsageStatsEventProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEventBusSubscriber _subscriber;
    private readonly ILogger<UsageStatsEventProcessor> _logger;

    private const string ConsumerGroup = "usage-stats-processor";

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
        "TraceDeletedEvent"
    };

    private static readonly HashSet<string> AlignmentEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "AlignmentCreatedEvent",
        "AlignmentCompletedEvent",
        "AlignmentFailedEvent"
    };

    // Events whose payload does carry a user context AND that we update stats for.
    // Events arriving on the same Redis category but not in this set are skipped
    // silently (e.g. TraceProcessed/TraceProcessingStartedEvent are emitted by
    // the worker / DomainEventDispatcher but have no owner field — they're not
    // billable transitions, so the projector ignores them).
    private static readonly HashSet<string> HandledEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "StudyCreatedEvent",
        "StudyDeletedEvent",
        "StudyMemberAddedEvent",
        "StudyMemberRemovedEvent",
        "TraceUploadedEvent",
        "TracesUploadedEvent",
        "AlignmentCreatedEvent",
        "AlignmentCompletedEvent"
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Redis error in {Category} event processor", category);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error in {Category} event processor", category);
        }
    }

    private async Task HandleEventAsync(EventMessage message, string category)
    {
        _logger.LogDebug(
            "Processing event {EventType} from {Category} (ID: {EventId})",
            message.EventType, category, message.EventId);

        try
        {
            // Both the Python worker (bare class names) and the .NET re-publish
            // (with "Event" suffix) write to the same Redis category. Many of
            // those events legitimately have no user context (e.g. trace
            // lifecycle transitions). Skip silently for anything we don't
            // actually project into the usage datamart so the logs stay clean.
            if (!HandledEvents.Contains(message.EventType))
            {
                _logger.LogDebug(
                    "Ignoring non-projected event {EventType} (ID: {EventId})",
                    message.EventType, message.EventId);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IUsageStatsRepository>();

            var eventData = JsonDocument.Parse(message.Data);
            var userId = ExtractUserId(eventData, message.EventType);

            if (userId is null)
            {
                _logger.LogWarning(
                    "Could not extract userId from event {EventType}",
                    message.EventType);
                return;
            }

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
            throw;
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
        var propertyNames = new[] {
            "userId", "UserId", "user_id",
            "ownerId", "OwnerId", "owner_id",
            "createdBy", "CreatedBy", "created_by",
            "trimmedBy", "TrimmedBy", "trimmed_by",
            "appliedBy", "AppliedBy", "applied_by",
            "uploadedBy", "UploadedBy", "uploaded_by",
            "removedBy", "RemovedBy", "removed_by",
            "addedBy", "AddedBy", "added_by",
            "editedBy", "EditedBy", "edited_by",
            "undoneBy", "UndoneBy", "undone_by"
        };

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
                if (prop.ValueKind == JsonValueKind.String)
                {
                    var strValue = prop.GetString();
                    if (!string.IsNullOrEmpty(strValue) && UserId.TryParse(strValue, out var userId))
                    {
                        return userId;
                    }
                }
                else if (prop.ValueKind == JsonValueKind.Object)
                {
                    JsonElement valueProp = default;
                    if (prop.TryGetProperty("value", out valueProp) || prop.TryGetProperty("Value", out valueProp))
                    {
                        if (valueProp.ValueKind == JsonValueKind.Number && valueProp.TryGetInt64(out var numValue))
                        {
                            return new UserId(numValue);
                        }
                        else if (valueProp.ValueKind == JsonValueKind.String)
                        {
                            var strValue = valueProp.GetString();
                            if (!string.IsNullOrEmpty(strValue) && UserId.TryParse(strValue, out var userId))
                            {
                                return userId;
                            }
                        }
                    }
                }
                else if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var numValue))
                {
                    return new UserId(numValue);
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
