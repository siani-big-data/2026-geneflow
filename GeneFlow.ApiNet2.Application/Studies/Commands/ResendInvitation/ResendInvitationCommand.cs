using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.ResendInvitation;

/// <summary>
/// Command to resend a study invitation.
/// Requires Admin role or higher.
/// </summary>
public sealed record ResendInvitationCommand(
    string StudyId,
    string InvitationId,
    string ResentByUserId) : ICommand<Result<StudyInvitationDto>>, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
    StudyRole? IRequireStudyMembership.MinimumRole => StudyRole.Admin;
}
