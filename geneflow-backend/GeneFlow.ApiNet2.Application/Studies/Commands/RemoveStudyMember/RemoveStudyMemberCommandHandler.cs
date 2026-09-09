using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.RemoveStudyMember;

/// <summary>
/// Handler for RemoveStudyMemberCommand.
/// </summary>
public sealed class RemoveStudyMemberCommandHandler
    : ICommandHandler<RemoveStudyMemberCommand, Result>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IStudyUnitOfWork _unitOfWork;

    public RemoveStudyMemberCommandHandler(
        IStudyRepository studyRepository,
        IStudyUnitOfWork unitOfWork)
    {
        _studyRepository = studyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        RemoveStudyMemberCommand request,
        CancellationToken cancellationToken)
    {
        // Parse IDs
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure(StudyErrors.NotFound);

        if (!UserId.TryParse(request.RequestingUserId, out var requestingUserId) || requestingUserId is null)
            return Result.Failure(StudyErrors.InvalidUserId);

        if (!UserId.TryParse(request.MemberUserId, out var memberUserId) || memberUserId is null)
            return Result.Failure(StudyErrors.InvalidUserId);

        // Get study
        var study = await _studyRepository.GetByIdAsync(studyId, cancellationToken);
        if (study is null)
            return Result.Failure(StudyErrors.NotFound);

        // Remove member
        var result = study.RemoveMember(memberUserId, requestingUserId);
        if (result.IsFailure)
            return result;

        // Persist
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
