using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Identity.Events;

/// <summary>
/// Raised when a new user registers.
/// </summary>
public sealed record UserRegisteredEvent(
    UserId UserId,
    string Email,
    string Username,
    string EmailVerificationToken) : DomainEvent;
