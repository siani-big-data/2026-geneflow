using System.Text.Json;
using GeneFlow.ApiNet2.Domain.Activity;
using GeneFlow.ApiNet2.Domain.Activity.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Infrastructure.Activity.Projection;

/// <summary>
/// Convention-based projector that decomposes <see cref="EventMessage.EventType"/> into
/// a verb and an object type using simple string heuristics, and harvests well-known
/// fields from the payload to populate the activity event.
/// </summary>
/// <remarks>
/// <para>
/// Naming convention assumed for source events:
/// </para>
/// <list type="bullet">
///   <item><c>UserRegistered</c> → object=<c>User</c>, verb=<c>Registered</c></item>
///   <item><c>StudyCreated</c> → object=<c>Study</c>, verb=<c>Created</c></item>
///   <item><c>TraceUploaded</c> → object=<c>Trace</c>, verb=<c>Uploaded</c></item>
///   <item><c>PipelineExecutionStarted</c> → object=<c>PipelineExecution</c>, verb=<c>Started</c></item>
/// </list>
/// <para>
/// The <c>Event</c> / <c>DomainEvent</c> suffix is stripped before splitting.
/// </para>
/// </remarks>
public sealed class ConventionActivityEventProjector : IActivityEventProjector
{
    private static readonly string[] EventTypeSuffixes =
    [
        "DomainEvent",
        "Event"
    ];

    private static readonly string[] ActorIdProperties =
    [
        "actorUserId",
        "actor_user_id",
        "userId",
        "user_id",
        "ownerId",
        "owner_id",
        "uploadedBy",
        "uploaded_by",
        "createdBy",
        "created_by"
    ];

    private static readonly string[] StudyIdProperties =
    [
        "studyId",
        "study_id"
    ];

    private static readonly string[] ObjectIdProperties =
    [
        "id",
        "Id"
    ];

    private readonly ILogger<ConventionActivityEventProjector> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConventionActivityEventProjector"/>.
    /// </summary>
    public ConventionActivityEventProjector(ILogger<ConventionActivityEventProjector> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public ActivityEvent? Project(EventMessage message, string category)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        if (string.IsNullOrWhiteSpace(message.EventType))
        {
            _logger.LogDebug("Skipping event with empty type from category {Category}", category);
            return null;
        }

        var (objectType, verb) = ParseEventType(message.EventType);
        if (objectType is null || verb is null)
        {
            _logger.LogDebug(
                "Cannot map event {EventType} to a verb/object pair; skipping",
                message.EventType);
            return null;
        }

        JsonElement? payload = TryParse(message.Data);

        var actorUserId = payload is not null ? GetString(payload.Value, ActorIdProperties) : null;
        var studyId = payload is not null ? GetString(payload.Value, StudyIdProperties) : null;
        var objectId = payload is not null ? GetString(payload.Value, ObjectIdProperties) : null;

        // Fallback: when the payload does not carry an explicit id, use the EventId so the
        // row remains uniquely addressable.
        objectId ??= message.EventId;
        if (string.IsNullOrWhiteSpace(objectId))
        {
            _logger.LogDebug(
                "Skipping event {EventType} because no object id could be derived",
                message.EventType);
            return null;
        }

        var visibility = ResolveVisibility(objectType, studyId);

        return ActivityEvent.Create(
            id: ActivityEventId.New(),
            actorUserId: actorUserId,
            verb: verb,
            objectType: objectType,
            objectId: objectId,
            studyId: studyId,
            occurredAt: new DateTimeOffset(DateTime.SpecifyKind(message.OccurredAt, DateTimeKind.Utc), TimeSpan.Zero),
            visibility: visibility,
            payloadJson: message.Data ?? "{}",
            sourceEventType: message.EventType,
            sourceMessageId: message.MessageId);
    }

    private static JsonElement? TryParse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? GetString(JsonElement element, string[] propertyNames)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var name in propertyNames)
        {
            if (!element.TryGetProperty(name, out var prop))
            {
                continue;
            }

            switch (prop.ValueKind)
            {
                case JsonValueKind.String:
                    var s = prop.GetString();
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        return s;
                    }
                    break;
                case JsonValueKind.Number:
                    return prop.GetRawText();
            }
        }

        return null;
    }

    private static (ActivityObjectType? Object, ActivityVerb? Verb) ParseEventType(string eventType)
    {
        var trimmed = TrimSuffixes(eventType);
        if (string.IsNullOrEmpty(trimmed))
        {
            return (null, null);
        }

        // Search every known verb name as a suffix of the trimmed event type.
        foreach (var verb in ActivityVerb.GetAll())
        {
            if (verb == ActivityVerb.Unknown)
            {
                continue;
            }

            if (trimmed.EndsWith(verb.Name, StringComparison.Ordinal)
                && trimmed.Length > verb.Name.Length)
            {
                var objectName = trimmed[..^verb.Name.Length];
                var objectType = MapObjectType(objectName);
                if (objectType is not null)
                {
                    return (objectType, verb);
                }
            }
        }

        return (null, null);
    }

    private static string TrimSuffixes(string eventType)
    {
        foreach (var suffix in EventTypeSuffixes)
        {
            if (eventType.EndsWith(suffix, StringComparison.Ordinal))
            {
                return eventType[..^suffix.Length];
            }
        }
        return eventType;
    }

    private static ActivityObjectType? MapObjectType(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return null;
        }

        // Try exact match against any defined ActivityObjectType.
        foreach (var candidate in ActivityObjectType.GetAll())
        {
            if (candidate == ActivityObjectType.Other)
            {
                continue;
            }

            if (string.Equals(candidate.Name, objectName, StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        return null;
    }

    private static ActivityVisibility ResolveVisibility(ActivityObjectType objectType, string? studyId)
    {
        // Subscriptions and Plans are administrative; keep them out of public feeds.
        if (objectType == ActivityObjectType.Subscription || objectType == ActivityObjectType.Plan)
        {
            return ActivityVisibility.Private;
        }

        // Anything attached to a study belongs to that study's timeline.
        if (!string.IsNullOrWhiteSpace(studyId))
        {
            return ActivityVisibility.StudyMembers;
        }

        // User registrations are public on the global feed.
        if (objectType == ActivityObjectType.User)
        {
            return ActivityVisibility.Public;
        }

        return ActivityVisibility.Private;
    }
}
