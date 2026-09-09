using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Alignments.Events;

/// <summary>
/// Event raised when a new alignment is created/requested.
/// </summary>
public sealed record AlignmentCreatedEvent(
    AlignmentId AlignmentId,
    StudyId StudyId,
    UserId RequestedBy) : DomainEvent;
