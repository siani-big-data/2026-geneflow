using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Subscriptions.Events;

/// <summary>
/// Event raised when a subscription is renewed.
/// </summary>
public sealed record SubscriptionRenewedEvent(
    SubscriptionId SubscriptionId,
    UserId UserId,
    PlanId PlanId) : DomainEvent;
