using GeneFlow.ApiNet2.Domain.Identity.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Identity.Events;

/// <summary>
/// Raised when a new user registers via OAuth.
/// </summary>
public sealed record UserRegisteredViaOAuthEvent(
    UserId UserId,
    string Email,
    string Username,
    ExternalProvider Provider,
    string ProviderKey) : DomainEvent;
