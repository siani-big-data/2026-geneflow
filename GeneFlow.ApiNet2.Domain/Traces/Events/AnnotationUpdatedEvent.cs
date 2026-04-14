using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Domain.Events;

namespace GeneFlow.ApiNet2.Domain.Traces.Events;

/// <summary>
/// Event raised when an annotation is updated.
/// </summary>
public sealed record AnnotationUpdatedEvent(
    Guid AnnotationId,
    TraceId TraceId,
    StudyId StudyId,
    UserId UpdatedBy) : IDomainEvent;
