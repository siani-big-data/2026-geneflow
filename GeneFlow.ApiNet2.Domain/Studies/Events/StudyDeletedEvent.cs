using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Studies.Events;

/// <summary>
/// Event raised when a study is deleted.
/// </summary>
public sealed record StudyDeletedEvent(
    StudyId StudyId,
    UserId DeletedBy) : DomainEvent;
