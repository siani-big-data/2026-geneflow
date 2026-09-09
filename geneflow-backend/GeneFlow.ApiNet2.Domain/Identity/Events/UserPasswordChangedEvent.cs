using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Identity.Events;

/// <summary>
/// Raised when a user changes their password.
/// </summary>
public sealed record UserPasswordChangedEvent(UserId UserId) : DomainEvent;
