using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.ChangeMemberRole;

/// <summary>
/// Command to change a member's role in a study.
/// </summary>
public sealed record ChangeMemberRoleCommand(
    string StudyId,
    string RequestingUserId,
    string MemberUserId,
    int NewRoleId) : ICommand<Result<StudyMemberDto>>;
