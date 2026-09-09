using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Studies.Events;

public sealed record StudyMemberRemovedEvent(
    StudyId StudyId,
    UserId MemberUserId,
    UserId RemovedBy) : DomainEvent;
