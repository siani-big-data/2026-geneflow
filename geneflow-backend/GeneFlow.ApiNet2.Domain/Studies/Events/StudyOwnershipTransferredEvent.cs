using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Studies.Events;

public sealed record StudyOwnershipTransferredEvent(
    StudyId StudyId,
    UserId PreviousOwnerId,
    UserId NewOwnerId) : DomainEvent;
