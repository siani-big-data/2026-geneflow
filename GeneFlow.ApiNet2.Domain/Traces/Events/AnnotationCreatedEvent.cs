using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Traces.Events;

/// <summary>
/// Event raised when an annotation is created.
/// </summary>
public sealed record AnnotationCreatedEvent(
    Guid AnnotationId,
    TraceId TraceId,
    StudyId StudyId,
    AnnotationType Type,
    string Label,
    int StartPosition,
    int EndPosition,
    UserId CreatedBy) : DomainEvent;
