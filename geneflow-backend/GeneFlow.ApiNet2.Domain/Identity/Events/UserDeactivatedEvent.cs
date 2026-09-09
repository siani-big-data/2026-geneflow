using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Identity.Events;

/// <summary>
/// Raised when a user account is deactivated.
/// </summary>
public sealed record UserDeactivatedEvent(UserId UserId) : DomainEvent;
