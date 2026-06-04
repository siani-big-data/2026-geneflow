using GeneFlow.ApiNet2.Application.Activity.DTOs;
using GeneFlow.ApiNet2.Domain.Activity;

namespace GeneFlow.ApiNet2.Application.Activity.Mappings;

/// <summary>
/// Mappings between <see cref="ActivityEvent"/> domain entities and
/// <see cref="ActivityEventDto"/> application DTOs.
/// </summary>
public static class ActivityMappings
{
    /// <summary>Maps a single activity event to a DTO.</summary>
    public static ActivityEventDto ToDto(this ActivityEvent entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new ActivityEventDto
        {
            Id = entity.Id.Value.ToString("N"),
            ActorUserId = entity.ActorUserId,
            Verb = entity.Verb.Name,
            ObjectType = entity.ObjectType.Name,
            ObjectId = entity.ObjectId,
            StudyId = entity.StudyId,
            OccurredAt = entity.OccurredAt,
            Visibility = entity.Visibility.Name,
            PayloadJson = entity.PayloadJson,
            SourceEventType = entity.SourceEventType,
        };
    }

    /// <summary>Maps a sequence of activity events to DTOs.</summary>
    public static IReadOnlyList<ActivityEventDto> ToDtos(this IEnumerable<ActivityEvent> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        return entities.Select(ToDto).ToList();
    }
}
