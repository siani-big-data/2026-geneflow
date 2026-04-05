using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Identity.Events;

/// <summary>
/// Raised when a user disables two-factor authentication.
/// </summary>
public sealed record UserTwoFactorDisabledEvent(UserId UserId) : DomainEvent;
