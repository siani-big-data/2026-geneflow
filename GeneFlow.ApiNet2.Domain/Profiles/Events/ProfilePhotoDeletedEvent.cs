using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Profiles.Events;

/// <summary>
/// Raised when a profile photo is deleted.
/// </summary>
public sealed record ProfilePhotoDeletedEvent(ProfileId ProfileId) : DomainEvent;
