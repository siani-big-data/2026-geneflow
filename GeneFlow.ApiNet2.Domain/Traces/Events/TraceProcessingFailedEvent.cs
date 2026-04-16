using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Traces.Events;

/// <summary>
/// Event raised when trace processing fails.
/// </summary>
public sealed record TraceProcessingFailedEvent(
    TraceId TraceId,
    StudyId StudyId,
    string Reason) : DomainEvent;
