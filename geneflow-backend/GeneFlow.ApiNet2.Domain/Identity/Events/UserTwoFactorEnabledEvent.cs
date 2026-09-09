using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Identity.Events;

/// <summary>
/// Raised when a user enables two-factor authentication.
/// </summary>
public sealed record UserTwoFactorEnabledEvent(UserId UserId) : DomainEvent;
