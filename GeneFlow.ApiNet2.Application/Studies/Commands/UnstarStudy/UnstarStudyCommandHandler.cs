using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.UnstarStudy;

/// <summary>
/// Handler for UnstarStudyCommand.
/// </summary>
public sealed class UnstarStudyCommandHandler
    : ICommandHandler<UnstarStudyCommand, Result>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IStudyUnitOfWork _unitOfWork;

    public UnstarStudyCommandHandler(
        IStudyRepository studyRepository,
        IStudyUnitOfWork unitOfWork)
    {
        _studyRepository = studyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        UnstarStudyCommand request,
        CancellationToken cancellationToken)
    {
        // Parse IDs
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure(StudyErrors.NotFound);

        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure(StudyErrors.InvalidUserId);

        // Check if starred
        if (!await _studyRepository.IsStarredByUserAsync(studyId, userId, cancellationToken))
            return Result.Failure(StudyErrors.NotStarred);

        // Get study to update metrics
        var study = await _studyRepository.GetByIdAsync(studyId, cancellationToken);
        if (study is null)
            return Result.Failure(StudyErrors.NotFound);

        // Remove star
        await _studyRepository.RemoveStarAsync(studyId, userId, cancellationToken);

        // Decrement star count
        study.DecrementStars();

        // Persist
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
