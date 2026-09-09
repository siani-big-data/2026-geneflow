using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Application.Studies.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.AddStudyMember;

/// <summary>
/// Handler for AddStudyMemberCommand.
/// </summary>
public sealed class AddStudyMemberCommandHandler
    : ICommandHandler<AddStudyMemberCommand, Result<StudyMemberDto>>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IStudyUnitOfWork _unitOfWork;

    public AddStudyMemberCommandHandler(
        IStudyRepository studyRepository,
        IStudyUnitOfWork unitOfWork)
    {
        _studyRepository = studyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<StudyMemberDto>> Handle(
        AddStudyMemberCommand request,
        CancellationToken cancellationToken)
    {
        // Parse IDs
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure<StudyMemberDto>(StudyErrors.NotFound);

        if (!UserId.TryParse(request.RequestingUserId, out var requestingUserId) || requestingUserId is null)
            return Result.Failure<StudyMemberDto>(StudyErrors.InvalidUserId);

        if (!UserId.TryParse(request.NewMemberUserId, out var newMemberUserId) || newMemberUserId is null)
            return Result.Failure<StudyMemberDto>(StudyErrors.InvalidUserId);

        // Validate role
        var role = StudyRole.FromId(request.RoleId);
        if (role is null || role == StudyRole.Owner)
            return Result.Failure<StudyMemberDto>(StudyErrors.InvalidRole);

        // Get study
        var study = await _studyRepository.GetByIdAsync(studyId, cancellationToken);
        if (study is null)
            return Result.Failure<StudyMemberDto>(StudyErrors.NotFound);

        // Add member
        var result = study.AddMember(newMemberUserId, role, requestingUserId);
        if (result.IsFailure)
            return Result.Failure<StudyMemberDto>(result.Error);

        // Persist
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Get the newly added member
        var member = study.GetMember(newMemberUserId)!;
        return Result.Success(member.ToDto());
    }
}
