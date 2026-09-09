using GeneFlow.ApiNet2.API.Contracts.Notifications.Responses;
using GeneFlow.ApiNet2.Application.Notifications.DTOs;

namespace GeneFlow.ApiNet2.API.Extensions;

public static class NotificationMappingExtensions
{
    public static NotificationResponse ToResponse(this NotificationDto dto) => new(
        dto.Id,
        dto.RecipientId,
        dto.Type,
        dto.Subject,
        dto.Url,
        dto.IsRead,
        dto.ReadAt,
        dto.CreatedAt);
}
