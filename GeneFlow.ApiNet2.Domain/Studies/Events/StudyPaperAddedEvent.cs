using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Studies.Events;

public sealed record StudyPaperAddedEvent(
    StudyId StudyId,
    StudyPaperId PaperId,
    string Title,
    string? Doi,
    UserId AddedBy) : DomainEvent;
