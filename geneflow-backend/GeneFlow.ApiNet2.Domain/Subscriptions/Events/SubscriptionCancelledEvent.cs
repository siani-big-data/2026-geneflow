using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Subscriptions.Events;

/// <summary>
/// Event raised when a subscription is cancelled.
/// </summary>
public sealed record SubscriptionCancelledEvent(
    SubscriptionId SubscriptionId,
    UserId UserId,
    string? Reason) : DomainEvent;
