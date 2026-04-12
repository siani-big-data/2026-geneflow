using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Studies.Events;

public sealed record StudyPaperRemovedEvent(
    StudyId StudyId,
    StudyPaperId PaperId,
    UserId RemovedBy) : DomainEvent;
