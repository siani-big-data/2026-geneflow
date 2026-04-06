using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Profiles.Events;

/// <summary>
/// Raised when a new profile is created.
/// </summary>
public sealed record ProfileCreatedEvent(
    ProfileId ProfileId,
    UserId UserId,
    string FirstName) : DomainEvent;
