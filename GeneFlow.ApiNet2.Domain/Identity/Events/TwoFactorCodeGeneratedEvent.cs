using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Identity.Events;

/// <summary>
/// Raised when a two-factor authentication code is generated.
/// </summary>
public sealed record TwoFactorCodeGeneratedEvent(
    UserId UserId,
    string Email,
    string Username,
    string Code) : DomainEvent;
