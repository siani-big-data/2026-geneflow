namespace GeneFlow.ApiNet2.API.Contracts.Activity.Responses;

/// <summary>
/// Wire representation of an activity event.
/// </summary>
public sealed record ActivityEventResponse(
    string Id,
    string? ActorUserId,
    string Verb,
    string ObjectType,
    string ObjectId,
    string? StudyId,
    DateTimeOffset OccurredAt,
    string Visibility,
    string PayloadJson,
    string? SourceEventType);
