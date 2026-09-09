using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Identity.Events;

/// <summary>
/// Raised when a user is locked out due to failed login attempts.
/// </summary>
public sealed record UserLockedOutEvent(
    UserId UserId,
    DateTime LockoutEnd,
    int FailedAttempts) : DomainEvent;
