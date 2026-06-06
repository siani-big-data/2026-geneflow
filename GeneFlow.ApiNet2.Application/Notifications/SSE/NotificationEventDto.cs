namespace GeneFlow.ApiNet2.Application.Notifications.SSE;

/// <summary>
/// Wire format pushed over the SSE stream. Kept intentionally minimal so
/// the client just needs to invalidate its <c>["notifications"]</c> cache
/// and bump the unread badge — the full payload is fetched on demand.
/// </summary>
public sealed record NotificationEventDto(
    string Id,
    string RecipientId,
    string Type,
    string Subject,
    string? Url,
    DateTime CreatedAt);
