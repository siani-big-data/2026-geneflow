using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.AddStudyMember;

/// <summary>
/// Command to add a member to a study.
/// </summary>
public sealed record AddStudyMemberCommand(
    string StudyId,
    string RequestingUserId,
    string NewMemberUserId,
    int RoleId) : ICommand<Result<StudyMemberDto>>;
