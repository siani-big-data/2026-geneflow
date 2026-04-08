using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Plans.Events;

/// <summary>
/// Event raised when a plan is created.
/// </summary>
public sealed record PlanCreatedEvent(PlanId PlanId) : DomainEvent;
