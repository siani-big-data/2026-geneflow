using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.RemoveStudyMember;

/// <summary>
/// Command to remove a member from a study.
/// Requires Admin role or higher.
/// </summary>
public sealed record RemoveStudyMemberCommand(
    string StudyId,
    string RequestingUserId,
    string MemberUserId) : ICommand<Result>, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
    StudyRole? IRequireStudyMembership.MinimumRole => StudyRole.Admin;
}
