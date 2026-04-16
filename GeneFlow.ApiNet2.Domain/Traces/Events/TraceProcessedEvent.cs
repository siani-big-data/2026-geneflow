using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Traces.Events;

/// <summary>
/// Event raised when trace processing completes successfully.
/// </summary>
public sealed record TraceProcessedEvent(
    TraceId TraceId,
    StudyId StudyId,
    decimal AverageQualityScore) : DomainEvent;
