using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Notifications.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Notifications.Events;

public sealed record NotificationCreatedEvent(
    NotificationId NotificationId,
    UserId RecipientId,
    NotificationType Type,
    string Subject,
    string? Url) : DomainEvent;
