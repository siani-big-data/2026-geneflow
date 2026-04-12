using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Application.Studies.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.ChangeMemberRole;

/// <summary>
/// Handler for ChangeMemberRoleCommand.
/// </summary>
public sealed class ChangeMemberRoleCommandHandler
    : ICommandHandler<ChangeMemberRoleCommand, Result<StudyMemberDto>>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IStudyUnitOfWork _unitOfWork;

    public ChangeMemberRoleCommandHandler(
        IStudyRepository studyRepository,
        IStudyUnitOfWork unitOfWork)
    {
        _studyRepository = studyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<StudyMemberDto>> Handle(
        ChangeMemberRoleCommand request,
        CancellationToken cancellationToken)
    {
        // Parse IDs
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure<StudyMemberDto>(StudyErrors.NotFound);

        if (!UserId.TryParse(request.RequestingUserId, out var requestingUserId) || requestingUserId is null)
            return Result.Failure<StudyMemberDto>(StudyErrors.InvalidUserId);

        if (!UserId.TryParse(request.MemberUserId, out var memberUserId) || memberUserId is null)
            return Result.Failure<StudyMemberDto>(StudyErrors.InvalidUserId);

        // Validate role
        var newRole = StudyRole.FromId(request.NewRoleId);
        if (newRole is null || newRole == StudyRole.Owner)
            return Result.Failure<StudyMemberDto>(StudyErrors.InvalidRole);

        // Get study
        var study = await _studyRepository.GetByIdAsync(studyId, cancellationToken);
        if (study is null)
            return Result.Failure<StudyMemberDto>(StudyErrors.NotFound);

        // Change role
        var result = study.ChangeMemberRole(memberUserId, newRole, requestingUserId);
        if (result.IsFailure)
            return Result.Failure<StudyMemberDto>(result.Error);

        // Persist
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Get the updated member
        var member = study.GetMember(memberUserId)!;
        return Result.Success(member.ToDto());
    }
}
