using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.LeaveStudy;

/// <summary>
/// Handler for LeaveStudyCommand.
/// </summary>
public sealed class LeaveStudyCommandHandler
    : ICommandHandler<LeaveStudyCommand, Result>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IStudyUnitOfWork _unitOfWork;

    public LeaveStudyCommandHandler(
        IStudyRepository studyRepository,
        IStudyUnitOfWork unitOfWork)
    {
        _studyRepository = studyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        LeaveStudyCommand request,
        CancellationToken cancellationToken)
    {
        // Parse IDs
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure(StudyErrors.NotFound);

        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure(StudyErrors.InvalidUserId);

        // Get study
        var study = await _studyRepository.GetByIdAsync(studyId, cancellationToken);
        if (study is null)
            return Result.Failure(StudyErrors.NotFound);

        // Owner cannot leave (must transfer ownership first)
        if (study.OwnerId == userId)
            return Result.Failure(StudyErrors.OwnerCannotLeave);

        // Check if user is a member
        if (!study.IsMember(userId))
            return Result.Failure(StudyErrors.NotAMember);

        // Remove the member (user removing themselves)
        var result = study.RemoveMember(userId, userId);
        if (result.IsFailure)
            return result;

        // Persist
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
