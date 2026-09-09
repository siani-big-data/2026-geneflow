using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Traces.Events;

/// <summary>
/// Event raised when a sequence edit is undone.
/// </summary>
public sealed record SequenceEditUndoneEvent(
    TraceId TraceId,
    StudyId StudyId,
    Guid EditId,
    UserId UndoneBy) : DomainEvent;
