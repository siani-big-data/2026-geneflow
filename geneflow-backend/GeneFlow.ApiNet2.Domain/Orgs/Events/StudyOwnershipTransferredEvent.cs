using GeneFlow.ApiNet2.Domain.Orgs.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Orgs.Events;

/// <summary>
/// Raised when a study's owner-type principal changes (e.g. user-owned study
/// transferred to an org, or vice-versa). Distinct from the in-aggregate
/// <see cref="Studies.Events.StudyOwnershipTransferredEvent"/> which only
/// covers member-to-member transfer within the same user-owned study.
/// </summary>
public sealed record StudyOwnershipTransferredEvent(
    StudyId StudyId,
    StudyOwnerType PreviousOwnerType,
    string PreviousOwnerId,
    StudyOwnerType NewOwnerType,
    string NewOwnerId) : DomainEvent;
