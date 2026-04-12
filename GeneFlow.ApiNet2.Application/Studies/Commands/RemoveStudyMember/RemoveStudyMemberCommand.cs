using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.RemoveStudyMember;

/// <summary>
/// Command to remove a member from a study.
/// </summary>
public sealed record RemoveStudyMemberCommand(
    string StudyId,
    string RequestingUserId,
    string MemberUserId) : ICommand<Result>;
