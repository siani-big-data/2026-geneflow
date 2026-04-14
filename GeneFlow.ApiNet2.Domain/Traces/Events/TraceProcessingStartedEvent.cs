using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Domain.Events;

namespace GeneFlow.ApiNet2.Domain.Traces.Events;

/// <summary>
/// Event raised when trace processing begins.
/// </summary>
public sealed record TraceProcessingStartedEvent(
    TraceId TraceId,
    StudyId StudyId) : IDomainEvent;
