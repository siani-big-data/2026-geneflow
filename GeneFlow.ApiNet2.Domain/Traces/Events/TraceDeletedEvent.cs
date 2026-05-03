using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Traces.Events;

/// <summary>
/// Event raised when a trace is deleted.
/// </summary>
public sealed record TraceDeletedEvent(
    TraceId TraceId,
    StudyId StudyId,
    UserId DeletedBy) : DomainEvent;
