using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Profiles.Events;

/// <summary>
/// Raised when a profile photo is uploaded.
/// Contains the binary data for storage in the datalake.
/// </summary>
public sealed record ProfilePhotoUploadedEvent(
    ProfileId ProfileId,
    string PhotoData,
    string Extension,
    string ContentType,
    long SizeBytes) : DomainEvent;
