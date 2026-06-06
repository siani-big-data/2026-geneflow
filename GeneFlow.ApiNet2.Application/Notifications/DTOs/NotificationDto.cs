namespace GeneFlow.ApiNet2.Application.Notifications.DTOs;

public sealed record NotificationDto
{
    public required string Id { get; init; }
    public required string RecipientId { get; init; }
    public required string Type { get; init; }
    public required string Subject { get; init; }
    public string? Url { get; init; }
    public bool IsRead { get; init; }
    public DateTime? ReadAt { get; init; }
    public required DateTime CreatedAt { get; init; }
}
