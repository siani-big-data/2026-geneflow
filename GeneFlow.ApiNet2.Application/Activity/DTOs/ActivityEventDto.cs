namespace GeneFlow.ApiNet2.Application.Activity.DTOs;

/// <summary>
/// Application-layer projection of an activity event.
/// Smart-enum members are exposed as their canonical names (e.g. <c>"Created"</c>).
/// </summary>
public sealed record ActivityEventDto
{
    /// <summary>Identifier of the activity event (GUID, "N" format).</summary>
    public required string Id { get; init; }

    /// <summary>Prefixed identifier of the actor that produced the event, when known.</summary>
    public string? ActorUserId { get; init; }

    /// <summary>Action performed by the actor (name of the <c>ActivityVerb</c>).</summary>
    public required string Verb { get; init; }

    /// <summary>Type of the affected object (name of the <c>ActivityObjectType</c>).</summary>
    public required string ObjectType { get; init; }

    /// <summary>Identifier of the affected object.</summary>
    public required string ObjectId { get; init; }

    /// <summary>Prefixed identifier of the study the event is scoped to, when applicable.</summary>
    public string? StudyId { get; init; }

    /// <summary>UTC instant at which the underlying domain event occurred.</summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>Audience for the event (name of the <c>ActivityVisibility</c>).</summary>
    public required string Visibility { get; init; }

    /// <summary>Original JSON payload of the projected event (forwarded as-is).</summary>
    public required string PayloadJson { get; init; }

    /// <summary>Original event type name (e.g. <c>"StudyCreatedEvent"</c>).</summary>
    public string? SourceEventType { get; init; }
}
