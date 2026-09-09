using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.ChangeMemberRole;

/// <summary>
/// Command to change a member's role in a study.
/// Requires Owner role.
/// </summary>
public sealed record ChangeMemberRoleCommand(
    string StudyId,
    string RequestingUserId,
    string MemberUserId,
    int NewRoleId) : ICommand<Result<StudyMemberDto>>, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
    StudyRole? IRequireStudyMembership.MinimumRole => StudyRole.Owner;
}
