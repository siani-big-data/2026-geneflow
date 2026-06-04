using GeneFlow.ApiNet2.Domain.Activity.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;

namespace GeneFlow.ApiNet2.Domain.Activity;

/// <summary>
/// A single entry in the activity stream.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="ActivityEvent"/> is a denormalised projection of a domain event published
/// by another bounded context on the event bus. It is NOT an aggregate root: it never raises
/// further domain events and is owned exclusively by the activity projector.
/// </para>
/// <para>
/// Cross-context identifiers (actor, study, object) are stored as opaque strings to avoid
/// taking hard dependencies on the source contexts' strongly-typed id types.
/// </para>
/// </remarks>
public sealed class ActivityEvent : Entity<ActivityEventId>
{
    /// <summary>
    /// Identifier of the user that triggered the action, when available.
    /// </summary>
    public string? ActorUserId { get; private set; }

    /// <summary>
    /// Action performed by the actor.
    /// </summary>
    public ActivityVerb Verb { get; private set; } = default!;

    /// <summary>
    /// Type of the object affected by the action.
    /// </summary>
    public ActivityObjectType ObjectType { get; private set; } = default!;

    /// <summary>
    /// Identifier of the affected object (opaque string).
    /// </summary>
    public string ObjectId { get; private set; } = default!;

    /// <summary>
    /// Identifier of the study this event belongs to, when applicable.
    /// </summary>
    public string? StudyId { get; private set; }

    /// <summary>
    /// Moment at which the underlying domain event occurred.
    /// </summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>
    /// Visibility scope used to filter platform feeds.
    /// </summary>
    public ActivityVisibility Visibility { get; private set; } = default!;

    /// <summary>
    /// Domain-event-shaped JSON payload preserved for the UI renderers.
    /// </summary>
    public string PayloadJson { get; private set; } = default!;

    /// <summary>
    /// Fully qualified type name of the source domain event.
    /// </summary>
    public string? SourceEventType { get; private set; }

    /// <summary>
    /// Redis Streams message id of the originating envelope; unique per stream for idempotency.
    /// </summary>
    public string? SourceMessageId { get; private set; }

    // EF Core
    private ActivityEvent() { }

    private ActivityEvent(
        ActivityEventId id,
        string? actorUserId,
        ActivityVerb verb,
        ActivityObjectType objectType,
        string objectId,
        string? studyId,
        DateTimeOffset occurredAt,
        ActivityVisibility visibility,
        string payloadJson,
        string? sourceEventType,
        string? sourceMessageId) : base(id)
    {
        ActorUserId = actorUserId;
        Verb = verb;
        ObjectType = objectType;
        ObjectId = objectId;
        StudyId = studyId;
        OccurredAt = occurredAt;
        Visibility = visibility;
        PayloadJson = payloadJson;
        SourceEventType = sourceEventType;
        SourceMessageId = sourceMessageId;
    }

    /// <summary>
    /// Creates a projected activity event.
    /// </summary>
    public static ActivityEvent Create(
        ActivityEventId id,
        string? actorUserId,
        ActivityVerb verb,
        ActivityObjectType objectType,
        string objectId,
        string? studyId,
        DateTimeOffset occurredAt,
        ActivityVisibility visibility,
        string payloadJson,
        string? sourceEventType,
        string? sourceMessageId)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(verb);
        ArgumentNullException.ThrowIfNull(objectType);
        ArgumentNullException.ThrowIfNull(visibility);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);

        return new ActivityEvent(
            id,
            actorUserId,
            verb,
            objectType,
            objectId,
            studyId,
            occurredAt,
            visibility,
            payloadJson,
            sourceEventType,
            sourceMessageId);
    }
}
