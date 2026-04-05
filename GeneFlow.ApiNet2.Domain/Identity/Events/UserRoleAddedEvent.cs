using GeneFlow.ApiNet2.Domain.Identity.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Identity.Events;

/// <summary>
/// Raised when a role is added to a user.
/// </summary>
public sealed record UserRoleAddedEvent(UserId UserId, Role Role) : DomainEvent;
