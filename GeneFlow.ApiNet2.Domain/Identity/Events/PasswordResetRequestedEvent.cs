using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Identity.Events;

/// <summary>
/// Raised when a user requests a password reset.
/// </summary>
public sealed record PasswordResetRequestedEvent(
    UserId UserId,
    string Email,
    string Username,
    string ResetToken) : DomainEvent;
