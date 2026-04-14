using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Domain.Events;

namespace GeneFlow.ApiNet2.Domain.Traces.Events;

/// <summary>
/// Event raised when trace processing fails.
/// </summary>
public sealed record TraceProcessingFailedEvent(
    TraceId TraceId,
    StudyId StudyId,
    string Reason) : IDomainEvent;
