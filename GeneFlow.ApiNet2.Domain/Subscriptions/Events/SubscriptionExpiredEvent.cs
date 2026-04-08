using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Subscriptions.Events;

/// <summary>
/// Event raised when a subscription expires.
/// </summary>
public sealed record SubscriptionExpiredEvent(
    SubscriptionId SubscriptionId,
    UserId UserId) : DomainEvent;
