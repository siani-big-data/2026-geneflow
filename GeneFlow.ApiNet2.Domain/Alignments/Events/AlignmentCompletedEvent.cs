using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Alignments.Events;

/// <summary>
/// Event raised when an alignment is completed successfully.
/// </summary>
public sealed record AlignmentCompletedEvent(
    AlignmentId AlignmentId,
    StudyId StudyId,
    double? IdentityPercentage,
    int? AlignmentLength) : DomainEvent;
