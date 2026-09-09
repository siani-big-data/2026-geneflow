using GeneFlow.ApiNet2.Domain.Identity.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Identity.Events;

/// <summary>
/// Raised when an external login is linked to a user account.
/// </summary>
public sealed record ExternalLoginLinkedEvent(
    UserId UserId,
    ExternalProvider Provider,
    string ProviderKey) : DomainEvent;
