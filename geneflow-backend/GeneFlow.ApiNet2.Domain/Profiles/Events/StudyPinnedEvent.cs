using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Profiles.Events;

public sealed record StudyPinnedEvent(
    UserId UserId,
    StudyId StudyId,
    int Order) : DomainEvent;
