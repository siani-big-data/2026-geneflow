using GeneFlow.ApiNet2.Application.Notifications.DTOs;
using GeneFlow.ApiNet2.Domain.Notifications;

namespace GeneFlow.ApiNet2.Application.Notifications.Mappings;

public static class NotificationMappings
{
    public static NotificationDto ToDto(this Notification n) => new()
    {
        Id = n.Id.ToString(),
        RecipientId = n.RecipientId.ToString(),
        Type = n.Type.Name,
        Subject = n.Subject,
        Url = n.Url,
        IsRead = n.IsRead,
        ReadAt = n.ReadAt,
        CreatedAt = n.CreatedAt
    };
}
