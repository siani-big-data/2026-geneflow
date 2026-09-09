using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Alignments.Events;

/// <summary>
/// Event raised when an alignment fails.
/// </summary>
public sealed record AlignmentFailedEvent(
    AlignmentId AlignmentId,
    StudyId StudyId,
    string ErrorMessage) : DomainEvent;
