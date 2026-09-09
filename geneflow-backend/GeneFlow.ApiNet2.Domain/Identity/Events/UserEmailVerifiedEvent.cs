using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Identity.Events;

/// <summary>
/// Raised when a user verifies their email.
/// </summary>
public sealed record UserEmailVerifiedEvent(UserId UserId) : DomainEvent;
