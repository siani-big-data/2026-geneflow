using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Profiles.Events;

/// <summary>
/// Raised when a profile photo is updated.
/// </summary>
public sealed record ProfilePhotoUpdatedEvent(
    ProfileId ProfileId,
    string? PhotoUrl) : DomainEvent;
