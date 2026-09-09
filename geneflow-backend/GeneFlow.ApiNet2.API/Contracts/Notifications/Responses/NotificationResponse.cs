namespace GeneFlow.ApiNet2.API.Contracts.Notifications.Responses;

public sealed record NotificationResponse(
    string Id,
    string RecipientId,
    string Type,
    string Subject,
    string? Url,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt);
