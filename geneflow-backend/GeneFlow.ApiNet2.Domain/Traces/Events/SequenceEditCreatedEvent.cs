using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Traces.Events;

/// <summary>
/// Event raised when a sequence edit is created.
/// </summary>
public sealed record SequenceEditCreatedEvent(
    TraceId TraceId,
    StudyId StudyId,
    Guid EditId,
    EditType EditType,
    int Position,
    UserId EditedBy) : DomainEvent;
