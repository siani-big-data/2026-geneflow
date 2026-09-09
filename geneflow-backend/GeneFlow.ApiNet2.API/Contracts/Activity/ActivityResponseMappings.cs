using GeneFlow.ApiNet2.API.Contracts.Activity.Responses;
using GeneFlow.ApiNet2.Application.Activity.DTOs;

namespace GeneFlow.ApiNet2.API.Contracts.Activity;

/// <summary>
/// Maps Activity application DTOs to API contracts.
/// </summary>
public static class ActivityResponseMappings
{
    /// <summary>Projects an <see cref="ActivityEventDto"/> to its wire contract.</summary>
    public static ActivityEventResponse ToResponse(this ActivityEventDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return new ActivityEventResponse(
            dto.Id,
            dto.ActorUserId,
            dto.Verb,
            dto.ObjectType,
            dto.ObjectId,
            dto.StudyId,
            dto.OccurredAt,
            dto.Visibility,
            dto.PayloadJson,
            dto.SourceEventType);
    }
}
