using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.CancelInvitation;

/// <summary>
/// Command to cancel a study invitation.
/// Requires Admin role or higher.
/// </summary>
public sealed record CancelInvitationCommand(
    string StudyId,
    string InvitationId,
    string CancelledByUserId) : ICommand<Result>, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
    StudyRole? IRequireStudyMembership.MinimumRole => StudyRole.Admin;
}
