using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Traces.Events;

/// <summary>
/// Event raised when a trim is undone (deactivated) on a trace sequence.
/// </summary>
public sealed record TraceTrimUndoneEvent(
    TraceId TraceId,
    StudyId StudyId,
    Guid TrimId,
    UserId UndoneBy) : DomainEvent;
