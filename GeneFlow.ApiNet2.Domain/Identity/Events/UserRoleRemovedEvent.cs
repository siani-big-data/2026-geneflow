using GeneFlow.ApiNet2.Domain.Identity.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Identity.Events;

/// <summary>
/// Raised when a role is removed from a user.
/// </summary>
public sealed record UserRoleRemovedEvent(UserId UserId, Role Role) : DomainEvent;
