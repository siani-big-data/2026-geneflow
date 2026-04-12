using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.StarStudy;

/// <summary>
/// Handler for StarStudyCommand.
/// </summary>
public sealed class StarStudyCommandHandler
    : ICommandHandler<StarStudyCommand, Result>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IStudyUnitOfWork _unitOfWork;

    public StarStudyCommandHandler(
        IStudyRepository studyRepository,
        IStudyUnitOfWork unitOfWork)
    {
        _studyRepository = studyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        StarStudyCommand request,
        CancellationToken cancellationToken)
    {
        // Parse IDs
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure(StudyErrors.NotFound);

        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure(StudyErrors.InvalidUserId);

        // Check if already starred
        if (await _studyRepository.IsStarredByUserAsync(studyId, userId, cancellationToken))
            return Result.Failure(StudyErrors.AlreadyStarred);

        // Get study to verify it exists and update metrics
        var study = await _studyRepository.GetByIdAsync(studyId, cancellationToken);
        if (study is null)
            return Result.Failure(StudyErrors.NotFound);

        // Add star
        await _studyRepository.AddStarAsync(studyId, userId, cancellationToken);

        // Increment star count
        study.IncrementStars();

        // Persist
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
