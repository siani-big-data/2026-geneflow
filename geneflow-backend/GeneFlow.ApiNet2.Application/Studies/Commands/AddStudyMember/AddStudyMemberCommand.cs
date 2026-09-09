using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.AddStudyMember;

/// <summary>
/// Command to add a member to a study.
/// Requires Admin role or higher and validates member limit.
/// </summary>
public sealed record AddStudyMemberCommand(
    string StudyId,
    string RequestingUserId,
    string NewMemberUserId,
    int RoleId) : ICommand<Result<StudyMemberDto>>, IRequireStudyMembership, IRequiresMemberLimit
{
    string IRequireStudyMembership.StudyId => StudyId;
    StudyRole? IRequireStudyMembership.MinimumRole => StudyRole.Admin;

    // IRequiresMemberLimit uses StudyId property directly
}
