using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Subscriptions.Events;

/// <summary>
/// Event raised when a subscription plan is changed.
/// </summary>
public sealed record SubscriptionPlanChangedEvent(
    SubscriptionId SubscriptionId,
    UserId UserId,
    PlanId OldPlanId,
    PlanId NewPlanId,
    string OldPlanName,
    string NewPlanName,
    bool IsUpgrade) : DomainEvent;
