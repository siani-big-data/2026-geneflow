using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Notifications.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Notifications.Events;

public sealed record WatchUpdatedEvent(
    Guid WatchId,
    UserId UserId,
    StudyId StudyId,
    WatchLevel Level) : DomainEvent;
