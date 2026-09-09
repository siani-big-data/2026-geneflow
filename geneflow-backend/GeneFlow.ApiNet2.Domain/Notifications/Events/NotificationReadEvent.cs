using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Notifications.Events;

public sealed record NotificationReadEvent(
    NotificationId NotificationId,
    UserId RecipientId,
    DateTime ReadAt) : DomainEvent;
