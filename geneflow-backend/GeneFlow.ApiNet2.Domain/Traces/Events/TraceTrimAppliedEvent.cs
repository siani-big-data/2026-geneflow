using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Traces.Events;

/// <summary>
/// Event raised when a trim is applied to a trace sequence.
/// </summary>
public sealed record TraceTrimAppliedEvent(
    TraceId TraceId,
    StudyId StudyId,
    Guid TrimId,
    int StartPosition,
    int EndPosition,
    TrimEnd TrimEnd,
    string Algorithm,
    UserId AppliedBy) : DomainEvent;
