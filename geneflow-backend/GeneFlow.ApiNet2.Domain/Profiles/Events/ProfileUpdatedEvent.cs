using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Profiles.Events;

/// <summary>
/// Raised when a profile is updated.
/// </summary>
public sealed record ProfileUpdatedEvent(
    ProfileId ProfileId) : DomainEvent;
