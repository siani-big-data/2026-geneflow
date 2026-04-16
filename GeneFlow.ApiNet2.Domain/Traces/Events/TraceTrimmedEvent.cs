using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Traces.Events;

/// <summary>
/// Event raised when a trace sequence is trimmed.
/// </summary>
public sealed record TraceTrimmedEvent(
    TraceId TraceId,
    StudyId StudyId,
    int TrimStart,
    int TrimEnd,
    string Algorithm,
    UserId TrimmedBy) : DomainEvent;
