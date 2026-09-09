using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.SendInvitation;

/// <summary>
/// Command to send a study invitation.
/// Requires Admin role or higher and validates member limit.
/// </summary>
public sealed record SendInvitationCommand(
    string StudyId,
    string InvitedByUserId,
    string Email,
    int RoleId,
    string? Message = null) : ICommand<Result<StudyInvitationDto>>, IRequireStudyMembership, IRequiresMemberLimit
{
    string IRequireStudyMembership.StudyId => StudyId;
    StudyRole? IRequireStudyMembership.MinimumRole => StudyRole.Admin;

    // IRequiresMemberLimit uses StudyId property directly
}
