using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Studies.Events;

public sealed record StudyMemberAddedEvent(
    StudyId StudyId,
    UserId MemberUserId,
    StudyRole Role,
    UserId AddedBy) : DomainEvent;
